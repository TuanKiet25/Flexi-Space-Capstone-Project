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
            CreateMap<Space, GetAllSpace>().ReverseMap();
            CreateMap<CreateSpaceRQ, Space>()
                .ForMember(dest => dest.OperatingHour, opt => opt.MapFrom(src => src.OperatingHours))
                .ForMember(dest => dest.Amenity, opt => opt.MapFrom(src => src.Amenities))
                .ForMember(dest => dest.SpaceAllowedCategory, opt => opt.MapFrom(src => src.SpaceAllowedCategories))
                .ReverseMap();
        }
    }
}
