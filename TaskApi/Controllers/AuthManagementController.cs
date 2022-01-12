using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ITokenService _tokenService;

        public AuthManagementController(
            UserManager<IdentityUser> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
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

            var newUser = new IdentityUser{ Email = user.Email, UserName = user.Email };
            var isCreated = await _userManager.CreateAsync(newUser, user.Password);
            if(isCreated.Succeeded)
            {
                return Ok(await _tokenService.GenerateJwtToken(newUser));
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
                return Ok(await _tokenService.GenerateJwtToken(existingUser));
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

            var res = await _tokenService.RefreshTokenAsync(tokenRequest);
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