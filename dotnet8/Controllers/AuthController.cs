﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoApi.Controllers.Models;


namespace TodoApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AuthController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        public AuthController(UserManager<IdentityUser> userManager , ILogger<AuthController> logger, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _logger = logger;
            _signInManager = signInManager;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);
            if (!result.Succeeded)
            {

                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("Error", "You must confirm your e-mail before login");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("Error", "You are lock out");
                }

                return BadRequest(ModelState);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("345c5767-ab18-42f9-a986-862f2dca45c8");


            var claims = new List<Claim>();

            //claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.UserName));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));
            claims.Add(new Claim("Todo", "GetAll"));

            var identityClaims = new ClaimsIdentity();
            identityClaims.AddClaims(claims);

            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "todo-api",
                Audience = "https://localhost:7047/",
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(10),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),

            });

            return Ok(new { token = tokenHandler.WriteToken(token) });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = new IdentityUser {
                Email = model.Email,
                UserName = model.UserName,
                EmailConfirmed = true,
            };



            var result = await _userManager.CreateAsync(user, model.Password);


            if(!result.Succeeded)
            {
                return BadRequest(result);
            }
  
            return Ok(user);
        }
        private static long ToUnixEpochDate(DateTime date)
                 => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }


}
