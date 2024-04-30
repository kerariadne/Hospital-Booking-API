using Hospital_Booking_Page_BackEnd.Data;
using Hospital_Booking_Page_BackEnd.helpers;
using Hospital_Booking_Page_BackEnd.Models; 
using Hospital_Booking_Page_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Hospital_Booking_Page_BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _hostingEnvironment;


        public DoctorController(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;

            _emailService = emailService;
        }

        public class model
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public required string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Authenticate([FromBody] model login)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (login == null || string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest(new { message = "Invalid login request." });
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(u => u.Email == login.Email);

            if (doctor == null || !PasswordHasher.VerifyPassword(login.Password, doctor.Password))
            {
                return NotFound(new { message = "doctor not found or password does not match." });
            }


            doctor.Token = CreateJwt(doctor);

            var newAccessToken = doctor.Token;
            var newRefreshToken = CreateRefreshToken();
            doctor.RefreshToken = newRefreshToken;
            doctor.RefreshTokenTime = DateTime.Now.AddDays(10);
            await _context.SaveChangesAsync();

            return Ok(new RefreshTokenAPI()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });


            /*
        {
            token = doctor.Token,
            message = "doctor authenticated successfully."
        } */
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Doctor>> Createdoctor([FromForm] Doctor doctor, IFormFile cvFile, IFormFile photoFile)
        {
            if (doctor == null)
                return BadRequest();
            var pass = CheckPasswordStrength(doctor.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });
            doctor.Password = PasswordHasher.HashPassword(doctor.Password);
            doctor.Role = "Doctor";
            doctor.Token = "";
           
            if (string.IsNullOrEmpty(doctor.CategoryName))
                return BadRequest("Category name is required.");

            Category category = new Category { Name = doctor.CategoryName };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            doctor.CategoryName = category.Name;
            if (cvFile != null)
            {
                var cvFileName = ContentDispositionHeaderValue.Parse(cvFile.ContentDisposition).FileName.Trim('"');
                var cvPath = Path.Combine(_hostingEnvironment.WebRootPath, "cvs", cvFileName);
                using (var stream = new FileStream(cvPath, FileMode.Create))
                {
                    await cvFile.CopyToAsync(stream);
                }
                doctor.CV = "cvs/" + cvFileName; 
            }

            if (photoFile != null)
            {
                var photoFileName = ContentDispositionHeaderValue.Parse(photoFile.ContentDisposition).FileName.Trim('"');
                var photoPath = Path.Combine(_hostingEnvironment.WebRootPath, "photos", photoFileName);
                var thumbnailPath = Path.Combine(_hostingEnvironment.WebRootPath, "thumbnails", photoFileName);

                using (var stream = new FileStream(photoPath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }

                using (var image = Image.Load(photoFile.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(100, 100)); 
                    await image.SaveAsJpegAsync(thumbnailPath); 
                }

                doctor.Photo = "photos/" + photoFileName;
                doctor.Thumbnail = "thumbnails/" + photoFileName;
            }


            await _context.Doctors.AddAsync(doctor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Getdoctor), new { id = doctor.Id }, doctor);
        }

        private string CreateJwt(Doctor doctor)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            //  var key = Encoding.ASCII.GetBytes("secretforbookingproject");

            var key = new byte[32]; 
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }

            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, doctor.Role),
                new Claim(ClaimTypes.Email, doctor.Email),
                new Claim(ClaimTypes.Name, doctor.Name)
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
            var tokenIndoctor = _context.Doctors
                .Any(a => a.RefreshToken == refreshToken);
            if (tokenIndoctor)
            {
                return CreateRefreshToken();
            }
            return refreshToken;
        }


        private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes("secretforbookingproject");
            // var key = new byte[32]; // 256 bits
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
            var doctor = await _context.Doctors.FirstOrDefaultAsync(u => u.Name == name);
            if (doctor is null || doctor.RefreshToken != refreshToken || doctor.RefreshTokenTime <= DateTime.Now)
                return BadRequest("Invalid Request");
            var newAccessToken = CreateJwt(doctor);
            var newRefreshToken = CreateRefreshToken();
            doctor.RefreshToken = newRefreshToken;
            await _context.SaveChangesAsync();
            return Ok(new RefreshTokenAPI()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
            });
        }


        private Task<bool> CheckEmailExistAsync(string email)
        => _context.Doctors.AnyAsync(x => x.Email == email);


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
        public async Task<ActionResult<IEnumerable<Doctor>>> GetAlldoctors()
        {

            var doctors = await _context.Doctors.ToListAsync();
            if (doctors == null)
            {
                return NotFound(new { message = "doctors not found." });
            }
            return Ok(doctors);

        }

        //[Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Doctor>> Getdoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound(new { message = "doctors not found." });
            }
            return Ok(doctor);
        }

        //[Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Updatedoctor(int id, [FromBody] Doctor doctor)
        {
            if (id != doctor.Id)
            {
                return BadRequest(new { message = "doctor ID mismatch." });
            }

            _context.Entry(doctor).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Doctors.Any(e => e.Id == id))
                {
                    return NotFound(new { message = "doctors not found." });
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
        public async Task<IActionResult> Deletedoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound(new { message = "doctors not found." });
            }

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
            return NoContent();
        }



        [HttpPost("send-reset-email/{email}")]
        public async Task<IActionResult> SendEmail(string email)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(a => a.Email == email);
            if (doctor is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "email Doesn't Exist"
                });
            }
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var emailToken = Convert.ToBase64String(tokenBytes);
            doctor.ResetPasswordToken = emailToken;
            doctor.ResetPasswordTokenTime = DateTime.Now.AddMinutes(5);
            string from = _configuration["EmailSettings: From"];
            var emailModel = new EmailModel(email, "Reset Password!", EmailBody.EmailStringBody(email, emailToken));
            _emailService.SendEmail(emailModel);
            _context.Entry(doctor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "Email Sent!"
            });
        }

        


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordAPI resetPassword)
        {
            var newToken = resetPassword.EmailToken.Replace(" ", "+");
            var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(a => a.Email == resetPassword.Email);
            if (doctor is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "doctor Doesn't Exist"
                });
            }
            var tokenCode = doctor.ResetPasswordToken;
            DateTime emailTokenExpiry = doctor.ResetPasswordTokenTime;
            if (tokenCode != resetPassword.EmailToken || emailTokenExpiry < DateTime.Now)
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "Invalid Reset link"
                });
            }
            doctor.Password = PasswordHasher.HashPassword(resetPassword.NewPassword);
            _context.Entry(doctor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "Password Reset Successfully"

            });
        }


        [HttpGet("search/category/{categoryName}")]
        public async Task<IActionResult> GetDoctorsByCategoryName(string categoryName)
        {
            var doctors = await _context.Doctors
                                        .Where(d => d.CategoryName == categoryName)
                                        .ToListAsync();

            if (!doctors.Any())
                return NotFound(new { message = "No doctors found in this category." });

            return Ok(doctors);
        }

        [HttpGet("search/name/{firstName}")]
        public async Task<IActionResult> GetDoctorsByName(string firstName)
        {
            var doctors = await _context.Doctors
                                        .Where(d => d.Name == firstName)
                                        .ToListAsync();

            if (!doctors.Any())
            {
                return NotFound(new { message = "No doctors found with this firstName" });
            }

            return Ok(doctors);
        }


        [NonAction]
        private async Task<string> WriteFile(IFormFile file)
        {
            string filename = "";
            try
            {
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                filename = DateTime.Now.Ticks.ToString() + extension;

                var filepath = Path.Combine(Directory.GetCurrentDirectory(), "Files");

                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }

                var exactpath = Path.Combine(Directory.GetCurrentDirectory(), "Files", filename);
                using (var stream = new FileStream(exactpath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return filename;
        }

        private async Task<string> GenerateThumbnail(IFormFile photoFile)
        {
            if (photoFile == null || photoFile.Length == 0)
            {
                return null;
            }
            var thumbnailFileName = Guid.NewGuid().ToString() + ".jpg";
            var thumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "thumbnails", thumbnailFileName);
            var directoryPath = Path.GetDirectoryName(thumbnailPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var image = Image.Load(photoFile.OpenReadStream()))
            {
                var thumbnailSize = new Size(100, 100);
                image.Mutate(x => x.Resize(thumbnailSize));
                await image.SaveAsJpegAsync(thumbnailPath);
            }

            return thumbnailPath;
        }


        [HttpGet]
        [Route("DownloadFile")]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            var filepath = Path.Combine(Directory.GetCurrentDirectory(), "Files", filename);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filepath, out var contenttype))
            {
                contenttype = "application/octet-stream";
            }
            var bytes = await System.IO.File.ReadAllBytesAsync(filepath);
            return File(bytes, contenttype, Path.GetFileName(filepath));
        }

    }

}
