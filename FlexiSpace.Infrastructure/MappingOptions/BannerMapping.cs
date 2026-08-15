using AutoMapper;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;

namespace FlexiSpace.Infrastructure.MappingOptions
{
    public class BannerMapping : Profile
    {
        public BannerMapping()
        {
            CreateMap<CreateBannerRequest, Banner>();
            CreateMap<UpdateBannerRequest, Banner>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Banner, BannerResponse>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.PictureURL != null ? src.PictureURL.ImageUrl : null));
        }
    }
}
