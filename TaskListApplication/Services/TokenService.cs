using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TaskApi.Domain;
using TaskApi.Models.DTO.Requests;

namespace TaskListApplication.Services
{
    public interface ITokenService
    {
        Task<AuthResult> RefreshTokenAsync(string token, string refreshToken);
    }

    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        
        public TokenService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:5021");
        }
        
        public async Task<AuthResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var json = new TokenRequest
            {
                Token = token,
                RefreshToken = refreshToken
            };
            var result = await _httpClient.PostAsJsonAsync("/api/token/refresh", json);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}