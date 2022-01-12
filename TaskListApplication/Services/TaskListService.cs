using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Models;
using TaskListApplication.Http;

namespace TaskListApplication.Services
{
    public interface ITaskListService
    {
        Task<ActionResult<TaskList>> CreateTaskListAsync(string name, string userId);
        Task<IEnumerable<TaskList>> GetUserTaskLists(string userId);
        Task<bool> DeleteTaskListAsync(int taskListId);
    }
    
    public class TaskListService : ITaskListService
    {
        private readonly IHttpClientProvider _clientProvider;

        public TaskListService(IHttpClientProvider clientProvider)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
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
            var client = await _clientProvider.GetAuthenticatedClient();
            var createResponse = await client.PostAsJsonAsync("/api/tasklist", newTaskList);
            return !createResponse.IsSuccessStatusCode 
                ? null 
                : JsonSerializer.Deserialize<TaskList>(await createResponse.Content.ReadAsStringAsync());
        }
        
        public async Task<IEnumerable<TaskList>> GetUserTaskLists(string userId)
        {
            var client = await _clientProvider.GetAuthenticatedClient();
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
        
        public async Task<bool> DeleteTaskListAsync(int taskListId)
        {
            var client = await _clientProvider.GetAuthenticatedClient();
            var result = await client.DeleteAsync($"/api/taskList/{taskListId}");
            return result.IsSuccessStatusCode;
        }
    }
}