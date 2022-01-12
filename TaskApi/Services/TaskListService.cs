using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;

namespace TaskApi.Services
{
    public interface ITaskListService
    {
        Task<IEnumerable<TaskList>> GetTaskListsByOwner(string ownerId);
    }
    
    public class TaskListService : ITaskListService
    {
        private readonly ApplicationDbContext _dbContext;

        public TaskListService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        public async Task<IEnumerable<TaskList>> GetTaskListsByOwner(string ownerId)
        {
            return await _dbContext.TaskLists.Where(tl => tl.OwnerId.Equals(ownerId)).ToListAsync();
        }
    }
}