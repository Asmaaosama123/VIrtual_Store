using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace Vstore.Models
{     public enum Roles
    {
        Admin, Owner,User
    }
    public class User: IdentityUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string FName { get; set; }
        public string LName { get; set; }
       
        public string Address { get; set; }
        public byte[] Image { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDelete { get; set; }= false;

        public string? Delete_Reason { get; set; }
        public DateTime RegistirationDate { get; set; } = DateTime.Now;
        public Roles Roles { get; set; }
        public ICollection<Rate> rate { get; set; }
        public ICollection<Notification> Notifications { get; set; } // One-to-Many with Notification
        public ICollection<FavList> FavLists { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Cart> Carts { get; set; }


    }
}
