using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vstore.Models;
namespace Vstore.Data
{
    public class AppDBContext : IdentityDbContext<User>
    {
        public AppDBContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Owner> Owners { get; set; }
      //  public DbSet<Categories> Categories { get; set; }
      //  public DbSet<Category> Category { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<FavList> FavLists { get; set; }
        public DbSet<FavListShop> FavListShops { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Order_Product> Order_Products { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems {get; set;}
        public DbSet<Payment> Payments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .ToTable("Users");

            modelBuilder.Entity<Owner>()
                .ToTable("Owners")
                .HasBaseType<User>();

          

            modelBuilder.Entity<FavListShop>()
                .HasKey(o => new { o.Owner_Id, o.FavList_Id });

            modelBuilder.Entity<Order_Product>()
                .HasKey(o => new { o.Order_Id, o.Product_Id });

            modelBuilder.Entity<Rate>()
                .HasKey(o => new { o.Product_Id, o.User_Id });
            modelBuilder.Entity<CartItem>()
               .HasKey(o => new { o.CartId, o.ProductId });

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Stock)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.Product_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Rate>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Rate)
                .HasForeignKey(r => r.Product_Id)
                .OnDelete(DeleteBehavior.Cascade);

          

           

            modelBuilder.Entity<Owner>()
                .HasMany(o => o.Products)
                .WithOne(p => p.owner)
                .HasForeignKey(p => p.Owner_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                 .HasOne(o => o.User)
                 .WithMany() 
                 .HasForeignKey(o => o.User_Id)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order_Product>()
                .HasOne(op => op.Order)
                .WithMany(o => o.Order_Products)
                .HasForeignKey(op => op.Order_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order_Product>()
                .HasOne(op => op.Product)
                .WithMany() 
                .HasForeignKey(op => op.Product_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order_Product>()
                .HasOne(op => op.Stock)
                .WithMany()
                .HasForeignKey(op => op.StockId)
               ;
        

        modelBuilder.Entity<Image>()
                .HasOne(i => i.Products)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.Product_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavListShop>()
                .HasOne(fls => fls.FavList)
                .WithMany(fl => fl.FavListShops)
                .HasForeignKey(fls => fls.FavList_Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.User_Id)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Stock>()
        .HasOne(s => s.color)
        .WithMany(c => c.Stock)
        .HasForeignKey(s => s.Color_id)
        .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Stock>()
    .HasOne(s => s.Product)
    .WithMany(p => p.Stock)
    .HasForeignKey(s => s.Product_Id)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Stock>()
                .HasOne(s => s.size)
                .WithMany(sz => sz.Stock)
                .HasForeignKey(s => s.Size_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
              .HasOne(c => c.User)
              .WithMany()
              .HasForeignKey(c => c.UserId)
              .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Stock)
                .WithMany()
                .HasForeignKey(ci => ci.StockId)
               ;


            modelBuilder.Entity<Order>()
   .HasOne(o => o.Payment)
   .WithOne(p => p.Order)  
   .HasForeignKey<Payment>(p => p.OrderId);
            modelBuilder.Entity<Owner>()
    .HasOne(o => o.Request)
    .WithOne(r => r.Owner)
    .HasForeignKey<Request>(r => r.OwnerId)
    .OnDelete(DeleteBehavior.Cascade); 


            base.OnModelCreating(modelBuilder);
        }

    }
}