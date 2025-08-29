using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using System.Runtime.CompilerServices;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

[assembly: InternalsVisibleTo("VendingMachine.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace VendingMachine;

// Acts as an entry point for AWS to invoke the Lambda function. Only worry about
// setting up dependencies and passing the request to the Server where the logic is.
public class Function
{
    readonly Server _server;

    public Function()
    {
        AWSSDKHandler.RegisterXRayForAllServices();

        var db = new AmazonDynamoDBClient();
        var tableName = Environment.GetEnvironmentVariable("EVERTRAS_TABLE_NAME") ?? "evertras-vendomatic-db";
        var repository = new Repository(db, tableName);
        _server = new Server(repository);
    }
    
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest input)
    {
        AWSXRayRecorder.Instance.BeginSubsegment("Function");
        AWSXRayRecorder.Instance.AddAnnotation("RouteKey", input.RouteKey ?? "NULL");

        try
        {
            return await _server.HandleRequest(input);
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }
    }
}
