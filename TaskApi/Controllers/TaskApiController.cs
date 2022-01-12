using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using Task = TaskApi.Models.Task;

namespace TaskApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Task>>> GetTasks()
        {
            return await _context.Tasks.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Task>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            return task;
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<Task>> PatchTask(int id, [FromBody] JsonPatchDocument<Task> taskPatch)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound("Task Id not found");
            taskPatch.ApplyTo(task);
            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return (Ok(task));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTask(int id, Task task)
        {
            if (id != task.Id)
            {
                return BadRequest();
            }

            _context.Entry(task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }
        
        [HttpPost]
        public async Task<ActionResult<Task>> PostTask(Task task)
        {
            if (string.IsNullOrEmpty(task.OwnerId))
                task.OwnerId = User.Claims.FirstOrDefault(i => i.Type == "Id").Value;
            if (!_context.TaskLists.Any(tl => tl.Id == task.TaskListId))
            {
                return NotFound("No task list found");   
            }
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTask", new { id = task.Id }, task);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}
