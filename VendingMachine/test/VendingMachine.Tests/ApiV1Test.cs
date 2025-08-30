using Xunit;
using NSubstitute;
using System.Text.Json;
using VendingMachine.Dtos;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Amazon.Runtime.Internal.Util;

namespace VendingMachine.Tests;

// TODO: This is very wide for unit tests, should split between Server/Repository tests before
// this gets even more tangled...
public class ApiV1Test
{
    const string tableName = "test-table";

    [Fact]
    public async Task TestGetMachineListGetsMachines()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();

        mockAmazonDB.ScanAsync(Arg.Any<ScanRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResponse
            {
                Items =
                [
                    new() {
                        { "PK", new AttributeValue { S = "MAC#1234" } },
                        { "SK", new AttributeValue { S = "MAC#1234" } },
                        { "Name", new AttributeValue { S = "Test Machine" } },
                        { "ExtraField", new AttributeValue { S = "Shouldn't bother anyone" } },
                        { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                    }
                ]
            });

        var server = new Server(new Repository(mockAmazonDB, tableName));

        var res = await server.HandleRequest(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "GET /api/v1/machines",
        });

        var expectedResponse = new MachineListResponse
        {
            Machines =
            [
                new MachineSummary
                {
                    Id = "1234",
                    Name = "Test Machine",
                }
            ]
        };

        var resObj = HttpTestHelpers.GetResponseIsOK<MachineListResponse>(res);
        resObj.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestCreateMachineCreatesMachine()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.PutItemAsync(Arg.Any<PutItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutItemResponse());
        var server = new Server(new Repository(mockAmazonDB, tableName));
        var req = new MachineCreateRequest
        {
            Name = "New Machine",
        };
        var reqBody = JsonSerializer.Serialize(req);
        var reqBodyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(reqBody));
        var res = await server.HandleRequest(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "POST /api/v1/machines",
            Body = reqBodyBase64,
            IsBase64Encoded = true,
            Headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" }
            }
        });
        var resObj = HttpTestHelpers.GetResponseIsOK<MachineCreateResponse>(res);
        resObj.Machine.Should().NotBeNull();
        resObj.Machine.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TestDeleteMachine()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        // TODO: Mock out Query to return inventory later
        mockAmazonDB.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new QueryResponse() { Items = [] });
        mockAmazonDB.DeleteItemAsync(Arg.Any<DeleteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteItemResponse());
        mockAmazonDB.BatchWriteItemAsync(Arg.Any<BatchWriteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BatchWriteItemResponse());

        var server = new Server(new Repository(mockAmazonDB, tableName));
        var machineId = "1234";
        var res = await server.HandleRequest(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "DELETE /api/v1/machines/{id}",
            PathParameters = new Dictionary<string, string>
            {
                { "id", machineId }
            }
        });

        var resObj = HttpTestHelpers.GetResponseIsOK<MachineDeleteResponse>(res);

        resObj.Success.Should().BeTrue();

        var validator = Arg.Is<DeleteItemRequest>(r => r.TableName == tableName && r.Key["PK"].S == $"MAC#{machineId}");
        await mockAmazonDB.Received().DeleteItemAsync(validator, Arg.Any<CancellationToken>());
        // TODO: Add checks for batch write deletes later
    }

    [Fact]
    public async Task TestGetMachineDetails()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = "MAC#1234" } },
                    { "SK", new AttributeValue { S = "MAC#1234" } },
                    { "Name", new AttributeValue { S = "Test Machine" } },
                    { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                }
            });
        mockAmazonDB.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new QueryResponse
            {
                Items =
                [
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Soda" } },
                        { "Name", new AttributeValue { S = "Soda" } },
                        { "CostPennies", new AttributeValue { N = "150" } },
                        { "Quantity", new AttributeValue { N = "10" } },
                    },
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Chips" } },
                        { "Name", new AttributeValue { S = "Chips" } },
                        { "CostPennies", new AttributeValue { N = "100" } },
                        { "Quantity", new AttributeValue { N = "7" } },
                    }
                ]
            });

        var server = new Server(new Repository(mockAmazonDB, tableName));
        var res = await server.HandleRequest(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "GET /api/v1/machines/{id}",
            PathParameters = new Dictionary<string, string>
            {
                { "id", "1234" }
            }
        });
        var resObj = HttpTestHelpers.GetResponseIsOK<MachineDetailsResponse>(res);

        var expectedResponse = new MachineDetailsResponse
        {
            Machine = new MachineDetails
            {
                Id = "1234",
                Name = "Test Machine",
                Inventory =
                [
                    new MachineInventoryEntry
                    {
                        Name = "Soda",
                        CostPennies = 150,
                        Quantity = 10,
                    },
                    new MachineInventoryEntry
                    {
                        Name = "Chips",
                        CostPennies = 100,
                        Quantity = 7,
                    }
                ]
            }
        };

        resObj.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TestRestockMachine()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.PutItemAsync(Arg.Any<PutItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutItemResponse());
        mockAmazonDB.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new QueryResponse
            {
                Items =
                [
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Soda" } },
                        { "Name", new AttributeValue { S = "Soda" } },
                        { "CostPennies", new AttributeValue { N = "150" } },
                        { "Quantity", new AttributeValue { N = "10" } },
                    },
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Chips" } },
                        { "Name", new AttributeValue { S = "Chips" } },
                        { "CostPennies", new AttributeValue { N = "100" } },
                        { "Quantity", new AttributeValue { N = "7" } },
                    }
                ]
            });
        mockAmazonDB.BatchWriteItemAsync(Arg.Any<BatchWriteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BatchWriteItemResponse());

        var restockRequest = new MachineRestockRequest
        {
            Inventory = [
                new Inventory { Name = "Soda", CostPennies = 200, QuantityTarget = 10 },
                new Inventory { Name = "Chips", CostPennies = 100, QuantityTarget = 10 },
                new Inventory { Name = "Pretzels", CostPennies = 100, QuantityTarget = 10 },
            ]
        };

        var expectedResponse = new MachineRestockResponse
        {
            Success = true,
        };

        var server = new Server(new Repository(mockAmazonDB, tableName));
        var res = await server.HandleRequest(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "PUT /api/v1/machines/{id}/inventory",
            PathParameters = new Dictionary<string, string>
            {
                { "id", "1234" }
            },
            Body = JsonSerializer.Serialize(restockRequest),
            Headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" }
            }
        });
        var resObj = HttpTestHelpers.GetResponseIsOK<MachineRestockResponse>(res);

        resObj.Should().BeEquivalentTo(expectedResponse);

        // TODO: Better checks... this is getting complicated for a unit test
    }
}
