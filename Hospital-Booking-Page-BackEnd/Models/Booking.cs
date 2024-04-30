namespace Hospital_Booking_Page_BackEnd.Models
{
    public class Booking
    {
        public required int Id { get; set; }
        public required DateTime AppointmentDate { get; set; }
        public required int UserId { get; set; } 
        public required int DoctorId { get; set; }
        public string Notes { get; set; }
    }
}
