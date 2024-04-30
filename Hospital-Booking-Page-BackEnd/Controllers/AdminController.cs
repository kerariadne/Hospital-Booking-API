using Hospital_Booking_Page_BackEnd.Data;
using Hospital_Booking_Page_BackEnd.helpers;
using Hospital_Booking_Page_BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text;

namespace Hospital_Booking_Page_BackEnd.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("reports")]
        public IActionResult GetReports()
        {
            var userCount = _context.Users.Count();
            var doctorCount = _context.Doctors.Count();
            return Ok(new { UserCount = userCount, DoctorCount = doctorCount });
        }

        
        [HttpPut("updaterole/{userId}")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] string newRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            if (newRole != "Admin" && newRole != "Doctor" && newRole != "User")
                return BadRequest("Invalid role specified.");

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return Ok("Role updated successfully.");
        }

        [HttpPost("addadmin")]
        public async Task<IActionResult> AddAdmin([FromBody] User admin)
        {
            if (_context.Users.Any(u => u.Email == admin.Email))
                return BadRequest("Admin with this email already exists.");

            var pass = CheckPasswordStrength(admin.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });

            admin.IsEmailVerified = false;
            admin.Token = "";
            admin.IsTwoFactorEnabled = false;

            admin.Password = PasswordHasher.HashPassword(admin.Password);
            admin.Role = "Admin"; 

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            return Ok("Admin added successfully.");
        }

        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();
            if (password.Length < 8)
                sb.Append("Minimum password length should be 8" + Environment.NewLine);
            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]")
            && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should be Alphanumeric" + Environment.NewLine); 
            if (!Regex.IsMatch(password, "[<, >,.,0,!,#‚$‚¾‚ˆ‚&,*, (, ), _, +, \\[‚\\], {, },?,:,;, N\\, , -·, , /, ~`, ` ]"))
                sb.Append("Password should contain special chars" + Environment.NewLine);
            return sb.ToString();
        }
    }
}
