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

public class ApiV1Test
{
    const string tableName = "test-table";

    internal static T GetResponseIsOK<T>(APIGatewayHttpApiV2ProxyResponse res)
    {
        res.StatusCode.Should().Be((int)HttpStatusCode.OK);
        res.Body.Should().NotBeNull();
        res.Headers.Should().ContainKey("Content-Type");
        res.Headers["Content-Type"].Should().Be("application/json");

        var resObj = JsonSerializer.Deserialize<T>(res.Body!);
        resObj.Should().NotBeNull();
        return resObj;
    }

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
                new Machine
                {
                    Id = "1234",
                    Name = "Test Machine",
                }
            ]
        };

        var resObj = GetResponseIsOK<MachineListResponse>(res);
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
        var resObj = GetResponseIsOK<MachineCreateResponse>(res);
        resObj.Machine.Should().NotBeNull();
        resObj.Machine.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TestDeleteMachine()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.DeleteItemAsync(Arg.Any<DeleteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteItemResponse());

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

        var resObj = GetResponseIsOK<MachineDeleteResponse>(res);

        resObj.Success.Should().BeTrue();

        var validator = Arg.Is<DeleteItemRequest>(r => r.TableName == tableName && r.Key["PK"].S == $"MAC#{machineId}");
        await mockAmazonDB.Received().DeleteItemAsync(validator, Arg.Any<CancellationToken>());
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
                    },
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Chips" } },
                        { "Name", new AttributeValue { S = "Chips" } },
                        { "CostPennies", new AttributeValue { N = "100" } },
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
        var resObj = GetResponseIsOK<MachineDetailsResponse>(res);

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
                    },
                    new MachineInventoryEntry
                    {
                        Name = "Chips",
                        CostPennies = 100,
                    }
                ]
            }
        };

        resObj.Should().BeEquivalentTo(expectedResponse);
    }
}
