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
        Task<string> AddMachineAsync(Machine machine);
        Task<List<Machine>> ListMachinesAsync();
        Task DeleteMachineAsync(string id);
        Task<Machine?> GetMachineAsync(string id);
    }

    internal class Repository(IAmazonDynamoDB db, string tableName) : IRepository
    {
        public async Task<string> AddMachineAsync(Machine machine)
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
            var pk = "MAC#" + id;
            var deleteRequest = new DeleteItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = pk } },
                    { "SK", new AttributeValue { S = pk } }
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
            var machineAsync = db.GetItemAsync(getMachineRequest);

            var inventoryPK = "INV#" + id;
            var productScanRequest = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = "PK = :pkval AND begins_with(SK, :skval)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pkval", new AttributeValue { S = inventoryPK } },
                    { ":skval", new AttributeValue { S = "PROD#" } },
                }
            };

            var inventoryAsync = db.ScanAsync(productScanRequest);

            var machineResult = await machineAsync;

            if (machineResult.Item == null || machineResult.Item.Count == 0)
            {
                return null;
            }

            var productScanResult = await inventoryAsync;

            return new Machine()
            {
                PK = machineResult.Item["PK"].S!,
                SK = machineResult.Item["SK"].S!,
                Name = machineResult.Item.TryGetValue("Name", out var nameAttr) ? nameAttr.S : null,
                CreatedAt = machineResult.Item.TryGetValue("CreatedAt", out var createdAtAttr) && DateTime.TryParse(createdAtAttr.S, out var createdAt) ? createdAt : null,
                Inventory = [.. productScanResult.Items.Select(i => new MachineInventoryEntry
                {
                    Name = i.TryGetValue("Name", out var itemNameAttr) ? itemNameAttr.S ?? string.Empty : string.Empty,
                    CostPennies = i.TryGetValue("CostPennies", out var costAttr) && int.TryParse(costAttr.N, out var cost) ? cost : 0,
                })],
            };
        }
    }
}
