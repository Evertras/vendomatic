using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace VendingMachine;

// Acts as an entry point for AWS to invoke the Lambda function. Only worry about
// setting up dependencies and passing the request to the Server where the logic is.
public class Function
{
    readonly Server _server;

    public Function()
    {
        var db = new AmazonDynamoDBClient();
        var tableName = Environment.GetEnvironmentVariable("EVERTRAS_TABLE_NAME") ?? "evertras-vendomatic-db";
        var repository = new Repository(db, tableName);
        _server = new Server(repository);
    }
    
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest input)
    {
        return await _server.HandlRequest(input);
    }
}
