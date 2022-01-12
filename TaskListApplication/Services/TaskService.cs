using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaskListApplication.Http;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TaskListApplication.Services
{
    public interface ITaskService
    {
        Task<ActionResult<TaskApi.Models.Task>> CreateTaskAsync(int taskListId, string title, string note);
        Task<TaskApi.Models.Task> UpdateTaskAsync(int taskId, string title, string note, int taskListId);
        Task<bool> DeleteTaskAsync(int taskId);
    }
    
    public class TaskService : ITaskService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientProvider _clientProvider;

        public TaskService(IHttpContextAccessor httpContextAccessor, IHttpClientProvider clientProvider)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
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
            var client = await _clientProvider.GetAuthenticatedClient();
            var createResponse = await client.PostAsJsonAsync("/api/task", newTask);
            return !createResponse.IsSuccessStatusCode 
                ? null 
                : JsonSerializer.Deserialize<TaskApi.Models.Task>(await createResponse.Content.ReadAsStringAsync());
        }
        
        public async Task<TaskApi.Models.Task> UpdateTaskAsync(int taskId, string title, string note, int taskListId)
        {
            var client = await _clientProvider.GetAuthenticatedClient();
            var patchTask = GetTaskAsJsonPatch(title, note, taskListId);
            var serializedDoc = JsonConvert.SerializeObject(patchTask);
            
            var result = await client.SendAsync(
                GetPatchRequestMessage($"/api/task/{taskId}", serializedDoc));

            if (!result.IsSuccessStatusCode)
                return null;

            var content = await result.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TaskApi.Models.Task>(content);
        }
        
        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var client = await _clientProvider.GetAuthenticatedClient();
            var result = await client.DeleteAsync($"/api/task/{taskId}");
            return result.IsSuccessStatusCode;
        }
        
        private static JsonPatchDocument<TaskApi.Models.Task> GetTaskAsJsonPatch(string title, string note, int taskListId)
        {
            var patch = new JsonPatchDocument<TaskApi.Models.Task>();
            patch.Replace(t => t.Title, title);
            patch.Replace(t => t.Note, note);
            patch.Replace(t => t.TaskListId, taskListId);
            return patch;
        }
        
        private static HttpRequestMessage GetPatchRequestMessage(string requestUri, string serializedContent)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(serializedContent, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
            return request;
        }
    }
}