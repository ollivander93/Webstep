using System;
using System.Text.Json;
using System.Threading.Tasks;
using TaskApi.Models.DTO.Responses;
using TaskListApplication.Http;

namespace TaskListApplication.Services
{
    public interface IUserService
    {
        Task<SignedInUserResponse> GetSignedInUser();
    }
    
    public class UserService : IUserService
    {
        private readonly IHttpClientProvider _clientProvider;

        public UserService(IHttpClientProvider clientProvider)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        }
        
        public async Task<SignedInUserResponse> GetSignedInUser()
        {
            var client = await _clientProvider.GetAuthenticatedClient();
            var userResponse = await client.GetAsync("/api/users/signedInUser");
            if (!userResponse.IsSuccessStatusCode)
                return null;
            
            return JsonSerializer.Deserialize<SignedInUserResponse>(
                await userResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }
}