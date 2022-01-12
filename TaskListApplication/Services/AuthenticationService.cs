using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TaskApi.Models.DTO.Requests;
using TaskApi.Models.DTO.Responses;
using TaskListApplication.Models.DOT.Responses;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TaskListApplication.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> SignInAsync(string email, string password);
        Task<RegistrationResponse> RegisterAsync(string email, string password);
        
        void SetSessionParameters(string token, string refreshToken, string userEmail);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public AuthenticationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:5021");
        }
        
        public async Task<RegistrationResponse> RegisterAsync(string email, string password)
        {
            var req = new UserRegistrationRequestDto
            {
                Email = email,
                Name = email,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync("/api/authentication/Register", req);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RegistrationResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        public async Task<LoginResponse> SignInAsync(string email, string password)
        {
            var req = new UserLoginRequest
            {
                Email = email,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync("/api/authentication/Login", req);
            if (!response.IsSuccessStatusCode)
                return null;
            var result = await response.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<LoginResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        public void SetSessionParameters(string token, string refreshToken, string userEmail)
        {
            if (!string.IsNullOrEmpty(token))
                _httpContextAccessor.HttpContext.Session.SetString("token", token);
            if (!string.IsNullOrEmpty(refreshToken))
                _httpContextAccessor.HttpContext.Session.SetString("refreshToken", refreshToken);
            if (!string.IsNullOrEmpty(userEmail))
                _httpContextAccessor.HttpContext.Session.SetString("userName", userEmail);
        }
    }
}