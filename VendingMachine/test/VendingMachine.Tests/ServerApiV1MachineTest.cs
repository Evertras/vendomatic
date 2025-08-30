using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using FluentAssertions;
using Xunit;
using VendingMachine.Dtos;

namespace VendingMachine.Tests
{
    public class ServerApiV1MachineTest
    {
        [Fact]
        public async Task TestListMachinesSucceeds()
        {
            var mockRepository = Substitute.For<IRepository>();
            mockRepository.ListMachinesAsync().Returns([
                new()
                {
                    PK = "MAC#abc-def",
                    SK = "MAC#abc-def",
                    Name = "Machine A",
                },
                new()
                {
                    PK = "MAC#123-xyz",
                    SK = "MAC#123-xyz",
                    Name = "Machine B",
                }
            ]);
            var server = new Server(mockRepository);
            var expectedResponse = new MachineListResponse
            {
                Machines = [
                    new() {
                        Id = "abc-def",
                        Name = "Machine A",
                    },
                    new() {
                        Id = "123-xyz",
                        Name = "Machine B",
                    },
                ]
            };
            var req = HttpTestHelpers.RequestFor("GET /api/v1/machines");
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIsOK<MachineListResponse>(res);
            resObj.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task TestCreateMachineSucceeds()
        {
            var mockRepository = Substitute.For<IRepository>();
            mockRepository.CreateMachineAsync(Arg.Is<Models.Machine>(m => m.Name == "Machine A")).Returns<string>("abc-def");
            var server = new Server(mockRepository);
            var expectedResponse = new MachineCreateResponse
            {
                Machine = new()
                {
                    Id = "abc-def",
                    Name = "Machine A"
                }
            };
            var req = HttpTestHelpers.RequestFor("POST /api/v1/machines", body: new MachineCreateRequest { Name = "Machine A" });
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIsOK<MachineCreateResponse>(res);
            resObj.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task TestCreateMachineFailsWithEmptyName()
        {
            var mockRepository = Substitute.For<IRepository>();
            var server = new Server(mockRepository);
            var req = HttpTestHelpers.RequestFor("POST /api/v1/machines", body: new MachineCreateRequest { Name = "" });
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIs<GenericErrorResponse>(res, System.Net.HttpStatusCode.BadRequest);
            resObj.Error.Should().StartWith("Validation failed");
        }
    }
}
