using todo_service.Models;
using todo_service.Repositories;
using todo_service.DTOs;
using todo_service.Services;

using AutoMapper;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly IMapper _mapper;

    public TodoService(ITodoRepository todoRepository, IMapper mapper)
    {
        _todoRepository = todoRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<TodoReadDto>> GetAllTodosAsync()
    {
        var todos = await _todoRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<TodoReadDto>>(todos);
    }

    public async Task<TodoReadDto?> GetTodoByIdAsync(int id)
    {
        var todo = await _todoRepository.GetByIdAsync(id);
        if (todo == null) return null;

        return _mapper.Map<TodoReadDto>(todo);
    }

    public async Task<TodoReadDto> AddTodoAsync(long? userId, TodoCreateDto todoDto)
    {
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("A valid user is required.");

        var newTodo = _mapper.Map<Todo>(todoDto);
        newTodo.UserId = userId.Value;
        newTodo.CreatedDate = DateTime.UtcNow;
        newTodo.UpdatedDate = DateTime.UtcNow;

        await _todoRepository.AddAsync(newTodo);
        return _mapper.Map<TodoReadDto>(newTodo);
    }

    public async Task<bool> UpdateTodoAsync(int id, TodoUpdateDto todoDto)
    {
        var existingTodo = await _todoRepository.GetByIdAsync(id);
        if (existingTodo == null) return false;

        _mapper.Map(todoDto, existingTodo);
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
