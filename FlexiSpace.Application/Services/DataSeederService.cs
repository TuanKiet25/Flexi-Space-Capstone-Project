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
            var adminEmail = _configuration["AdminAccount:Email"];
            var adminPassword = _configuration["AdminAccount:Password"];
            var adminName = _configuration["AdminAccount:Name"];
            var adminPhone = _configuration["AdminAccount:PhoneNumber"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                return; // Nếu không cấu hình thì bỏ qua
            }

            var existingAdmin = await _unitOfWork.userRepository.GetAsync(u => u.Email == adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    Email = adminEmail,
                    Name = adminName ?? "Administrator",
                    Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    IsActive = true, 
                    Role = RoleEnum.ADMIN,
                    CreatedAt = DateTime.UtcNow,
                    PhoneNumber = adminPhone ?? "0123456789"
                };

                await _unitOfWork.userRepository.AddAsync(adminUser);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}