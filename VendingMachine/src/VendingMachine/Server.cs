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

            // Dumbest router ever, but for now it works
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
                Body = $"Route {input.RouteKey ?? "NULL" } Not Found",
            };
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> CreateMachine(APIGatewayHttpApiV2ProxyRequest input)
        {
            var body = string.IsNullOrEmpty(input.Body) ? "ok" : input.Body;
            if (input.IsBase64Encoded)
            {
                var bytes = Convert.FromBase64String(input.Body);
                body = Encoding.UTF8.GetString(bytes);
            }
            if (string.IsNullOrEmpty(body))
            {
                throw new ArgumentException("No body");
            }
            var machine = new Models.Machine
            {
                Name = body
            };
            await _repository.AddMachineAsync(machine);
            var response = new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 201,
            };
            return response;
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
