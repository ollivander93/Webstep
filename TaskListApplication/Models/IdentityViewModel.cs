using Microsoft.AspNetCore.Identity;
using TaskListApplication.Models.DOT.Responses;

namespace TaskListApplication.Models
{
    public class IdentityViewModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public SignedInUserResponse User { get; set; }
    }
}