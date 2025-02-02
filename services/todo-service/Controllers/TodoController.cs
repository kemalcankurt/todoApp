using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using todo_service.DTOs;
using todo_service.Models;
using todo_service.Services;

namespace todo_service.Controllers
{
    [Authorize]
    [Route("api/todo")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;
        private readonly JwtService _jwtService;

        public TodoController(ITodoService todoService, JwtService jwtService)
        {
            _todoService = todoService;
            _jwtService = jwtService;
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdminRole")]
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
            string? token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { message = "A valid token is required." });

            long? userId = _jwtService.DecodeJwtToken(token);

            if (!userId.HasValue)
                return BadRequest(new { message = "Invalid token or user not found." });

            var addedTodo = await _todoService.AddTodoAsync(userId, todo);
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