using AutoBlog.Models;
using AutoBlog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;

namespace AutoBlog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public BlogMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpPost("save-update")]
        public IActionResult SaveUpdate(BlogMaster model)
        {
            using (var con = GetConnection())
            {
                using (var cmd = new MySqlCommand("sp_SaveUpdate_BlogMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("p_blog_id", model.BlogId);
                    cmd.Parameters.AddWithValue("p_blog_name", model.BlogName);
                    cmd.Parameters.AddWithValue("p_blog_code", model.BlogCode);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            return Ok(new { message = "Saved Successfully" });
        }

        [HttpGet("get-all")]
        public IActionResult GetAll()
        {
            var list = new List<BlogMaster>();

            using (var con = GetConnection())
            {
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("SELECT * FROM Blog_Master", con))
                {
                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new BlogMaster
                            {
                                BlogId = Convert.ToInt32(reader["blog_id"]),
                                BlogName = reader["blog_name"].ToString(),
                                BlogCode = reader["blog_code"].ToString()
                            });
                        }
                    }

                    con.Close();
                }
            }

            // 🔥 Convert to camelCase (BEST PRACTICE)
            var result = list.Select(x => new
            {
                blogId = x.BlogId,
                blogName = x.BlogName,
                blogCode = x.BlogCode
            }).ToList();

            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            using (var con = GetConnection())
            {
                using (var cmd = new MySqlCommand("DELETE FROM Blog_Master WHERE blog_id=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Deleted Successfully" });
        }
    }

}