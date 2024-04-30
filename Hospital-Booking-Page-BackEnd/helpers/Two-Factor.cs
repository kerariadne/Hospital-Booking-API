namespace Hospital_Booking_Page_BackEnd.helpers
{
    public class Two_Factor
    {
        public static string TwoFactorAuthEmailBody(string email, string verificationCode)
        {
            return $@"<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; }}
        .email-container {{ background: linear-gradient(to right, #ffffff 50%, #e0f7fa 100%); padding: 40px; width: 100%; max-width: 600px; margin: 20px auto; box-shadow: 0 4px 8px rgba(0,0,0,0.1); border-radius: 8px; }}
        .button {{ background: #26a69a; padding: 12px 24px; border: none; color: white; border-radius: 5px; text-align: center; text-decoration: none; display: inline-block; font-weight: bold; }}
        h1 {{ color: #26a69a; }}
        hr {{ border: 0; height: 1px; background-image: linear-gradient(to right, #f0f0f0, #8c8c8c, #f0f0f0); }}
    </style>
</head>
<body>
    <div class='email-container'>
        <h1>Verify Your Account</h1>
        <hr>
        <p>You're receiving this e-mail because you need to complete the two-factor authentication required to access your Hospital Booking Page account.</p>
        <p>Please enter the following verification code to continue:</p>
        <div style='text-align: center; margin: 20px;'>
            <span style='font-size: 20px; letter-spacing: 3px; padding: 10px 20px; background-color: #f0f0f0; border-radius: 4px; font-weight: bold;'>{verificationCode}</span>
        </div>
        <p>This code will expire in 5 minutes. If you did not request this, please ignore this email or secure your account.</p>
        <br>
        <p>With best wishes,<br><br>Hospital Booking Page Team</p>
    </div>
</body>
</html>";
        }

    }
}
