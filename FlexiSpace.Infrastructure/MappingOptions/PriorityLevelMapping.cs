using AutoMapper;
using FlexiSpace.Application.ViewModels.Requests.PriorityLevelRQ;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.MappingOptions
{
    public class PriorityLevelMapping : Profile
    {
        public PriorityLevelMapping()
        {
            CreateMap<CreatePriorityLevel, PriorityLevel>().ReverseMap();
            CreateMap<PriorityLevel, GetAllPriorityLevel>().ReverseMap();
        }
    }
}
