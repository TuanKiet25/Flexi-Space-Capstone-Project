using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FlexiSpace.Application;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.ViewModels;
using FlexiSpace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace FlexiSpace.Infrastructure.Services
{
    public class PictureURLService : IPictureURL
    {
        private readonly Cloudinary _cloudinary;
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public PictureURLService(Cloudinary cloudinary, AppDbContext db, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _cloudinary = cloudinary;
            _db = db;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }


        public async Task<bool> DeleteImageFromCloudAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }

        public async Task<List<PictureURLVModel>> UploadImagesAsync(List<IFormFile> files, long? spaceId)
        {
            if(spaceId == null) throw new Exception("SpaceId is required");
            var space = await _unitOfWork.spaceRepository.GetAsync(s => s.Id == spaceId.Value) ?? throw new Exception("Space not found");

            var spaceImages = new List<PictureURL>();

            if (files == null || !files.Any())
                throw new Exception("null");

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
                    Folder = "flexispace_assets"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error == null)
                {
                    // Trả ra Object PictureURL đã điền sẵn URL và PublicId từ mây về
                    // Lưu ý: Trường SpaceId lúc này chưa có (bằng Guid.Empty) vì chưa biết thuộc về Space nào
                    spaceImages.Add(new PictureURL
                    {
                        ImageUrl = uploadResult.SecureUrl.AbsoluteUri,
                        PublicId = uploadResult.PublicId,
                        IsPrimary = false,
                        SpaceId = spaceId
                    });
                }

                await _db.AddRangeAsync(spaceImages);
            }
            await _db.SaveChangesAsync();
            
            var result = _mapper.Map<List<PictureURLVModel>>(spaceImages);

            return result;
        }
    }
}
