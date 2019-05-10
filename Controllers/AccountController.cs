using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NG_Core_Auth.Helpers;
using NG_Core_Auth.Models;

namespace NG_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppSettings _appSettings;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IOptions<AppSettings> appsettingsOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appSettings = appsettingsOptions.Value;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Register([FromBody]RegisterViewModel registerViewModel)
        {
            try
            {
                //Will hold all the registration related errors
                var errors = new List<string>();

                var user = new IdentityUser()
                {
                    Email = registerViewModel.Email,
                    UserName = registerViewModel.Username,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await _userManager.CreateAsync(user, registerViewModel.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    return Ok(new { username = user.UserName, email = user.Email, status = 1, message = "Registration Successful." });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        errors.Add(error.Description);
                    }

                    return BadRequest(new JsonResult(errors));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel loginViewModel)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(loginViewModel.Username);

                if (user != null && await _userManager.CheckPasswordAsync(user, loginViewModel.Password))
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));
                    var expireTime = Convert.ToDouble(_appSettings.ExpireTime);
                    //Generate Token
                    var tokenHandler = new JwtSecurityTokenHandler();

                    var tokenDescriptor = new SecurityTokenDescriptor()
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, loginViewModel.Username),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.NameIdentifier, user.Id),
                            new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                            new Claim("LoggedOn", DateTime.Now.ToString())
                        }),
                        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                        Issuer = _appSettings.Site,
                        Audience = _appSettings.Audience,
                        Expires = DateTime.UtcNow.AddMinutes(expireTime)
                    };

                    //Finally generate token
                    var token = tokenHandler.CreateToken(tokenDescriptor);

                    return Ok(new { token = tokenHandler.WriteToken(token), expireTime = token.ValidTo, userName = user.UserName, userRole = roles.FirstOrDefault() });
                }

                //return error
                ModelState.AddModelError("", "Username/Password not found.");
                return Unauthorized(new { LoginError = "Invalid Username or Password." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

    }
}
