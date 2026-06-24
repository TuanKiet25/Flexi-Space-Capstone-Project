using AutoMapper;
using FlexiSpace.Application.ViewModels.Requests;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
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
            CreateMap<Listing, ListingResponse>().ReverseMap();
            CreateMap<ListingRequest, Listing>().ReverseMap();
           


            CreateMap<PrimaryBookingRequest, BookingResponse>();
            CreateMap<BookingRequest, PrimaryBookingRequest>();

            CreateMap<Contract, ContractResponse>();
            CreateMap<ContractRequest, Contract>();

            CreateMap<Amentity, AmenityResponse>().ReverseMap();
            
            CreateMap<SharedSpaceAmenities, ShareSpaceAmenitiesResponse>().ReverseMap();
            CreateMap<ShareSpaceAmenitiesRequest, SharedSpaceAmenities>().ReverseMap();
            CreateMap<AvailabilitiesTime, AvailabilitiesResponse>().ReverseMap();
            CreateMap<AvailabilitiesTimeRequest, AvailabilitiesTime>().ReverseMap();
            CreateMap<ShareSpaceCategory, ShareSpaceCategoryResponse>().ReverseMap();
            CreateMap<ShareSpaceCategoryRequest, ShareSpaceCategory>().ReverseMap();
            CreateMap<SharedListingRequest, Listing>().ReverseMap();
            CreateMap<Listing, ShareListingResponse>().ReverseMap();
        }
    }
}
