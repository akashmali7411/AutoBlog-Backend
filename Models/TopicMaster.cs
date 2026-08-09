using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoBlog.Models
{
    [Table("Topic_Master")]
    public class TopicMaster
    {
        [Key]
        public int ConfigId { get; set; }

        public int BlogId { get; set; }

        public string TopicName { get; set; }

        public double KeywordDensity { get; set; }

        public int ExternalUrl { get; set; }

        public string Keyword { get; set; }

        public DateTime CreatedDate { get; set; }

        public int WordCount { get; set; }
    }
}