using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using TaskApi.Models;

namespace TaskListApplication.Models
{
    public class IndexViewModel
    {
        public string JwtToken { get; set; }
        public List<TaskList> TaskLists { get; set; }
        public IdentityUser User { get; set; }
    }
}