using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoBlog.Services
{
    public class AiService
    {
        private readonly IConfiguration _config;

        public AiService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> GenerateBlog(string topic, string keywords, int wordCount, double density)
        {
            var apiKey = _config["GroqApiKey"];

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
              //  string language = "English";
                /*
                                if (topic.Any(c => c >= 0x0900 && c <= 0x097F))
                                {
                                    language = "Marathi";
                                }*/

                /*  var prompt = $@"
  Write a 100% human-like blog in {language} language.

  Topic: {topic}
  Keywords: {keywords}
  Word Count: {wordCount}

  Instructions:
  - Use HTML format (<h2>, <p>, <ul>, <li>)
  - Do NOT use markdown (**, ##)
  - If Marathi, do not use English
  - If English, write professionally
  - Add SEO title
  - Add tags at end
  ";*/

                var isMarathi = topic.Any(c => c >= 0x0900 && c <= 0x097F);

                var language = isMarathi ? "Marathi" : "English";

                var prompt = $@"
Write a high-quality blog strictly in {language} language.

Topic: {topic}
Keywords: {keywords}
Word Count: {wordCount}

Instructions:
- Language MUST be {language} only (no mixing)
- Use clean HTML format (<h2>, <p>, <ul>, <li>)
- Do NOT use markdown (** ##)
- Start with introduction
- Use proper headings

Special:
- If recipe → include Ingredients + Steps
- If technical → include explanation + examples

End:
- Add conclusion
- Add tags
";

                var body = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    content
                );

                var responseString = await response.Content.ReadAsStringAsync();

                // 🔥 DEBUG (VERY IMPORTANT)
                Console.WriteLine(responseString);

                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    // ✅ SAFE CHECK
                    if (doc.RootElement.TryGetProperty("choices", out JsonElement choices))
                    {
                        return choices[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();
                    }
                    else
                    {
                        return "AI Error: " + responseString;
                    }
                }
            }
        }
    }
}