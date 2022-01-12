using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Newtonsoft.Json;
using TaskApi.Domain;
using TaskApi.Models;
using TaskApi.Models.DTO.Requests;
using TaskApi.Models.DTO.Responses;
using TaskListApplication.Models.DOT.Responses;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonSerializer = System.Text.Json.JsonSerializer;
using SignedInUserResponse = TaskListApplication.Models.DOT.Responses.SignedInUserResponse;
using Task = System.Threading.Tasks.Task;

namespace TaskListApplication.Http
{
    public interface ITaskApiHttpClientWrapper
    {

        Task<IEnumerable<TaskList>> GetAllTaskListsAsync();
        Task<LoginResponse> SignInAsync(string email, string password);
        Task<SignedInUserResponse> GetUserFromTokenAsync();
        Task<ActionResult<TaskList>> CreateTaskListAsync(string name, string userId);
        Task<AuthResult> RefreshTokenAsync(string token, string refreshToken);
        Task<RegistrationResponse> RegisterAsync(string email, string password);
        Task<ActionResult<TaskApi.Models.Task>> CreateTaskAsync(int taskListId, string title, string note);
        Task<IEnumerable<TaskList>> GetUserTaskLists(string userId);
        Task<SignedInUserResponse> GetSignedInUser();
        void SetSessionParameters(string token, string refreshToken, string userEmail);
        Task<bool> DeleteTaskAsync(int taskId);
        Task<bool> DeleteTaskListAsync(int taskListId);
        Task<TaskApi.Models.Task> UpdateTaskAsync(int taskId, string title, string note, int taskListId);
    }
    
    public class TaskApiHttpClientWrapper : ITaskApiHttpClientWrapper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskApiHttpClientWrapper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }
        
        private async Task<HttpClient> GetHttpClient(bool authenticated = true)
        {
            HttpClient client = null;
            if (!authenticated)
                client = new HttpClient();
            else
                client = await GetAuthenticatedClient();
            SetClientBaseAddress(client);
            
            return client;
        }

        private async Task<bool> RefreshToken(TokenRequest tokenRequest, HttpContext context)
        {
            var client = await GetHttpClient();
            var res = await client.PostAsJsonAsync("/api/token/refresh", tokenRequest);
            return res.IsSuccessStatusCode;
        }
        
        public async Task<IEnumerable<TaskList>> GetAllTaskListsAsync()
        {
            var client = await GetHttpClient();
            var res = await client.GetAsync("/api/tasklist");
            if (!res.IsSuccessStatusCode)
                return new List<TaskList>();
            var content = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TaskList[]>(content);
        }

        public async Task<LoginResponse> SignInAsync(string email, string password)
        {
            var req = new UserLoginRequest
            {
                Email = email,
                Password = password
            };
            var client = await GetHttpClient(false);
            var response = await client.PostAsJsonAsync("/api/authentication/Login", req);
            if (!response.IsSuccessStatusCode)
                return null;
            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        public async Task<RegistrationResponse> RegisterAsync(string email, string password)
        {
            var req = new UserRegistrationRequestDto
            {
                Email = email,
                Name = email,
                Password = password
            };
            var client = await GetHttpClient(false);
            var response = await client.PostAsJsonAsync("/api/authentication/Register", req);
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RegistrationResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<SignedInUserResponse> GetUserFromTokenAsync()
        {
            var client = await GetHttpClient();
            var response = await client.GetAsync("/api/users/signedInUser");
            if (!response.IsSuccessStatusCode)
                return null;
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SignedInUserResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<ActionResult<TaskList>> CreateTaskListAsync(string name, string userId)
        {
            var newTaskList = new TaskList
            {
                Id = 0,
                Name = name,
                OwnerId = userId,
                CreatedTime = DateTime.UtcNow,
                TimeStamp = DateTime.UtcNow
            };
            var client = await GetHttpClient();
            var createResponse = await client.PostAsJsonAsync("/api/tasklist", newTaskList);
            return !createResponse.IsSuccessStatusCode 
                ? null 
                : JsonSerializer.Deserialize<TaskList>(await createResponse.Content.ReadAsStringAsync());
        }
        
        public async Task<ActionResult<TaskApi.Models.Task>> CreateTaskAsync(int taskListId, string title, string note)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetString("userId");
            var newTask = new TaskApi.Models.Task
            {
                Id = 0,
                Title = title,
                Note = note,
                OwnerId = userId,
                TaskListId = taskListId,
                CreatedTime = DateTime.UtcNow,
                TimeStamp = DateTime.UtcNow
            };
            var client = await GetHttpClient();
            var createResponse = await client.PostAsJsonAsync("/api/task", newTask);
            return !createResponse.IsSuccessStatusCode 
                ? null 
                : JsonSerializer.Deserialize<TaskApi.Models.Task>(await createResponse.Content.ReadAsStringAsync());
        }

        public async Task<IEnumerable<TaskList>> GetUserTaskLists(string userId)
        {
            var client = await GetHttpClient();
            var res = await client.GetAsync(
                $"/api/taskList/owner/{userId}?includeTasks=true");
            if (!res.IsSuccessStatusCode)
                return new List<TaskList>();
            var content = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TaskList[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<SignedInUserResponse> GetSignedInUser()
        {
            var client = await GetHttpClient();
            var userResponse = await client.GetAsync("/api/users/signedInUser");
            if (!userResponse.IsSuccessStatusCode)
                return null;
            
            return JsonSerializer.Deserialize<SignedInUserResponse>(
                await userResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public async Task<AuthResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var json = new TokenRequest
            {
                Token = token,
                RefreshToken = refreshToken
            };
            var client = await GetHttpClient(false);
            var result = await client.PostAsJsonAsync("/api/token/refresh", json);
            var content = await result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResult>(content, new JsonSerializerOptions
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

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var client = await GetHttpClient();
            var result = await client.DeleteAsync($"/api/task/{taskId}");
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTaskListAsync(int taskListId)
        {
            var client = await GetHttpClient();
            var result = await client.DeleteAsync($"/api/taskList/{taskListId}");
            return result.IsSuccessStatusCode;
        }

        public async Task<TaskApi.Models.Task> UpdateTaskAsync(int taskId, string title, string note, int taskListId)
        {
            var client = await GetHttpClient();
            var patchTask = GetTaskAsJsonPatch(title, note, taskListId);
            var serializedDoc = JsonConvert.SerializeObject(patchTask);
            
            var result = await client.SendAsync(
                GetPatchRequestMessage($"/api/task/{taskId}", serializedDoc));

            if (!result.IsSuccessStatusCode)
                return null;

            var content = await result.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TaskApi.Models.Task>(content);
        }

        private HttpRequestMessage GetPatchRequestMessage(string requestUri, string serializedContent)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(serializedContent, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
            return request;
        }

        private JsonPatchDocument<TaskApi.Models.Task> GetTaskAsJsonPatch(string title, string note, int taskListId)
        {
            var patch = new JsonPatchDocument<TaskApi.Models.Task>();
            patch.Replace(t => t.Title, title);
            patch.Replace(t => t.Note, note);
            patch.Replace(t => t.TaskListId, taskListId);
            return patch;
        }

        private async Task<HttpClient> GetAuthenticatedClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var currentToken = _httpContextAccessor.HttpContext.Session.GetString("token");
            var currentRefreshToken = _httpContextAccessor.HttpContext.Session.GetString("refreshToken");
            AuthResult authResult;
            if (!string.IsNullOrEmpty(currentToken))
            { 
                var jwtHandler = new JwtSecurityTokenHandler();
                var jToken = jwtHandler.ReadToken(currentToken);
                var expDate = jToken.ValidTo;
                if (expDate < DateTime.UtcNow.AddMinutes(1))
                    authResult = await RefreshTokenAsync(currentToken, currentRefreshToken);
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
                authResult = await RefreshTokenAsync(currentToken, currentRefreshToken);
            }

            if (authResult.Result == false || 
                (authResult.Errors != null && authResult.Errors.Any(e => e.Contains("token has not expired"))))
            {
                authResult.Token = currentToken;
                authResult.RefreshToken = currentRefreshToken;
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", 
                authResult.Token);
            
            SetSessionParameters(authResult.Token, authResult.RefreshToken, string.Empty);

            return client;
        }

        private void SetClientBaseAddress(HttpClient client)
        {
            client.BaseAddress = new Uri("https://localhost:5021");
        }
    }
}