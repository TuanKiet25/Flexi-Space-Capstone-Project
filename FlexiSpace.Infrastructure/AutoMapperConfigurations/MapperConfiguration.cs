using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.AutoMapperConfigurations
{
    public class MapperConfiguration : Profile
    {
        public MapperConfiguration()
        {
            CreateMap<Listing, ListingResponse>()
                .ForMember(dest => dest.ListingPictures, opt => opt.MapFrom(src => src.PictureURLs))
                .ReverseMap();
            CreateMap<ListingRequest, Listing>().ReverseMap();
           


            CreateMap<PrimaryBookingRequest, BookingResponse>();
            CreateMap<BookingRequest, PrimaryBookingRequest>();

            CreateMap<Contract, ContractResponse>()
                .ForMember(dest => dest.ContractSchedules, opt => opt.MapFrom(src => src.ContractSchedules));
            CreateMap<ContractRequest, Contract>();
            CreateMap<ContractSchedule, ContractScheduleRequest>().ReverseMap();

            CreateMap<Amentity, AmenityResponse>().ReverseMap();
            
            CreateMap<SharedSpaceAmenities, ShareSpaceAmenitiesResponse>().ReverseMap();
            CreateMap<ShareSpaceAmenitiesRequest, SharedSpaceAmenities>().ReverseMap();
            CreateMap<AvailabilitiesTime, AvailabilitiesResponse>().ReverseMap();
            CreateMap<AvailabilitiesTimeRequest, AvailabilitiesTime>().ReverseMap();
            CreateMap<ShareSpaceCategory, ShareSpaceCategoryResponse>().ReverseMap();
            CreateMap<ShareSpaceCategoryRequest, ShareSpaceCategory>().ReverseMap();
            CreateMap<SharedListingRequest, Listing>().ReverseMap();
            CreateMap<Listing, ShareListingResponse>()
                .ForMember(dest => dest.ListingPictures, opt => opt.MapFrom(src => src.PictureURLs))
                .ReverseMap();
            
            CreateMap<Wallet, WalletRespnse>().ReverseMap();
            CreateMap<User, UserResponse>().ReverseMap();
            CreateMap<ProfileRequest, UserProfile>().ReverseMap();
            CreateMap<UserProfile, ProfileResponse>().ReverseMap();

            CreateMap<Message, MessageResponse>().ReverseMap();
            CreateMap<ListingReport, ListingReportResponse>()
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reporter != null ? (src.Reporter.UserName ?? src.Reporter.Profile.FullName) : string.Empty))
                .ForMember(dest => dest.Reasons, opt => opt.MapFrom(src => ParseReasons(src.ReasonType)))
                .ReverseMap();

            CreateMap<VerifyProfileRequest, UserProfile>().ReverseMap();
            CreateMap<Conversation, ConversationResp>()
                .ForMember(dest => dest.LessorUserName, opt => opt.MapFrom(src => src.Lessor.UserName))
                .ForMember(dest => dest.LesseeUserName, opt => opt.MapFrom(src => src.Lessee.UserName))
                .ReverseMap();

        }
        private static List<ReportReasonEnum> ParseReasons(string reasons)
        {
            if (string.IsNullOrWhiteSpace(reasons))
            {
                return new List<ReportReasonEnum>();
            }

            return reasons
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => Enum.TryParse<ReportReasonEnum>(x, out var reason) ? reason : ReportReasonEnum.Other)
                .ToList();
        }
    }
}
