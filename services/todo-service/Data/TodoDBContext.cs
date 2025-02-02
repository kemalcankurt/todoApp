using Microsoft.EntityFrameworkCore;

using todo_service.Models;

namespace todo_service.Data
{
    public class TodoDBContext : DbContext
    {
        public TodoDBContext(DbContextOptions<TodoDBContext> options) : base(options) { }

        public DbSet<Todo> Todos { get; set; }
    }
}