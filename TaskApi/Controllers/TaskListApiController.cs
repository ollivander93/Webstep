using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;
using TaskApi.Services;

namespace TaskApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class TaskListApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskListService _taskListService;

        public TaskListApiController(ITaskListService taskListService, ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _taskListService = taskListService ?? throw new ArgumentNullException(nameof(taskListService));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskList>>> GetTaskLists()
        {
            return await _context.TaskLists.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskList>> GetTaskList(string id)
        {
            var taskList = await _context.TaskLists.FindAsync(id);

            if (taskList == null)
            {
                return NotFound();
            }

            return taskList;
        }

        [HttpGet("Owner/{ownerId}")]
        public async Task<ActionResult<IEnumerable<TaskList>>> GetTaskListsByOwner(string ownerId, [FromQuery]bool includeTasks = false)
        {
            var res = await _taskListService.GetTaskListsByOwner(ownerId);
            if (!includeTasks)
                return res.ToList();
            var taskLists = new List<TaskList>();
            foreach (var tl in res)
            {
                var tasks = _context.Tasks.Where(x =>
                    x.TaskListId == tl.Id);
                tl.Tasks = await tasks.ToListAsync();
                taskLists.Add(tl);
            }

            return taskLists;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTaskList(int id, TaskList taskList)
        {
            if (id != taskList.Id)
            {
                return BadRequest();
            }

            _context.Entry(taskList).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskListExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<TaskList>> PostTaskList(TaskList taskList)
        {
            if (string.IsNullOrEmpty(taskList.OwnerId))
                taskList.OwnerId = User.Claims.FirstOrDefault(i => i.Type == "Id").Value;
            _context.TaskLists.Add(taskList);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTaskList", new { id = taskList.Id }, taskList);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskList(int id)
        {
            var taskList = await _context.TaskLists.FindAsync(id);
            if (taskList == null)
            {
                return NotFound();
            }

            _context.TaskLists.Remove(taskList);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskListExists(int id)
        {
            return _context.TaskLists.Any(e => e.Id == id);
        }
    }
}
