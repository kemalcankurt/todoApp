using todo_service.DTOs;

namespace todo_service.Services
{
    public interface ITodoService
    {
        Task<IEnumerable<TodoReadDto>> GetAllTodosAsync();
        Task<TodoReadDto?> GetTodoByIdAsync(int id);
        Task<TodoReadDto> AddTodoAsync(TodoCreateDto todoDto);
        Task<bool> UpdateTodoAsync(int id, TodoUpdateDto todoDto);
        Task<bool> DeleteTodoAsync(int id);
    }
}
