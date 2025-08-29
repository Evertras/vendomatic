using Amazon.DynamoDBv2.DataModel;

namespace VendingMachine.Models
{
    public class Machine
    {
        public string PK { get; set; } = string.Empty;

        public string SK { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.MinValue;

        public List<MachineInventoryEntry> Inventory { get; set; } = [];
    }

    public class MachineInventoryEntry
    {
        public string Name { get; set; } = string.Empty;

        public int CostPennies { get; set; } = 0;

        public int Quantity { get; set; } = 0;

        public DateTime RestockedAt { get; set; } = DateTime.MinValue;
    }
}
