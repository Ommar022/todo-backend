using AutoMapper;
using eco_m.DTO;
using TODO.DTO_s.RequestDTO;
using TODO.DTO_s.ResponseDTO;
using TODO.Model;

namespace eco_m.Mapper
{
    public class Mapper : Profile
    {
        public Mapper()
        {
            CreateMap<SignUpDTO, User>();
            CreateMap<TodoRequestDto, Todo>();
            CreateMap<Todo, TodoResponseDto>();
        }
    }
}
