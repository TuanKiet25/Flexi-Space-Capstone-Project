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

        public async Task<List<PictureURLVModel>> UploadImagesAsync(List<IFormFile> files, long? spaceId, string? userProfileId, long? listingId)
        {
            if (spaceId == null && string.IsNullOrEmpty(userProfileId) && listingId == null)
            {
                throw new Exception("At least one target (SpaceId, UserProfileId, or ListingId) must be specified.");
            }

            if (spaceId.HasValue)
            {
                var space = await _unitOfWork.spaceRepository.GetAsync(s => s.Id == spaceId.Value) ?? throw new Exception("Space not found");
            }

            if (!string.IsNullOrEmpty(userProfileId))
            {
                var userProfile = await _unitOfWork.profileRepository.GetAsync(u => u.UserId == userProfileId) ?? throw new Exception("User not found");
            }

            if (listingId.HasValue)
            {
                var listing = await _unitOfWork.listingRepository.GetAsync(l => l.Id == listingId.Value) ?? throw new Exception("Listing not found");
            }

            var Images = new List<PictureURL>();

            if (files == null || !files.Any())
                throw new Exception("No files provided for upload.");

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
                    Images.Add(new PictureURL
                    {
                        ImageUrl = uploadResult.SecureUrl.AbsoluteUri,
                        PublicId = uploadResult.PublicId,
                        IsPrimary = false,
                        SpaceId = spaceId,
                        UserProfileId = userProfileId,
                        ListingId = listingId
                    });
                }
            }

            if (Images.Any())
            {
                await _db.AddRangeAsync(Images);
            }
            await _db.SaveChangesAsync();
            
            var result = _mapper.Map<List<PictureURLVModel>>(Images);

            return result;
        }
    }
}
