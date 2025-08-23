using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

namespace VendingMachine.Tests;

public class FunctionTest
{
    [Fact]
    public void TestToUpperFunction()
    {

        // Invoke the lambda function and confirm the string was upper cased.
        var function = new Function();
        var context = new TestLambdaContext();
        var input = new Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest
        {
            Body = "hello world",
            IsBase64Encoded = false
        };

        var result = function.FunctionHandler(input);

        Assert.Equal("HELLO WORLD!", result.Body);
        Assert.Equal(200, result.StatusCode);
    }
}
