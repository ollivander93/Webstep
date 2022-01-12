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
using TaskApi.Extensions;
using TaskApi.Models;
using TaskApi.Models.DTO.Requests;

namespace TaskApi.Services
{
    public interface ITokenService
    {
        Task<AuthResult> GenerateJwtToken(IdentityUser user);
        Task<AuthResult> RefreshTokenAsync(TokenRequest tokenRequest);
    }
    
    public class TokenService : ITokenService
    {
        private readonly JwtConfig _jwtConfig;
        private readonly ApplicationDbContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly UserManager<IdentityUser> _userManager;

        public TokenService(IOptionsMonitor<JwtConfig> optionsMonitor, ApplicationDbContext context,
            TokenValidationParameters tokenValidationParameters, UserManager<IdentityUser> userManager)
        {
            _jwtConfig = optionsMonitor.CurrentValue;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tokenValidationParameters = tokenValidationParameters ?? throw new ArgumentNullException(nameof(tokenValidationParameters));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<AuthResult> RefreshTokenAsync(TokenRequest tokenRequest)
        {
            try
            {
                var storedRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => 
                    x.Token == tokenRequest.RefreshToken);
                
                VerifyToken(tokenRequest, storedRefreshToken);
                
                storedRefreshToken.IsUsed = true;
                _context.RefreshTokens.Update(storedRefreshToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedRefreshToken.UserId);
                return await GenerateJwtToken(dbUser);
            }
            catch (AggregateException e)
            {
                return new AuthResult
                {
                    Result = false,
                    Errors = e.InnerExceptions.Select(eInnerException => eInnerException.Message).ToList()
                };
            }
        }
        
        private void VerifyToken(TokenRequest tokenRequest, RefreshToken storedRefreshToken)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            List<Exception> errors = new List<Exception>();
            try
            {
                var principal = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, 
                    out var validatedToken);

                if(validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);

                    if(result == false) {
                         errors.Add(new Exception("Token is not valid"));
                    }
                }
                
                if (!TokenExpired(principal))
                    errors.Add(new Exception("Token has not expired"));

                if(storedRefreshToken == null)
                    errors.Add(new Exception("refresh token does not exist"));
                if(DateTime.UtcNow > storedRefreshToken.ExpiryDate)
                    errors.Add(new Exception("refresh token has expired, user needs to log in"));
                if(storedRefreshToken.IsUsed)
                    errors.Add(new Exception("token has been used"));

                if(storedRefreshToken.IsRevoked)
                    errors.Add(new Exception("token has been revoked"));
                
                if (!ValidateTokenId(principal, storedRefreshToken))
                    errors.Add(new Exception("token does not match saved token"));
            }
            catch(Exception e)
            {
                errors.Add(e);
            }

            if (errors.Any())
                throw new AggregateException(errors);
        }

        private bool ValidateTokenId(ClaimsPrincipal principal, RefreshToken token)
        {
            var jti = principal.Claims.SingleOrDefault(x => 
                x.Type == JwtRegisteredClaimNames.Jti).Value;

            // check the id that the recieved token has against the id saved in the db
            return token.JwtId == jti;
        }

        public async Task<AuthResult> GenerateJwtToken(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = GetTokenDescriptor(user);

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
                Token = StringExtensions.RandomString(25) + Guid.NewGuid()
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

        private SecurityTokenDescriptor GetTokenDescriptor(IdentityUser user)
        {
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            return new SecurityTokenDescriptor
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
        }
        
        private static bool TokenExpired(ClaimsPrincipal principal)
        {
            var utcExpiryDate = long.Parse(principal.Claims
                .FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            var expiryDate = DateExtensions.UnixTimeStampToDateTime(utcExpiryDate);

            return expiryDate >= DateTime.UtcNow;
        }
    }
}