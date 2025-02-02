using Microsoft.EntityFrameworkCore;

using todo_service.Data;
using todo_service.Models;

namespace todo_service.Repositories
{
    public class TodoRepository : ITodoRepository
    {
        private readonly TodoDBContext _dbContext;

        public TodoRepository(TodoDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Todo>> GetAllAsync()
        {
            return await _dbContext.Todos.Where(t => !t.IsDeleted).ToListAsync();
        }

        public async Task<Todo?> GetByIdAsync(int id)
        {
            return await _dbContext.Todos
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }
        public async Task<Todo> AddAsync(Todo todo)
        {
            await _dbContext.Todos.AddAsync(todo);
            await _dbContext.SaveChangesAsync();
            return todo;
        }

        public async Task UpdateAsync(Todo todo)
        {
            _dbContext.Todos.Update(todo);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var todo = await _dbContext.Todos.FindAsync(id);
            if (todo != null)
            {
                todo.IsDeleted = true;
                _dbContext.Todos.Update(todo);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}