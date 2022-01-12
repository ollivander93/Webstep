using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskApi.Configuration;
using TaskApi.Data;
using TaskApi.Domain;
using TaskApi.Models;
using TaskApi.Models.DTO.Requests;

namespace TaskApi.Services
{
    public interface IUserService
    {
        public Task<AuthResult> GenerateJwtToken(IdentityUser user);
        Task<IdentityUser> GetUserFromToken(string jwtToken);
        Task<AuthResult> VerifyToken(TokenRequest tokenRequest);
    }
    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly ApplicationDbContext _context;
        private readonly JwtConfig _jwtConfig;

        public UserService(
            UserManager<IdentityUser> userManager, 
            IOptionsMonitor<JwtConfig> optionsMonitor,
            TokenValidationParameters tokenValidationParameters,
            ApplicationDbContext context)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tokenValidationParameters = tokenValidationParameters ?? throw new ArgumentNullException(nameof(tokenValidationParameters));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtConfig = optionsMonitor.CurrentValue;
        }

        public async Task<AuthResult> GenerateJwtToken(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new []
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddSeconds(_jwtConfig.ExpiryTimeFrame),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            var jwtToken = tokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                IsUsed = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsRevoked = false,
                Token = RandomString(25) + Guid.NewGuid()
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Token = jwtToken,
                Result = true,
                RefreshToken = refreshToken.Token
            };
        }

        private static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<IdentityUser> GetUserFromToken(string jwtToken)
        {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t =>
                t.Token.StartsWith(jwtToken));
            return refreshToken == null
                ? null 
                : await _context.Users.FirstOrDefaultAsync(u => u.Id.Equals(refreshToken.UserId));
        }

        public async Task<AuthResult> VerifyToken(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var principal = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, 
                    out var validatedToken);

                // Now we need to check if the token has a valid security algorithm
                if(validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);

                    if(result == false) {
                         return null;
                    }
                }

                        // Will get the time stamp in unix time
                var utcExpiryDate = long.Parse(principal.Claims
                    .FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                // we convert the expiry date from seconds to the date
                var expDate = UnixTimeStampToDateTime(utcExpiryDate);

                if(expDate > DateTime.UtcNow)
                {
                    return new AuthResult(){
                        Errors = new List<string>() {"token has not expired"},
                        Result = false
                    };
                }

                // Check the token we got if its saved in the db
                var storedRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => 
                    x.Token == tokenRequest.RefreshToken); 

                if(storedRefreshToken == null)
                {
                    return new AuthResult(){
                        Errors = new List<string>() {"refresh token doesnt exist"},
                        Result = false
                    };
                }
                
                if(DateTime.UtcNow > storedRefreshToken.ExpiryDate)
                {
                    return new AuthResult(){
                        Errors = new List<string>() {"token has expired, user needs to relogin"},
                        Result = false
                    };
                }
                
                if(storedRefreshToken.IsUsed)
                {
                    return new AuthResult(){
                        Errors = new List<string>() {"token has been used"},
                        Result = false
                    };
                }

                // Check if the token is revoked
                if(storedRefreshToken.IsRevoked)
                {
                    return new AuthResult(){
                        Errors = new List<string>() {"token has been revoked"},
                        Result = false
                    };
                }

                 // we are getting here the jwt token id
                var jti = principal.Claims.SingleOrDefault(x => 
                    x.Type == JwtRegisteredClaimNames.Jti).Value;

                // check the id that the recieved token has against the id saved in the db
                if(storedRefreshToken.JwtId != jti)
                {
                   return new AuthResult(){
                        Errors = new List<string>() {"the token doenst mateched the saved token"},
                        Result = false
                    };
                }

                storedRefreshToken.IsUsed = true;
                _context.RefreshTokens.Update(storedRefreshToken);
                await _context.SaveChangesAsync();

                        var dbUser = await _userManager.FindByIdAsync(storedRefreshToken.UserId);
                return await GenerateJwtToken(dbUser);
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        
        
        private DateTime UnixTimeStampToDateTime( double unixTimeStamp )
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ). ToUniversalTime();
            return dtDateTime;
        }
    }
}