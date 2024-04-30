using Hospital_Booking_Page_BackEnd.helpers;
using Hospital_Booking_Page_BackEnd.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hospital_Booking_Page_BackEnd.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>   
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Doctor>().ToTable("doctors");
            modelBuilder.Entity<Booking>().ToTable("bookings");
            modelBuilder.Entity<Category>().ToTable("categories");

            /*  modelBuilder.Entity<Category>()
          .HasMany(c => c.Doctor)
          .WithOne(d => d.Category)
          .HasForeignKey(d => d.CategoryId)
          .OnDelete(DeleteBehavior.Restrict); */

        }
    }

}



