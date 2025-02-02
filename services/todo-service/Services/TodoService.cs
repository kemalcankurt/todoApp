using todo_service.Models;
using todo_service.Repositories;
using todo_service.DTOs;
using todo_service.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;

    public TodoService(ITodoRepository todoRepository)
    {
        _todoRepository = todoRepository;
    }

    public async Task<IEnumerable<TodoReadDto>> GetAllTodosAsync()
    {
        var todos = await _todoRepository.GetAllAsync();
        return todos.Select(t => new TodoReadDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            IsCompleted = t.IsCompleted,
            CreatedDate = t.CreatedDate,
            UpdatedDate = t.UpdatedDate
        });
    }

    public async Task<TodoReadDto?> GetTodoByIdAsync(int id)
    {
        var todo = await _todoRepository.GetByIdAsync(id);
        if (todo == null) return null;

        return new TodoReadDto
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            IsCompleted = todo.IsCompleted,
            CreatedDate = todo.CreatedDate,
            UpdatedDate = todo.UpdatedDate
        };
    }

    public async Task<TodoReadDto> AddTodoAsync(TodoCreateDto todoDto)
    {
        var newTodo = new Todo
        {
            Title = todoDto.Title,
            Description = todoDto.Description,
            IsCompleted = todoDto.IsCompleted,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            // TODO: change after auth
            UserId = 1, // const for now, will change after auth
            IsDeleted = false
        };

        var createdTodo = await _todoRepository.AddAsync(newTodo);

        return new TodoReadDto
        {
            Id = createdTodo.Id,
            Title = createdTodo.Title,
            Description = createdTodo.Description,
            IsCompleted = createdTodo.IsCompleted,
            CreatedDate = createdTodo.CreatedDate,
            UpdatedDate = createdTodo.UpdatedDate
        };
    }

    public async Task<bool> UpdateTodoAsync(int id, TodoUpdateDto todoDto)
    {
        var existingTodo = await _todoRepository.GetByIdAsync(id);
        if (existingTodo == null) return false;

        existingTodo.Title = todoDto.Title;
        existingTodo.Description = todoDto.Description;
        existingTodo.IsCompleted = todoDto.IsCompleted;
        existingTodo.UpdatedDate = DateTime.UtcNow;

        await _todoRepository.UpdateAsync(existingTodo);
        return true;
    }

    public async Task<bool> DeleteTodoAsync(int id)
    {
        var todo = await _todoRepository.GetByIdAsync(id);
        if (todo == null) return false;

        todo.IsDeleted = true;
        await _todoRepository.UpdateAsync(todo);
        return true;
    }
}
