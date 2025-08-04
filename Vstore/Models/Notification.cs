using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vstore.Models
{
    public class Notification
    {
       [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Notification_Id { get; set; }

        public string Title { get; set; }  
        public string Body { get; set; }   
        public DateTime DateTime { get; set; }
        public bool? isread { get; set; }
        public byte[] Image { get; set; }


        public string Notification_Message { get; set; }  

       
        public string User_Id { get; set; }  
        public User User { get; set; }
    }
}
