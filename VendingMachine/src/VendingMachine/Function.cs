using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace VendingMachine;

public class Function
{
    readonly Repository _repository;

    public Function()
    {
        var db = new AmazonDynamoDBClient();
        var tableName = Environment.GetEnvironmentVariable("EVERTRAS_TABLE_NAME") ?? "evertras-vendomatic-db";
        _repository = new Repository(db, tableName);
    }
    
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest input)
    {
        var body = string.IsNullOrEmpty(input.Body) ? "ok" : input.Body;
        if (input.IsBase64Encoded)
        {
            var bytes = Convert.FromBase64String(input.Body);
            body = System.Text.Encoding.UTF8.GetString(bytes);
        }

        if (string.IsNullOrEmpty(body))
        {
            var badResponse = new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 400,
                Body = "Bad Request: No body",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
            return badResponse;
        }

        var machine = new Models.Machine {
            Name = body
        };

        await _repository.AddMachineAsync(machine);

        var response = new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = "Created!",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }
}
