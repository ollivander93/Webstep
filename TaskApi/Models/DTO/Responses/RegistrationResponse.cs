using TaskApi.Domain;

namespace TaskApi.Models.DTO.Responses
{
    public class RegistrationResponse : AuthResult
    {
        public string UserId { get; set; }
        public string Email { get; set; }
    }
}