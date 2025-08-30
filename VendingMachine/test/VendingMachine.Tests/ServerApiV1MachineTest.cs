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

        [Fact]
        public async Task TestDeleteMachineSucceeds()
        {
            var mockRepository = Substitute.For<IRepository>();
            var server = new Server(mockRepository);
            var expectedResponse = new MachineDeleteResponse
            {
                Success = true
            };
            var req = HttpTestHelpers.RequestFor("DELETE /api/v1/machines/{id}", id: "abc-def");
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIsOK<MachineDeleteResponse>(res);
            resObj.Should().BeEquivalentTo(expectedResponse);
            await mockRepository.Received(1).DeleteMachineAsync("abc-def");
        }

        public static TheoryData<MachineInventoryEntry[], Inventory[]> RestockMachineTestData()
        {
            return new TheoryData<MachineInventoryEntry[], Inventory[]>
            {
                {
                    // Starting inventory
                    [
                        new() { Name = "Soda", Quantity = 5, CostPennies = 100 },
                        new() { Name = "Chips", Quantity = 2, CostPennies = 100 },
                    ],
                    // Target inventory
                    [
                        new() { Name = "Soda", QuantityTarget = 10, CostPennies = 100 },
                        new() { Name = "Chips", QuantityTarget = 5, CostPennies = 100 },
                        new() { Name = "Candy", QuantityTarget = 3, CostPennies = 200 },
                    ]
                },
                {
                    // Starting inventory
                    [],
                    // Target inventory
                    [
                        new() { Name = "Water", QuantityTarget = 20, CostPennies = 50 },
                    ]
                },
                {
                    // Starting inventory
                    [
                        new() { Name = "Juice", Quantity = 1, CostPennies = 150 },
                    ],
                    // Target inventory
                    [
                        new() { Name = "Juice", QuantityTarget = 1, CostPennies = 150 }, // No change
                    ]
                }
            };
        }

        [Theory]
        [MemberData(nameof(RestockMachineTestData))]
        public async Task TestRestockMachineSucceeds(MachineInventoryEntry[] startingInventory, Inventory[] targetInventory)
        {
            var mockRepository = Substitute.For<IRepository>();
            mockRepository.GetMachineAsync("abc-def").Returns(new Models.Machine
            {
                PK = "MAC#abc-def",
                SK = "MAC#abc-def",
                Name = "Machine A",
                Inventory = [.. startingInventory.Select(i => new Models.MachineInventoryEntry { Name = i.Name, Quantity = i.Quantity })]
            });
            var server = new Server(mockRepository);
            var expectedResponse = new MachineRestockResponse
            {
                Success = true
            };
            var req = HttpTestHelpers.RequestFor("PUT /api/v1/machines/{id}/inventory", id: "abc-def", body: new MachineRestockRequest
            {
                Inventory = [.. targetInventory],
            });
            var res = await server.HandleRequest(req);
            var resObj = HttpTestHelpers.GetResponseIsOK<MachineRestockResponse>(res);
            resObj.Should().BeEquivalentTo(expectedResponse);
            await mockRepository.Received(1).RestockMachineAsync("abc-def", Arg.Is<IEnumerable<Models.MachineInventoryEntry>>(inv =>
                inv.Count() == targetInventory.Length &&
                targetInventory.All(ti => inv.Any(i => i.Name == ti.Name && i.Quantity == ti.QuantityTarget && i.CostPennies == ti.CostPennies))
            ));
        }
    }
}
