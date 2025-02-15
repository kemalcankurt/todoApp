namespace todo_service.DTOs
{
    public class TodoCreateDto
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
}