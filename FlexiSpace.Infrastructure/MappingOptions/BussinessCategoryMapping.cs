using AutoMapper;
using FlexiSpace.Application.ViewModels.Requests.BussinessCategoryRQ;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.MappingOptions
{
    public class BussinessCategoryMapping : Profile
    {
        public BussinessCategoryMapping()
        {
            CreateMap<CreateBussinessCategory, BussinessCategory>().ReverseMap();
            CreateMap<BussinessCategory, GetAllBussinessCategory>().ReverseMap();
        }
    }
}
