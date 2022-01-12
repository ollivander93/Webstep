using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TaskApi.Domain;
using TaskApi.Models.DTO.Requests;
using TaskListApplication.Services;

namespace TaskListApplication.Http
{
    public interface IHttpClientProvider
    {
        Task<HttpClient> GetAuthenticatedClient();
    }
        
    public class HttpClientProvider : IHttpClientProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;
        private readonly IAuthenticationService _authenticationService;

        public HttpClientProvider(IHttpContextAccessor httpContextAccessor,
            ITokenService tokenService, IAuthenticationService authenticationService)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:5021");
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }
        
        public async Task<HttpClient> GetAuthenticatedClient()
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var currentToken = _httpContextAccessor.HttpContext.Session.GetString("token");
            var currentRefreshToken = _httpContextAccessor.HttpContext.Session.GetString("refreshToken");
            AuthResult authResult;
            if (!string.IsNullOrEmpty(currentToken))
            { 
                var jwtHandler = new JwtSecurityTokenHandler();
                var jToken = jwtHandler.ReadToken(currentToken);
                var expDate = jToken.ValidTo;
                if (expDate < DateTime.UtcNow.AddMinutes(1))
                    authResult = await _tokenService.RefreshTokenAsync(currentToken, currentRefreshToken);
                else
                    authResult = new AuthResult
                    {
                        Token = currentToken,
                        RefreshToken = currentRefreshToken,
                        Result = true
                    };
            }
            else
            {
                authResult = await _tokenService.RefreshTokenAsync(currentToken, currentRefreshToken);
            }

            if (authResult.Result == false || 
                (authResult.Errors != null && authResult.Errors.Any(e => e.Contains("token has not expired"))))
            {
                authResult.Token = currentToken;
                authResult.RefreshToken = currentRefreshToken;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", 
                authResult.Token);

            _authenticationService.SetSessionParameters(authResult.Token, authResult.RefreshToken, string.Empty);

            return _httpClient;
        }
    }
}