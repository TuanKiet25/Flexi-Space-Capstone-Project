using CloudinaryDotNet.Actions;
using FlexiSpace.Application.ViewModels;
using FlexiSpace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexiSpace.Application.IServices
{
    public interface IPictureURL
    {
        Task<List<PictureURLVModel>> UploadImagesAsync(List<IFormFile> files, long spaceId);
        Task<bool> DeleteImageFromCloudAsync(string publicId);
    }
}
