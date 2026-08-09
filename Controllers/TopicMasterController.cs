using AutoBlog.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace AutoBlog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TopicMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ SAVE / UPDATE
        [HttpPost("save-update")]
        public IActionResult SaveUpdate(TopicMaster model)
        {
            using (var con = GetConnection())
            {
                using (var cmd = new MySqlCommand("sp_SaveUpdate_TopicMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_config_id", model.ConfigId);
                    cmd.Parameters.AddWithValue("p_blog_id", model.BlogId);
                    cmd.Parameters.AddWithValue("p_topic_name", model.TopicName);
                    cmd.Parameters.AddWithValue("p_keyword_density", model.KeywordDensity);
                    cmd.Parameters.AddWithValue("p_external_url", model.ExternalUrl);
                    cmd.Parameters.AddWithValue("p_keyword", model.Keyword);
                    cmd.Parameters.AddWithValue("p_word_count", model.WordCount); // ✅ NEW FIELD

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            return Ok(new { message = "Saved Successfully" });
        }

        // ✅ GET TODAY DATA
        [HttpGet("get-today")]
        public IActionResult GetToday()
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
                    t.word_count,   -- ✅ FIX ADDED
                    b.blog_name
                FROM Topic_Master t
                JOIN Blog_Master b ON t.blog_id = b.blog_id
                WHERE DATE(t.created_date) = CURDATE()";

                using (var cmd = new MySqlCommand(query, con))
                {
                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                configId = Convert.ToInt32(reader["config_id"]),
                                topicName = reader["topic_name"].ToString(),
                                keywordDensity = Convert.ToDouble(reader["keyword_density"]),
                                externalUrl = Convert.ToInt32(reader["external_url"]),
                                keyword = reader["keyword"].ToString(),
                                blogName = reader["blog_name"].ToString(),
                                wordCount = Convert.ToInt32(reader["word_count"]) // ✅ SAFE NOW
                            });
                        }
                    }

                    con.Close();
                }
            }

            return Ok(list);
        }

        // ✅ DELETE
        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            using (var con = GetConnection())
            {
                using (var cmd = new MySqlCommand("DELETE FROM Topic_Master WHERE config_id=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            return Ok(new { message = "Deleted Successfully" });
        }
    }
}