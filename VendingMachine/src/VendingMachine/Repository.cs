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
        Task AddMachineAsync(Machine machine);
    }

    internal class Repository : IRepository
    {
        private readonly IAmazonDynamoDB _dynamodDB;
        private readonly string _tableName;

        // Temporary
        public Repository(IAmazonDynamoDB db, string tableName)
        {
            _dynamodDB = db;
            _tableName = tableName;
        }

        public async Task AddMachineAsync(Machine machine)
        {
            machine.PK = "MAC#" + Guid.NewGuid().ToString();
            machine.SK = machine.PK;
            machine.CreatedAt = DateTime.UtcNow;

            var asJson = JsonSerializer.Serialize(machine);
            var asAttrs = Document.FromJson(asJson).ToAttributeMap();

            var createRequest = new PutItemRequest
            {
                TableName = _tableName,
                Item = asAttrs,
            };

            Console.WriteLine(JsonSerializer.Serialize(machine));

            var result = await _dynamodDB.PutItemAsync(createRequest);

            Console.WriteLine(JsonSerializer.Serialize(result));
        }
    }
}
