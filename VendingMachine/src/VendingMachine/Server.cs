using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        public async Task<APIGatewayHttpApiV2ProxyResponse> HandleRequest(APIGatewayHttpApiV2ProxyRequest input)
        {
            // TODO: better logging
            Console.WriteLine(JsonSerializer.Serialize(input));

            // Dumbest router ever, but for now it works... note this is keyed off AWS API Gateway integration entries,
            // not the actual HTTP method and path
            switch (input.RouteKey)
            {
                case "POST /api/v1/machine":
                    return await CreateMachine(input);

                case "GET /api/v1/machine":
                    return await ListMachines(input);
            }

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 404,
                Body = $"Route {input.RouteKey ?? "NULL"} Not Found",
            };
        }

        // Figure out a better place later, but for now just a cheaty spot for this
        internal class CreateMachineRequest
        {
            public string? Name { get; set; }
        }

        internal class CreateMachineResponse
        {
            public string Id { get; set; } = string.Empty;
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> CreateMachine(APIGatewayHttpApiV2ProxyRequest input)
        {
            if (string.IsNullOrEmpty(input.Body))
            {
                throw new ArgumentException("No body");
            }

            var body = input.Body;

            if (input.IsBase64Encoded)
            {
                var bytes = Convert.FromBase64String(input.Body);
                body = Encoding.UTF8.GetString(bytes);
            }

            // TODO: try/catch here for json fails

            var req = JsonSerializer.Deserialize<CreateMachineRequest>(body);

            if (req == null || string.IsNullOrEmpty(req.Name))
            {
                throw new ArgumentException("Invalid request, must provide Name");
            }

            var machine = new Models.Machine
            {
                Name = req.Name,
            };

            var id = await _repository.AddMachineAsync(machine);

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 201,
                Body = JsonSerializer.Serialize(new CreateMachineResponse { Id = id }),
            };
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> ListMachines(APIGatewayHttpApiV2ProxyRequest input)
        {
            var machines = await _repository.ListMachinesAsync();

            var body = JsonSerializer.Serialize(machines);

            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 200,
                Body = body,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }
    }
}
