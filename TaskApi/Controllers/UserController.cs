using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;
using TaskApi.Models.DTO.Responses;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserService _userService;

        public UserController(ApplicationDbContext dbContext, IUserService userService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IdentityUser>> GetUser(string id)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
                u.Id.Equals(id));
            if (user == null)
                return NotFound();
            return user;
        }

        [HttpGet]
        [Route("GetUser")]
        public async Task<ActionResult<SignedInUserResponse>> GetSignedInUser()
        {
            var userId = User.Claims.FirstOrDefault(i => i.Type == "Id").Value;
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id.Equals(userId));

            if (user == null)
                return NotFound();

            return Ok(new SignedInUserResponse()
            {
                Email = user.Email,
                Name = user.UserName,
                UserId = user.Id
            });
        }
    }
}