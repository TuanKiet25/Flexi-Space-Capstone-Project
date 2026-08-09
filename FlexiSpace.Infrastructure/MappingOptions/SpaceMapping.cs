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
                .ForMember(dest => dest.PictureURL, opt => opt.MapFrom(src => src.PictureURLs))
                .ReverseMap();
            CreateMap<Space, GetSpaceByIdRP>()
                .ForMember(dest => dest.OperatingHours, opt => opt.MapFrom(src => src.OperatingHour))
                .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenity))
                .ForMember(dest => dest.SpaceAllowedCategories, opt => opt.MapFrom(src => src.SpaceAllowedCategory))
                .ForMember(dest => dest.PictureURLs, opt => opt.MapFrom(src => src.PictureURL))
                .ReverseMap();
            CreateMap<CreateSpaceRQ, Space>()
                .ForMember(dest => dest.OperatingHour, opt => opt.MapFrom(src => src.OperatingHours))
                .ForMember(dest => dest.Amenity, opt => opt.MapFrom(src => src.Amenities))
                .ForMember(dest => dest.SpaceAllowedCategory, opt => opt.MapFrom(src => src.SpaceAllowedCategories))
                //.ForMember(dest => dest.PictureURL, opt => opt.MapFrom(src => src.PictureURLs))
                .ReverseMap();
            CreateMap<CreateSpacePartRQ, Space>()
                .ForMember(dest => dest.OperatingHour, opt => opt.MapFrom(src => src.OperatingHours))
                .ForMember(dest => dest.Amenity, opt => opt.MapFrom(src => src.Amenities))
                .ForMember(dest => dest.SpaceAllowedCategory, opt => opt.MapFrom(src => src.SpaceAllowedCategories))
                .AfterMap((src, dest, context) =>
                {
                    if (!context.Items.TryGetValue("ParentSpace", out var parentSpaceValue) || parentSpaceValue is not Space parentSpace)
                    {
                        return;
                    }

                    dest.ParentSpaceId = parentSpace.Id;
                    dest.OwnerId = parentSpace.OwnerId;
                    dest.Address = string.IsNullOrWhiteSpace(src.Address) ? parentSpace.Address : src.Address;
                    dest.City = parentSpace.City;
                    dest.Latitude = src.Latitude ?? parentSpace.Latitude;
                    dest.Longitude = src.Longitude ?? parentSpace.Longitude;
                })
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Space, SpacePartResponse>()
                .ForMember(dest => dest.ParentSpaceName, opt => opt.MapFrom(src => src.ParentSpace != null ? src.ParentSpace.Name : null))
                .ForMember(dest => dest.ParentSpaceId, opt => opt.MapFrom(src => src.ParentSpaceId ?? 0))
                .ForMember(dest => dest.OperatingHours, opt => opt.MapFrom(src => src.OperatingHour))
                .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenity))
                .ForMember(dest => dest.SpaceAllowedCategories, opt => opt.MapFrom(src => src.SpaceAllowedCategory))
                .ForMember(dest => dest.PictureURLs, opt => opt.MapFrom(src => src.PictureURL));
            CreateMap<Space, CreateSpaceRP>()
                .ForMember(dest => dest.OperatingHours, opt => opt.MapFrom(src => src.OperatingHour))
                .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenity))
                .ForMember(dest => dest.SpaceAllowedCategories, opt => opt.MapFrom(src => src.SpaceAllowedCategory))
                .ReverseMap();
        }
    }
}
