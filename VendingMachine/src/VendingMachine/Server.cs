using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VendingMachine
{
    // There's probably a million better ways to do this, but for now...
    internal class Server
    {
        readonly IRepository _repository;

        public Server(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<APIGatewayHttpApiV2ProxyResponse> HandlRequest(APIGatewayHttpApiV2ProxyRequest input)
        {
            var body = string.IsNullOrEmpty(input.Body) ? "ok" : input.Body;
            if (input.IsBase64Encoded)
            {
                var bytes = Convert.FromBase64String(input.Body);
                body = Encoding.UTF8.GetString(bytes);
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
            var machine = new Models.Machine
            {
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
}
