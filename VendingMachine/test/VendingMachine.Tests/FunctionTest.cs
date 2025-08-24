using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Moq;
using System.Collections.Generic;
using System.Text.Json;
using VendingMachine.Server;
using VendingMachine.Models;

namespace VendingMachine.Tests;

public class FunctionTest
{
    [Fact]
    public async void TestCreateMachine()
    {
        // Arrange
        var testMachine = new Machine { Name = "Test Machine" };
        var expectedId = "test-id";
        var mockRepository = new Mock<IRepository>();
        mockRepository.Setup(r => r.AddMachineAsync(It.Is<Machine>(m => m.Name == testMachine.Name)))
            .ReturnsAsync(expectedId);

        var server = new Server(mockRepository.Object);
        var request = new APIGatewayHttpApiV2ProxyRequest
        {
            RouteKey = "POST /api/v1/machines",
            Body = JsonSerializer.Serialize(new CreateMachineRequest { Name = testMachine.Name }),
            Headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" }
            }
        };

        // Act
        var response = await server.HandleRequest(request);

        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("application/json", response.Headers["Content-Type"]);
        
        var createResponse = JsonSerializer.Deserialize<CreateMachineResponse>(response.Body);
        Assert.NotNull(createResponse);
        Assert.Equal(expectedId, createResponse.Id);

        mockRepository.Verify(r => r.AddMachineAsync(It.Is<Machine>(m => m.Name == testMachine.Name)), Times.Once);
    }
}
