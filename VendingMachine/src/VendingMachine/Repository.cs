using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VendingMachine.Models;

namespace VendingMachine
{
    internal interface IRepository
    {
        Task<string> CreateMachineAsync(Machine machine);
        Task<List<Machine>> ListMachinesAsync();
        Task DeleteMachineAsync(string id);
        Task<Machine?> GetMachineAsync(string id);
        Task<List<MachineInventoryEntry>> RestockMachineAsync(string id, IEnumerable<MachineInventoryEntry> items);
    }

    internal class Repository(IAmazonDynamoDB db, string tableName) : IRepository
    {
        public async Task<string> CreateMachineAsync(Machine machine)
        {
            var id = Guid.NewGuid().ToString();
            machine.PK = "MAC#" + id;
            machine.SK = machine.PK;
            machine.CreatedAt = DateTime.UtcNow;

            var asJson = JsonSerializer.Serialize(machine);
            var asAttrs = Document.FromJson(asJson).ToAttributeMap();

            var createRequest = new PutItemRequest
            {
                TableName = tableName,
                Item = asAttrs,
            };

            Console.WriteLine(JsonSerializer.Serialize(machine));

            var result = await db.PutItemAsync(createRequest);

            Console.WriteLine(JsonSerializer.Serialize(result));

            return id;
        }

        public async Task<List<Machine>> ListMachinesAsync()
        {
            var scanRequest = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = "begins_with(PK, :pkval)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pkval", new AttributeValue { S = "MAC#" } }
                }
            };
            var result = await db.ScanAsync(scanRequest);
            var machines = result.Items.Select(item =>
            {
                var json = Document.FromAttributeMap(item).ToJson();
                return JsonSerializer.Deserialize<Machine>(json);
            }).Where(m => m != null).ToList();
            return machines!;
        }

        public async Task DeleteMachineAsync(string id)
        {
            // Get all inventory items remaining
            var inventoryQuery = new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "PK = :pkval AND begins_with(SK, :skval)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pkval", new AttributeValue { S = $"INV#{id}" } },
                    { ":skval", new AttributeValue { S = "PROD#" } }
                }
            };

            var inventoryResult = await db.QueryAsync(inventoryQuery);

            // Delete in batches, we don't actually gain much from transact here
            // because if anything fails we want to retry anyway... we're left
            // with a partial inventory, but the real fix would be to disable
            // the machine before attempting this deleting, which is a nice TODO
            var requestItems = new List<WriteRequest>();
            foreach (var item in inventoryResult.Items)
            {
                var invDeleteRequest = new WriteRequest(
                    new DeleteRequest(new Dictionary<string, AttributeValue>
                        {
                            { "PK", item["PK"] },
                            { "SK", item["SK"] }
                        }));
                requestItems.Add(invDeleteRequest);
            }

            await DoBatchRequestsAsync(requestItems);

            // Delete the machine itself only after inventory items are successfully deleted,
            // because if any fail then we might have some orphaned inventory hanging around.
            var machinePk = "MAC#" + id;
            var deleteRequest = new DeleteItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = machinePk } },
                    { "SK", new AttributeValue { S = machinePk } }
                }
            };

            var result = await db.DeleteItemAsync(deleteRequest);
            Console.WriteLine(JsonSerializer.Serialize(result));
        }

        public async Task<Machine?> GetMachineAsync(string id)
        {
            var machinePK = "MAC#" + id;
            var getMachineRequest = new GetItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = machinePK } },
                    { "SK", new AttributeValue { S = machinePK } }
                }
            };

            // Start the calls concurrently
            var inventoryAsync = GetMachineInventoryAsync(id);
            var machineAsync = db.GetItemAsync(getMachineRequest);

            // Merge results
            var inventoryResult = await inventoryAsync;
            var machineResult = await machineAsync;

            if (machineResult.Item == null || machineResult.Item.Count == 0)
            {
                return null;
            }

            return new Machine()
            {
                PK = machineResult.Item["PK"].S!,
                SK = machineResult.Item["SK"].S!,
                Name = machineResult.Item.TryGetValue("Name", out var nameAttr) ? nameAttr.S : "",
                CreatedAt = machineResult.Item.TryGetValue("CreatedAt", out var createdAtAttr) && DateTime.TryParse(createdAtAttr.S, out var createdAt) ? createdAt : DateTime.MinValue,
                Inventory = inventoryResult,
            };
        }

        private async Task<List<MachineInventoryEntry>> GetMachineInventoryAsync(string id)
        {
            var result = await db.QueryAsync(new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "PK = :pkval AND begins_with(SK, :skval)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pkval", new AttributeValue { S = $"INV#{id}" } },
                    { ":skval", new AttributeValue { S = "PROD#" } },
                }
            });

            return [.. result.Items.Select(i => new MachineInventoryEntry
            {
                Name = i.TryGetValue("Name", out var itemNameAttr) ? itemNameAttr.S ?? string.Empty : string.Empty,
                CostPennies = i.TryGetValue("CostPennies", out var costAttr) && int.TryParse(costAttr.N, out var cost) ? cost : 0,
                Quantity = i.TryGetValue("Quantity", out var qtyAttr) && int.TryParse(qtyAttr.N, out var qty) ? qty : 0,
            })];
        }

        public async Task<List<MachineInventoryEntry>> RestockMachineAsync(string id, IEnumerable<MachineInventoryEntry> items)
        {
            var currentInventory = await GetMachineInventoryAsync(id);
            var updatedInventoryDict = items.ToDictionary(i => i.Name, i => i);

            var requestItems = new List<WriteRequest>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException("Inventory item cannot be null");
                }

                var innerItem = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = $"INV#{id}" } },
                    { "SK", new AttributeValue { S = "PROD#" + item.Name } },
                    { "Name", new AttributeValue { S = item.Name } },
                    { "CostPennies", new AttributeValue { N = item.CostPennies.ToString() } },
                    { "Quantity", new AttributeValue { N = item.Quantity.ToString() } },
                    { "RestockedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                };

                var putRequest = new WriteRequest(new PutRequest(innerItem));

                requestItems.Add(putRequest);
            }

            foreach (var item in currentInventory.Where(i => !updatedInventoryDict.ContainsKey(i.Name)))
            {
                // Delete the old item entirely
                var innerItem = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = $"INV#{id}" } },
                    { "SK", new AttributeValue { S = "PROD#" + item.Name } },
                };
                requestItems.Add(new WriteRequest(new DeleteRequest(innerItem)));
            }

            await DoBatchRequestsAsync(requestItems);

            return currentInventory;
        }

        // Does the specified requests as a batch. This is NOT atomic/transactional!
        private async Task DoBatchRequestsAsync(IEnumerable<WriteRequest> requests)
        {
            var requestItems = new List<WriteRequest>();

            async Task flushAsync()
            {
                if (requestItems.Count == 0) return;

                var batchRequest = new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                        {
                            { tableName, requestItems }
                        }
                };

                var batchResult = await db.BatchWriteItemAsync(batchRequest);

                // For now don't retry here, just fail
                if (batchResult.UnprocessedItems != null && batchResult.UnprocessedItems.Count > 0)
                {
                    throw new Exception($"Failed to process all batch items, {batchResult.UnprocessedItems.Count} failed");
                }

                requestItems.Clear();
            }

            foreach (var req in requests)
            {
                requestItems.Add(req);
                if (requestItems.Count == 25)
                {
                    await flushAsync();
                }
            }

            await flushAsync();
        }
    }
}
