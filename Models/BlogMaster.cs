using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoBlog.Models
{
    [Table("Blog_Master")]
    public class BlogMaster
    {
        [Key]
        [Column("blog_id")]
        public int BlogId { get; set; }

        [Required]
        [Column("blog_name")]
        public string BlogName { get; set; }

        [Required]
        [Column("blog_code")]
        public string BlogCode { get; set; }
    }
}