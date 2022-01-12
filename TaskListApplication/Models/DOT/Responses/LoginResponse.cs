namespace TaskListApplication.Models.DOT.Responses
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool Result { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string[] Errors { get; set; }
    }
}