namespace TaskApi.Configuration
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public int ExpiryTimeFrame { get; set; }
    }
}