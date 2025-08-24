using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            try
            {
                // Dumbest router ever, but for now it works... note this is keyed off AWS API Gateway integration entries,
                // not the actual HTTP method and path
                switch (input.RouteKey)
                {
                    case "POST /api/v1/machine":
                        return await CreateMachine(input);

                    case "GET /api/v1/machine":
                        return await ListMachines(input);

                    default:
                        return new APIGatewayHttpApiV2ProxyResponse
                        {
                            StatusCode = 404,
                            Body = $"Route {input.RouteKey ?? "NULL"} Not Found",
                        };
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument exception: {ex}");
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = ex.Message,
                };
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"Not supported exception: {ex}");
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                    Body = ex.Message,
                };
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Json exception: {ex}");
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = ex.Message,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown exception: {ex}");
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    // Temporary for quick debugging
                    Body = ex.Message,
                    // Body = "Internal Server Error",
                };
            }
        }

        internal static APIGatewayHttpApiV2ProxyResponse JsonResponse<T>(T obj)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(obj),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };
        }

        internal static T ParseBodyRequest<T>(APIGatewayHttpApiV2ProxyRequest input)
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

            string contentType = "text/plain";
            if (input.Headers.TryGetValue("content-type", out var headerContentType) && !string.IsNullOrEmpty(headerContentType))
            {
                contentType = headerContentType;
            }

            switch (contentType)
            {
                case "application/json":
                    return JsonSerializer.Deserialize<T>(body) ?? throw new ArgumentException("Failed to parse JSON body");
                default:
                    throw new NotSupportedException($"Unsupported Content-Type {contentType}");
            }
        }

        // Figure out a better place later, but for now just a cheaty spot for this
        internal class CreateMachineRequest
        {
            public string? Name { get; init; }
        }

        internal class CreateMachineResponse
        {
            public string Id { get; init; } = string.Empty;
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> CreateMachine(APIGatewayHttpApiV2ProxyRequest input)
        {
            var req = ParseBodyRequest<CreateMachineRequest>(input);

            if (req == null || string.IsNullOrEmpty(req.Name))
            {
                throw new ArgumentException("Invalid request, must provide Name");
            }

            var machine = new Models.Machine
            {
                Name = req.Name,
            };

            var id = await _repository.AddMachineAsync(machine);

            return JsonResponse(new CreateMachineResponse { Id = id });
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> ListMachines(APIGatewayHttpApiV2ProxyRequest input)
        {
            var machines = await _repository.ListMachinesAsync();

            return JsonResponse(machines);
        }
    }
}
