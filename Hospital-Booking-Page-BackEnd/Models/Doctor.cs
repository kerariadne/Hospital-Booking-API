using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_Booking_Page_BackEnd.Models
{
    public class Doctor 
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? CV { get; set; }
        public string? Photo { get; set; }
        public string? Thumbnail { get; set; } 
        public required string PersonalNumber { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? ShortDescription { get; set; }
        public int NumberOfBookings { get; set; }
        public DateTime RefreshTokenTime { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime ResetPasswordTokenTime { get; set; }
        public string CategoryName { get; set;  }

        // public ICollection<Booking> Bookings { get; set; } 
    }
}
