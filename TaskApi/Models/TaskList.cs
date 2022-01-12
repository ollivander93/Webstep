using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TaskApi.Models
{
    public class TaskList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime TimeStamp { get; set; }
        
        [ForeignKey("Owner")] 
        public string OwnerId { get; set; }

        public List<Task> Tasks { get; set; }
    }
}