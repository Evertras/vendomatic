using Amazon.DynamoDBv2.DataModel;

namespace VendingMachine.Models
{
    public class Machine
    {
        public string PK { get; set; } = string.Empty;

        public string SK { get; set; } = string.Empty;

        public string? Name { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
