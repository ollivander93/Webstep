using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TaskApi.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime TimeStamp { get; set; }
        [ForeignKey("Owner")]
        public string OwnerId { get; set; }
        [ForeignKey("TaskList")]
        public int TaskListId { get; set; }
    }
}