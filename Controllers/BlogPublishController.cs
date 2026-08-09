using AutoBlog.Services;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace AutoBlog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPublishController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AiService _aiService;
        private readonly GoogleService _googleService;

        // ✅ SINGLE CONSTRUCTOR (FIXED)
        public BlogPublishController(
            IConfiguration configuration,
            AiService aiService,
            GoogleService googleService)
        {
            _configuration = configuration;
            _aiService = aiService;
            _googleService = googleService;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // 🔥 Publish Blog
        [HttpPost("publish/{topicId}")]
        public async Task<IActionResult> Publish(int topicId)
        {
            string topicName = "";
            string keywords = "";
            int wordCount = 0;
            double keywordDensity = 0;
            int externalUrl = 0;

            using (var con = GetConnection())
            {
                string query = "SELECT * FROM Topic_Master WHERE config_id=@id";

                using (var cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", topicId);
                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            topicName = reader["topic_name"].ToString();
                            keywords = reader["keyword"].ToString();
                            wordCount = Convert.ToInt32(reader["word_count"]);
                            keywordDensity = Convert.ToDouble(reader["keyword_density"]);
                            externalUrl = Convert.ToInt32(reader["external_url"]);
                        }
                    }

                    con.Close();
                }
            }

            var blogContent = await _aiService.GenerateBlog(
                topicName,
                keywords,
                wordCount,
                keywordDensity
            );

            using (var con = GetConnection())
            {
                using (var cmd = new MySqlCommand("sp_Save_BlogPublish", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_topic_id", topicId);
                    cmd.Parameters.AddWithValue("p_blog_name", topicName);
                    cmd.Parameters.AddWithValue("p_blog_description", blogContent);
                    cmd.Parameters.AddWithValue("p_keyword_density", keywordDensity);
                    cmd.Parameters.AddWithValue("p_external_url", externalUrl);
                    cmd.Parameters.AddWithValue("p_keyword", keywords);
                    cmd.Parameters.AddWithValue("p_word_count", wordCount);
                    cmd.Parameters.AddWithValue("p_status", "Published");

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            return Ok(new
            {
                message = "Blog Generated & Saved Successfully 🚀",
                blogPreview = blogContent
            });
        }

        // 🔍 ✅ FINAL SEARCH (FULL DATA)
        [HttpGet("search")]
        public IActionResult Search(string term)
        {
            var list = new List<dynamic>();

            using (var con = GetConnection())
            {
                string query = @"
                SELECT 
                    t.config_id,
                    t.topic_name,
                    t.keyword_density,
                    t.external_url,
                    t.keyword,
                    t.word_count,
                    b.blog_name
                FROM Topic_Master t
                JOIN Blog_Master b ON t.blog_id = b.blog_id
                WHERE t.topic_name LIKE @term";

                using (var cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@term", "%" + term + "%");

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                id = Convert.ToInt32(reader["config_id"]),
                                name = reader["topic_name"].ToString(),
                                blogName = reader["blog_name"].ToString(),
                                keywordDensity = Convert.ToDouble(reader["keyword_density"]),
                                externalUrl = Convert.ToInt32(reader["external_url"]),
                                keyword = reader["keyword"].ToString(),
                                wordCount = Convert.ToInt32(reader["word_count"])
                            });
                        }
                    }
                }
            }

            return Ok(list);
        }

        // 💾 Save Edited Blog
        [HttpPost("save-edited")]
        public IActionResult SaveEdited([FromBody] dynamic data)
        {
            int topicId = data.topicId;
            string blogDescription = data.blogDescription;

            using (var con = GetConnection())
            {
                string query = @"
                INSERT INTO Blog_Publish
                (topic_id, blog_name, blog_description, status)
                VALUES (@topic_id, 'Edited Blog', @desc, 'Draft')";

                using (var cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@topic_id", topicId);
                    cmd.Parameters.AddWithValue("@desc", blogDescription);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Edited blog saved" });
        }


      
        [HttpPost("publish-google/{topicId}")]
public async Task<IActionResult> PublishToGoogle(int topicId)
    {
        string blogContent = "";
        string blogTitle = "";
        string blogCode = "";

        using (var con = GetConnection())
        {
            string query = @"
SELECT p.blog_description, b.blog_code, t.topic_name
FROM Blog_Publish p
JOIN Topic_Master t ON p.topic_id = t.config_id
JOIN Blog_Master b ON t.blog_id = b.blog_id
WHERE t.config_id = @id
ORDER BY p.publish_id DESC
LIMIT 1;";

            using (var cmd = new MySqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@id", topicId);
                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        blogContent = reader["blog_description"]?.ToString();
                        blogTitle = reader["topic_name"]?.ToString();
                        blogCode = reader["blog_code"]?.ToString();
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(blogContent) || string.IsNullOrEmpty(blogCode))
            return BadRequest("Blog data not found");

            // ================== 🔥 FORMATTING FIX START ==================

            // 1. remove empty <p>
            blogContent = Regex.Replace(blogContent, @"<p>\s*</p>", "");

            // 2. markdown bold → heading
            blogContent = Regex.Replace(blogContent, @"\*\*(.*?)\*\*", "<h2>$1</h2>");

            // 3. bullet heading fix
            blogContent = Regex.Replace(blogContent, @"\*\s*<h2>(.*?)</h2>\s*:", "<li><b>$1:</b> ");

            // 4. normal bullet → li
            blogContent = Regex.Replace(blogContent, @"\*\s+(.*?)(?=<|$)", "<li>$1</li>");

            // 🔥 FIXED PART (IMPORTANT)
            if (blogContent.Contains("<li>"))
            {
                blogContent = Regex.Replace(
                    blogContent,
                    @"(<li>.*?</li>)+",
                    match => "<ul>" + match.Value + "</ul>"
                );
            }

            // 5. code block fix
            blogContent = Regex.Replace(blogContent, @"```csharp", "<pre><code>");
            blogContent = Regex.Replace(blogContent, @"```", "</code></pre>");

            // 6. remove <p> around heading
            blogContent = Regex.Replace(blogContent, @"<p>\s*(<h2>.*?</h2>)\s*</p>", "$1");

            // ================== 🔥 FORMATTING FIX END ==================

            // 👉 existing logic (unchanged)
            string formattedContent;

        if (blogContent.Contains("<p>") || blogContent.Contains("<h2>"))
        {
            formattedContent = blogContent;
        }
        else
        {
            formattedContent = "<p>" + blogContent.Replace("\n", "</p><p>") + "</p>";
        }

        var accessToken = await _googleService.GetAccessToken();

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var postData = new
        {
            kind = "blogger#post",
            title = blogTitle,
            content = formattedContent,
           // labels = new string[] { "AI", "Tech", "Programming" }
            labels = new string[] {  "Tech", "Programming" }
        };

        var response = await client.PostAsJsonAsync(
            $"https://www.googleapis.com/blogger/v3/blogs/{blogCode}/posts/",
            postData
        );

        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(result);

        return Ok(new
        {
            message = "Published Successfully 🚀",
            data = result
        });
    }

}
}