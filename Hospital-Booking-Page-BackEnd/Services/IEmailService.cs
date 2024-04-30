using Hospital_Booking_Page_BackEnd.Models;

namespace Hospital_Booking_Page_BackEnd.Services
{
    public interface IEmailService
    {
        void SendEmail(EmailModel emailModel);
       
    }
}
