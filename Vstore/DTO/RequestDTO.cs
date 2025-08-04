namespace Vstore.DTO
{
    public class RequestDTO
    {

        public string OwnerId { get; set; } 
        public Status status { get; set; }
        public string? RejectionReason { get; set; }

    }
}
