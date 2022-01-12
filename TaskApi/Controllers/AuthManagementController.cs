using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Data;
using TaskApi.Domain;
using TaskApi.Models.DTO.Requests;
using TaskApi.Models.DTO.Responses;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserService _userService;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ApplicationDbContext _context;

        public AuthManagementController(
            UserManager<IdentityUser> userManager, IUserService userService, 
            TokenValidationParameters tokenValidationParameters, ApplicationDbContext context)
        {
            _userManager = userManager;
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _tokenValidationParameters = tokenValidationParameters ?? throw new ArgumentNullException(nameof(tokenValidationParameters));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpPost]
        [Route("Register")]
        public async Task<ActionResult<AuthResult>> Register([FromBody] UserRegistrationRequestDto user)
        {
            if (!ModelState.IsValid)
                return BadRequest(new RegistrationResponse()
                {
                    Result = false,
                    Errors = new List<string>() { "Invalid payload" }
                });

            var existingUser = await _userManager.FindByEmailAsync(user.Email);

            if(existingUser != null) 
            {
                return BadRequest(new AuthResult {
                    Result = false,
                    Errors = new List<string>(){
                        "Email already exist"
                    }});
            }

            var newUser = new IdentityUser(){Email = user.Email, UserName = user.Email};
            var isCreated = await _userManager.CreateAsync(newUser, user.Password);
            if(isCreated.Succeeded)
            {
                return Ok(await _userService.GenerateJwtToken(newUser));
            }

            return new JsonResult(new AuthResult{
                Result = false,
                Errors = isCreated.Errors.Select(x => x.Description).ToList()}
            ) {StatusCode = 500};

        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult<AuthResult>> Login([FromBody] UserLoginRequest user)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AuthResult {
                    Result = false,
                    Errors = new List<string>(){
                        "Invalid authentication request"
                    }});

            var existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
                return BadRequest(new AuthResult {
                    Result = false,
                    Errors = new List<string>(){
                        "User Email does not exist"
                    }});

            if (await _userManager.CheckPasswordAsync(existingUser, user.Password))
            {
                return Ok(await _userService.GenerateJwtToken(existingUser));
            }

            return BadRequest(new RegistrationResponse() {
                Result = false,
                Errors = new List<string>(){
                    "Invalid payload"
                }});
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(new RegistrationResponse
                {
                    Errors = new List<string>
                    {
                        "Invalid payload"
                    },
                    Result = false
                });

            var res = await _userService.VerifyToken(tokenRequest);
            if (res == null)
                return BadRequest(new RegistrationResponse(){
                    Errors = new List<string>() {
                        "Invalid tokens"
                    },
                    Result = false
                });
            return Ok(res);
        }
    }
}