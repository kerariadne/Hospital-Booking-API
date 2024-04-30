namespace Hospital_Booking_Page_BackEnd.helpers
{
    public class EmailConfirm
    {
        public static string ConfirmEmail(string email, string emailToken)
        {
            return $@"<html>
                <head>
                    <style>
                        body {{ font-family: Arial, Helvetica, sans-serif; }}
                        .button {{ background-color: #4CAF50; border: none; color: white; padding: 15px 32px; text-align: center; text-decoration: none; display: inline-block; font-size: 16px; margin: 4px 2px; cursor: pointer; border-radius: 8px; }}
                    </style>
                </head>
                <body>
                    <h2>Verify Your Email Address</h2>
                    <p>Thanks for registering with us. Please follow this link to verify your email address:</p>
                    <a href='http://localhost:4200/confirm?email={email}&code={emailToken}' target='_blank'  class='button'>Verify Email</a>
                    
                </body>
            </html>";


        }
    }
}
