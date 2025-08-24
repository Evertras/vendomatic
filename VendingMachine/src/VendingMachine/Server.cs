using Amazon.Lambda.APIGatewayEvents;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;

namespace VendingMachine
{
    // There's probably a million better ways to do this, but for now...
    public class Server
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
                    case "POST /api/v1/machines":
                        return await CreateMachine(input);

                    case "GET /api/v1/machines":
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
                    Body = "Internal Server Error",
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

            string contentType = "application/json";
            if (input.Headers.TryGetValue("content-type", out var headerContentType) && !string.IsNullOrEmpty(headerContentType))
            {
                contentType = headerContentType;
            }

            var ret = contentType switch
            {
                "application/json" => JsonSerializer.Deserialize<T>(body) ?? throw new ArgumentException("Failed to parse JSON body"),
                _ => throw new NotSupportedException($"Unsupported Content-Type {contentType}"),
            };

            var validationContext = new ValidationContext(ret, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(
                ret,
                validationContext,
                validationResults,
                validateAllProperties: true
            );

            if (!isValid)
            {
                var messages = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error");
                throw new ArgumentException("Validation failed: " + string.Join("; ", messages));
            }

            return ret;
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> CreateMachine(APIGatewayHttpApiV2ProxyRequest input)
        {
            var req = ParseBodyRequest<Dtos.MachineCreateRequest>(input);

            var machine = new Models.Machine
            {
                Name = req.Name,
            };

            var id = await _repository.AddMachineAsync(machine);

            return JsonResponse(new Dtos.MachineCreateResponse { Machine = { Id = id } });
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> ListMachines(APIGatewayHttpApiV2ProxyRequest input)
        {
            var machinesRaw = await _repository.ListMachinesAsync();

            var machines = machinesRaw.Select(m => new Dtos.Machine
            {
                Id = m.PK.Substring(4), // trim off MAC#
                Name = m.Name,
            }).ToList();

            return JsonResponse(new Dtos.MachineListResponse { Machines = machines });
        }
    }
}
