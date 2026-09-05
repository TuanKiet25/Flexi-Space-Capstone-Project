using FlexiSpace.Application.IServices;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Services
{
    public class DataSeederService : IDataSeederService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public DataSeederService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task SeedAdminAccountAsync()
        {
            var hasChanges = false;
            var adminEmail = _configuration["AdminAccount:Email"];
            var adminPassword = _configuration["AdminAccount:Password"];
            var adminName = _configuration["AdminAccount:Name"];
            var adminPhone = _configuration["AdminAccount:PhoneNumber"];

            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                var existingAdmin = await _unitOfWork.userRepository.GetAsync(u => u.Email == adminEmail);
                if (existingAdmin == null)
                {
                    var adminUser = new User
                    {
                        Email = adminEmail,
                        Name = adminName ?? "Administrator",
                        Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                        IsActive = true,
                        UserStatus = UserStatus.Active,
                        Role = RoleEnum.ADMIN,
                        CreatedAt = DateTime.UtcNow,
                        PhoneNumber = adminPhone ?? "0123456789"
                    };

                    await _unitOfWork.userRepository.AddAsync(adminUser);
                    hasChanges = true;
                }
            }

            var ownerSeed = await SeedUserAsync("MockAccounts:Owner", "owner@flexispace.com", "FlexiSpace Owner");
            var owner = ownerSeed.User;
            hasChanges = hasChanges || ownerSeed.Created;
            if (owner != null)
            {
                var existingSpace = await _unitOfWork.spaceRepository.GetAsync(s => s.OwnerId == owner.UserId && !s.IsDeleted);
                if (existingSpace == null)
                {
                    await _unitOfWork.spaceRepository.AddAsync(new Space
                    {
                        OwnerId = owner.UserId,
                        Name = "FlexiSpace Owner Workspace",
                        Address = "123 Nguyen Hue",
                        City = "Ho Chi Minh City",
                        Area = 80,
                        SpacePictures = string.Empty,
                        Latitude = 10.7769,
                        Longitude = 106.7009,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    hasChanges = true;
                }
            }

            var renterSeed = await SeedUserAsync("MockAccounts:Renter", "renter@flexispace.com", "FlexiSpace Renter");
            hasChanges = hasChanges || renterSeed.Created;

            if (hasChanges)
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task<(User? User, bool Created)> SeedUserAsync(string configurationKey, string defaultEmail, string defaultName)
        {
            var email = _configuration[$"{configurationKey}:Email"];
            var password = _configuration[$"{configurationKey}:Password"];
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return (null, false);
            }

            var existingUser = await _unitOfWork.userRepository.GetAsync(u => u.Email == email);
            if (existingUser != null)
            {
                return (existingUser, false);
            }

            var user = new User
            {
                Email = email,
                Name = _configuration[$"{configurationKey}:Name"] ?? defaultName,
                UserName = _configuration[$"{configurationKey}:UserName"] ?? email,
                PhoneNumber = _configuration[$"{configurationKey}:PhoneNumber"] ?? "0900000000",
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                UserStatus = UserStatus.Active,
                Role = RoleEnum.USER,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.userRepository.AddAsync(user);
            return (user, true);
        }
    }
}