using Xunit;
using NSubstitute;
using System.Text.Json;
using VendingMachine.Dtos;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using Amazon.DynamoDBv2.Model;

namespace VendingMachine.Tests;

public class ApiV1Test
{
    [Fact]
    public async Task TestGetMachineListGetsMachines()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();

        mockAmazonDB.ScanAsync(Arg.Any<Amazon.DynamoDBv2.Model.ScanRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Amazon.DynamoDBv2.Model.ScanResponse
            {
                Items =
                [
                    new() {
                        { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = "MAC#1234" } },
                        { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = "MAC#1234" } },
                        { "Name", new Amazon.DynamoDBv2.Model.AttributeValue { S = "Test Machine" } },
                        { "ExtraField", new Amazon.DynamoDBv2.Model.AttributeValue { S = "Shouldn't bother anyone" } },
                        { "CreatedAt", new Amazon.DynamoDBv2.Model.AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                    }
                ]
            });

        var server = new Server(new Repository(mockAmazonDB, "test-table"));

        var res = await server.HandleRequest(new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "GET /api/v1/machines",
        });

        var expectedBody = JsonSerializer.Serialize(new MachineListResponse
        {
            Machines =
            [
                new Machine
                {
                    Id = "1234",
                    Name = "Test Machine",
                }
            ]
        });

        Assert.Equal(200, res.StatusCode);
        Assert.NotNull(res.Body);
        Assert.Equal(expectedBody, res.Body);
        Assert.True(res.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", res.Headers["Content-Type"]);
    }

    [Fact]
    public async Task TestCreateMachineCreatesMachine()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.PutItemAsync(Arg.Any<Amazon.DynamoDBv2.Model.PutItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Amazon.DynamoDBv2.Model.PutItemResponse());
        var server = new Server(new Repository(mockAmazonDB, "test-table"));
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
        Assert.Equal(200, res.StatusCode);
        Assert.NotNull(res.Body);
        Assert.True(res.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", res.Headers["Content-Type"]);
        var resObj = JsonSerializer.Deserialize<MachineCreateResponse>(res.Body!);
        Assert.NotNull(resObj);
        Assert.False(string.IsNullOrEmpty(resObj.Machine.Id));
    }

    [Fact]
    public async Task TestDeleteMachine()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        const string tableName = "test-table";
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

        Assert.Equal(200, res.StatusCode);

        var validator = Arg.Is<DeleteItemRequest>(r => r.TableName == tableName && r.Key["PK"].S == $"MAC#{machineId}");
        await mockAmazonDB.Received().DeleteItemAsync(validator, Arg.Any<CancellationToken>());
    }
}
