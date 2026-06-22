using AutoMapper;
using FlexiSpace.Application.ViewModels;
using FlexiSpace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Infrastructure.MappingOptions
{
    public class PictureURLMapping : Profile
    {
        public PictureURLMapping()
        {
            CreateMap<PictureURLVModel, PictureURL>().ReverseMap();
        }
    }
}
