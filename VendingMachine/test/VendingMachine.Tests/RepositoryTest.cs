using Xunit;
using NSubstitute;
using VendingMachine.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;

namespace VendingMachine.Tests;

// TODO: This is very wide for unit tests, should split between Server/Repository tests before
// this gets even more tangled...
public class RepositoryTest
{
    const string tableName = "test-table";

    [Fact]
    public async Task TestListMachinesSucceeds()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        var now = DateTime.UtcNow;

        mockAmazonDB.ScanAsync(Arg.Any<ScanRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResponse
            {
                Items =
                [
                    new() {
                        { "PK", new AttributeValue { S = "MAC#1234" } },
                        { "SK", new AttributeValue { S = "MAC#1234" } },
                        { "Name", new AttributeValue { S = "Test Machine" } },
                        { "ExtraField", new AttributeValue { S = "Shouldn't bother anyone" } },
                        { "CreatedAt", new AttributeValue { S = now.ToString("o") } },
                    }
                ]
            });

        var repository = new Repository(mockAmazonDB, tableName);
        var expectedList = new List<Machine>{
            new()
            {
                PK = "MAC#1234",
                SK = "MAC#1234",
                Name = "Test Machine",
                CreatedAt = now,
            }
        };

        var resObj = await repository.ListMachinesAsync();
        resObj.Should().BeEquivalentTo(expectedList);
    }

    [Fact]
    public async Task TestCreateMachineSucceeds()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.PutItemAsync(Arg.Any<PutItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutItemResponse());
        var repository = new Repository(mockAmazonDB, tableName);
        var machineToCreate = new Machine
        {
            Name = "Test Machine",
        };

        var result = await repository.CreateMachineAsync(machineToCreate);

        result.Should().NotBeNullOrEmpty();
        await mockAmazonDB.Received().PutItemAsync(Arg.Is<PutItemRequest>(r =>
            r.TableName == tableName &&
            r.Item["PK"].S == $"MAC#{result}" &&
            r.Item["SK"].S == $"MAC#{result}" &&
            r.Item["Name"].S == "Test Machine" &&
            r.Item.ContainsKey("CreatedAt")
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestDeleteMachineNoInventorySucceeds()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new QueryResponse() { Items = [] });
        mockAmazonDB.DeleteItemAsync(Arg.Any<DeleteItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteItemResponse());
        var repository = new Repository(mockAmazonDB, tableName);

        await repository.DeleteMachineAsync("1234");

        await mockAmazonDB.Received().DeleteItemAsync(Arg.Is<DeleteItemRequest>(r =>
            r.TableName == tableName &&
            r.Key["PK"].S == "MAC#1234" &&
            r.Key["SK"].S == "MAC#1234"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestDeleteMachineWithSmallInventorySucceeds()
    {
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        var queryResponse = new QueryResponse
        {
            Items =
            [
                new() {
                    { "PK", new AttributeValue { S = "INV#1234" } },
                    { "SK", new AttributeValue { S = "ITEM#Soda" } },
                    { "Name", new AttributeValue { S = "Soda" } },
                    { "CostPennies", new AttributeValue { N = "150" } },
                    { "Quantity", new AttributeValue { N = "10" } },
                },
                new() {
                    { "PK", new AttributeValue { S = "INV#1234" } },
                    { "SK", new AttributeValue { S = "ITEM#Chips" } },
                    { "Name", new AttributeValue { S = "Chips" } },
                    { "CostPennies", new AttributeValue { N = "100" } },
                    { "Quantity", new AttributeValue { N = "7" } },
                }
            ]
        };
        var seenDeleteRequestsPKandSK = new List<Tuple<string, string>>();
        mockAmazonDB.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(queryResponse);
        mockAmazonDB.BatchWriteItemAsync(Arg.Do<BatchWriteItemRequest>(r =>
                {
                    r.RequestItems.Should().ContainSingle();
                    r.RequestItems.Should().ContainKey(tableName);
                    var deletes = r.RequestItems[tableName].Where(ri => ri.DeleteRequest != null).
                        Select(ri => new Tuple<string, string>(ri.DeleteRequest.Key["PK"].S, ri.DeleteRequest.Key["SK"].S));
                    seenDeleteRequestsPKandSK.AddRange(deletes);
                }
            ), Arg.Any<CancellationToken>())
            .Returns(new BatchWriteItemResponse());
        mockAmazonDB.DeleteItemAsync(Arg.Do<DeleteItemRequest>(r =>
            {
                r.TableName.Should().Be(tableName);
                seenDeleteRequestsPKandSK.Add(new Tuple<string, string>(r.Key["PK"].S, r.Key["SK"].S));
            }), Arg.Any<CancellationToken>()).Returns(new DeleteItemResponse());
        var repository = new Repository(mockAmazonDB, tableName);

        await repository.DeleteMachineAsync("1234");

        seenDeleteRequestsPKandSK.Should().HaveCount(3);
        seenDeleteRequestsPKandSK.Should().Contain(new Tuple<string, string>("INV#1234", "ITEM#Chips"));
        seenDeleteRequestsPKandSK.Should().Contain(new Tuple<string, string>("INV#1234", "ITEM#Soda"));
        seenDeleteRequestsPKandSK.Should().Contain(new Tuple<string, string>("MAC#1234", "MAC#1234"));
    }

    [Fact]
    public async Task TestGetMachineDetails()
    {
        var now = DateTime.Now;
        var mockAmazonDB = Substitute.For<IAmazonDynamoDB>();
        mockAmazonDB.GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = "MAC#1234" } },
                    { "SK", new AttributeValue { S = "MAC#1234" } },
                    { "Name", new AttributeValue { S = "Test Machine" } },
                    { "CreatedAt", new AttributeValue { S = now.ToString("o") } },
                }
            });
        mockAmazonDB.QueryAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new QueryResponse
            {
                Items =
                [
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Soda" } },
                        { "Name", new AttributeValue { S = "Soda" } },
                        { "CostPennies", new AttributeValue { N = "150" } },
                        { "Quantity", new AttributeValue { N = "10" } },
                    },
                    new() {
                        { "PK", new AttributeValue { S = "INV#1234" } },
                        { "SK", new AttributeValue { S = "ITEM#Chips" } },
                        { "Name", new AttributeValue { S = "Chips" } },
                        { "CostPennies", new AttributeValue { N = "100" } },
                        { "Quantity", new AttributeValue { N = "7" } },
                    }
                ]
            });
        var repository = new Repository(mockAmazonDB, tableName);
        var expectedMachineDetails = new Machine
        {
            PK = "MAC#1234",
            SK = "MAC#1234",
            Name = "Test Machine",
            CreatedAt = now,
            Inventory = [
                new MachineInventoryEntry
                {
                    Name = "Soda",
                    CostPennies = 150,
                    Quantity = 10,
                },
                new MachineInventoryEntry
                {
                    Name = "Chips",
                    CostPennies = 100,
                    Quantity = 7,
                }
            ]
        };

        var machineDetails = await repository.GetMachineAsync("1234");
        machineDetails.Should().NotBeNull();
        // Time will be truncated to the second when stored but should be close enough
        machineDetails.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        machineDetails.CreatedAt = now;
        machineDetails.Should().BeEquivalentTo(expectedMachineDetails);
    }

    // TODO: Restock test, but make it actually maintainable... the restock function does a lot of put/deletes,
    // do we need a mock memory store? Ideally we'd be using a local DynamoDB (localstack) but I don't want to
    // deal with adding docker to the stack for now.
}
