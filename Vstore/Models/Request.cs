using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vstore.Models
{
    public enum Status
    {
        Pending = 0,
        Accepted = 1, 
        Rejected = 2
    }
    public class Request
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Status status { get; set; } = Status.Pending;
        public string? RejectionReason { get; set; }
       
        public string OwnerId { get; set; }
        public Owner Owner { get; set; }
    }
}
