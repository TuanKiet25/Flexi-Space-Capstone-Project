using AutoMapper;
using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses.Space;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.MappingOptions
{
    public class SpaceMapping : Profile
    {
        public  SpaceMapping()
        {
            CreateMap<OperatingHourVmodel, OperatingHour>().ReverseMap();
            CreateMap<AmenityVModel, Amentity>().ReverseMap();
            CreateMap<SpaceAllowedCategoryVModel, SpaceAllowedCategory>().ReverseMap();
            CreateMap<GetAllSpace, Space>()
                .ForMember(dest => dest.OperatingHour, opt => opt.MapFrom(src => src.OperatingHours))
                .ForMember(dest => dest.Amenity, opt => opt.MapFrom(src => src.Amenities))
                .ForMember(dest => dest.SpaceAllowedCategory, opt => opt.MapFrom(src => src.SpaceAllowedCategories))
                .ReverseMap();
            CreateMap<Space, GetSpaceByIdRP>()
                .ForMember(dest => dest.OperatingHours, opt => opt.MapFrom(src => src.OperatingHour))
                .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenity))
                .ForMember(dest => dest.SpaceAllowedCategories, opt => opt.MapFrom(src => src.SpaceAllowedCategory))
                .ReverseMap();
            CreateMap<CreateSpaceRQ, Space>()
                .ForMember(dest => dest.OperatingHour, opt => opt.MapFrom(src => src.OperatingHours))
                .ForMember(dest => dest.Amenity, opt => opt.MapFrom(src => src.Amenities))
                .ForMember(dest => dest.SpaceAllowedCategory, opt => opt.MapFrom(src => src.SpaceAllowedCategories))
                .ReverseMap();
            CreateMap<CreateSpaceRQ, CreateSpaceRP>().ReverseMap();
        }
    }
}
