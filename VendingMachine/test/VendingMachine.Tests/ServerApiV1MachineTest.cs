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
                    new()
                    {
                        Id = "abc-def",
                        Name = "Machine A",
                    },
                    new()
                    {
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

        [Fact]
        public async Task TestGetMachineSucceeds()
        {
            var mockRepository = Substitute.For<IRepository>();
            mockRepository.GetMachineAsync("abc-def").Returns(new Models.Machine
            {
                PK = "MAC#abc-def",
                SK = "MAC#abc-def",
                Name = "Machine A",
                Inventory =
                [
                    new Models.MachineInventoryEntry { Name = "Soda", Quantity = 10 },
                    new Models.MachineInventoryEntry { Name = "Chips", Quantity = 5 },
                ]
            });
            var server = new Server(mockRepository);
            var expectedResponse = new MachineDetailsResponse
            {
                Machine = new()
                {
                    Id = "abc-def",
                    Name = "Machine A",
                    Inventory =
                    [
                        new() { Name = "Soda", Quantity = 10 },
                        new() { Name = "Chips", Quantity = 5 },
                    ]
                }
            };
            var req = HttpTestHelpers.RequestFor("GET /api/v1/machines/{id}", id: "abc-def");
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIsOK<MachineDetailsResponse>(res);
            resObj.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task TestGetMachineReturns404WithErrorMessage()
        {
            var mockRepository = Substitute.For<IRepository>();
            mockRepository.GetMachineAsync("nonexistent").Returns((Models.Machine?)null);
            var server = new Server(mockRepository);
            var req = HttpTestHelpers.RequestFor("GET /api/v1/machines/{id}", id: "nonexistent");
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIs<GenericErrorResponse>(res, System.Net.HttpStatusCode.NotFound);
            resObj.Error.Should().Be("Machine not found");
        }
    }
}
