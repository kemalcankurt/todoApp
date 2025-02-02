using AutoMapper;
using todo_service.DTOs;
using todo_service.Models;

namespace todo_service.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity → DTO and DTO → Entity
            CreateMap<Todo, TodoReadDto>().ReverseMap();
            CreateMap<TodoCreateDto, Todo>();
            CreateMap<TodoUpdateDto, Todo>();
        }
    }
}