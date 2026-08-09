using MySql.Data.MySqlClient;
using System.Text.Json;

namespace AutoBlog.Services
{
    public class GoogleService
    {
        private readonly IConfiguration _configuration;

        public GoogleService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetAccessToken()
        {
            string refreshToken = "";

            // 🔹 Step 1: DB मधून refresh_token घे
            using (var con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT refresh_token FROM GoogleTokens ORDER BY id DESC LIMIT 1";

                using (var cmd = new MySqlCommand(query, con))
                {
                    con.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                        refreshToken = result.ToString();
                }
            }

            if (string.IsNullOrEmpty(refreshToken))
                throw new Exception("Refresh token not found in DB");

            // 🔹 Step 2: Google ला request
            var client = new HttpClient();

            var values = new Dictionary<string, string>
    {
        { "client_id", _configuration["Google:ClientId"] },
        { "client_secret", _configuration["Google:ClientSecret"] },
        { "refresh_token", refreshToken },
        { "grant_type", "refresh_token" }
    };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);

            var resultJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Google Response: " + resultJson);

            // 🔥 Step 3: SAFE JSON parsing
            var jsonDoc = JsonDocument.Parse(resultJson);

            // ❌ जर access_token नाही मिळाला
            if (!jsonDoc.RootElement.TryGetProperty("access_token", out var tokenElement))
            {
                throw new Exception("❌ Access token not found. Google Error: " + resultJson);
            }

            var accessToken = tokenElement.GetString();

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("❌ Access token is empty");

            return accessToken;
        }
    }
}
