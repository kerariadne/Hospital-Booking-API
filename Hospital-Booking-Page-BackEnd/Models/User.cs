namespace Hospital_Booking_Page_BackEnd.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Password { get; set; } 
        public string? Token { get; set; }
        public required string PersonalNumber { get; set; }
        public required string Email { get; set; }
        public string? Role { get; set; } 
        public int NumberOfBookings { get; set; }
        public string? RefreshToken { get; set; }

        public DateTime RefreshTokenTime { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime ResetPasswordTokenTime { get; set; }

        public bool IsEmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime EmailVerificationTokenTime { get; set; }

        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorCodeExpiryTime { get; set; }
        public DateTime? Last2FACodeTime { get; set; }
        public bool IsTwoFactorEnabled { get; set; } 

        // public ICollection<Booking> Bookings { get; set; } // Navigation property
    }

}
