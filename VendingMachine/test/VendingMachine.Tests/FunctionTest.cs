using Xunit;
using NSubstitute;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using System.Collections.Generic;
using System.Text.Json;
using VendingMachine.Models;
using VendingMachine.Dtos;
using Amazon.DynamoDBv2;

namespace VendingMachine.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestV1GetMachineListGetsMachines()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();

        mockAmazonDB.ScanAsync(Arg.Any<Amazon.DynamoDBv2.Model.ScanRequest>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new Amazon.DynamoDBv2.Model.ScanResponse
            {
                Items =
                [
                    new() {
                        { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = "MAC#1234" } },
                        { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = "MAC#1234" } },
                        { "Name", new Amazon.DynamoDBv2.Model.AttributeValue { S = "Test Machine" } },
                        { "ExtraField", new Amazon.DynamoDBv2.Model.AttributeValue { S = "Shouldn't bother anyone" } },
                        { "CreatedAt", new Amazon.DynamoDBv2.Model.AttributeValue { S = System.DateTime.UtcNow.ToString("o") } },
                    }
                ]
            });

        var server = new Server(new Repository(mockAmazonDB, "test-table"));

        var res = await server.HandleRequest(new Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "GET /api/v1/machines",
        });

        var expectedBody = JsonSerializer.Serialize(new MachineListResponse
        {
            Machines =
            [
                new Dtos.Machine
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
}
