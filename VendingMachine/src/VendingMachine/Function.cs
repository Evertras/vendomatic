using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace VendingMachine;

public class Function
{
    
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public APIGatewayHttpApiV2ProxyResponse FunctionHandler(APIGatewayHttpApiV2ProxyRequest input)
    {
        var body = string.IsNullOrEmpty(input.Body) ? "ok" : input.Body;
        if (input.IsBase64Encoded)
        {
            var bytes = Convert.FromBase64String(input.Body);
            body = System.Text.Encoding.UTF8.GetString(bytes);
        }

        var response = new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = body.ToUpper() + "!",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }
}
