using Microsoft.AspNetCore.Mvc;

using todo_service.DTOs;
using todo_service.Models;
using todo_service.Services;

namespace todo_service.Controllers
{
    [Route("api/todo")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Todo>>> GetTodos()
        {
            var todos = await _todoService.GetAllTodosAsync();
            return Ok(todos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Todo>> GetTodoById(int id)
        {
            var todo = await _todoService.GetTodoByIdAsync(id);
            if (todo == null)
                return NotFound();

            return Ok(todo);
        }

        [HttpPost]
        public async Task<ActionResult<TodoReadDto>> CreateTodo(TodoCreateDto todo)
        {
            var addedTodo = await _todoService.AddTodoAsync(todo);
            return CreatedAtAction(nameof(GetTodoById), new { id = addedTodo.Id }, addedTodo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, TodoUpdateDto todoDto)
        {
            if (!await _todoService.UpdateTodoAsync(id, todoDto))
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            if (!await _todoService.DeleteTodoAsync(id))
                return NotFound();

            return NoContent();
        }
    }
}