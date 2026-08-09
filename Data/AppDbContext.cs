using AutoBlog.Models;
using AutoBlog.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoBlog.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<BlogMaster> BlogMasters { get; set; }
    }
}