namespace todo_service.DTOs
{
    public class TodoUpdateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
    }
}