using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Hospital_Booking_Page_BackEnd.Data;
using Hospital_Booking_Page_BackEnd.Models; 
using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Hospital_Booking_Page_BackEnd.helpers;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Hospital_Booking_Page_BackEnd.Services;


[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public UserController(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] LoginModel login)
    {

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (login == null || string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
        {
            return BadRequest(new { message = "Invalid login request." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);

        if (user == null || !PasswordHasher.VerifyPassword(login.Password, user.Password))
        {
            return NotFound(new { message = "User not found or password does not match." });
        }
        user.Token = CreateJwt(user);
        var newAccessToken = user.Token;
        var newRefreshToken = CreateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenTime = DateTime.Now.AddDays(10);
        await _context.SaveChangesAsync();

        return Ok(new RefreshTokenAPI()
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });

        /*
    {
        token = user.Token,
        message = "User authenticated successfully."
    } */
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {


        if (user == null)
            return BadRequest();

        if (await CheckEmailExistAsync(user.Email))
            return BadRequest(new { Message = "Email Already Exist!" });


        var pass = CheckPasswordStrength(user.Password);
        if (!string.IsNullOrEmpty(pass))
            return BadRequest(new { Message = pass.ToString() });

        user.IsEmailVerified = false;
        user.Password = PasswordHasher.HashPassword(user.Password);
        user.Role = "User";
        user.Token = "";
        user.IsTwoFactorEnabled = false;


        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    private string CreateJwt(User user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = new byte[32];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
        }

        var identity = new ClaimsIdentity(new Claim[]
        {
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Name)
        });

        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.Now.AddDays(1), 
            SigningCredentials = credentials
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        return jwtTokenHandler.WriteToken(token);
    }

    private string CreateRefreshToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(tokenBytes);
        var tokenInUser = _context.Users
            .Any(a => a.RefreshToken == refreshToken);
        if (tokenInUser)
        {
            return CreateRefreshToken();
        }
        return refreshToken;
    }


    private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
    {
        var key = Encoding.ASCII.GetBytes("secretforbookingproject");
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid Token");
        return principal;
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenAPI tokenAPI)
    {
        if (tokenAPI is null) return BadRequest("Invalid Client Request");
        string accessToken = tokenAPI.AccessToken;
        string refreshToken = tokenAPI.RefreshToken;
        var principal = GetPrincipleFromExpiredToken(accessToken);
        var name = principal.Identity.Name;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Name == name);
        if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenTime <= DateTime.Now)
            return BadRequest("Invalid Request");
        var newAccessToken = CreateJwt(user);
        var newRefreshToken = CreateRefreshToken();
        user.RefreshToken = newRefreshToken;
        await _context.SaveChangesAsync();
        return Ok(new RefreshTokenAPI()
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
        });
    }


    private Task<bool> CheckEmailExistAsync(string email)
    => _context.Users.AnyAsync(x => x.Email == email);


    private string CheckPasswordStrength(string password)
    {
        StringBuilder sb = new StringBuilder();
        if (password.Length < 8)
            sb.Append("Minimum password length should be 8" + Environment.NewLine);
        if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]")
        && Regex.IsMatch(password, "[0-9]")))
            sb.Append("Password should be Alphanumeric" + Environment.NewLine);  
        if (!Regex.IsMatch(password, "[<, >,0,!,#‚$‚¾‚ˆ‚&,*, (, ), _, +, \\[‚\\], {, },?,:,;, N\\, , -·, , /, ~`, ` ]"))
            sb.Append("Password should contain special chars" + Environment.NewLine);
        return sb.ToString();
    }

    //[Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {

        var users = await _context.Users.ToListAsync();
        if (users == null)
        {
            return NotFound(new { message = "Users not found." });
        }
        return Ok(users);

    }

    //[Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Users not found." });
        }
        return Ok(user);
    }

    //[Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
    {
        if (id != user.Id)
        {
            return BadRequest(new { message = "User ID mismatch." });
        }

        _context.Entry(user).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Users.Any(e => e.Id == id))
            {
                return NotFound(new { message = "Users not found." });
            }
            else
            {
                throw;
            }
        }
        return NoContent();
    }

    //[Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Users not found." });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }



    [HttpPost("send-reset-email/{email}")]
    public async Task<IActionResult> SendEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(a => a.Email == email);
        if (user is null)
        {
            return NotFound(new
            {
                StatusCode = 404,
                Message = "email Doesn't Exist"
            });
        }
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var emailToken = Convert.ToBase64String(tokenBytes);
        user.ResetPasswordToken = emailToken;
        user.ResetPasswordTokenTime = DateTime.Now.AddMinutes(5);
        string from = _configuration["EmailSettings: From"];
        var emailModel = new EmailModel(email, "Reset Password!", EmailBody.EmailStringBody(email, emailToken));
        _emailService.SendEmail(emailModel);
        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return Ok(new
        {
            StatusCode = 200,
            Message = "Email Sent!"
        });
    }

    [HttpPost("send-verification-email/{email}")]
    public async Task<IActionResult> SendEmailVerification(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound(new { StatusCode = 404, Message = "Email doesn't exist" });
        }

        if (user.IsEmailVerified) 
        {
            return BadRequest(new { StatusCode = 400, Message = "Email already verified." });
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var emailToken = Convert.ToBase64String(tokenBytes);
        user.EmailVerificationToken = emailToken;
        user.EmailVerificationTokenTime = DateTime.UtcNow.AddMinutes(30); 

        var emailModel = new EmailModel(email, "Confirm Email!", EmailConfirm.ConfirmEmail(email, emailToken));
        _emailService.SendEmail(emailModel);

        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { StatusCode = 200, Message = "Verification email sent successfully!" });
    }


    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string email, string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound(new { StatusCode = 404, Message = "User not found." });
        }
        if (user.IsEmailVerified)
        {
            return BadRequest(new { StatusCode = 400, Message = "Email is already verified." });
        }
        if (user.EmailVerificationToken != token || user.EmailVerificationTokenTime < DateTime.UtcNow)
        {
            return BadRequest(new { StatusCode = 400, Message = "Invalid or expired verification token." });
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { StatusCode = 200, Message = "Email verified successfully!" });
    }



    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordAPI resetPassword)
    {
        var newToken = resetPassword.EmailToken.Replace(" ", "+");
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(a => a.Email == resetPassword.Email);
        if (user is null)
        {
            return NotFound(new
            {
                StatusCode = 404,
                Message = "User Doesn't Exist"
            });
        }
        var tokenCode = user.ResetPasswordToken;
        DateTime emailTokenExpiry = user.ResetPasswordTokenTime;
        if (tokenCode != resetPassword.EmailToken || emailTokenExpiry < DateTime.Now)
        {
            return BadRequest(new
            {
                StatusCode = 400,
                Message = "Invalid Reset link"
            });
        }
        user.Password = PasswordHasher.HashPassword(resetPassword.NewPassword);
        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return Ok(new
        {
            StatusCode = 200,
            Message = "Password Reset Successfully"

        });
    }


    [HttpPost("send-2fa-code/{email}")]
    public async Task<IActionResult> Send2FACode(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound(new { Message = "Email doesn't exist" });
        }

        if (user.Last2FACodeTime.HasValue && (DateTime.UtcNow - user.Last2FACodeTime.Value).TotalMinutes < 1)
        {
            return BadRequest(new { Message = "A new code cannot be generated yet. Please wait." });
        }
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var emailToken = Convert.ToBase64String(tokenBytes);
        var code = GenerateCode(4);
        user.TwoFactorCode = code;
        user.Last2FACodeTime = DateTime.UtcNow;
        user.TwoFactorCodeExpiryTime = DateTime.UtcNow.AddMinutes(5);
        await _context.SaveChangesAsync();

        var emailModel = new EmailModel(email, "Enable two-factor authentication",Two_Factor.TwoFactorAuthEmailBody(email, code));
        _emailService.SendEmail(emailModel);

        return Ok(new { Message = "Authentication code sent successfully." });
    }

    private string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    [HttpPost("verify-2fa-code")]
    public async Task<IActionResult> Verify2FACode(string email, string code)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }  
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (user.TwoFactorCode != code || DateTime.UtcNow > user.TwoFactorCodeExpiryTime)
        {
            return BadRequest(new { Message = user.TwoFactorCode });
        }   
        user.IsTwoFactorEnabled = true;
        user.TwoFactorCode = null;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Code verified successfully!" });
    }


}
