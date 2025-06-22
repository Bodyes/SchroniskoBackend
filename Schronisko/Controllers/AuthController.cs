using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Schronisko.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
       UserManager<ApplicationUser> userManager,
       SignInManager<ApplicationUser> signInManager,
       RoleManager<IdentityRole> roleManager,
       IConfiguration configuration) // Dodaj IConfiguration
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration; // Przypisz do pola
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { Error = "Invalid registration data." });
            }

            // Sprawdź, czy istnieje użytkownik o takim username
            var existingUserByName = await _userManager.FindByNameAsync(model.Username);
            if (existingUserByName != null)
            {
                return BadRequest(new { Error = "Username is already taken." });
            }

            // Sprawdź, czy istnieje użytkownik o takim emailu
            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest(new { Error = "Email is already taken." });
            }



            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign default role
                await _userManager.AddToRoleAsync(user, "User");
                return Ok(new { Message = "User created successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {         

            var result = await _signInManager.PasswordSignInAsync(
                model.Username, model.Password, false, false);

            if (result.Succeeded)
            {

                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                if (!user.IsActive)
                {
                    return StatusCode(423, new { Error = "User is inactive" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.Id)
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:secretKey"]);
                var tokenLifetime = int.Parse(_configuration["JwtSettings:TokenLifetime"]);
                
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(tokenLifetime),
                    Audience = _configuration["JwtSettings:validAudience"],
                    Issuer = _configuration["JwtSettings:validIssuer"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new
                {
                    Username = user.UserName,
                    Email = user.Email,
                    Roles = roles,
                    ID = user.Id,
                    Token = tokenString
                });
            }

            return Unauthorized();
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logged out successfully" });
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] RoleModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                return BadRequest("Role does not exist");
            }
            //Remove all roles before assigning a new one
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(removeResult.Errors);
                }
            }


            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (result.Succeeded)
            {
                return Ok(new { Message = $"Role {model.Role} assigned to {model.Username}" });
            }

            return BadRequest(result.Errors);
        }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RoleModel
    {
        public string Username { get; set; }
        public string Role { get; set; }
    }
}
