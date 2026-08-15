using AutoMapper;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Requests.Space;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Application.ViewModels.Responses.Space;
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
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => src.viewCount))
                .ReverseMap();
            CreateMap<ListingRequest, Listing>().ReverseMap();
            CreateMap<FavoriteList, FavoriteListResponse>()
                .ForMember(dest => dest.Listings,
                    opt => opt.MapFrom(src => src.FavoriteListings.Select(x => x.Listing)));
           


            CreateMap<PrimaryBookingRequest, BookingResponse>();
            CreateMap<BookingRequest, PrimaryBookingRequest>();

            CreateMap<Contract, ContractResponse>()
                .ForMember(dest => dest.ContractSchedules, opt => opt.MapFrom(src => src.ContractSchedules))
                .ForMember(dest => dest.PictureURLs, opt => opt.MapFrom(src => src.PictureURLs))
                .ForMember(dest => dest.CurrentUserContractRole, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentUserCanShareSpace, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentUserCanGrantSharePermission, opt => opt.Ignore())
                .ForMember(dto => dto.LessorNickName, opt => opt.MapFrom(entity => entity.Lessor.Name))
                .ForMember(dto => dto.LesseeNickName, opt => opt.MapFrom(entity => entity.Lessee.Name));
            CreateMap<ContractRequest, Contract>();
            CreateMap<CreateExternalContractRequest, Contract>()
                .ForMember(dest => dest.LessorId, opt => opt.MapFrom((src, dest, member, context) => context.Items["CurrentUserId"]))
                .ForMember(dest => dest.Acreage, opt => opt.MapFrom((src, dest, member, context) => context.Items["SpaceArea"]))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(_ => DateOnly.FromDateTime(DateTime.UtcNow)))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(_ => NormalizeDate(DateTime.UtcNow.Date)))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(_ => NormalizeDate(DateTime.MaxValue.Date)))
                .ForMember(dest => dest.DurationUnit, opt => opt.MapFrom(_ => DurationUnitEnum.Days))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(_ => 1))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.DepositAmount, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(_ => "External contract document images"))
                .ForMember(dest => dest.BusinessPurpose, opt => opt.MapFrom(_ => (string?)null))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(_ => ContractSource.External))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ContractStatusEnum.PendingExternalVerification))
                .ForMember(dest => dest.ContractSnapshot, opt => opt.MapFrom(_ => string.Empty))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.PictureURLs, opt => opt.Ignore())
                .ForMember(dest => dest.ContractVerification, opt => opt.Ignore())
                .ForMember(dest => dest.ContractSchedules, opt => opt.Ignore());
            CreateMap<Contract, Message>()
                .ForMember(dest => dest.ConversationId, opt => opt.MapFrom(src => src.ConversationId))
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.LessorId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.MessageType, opt => opt.MapFrom(_ => MessageTypeEnum.ContractProposal))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(_ => false));
            CreateMap<ContractSchedule, ContractScheduleRequest>().ReverseMap();

            CreateMap<Amentity, AmenityResponse>().ReverseMap();
            
            CreateMap<SharedSpaceAmenities, ShareSpaceAmenitiesResponse>().ReverseMap();
            CreateMap<ShareSpaceAmenitiesRequest, SharedSpaceAmenities>().ReverseMap();
            CreateMap<AvailabilitiesTime, AvailabilitiesResponse>().ReverseMap();
            CreateMap<AvailabilitiesTimeRequest, AvailabilitiesTime>()
                .ForMember(dest => dest.DaysOfWeek, opt => opt.MapFrom(src => src.DaysOfWeek ?? new List<DayOfWeek>()))
                .ReverseMap();
            CreateMap<ShareSpaceCategory, ShareSpaceCategoryResponse>().ReverseMap();
            CreateMap<ShareSpaceCategoryRequest, ShareSpaceCategory>().ReverseMap();
            CreateMap<SharedListingRequest, Listing>().ReverseMap();
            CreateMap<Listing, ShareListingResponse>()
                .ForMember(dest => dest.ListingPictures, opt => opt.MapFrom(src => src.PictureURLs))
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => src.viewCount))
                .ReverseMap();
            
            CreateMap<Wallet, WalletRespnse>().ReverseMap();
            CreateMap<TransactionHistory, TransactionHistoryResponse>().ReverseMap();
            CreateMap<User, UserResponse>().ReverseMap();
            CreateMap<ProfileRequest, UserProfile>().ReverseMap();
            CreateMap<UserProfile, ProfileResponse>().ReverseMap();

            CreateMap<Message, MessageResponse>().ReverseMap();
            CreateMap<Notification, NotificationResponse>();
            CreateMap<ListingReport, ListingReportResponse>()
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reporter != null ? (src.Reporter.UserName ?? src.Reporter.Profile.FullName) : string.Empty))
                .ForMember(dest => dest.Reasons, opt => opt.MapFrom(src => ParseReasons(src.ReasonType)))
                .ReverseMap();

            CreateMap<VerifyProfileRequest, UserProfile>().ReverseMap();
            CreateMap<Conversation, ConversationResp>()
                .ForMember(dest => dest.LessorUserName, opt => opt.MapFrom(src => src.Lessor.UserName))
                .ForMember(dest => dest.LesseeUserName, opt => opt.MapFrom(src => src.Lessee.UserName))
                .ReverseMap();
            CreateMap<FavoriteList, FavoriteListIdsResponse>().ForMember(dest => dest.ListingIds,
               opt => opt.MapFrom(src => src.FavoriteListings.Select(x => x.ListingId).ToList()));

            CreateMap<Review, ReviewResponse>()
                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src => src.Reviewer != null ? (src.Reviewer.Profile != null ? src.Reviewer.Profile.FullName : src.Reviewer.UserName) : string.Empty))
                .ForMember(dest => dest.TargetUserName, opt => opt.MapFrom(src => src.TargetUser != null ? (src.TargetUser.Profile != null ? src.TargetUser.Profile.FullName : src.TargetUser.UserName) : null))
                .ForMember(dest => dest.SpaceId, opt => opt.MapFrom(src => src.PrimaryBookingRequest != null ? src.PrimaryBookingRequest.SpaceId : (long?)null))
                .ForMember(dest => dest.SpaceAddress, opt => opt.MapFrom(src => src.PrimaryBookingRequest != null && src.PrimaryBookingRequest.Space != null ? src.PrimaryBookingRequest.Space.Address : null));
            CreateMap<GrantSpaceUsageRightRequest, SpaceUsageRight>()
                .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => NormalizeDate(src.ValidFrom)))
                .ForMember(dest => dest.ValidTo, opt => opt.MapFrom(src => NormalizeDate(src.ValidTo)));
            CreateMap<UpdateSpaceUsageRightPermissionRequest, SpaceUsageRight>()
                .ForMember(dest => dest.CanShare, opt => opt.MapFrom(src => src.CanShare))
                .ForMember(dest => dest.CanGrantSharePermission, opt => opt.MapFrom(src => src.CanGrantSharePermission))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<SpaceUsageRight, SpaceUsageRightResponse>();


        }
        private static DateTime NormalizeDate(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
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
