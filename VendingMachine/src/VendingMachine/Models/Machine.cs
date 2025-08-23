using Amazon.DynamoDBv2.DataModel;

namespace VendingMachine.Models
{
    internal class Machine
    {
        public string? PK { get; set; }

        public string? SK { get; set; }

        public string? Name { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
