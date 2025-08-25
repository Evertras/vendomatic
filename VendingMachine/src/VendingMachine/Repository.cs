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
    }
}
