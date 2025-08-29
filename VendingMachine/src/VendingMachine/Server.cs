using Amazon.Lambda.APIGatewayEvents;
using Amazon.XRay.Recorder.Core;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using VendingMachine.Dtos;

namespace VendingMachine
{
    // There's probably a million better ways to do this, but for now...
    internal class Server(IRepository repository)
    {
        public async Task<APIGatewayHttpApiV2ProxyResponse> HandleRequest(APIGatewayHttpApiV2ProxyRequest input)
        {
            try
            {
                Console.WriteLine(input);

                // Dumbest router ever, but for now it works... note this is keyed off AWS API Gateway integration entries,
                // not the actual HTTP method and path
                return input.RouteKey switch
                {
                    "GET /api/v1/machines" => await ListMachinesAsync(input),
                    "POST /api/v1/machines" => await CreateMachineAsync(input),
                    "GET /api/v1/machines/{id}" => await GetMachineDetailsAsync(input),
                    "DELETE /api/v1/machines/{id}" => await DeleteMachineAsync(input),
                    "PUT /api/v1/machines/{id}/inventory" => await RestockMachineAsync(input),
                    _ => JsonResponse(
                        new GenericErrorResponse { Error = $"Route {input.RouteKey ?? "NULL"} Not Found" },
                        HttpStatusCode.NotFound),
                };
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument exception: {ex}");
                return JsonResponse(new GenericErrorResponse { Error = ex.Message }, HttpStatusCode.BadRequest);
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"Not supported exception: {ex}");
                return JsonResponse(new GenericErrorResponse { Error = ex.Message }, HttpStatusCode.UnsupportedMediaType);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Json exception: {ex}");
                return JsonResponse(new GenericErrorResponse { Error = ex.Message }, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown exception: {ex}");
                return JsonResponse(new GenericErrorResponse { Error = "Internal server error" }, HttpStatusCode.InternalServerError);
            }
        }

        internal static APIGatewayHttpApiV2ProxyResponse JsonResponse<T>(T obj, HttpStatusCode status = HttpStatusCode.OK)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = (int)status,
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

        internal async Task<APIGatewayHttpApiV2ProxyResponse> CreateMachineAsync(APIGatewayHttpApiV2ProxyRequest input)
        {
            var req = ParseBodyRequest<MachineCreateRequest>(input);

            var machine = new Models.Machine
            {
                Name = req.Name,
            };

            var id = await repository.CreateMachineAsync(machine);

            return JsonResponse(new MachineCreateResponse { Machine = { Id = id, Name = req.Name } });
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> ListMachinesAsync(APIGatewayHttpApiV2ProxyRequest input)
        {
            var machinesRaw = await repository.ListMachinesAsync();

            var machines = machinesRaw.Select(m => new Machine
            {
                Id = m.PK.Substring(4), // trim off MAC#
                Name = m.Name,
            }).ToList();

            return JsonResponse(new MachineListResponse { Machines = machines });
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> DeleteMachineAsync(APIGatewayHttpApiV2ProxyRequest input)
        {
            var id = input.PathParameters["id"];

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is required");
            }

            await repository.DeleteMachineAsync(id);

            return JsonResponse(new MachineDeleteResponse { Success = true });
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> GetMachineDetailsAsync(APIGatewayHttpApiV2ProxyRequest input)
        {
            var id = input.PathParameters["id"];

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is required");
            }

            var machine = await repository.GetMachineAsync(id);

            if (machine == null)
            {
                return JsonResponse(new GenericErrorResponse { Error = "Machine not found" }, HttpStatusCode.NotFound);
            }

            var dto = new MachineDetailsResponse
            {
                Machine = new MachineDetails
                {
                    Id = machine.PK.Substring(4), // trim off MAC#
                    Name = machine.Name,
                    Inventory = [.. machine.Inventory.Select(i => new MachineInventoryEntry
                    {
                        Name = i.Name,
                        CostPennies = i.CostPennies,
                        Quantity = i.Quantity,
                    })],
                }
            };

            return JsonResponse(dto);
        }

        internal async Task<APIGatewayHttpApiV2ProxyResponse> RestockMachineAsync(APIGatewayHttpApiV2ProxyRequest input)
        {
            var id = input.PathParameters["id"];
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is required");
            }
            var req = ParseBodyRequest<MachineRestockRequest>(input);
            var res = await repository.RestockMachineAsync(id, req.Inventory.Select(
                i => new Models.MachineInventoryEntry
                {
                    Name = i.Name,
                    CostPennies = i.CostPennies,
                    Quantity = i.QuantityTarget,
                }));

            return JsonResponse(new MachineRestockResponse { Success = true });
        }
    }
}
