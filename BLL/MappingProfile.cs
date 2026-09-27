using AutoMapper;
using BLL.DTO;
using DAL.Entities;
using System;    
using System.Collections.Generic;    
using System.Linq;    

namespace BLL.MappingProfiles     
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AnimalDTO, Animal>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details))
                .ForMember(dest => dest.Vaccinations, opt => opt.MapFrom(src => src.Vaccinations))
                .ForMember(dest => dest.Adoptions, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));


            CreateMap<Animal, AnimalDTO>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details))
                .ForMember(dest => dest.Vaccinations, opt => opt.MapFrom(src => src.Vaccinations ?? new List<Vaccination>()));     

            CreateMap<AnimalDetailsDTO, AnimalDetails>().ReverseMap();

            CreateMap<VaccinationDTO, Vaccination>().ReverseMap();

            CreateMap<AdoptionDTO, Adoption>()
                .ForMember(dest => dest.Animal, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<Adoption, AdoptionDTO>()
                .ForMember(dest => dest.AnimalName, opt => opt.MapFrom(src => src.Animal != null ? src.Animal.Name : "N/A"))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));

            CreateMap<ReviewDTO, Review>()
                .ForMember(dest => dest.Animal, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());
            CreateMap<Review, ReviewDTO>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Аноним"))
                .ForMember(dest => dest.AnimalName, opt => opt.MapFrom(src => src.Animal != null ? src.Animal.Name : "N/A"));

            CreateMap<ApplicationUser, UserDTO>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
        }
    }
}