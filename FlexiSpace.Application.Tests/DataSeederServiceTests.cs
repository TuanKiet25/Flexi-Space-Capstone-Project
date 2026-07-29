using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.Services;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Domain.Enum;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class DataSeederServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;

        public DataSeederServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork.SetupGet(u => u.userRepository).Returns(_mockUserRepository.Object);
        }

        [Fact]
        public async Task SeedAdminAccountAsync_MissingEmailOrPassword_DoesNotCreateAdmin()
        {
            // 1. ARRANGE
            var sut = new DataSeederService(_mockUnitOfWork.Object, CreateConfiguration(new Dictionary<string, string?>()));

            // 2. ACT
            await sut.SeedAdminAccountAsync();

            // 3. ASSERT
            _mockUserRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
            _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SeedAdminAccountAsync_AdminAlreadyExists_DoesNotCreateAdmin()
        {
            // 1. ARRANGE
            var sut = new DataSeederService(_mockUnitOfWork.Object, CreateValidConfiguration());
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new User { Email = "admin@test.com", Role = RoleEnum.ADMIN });

            // 2. ACT
            await sut.SeedAdminAccountAsync();

            // 3. ASSERT
            _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SeedAdminAccountAsync_AdminMissing_CreatesAdminAccount()
        {
            // 1. ARRANGE
            var sut = new DataSeederService(_mockUnitOfWork.Object, CreateValidConfiguration());
            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User)null!);
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // 2. ACT
            await sut.SeedAdminAccountAsync();

            // 3. ASSERT
            _mockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == "admin@test.com" &&
                u.Name == "Admin" &&
                u.PhoneNumber == "0123456789" &&
                u.Role == RoleEnum.ADMIN &&
                u.IsActive)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        private static IConfiguration CreateValidConfiguration() =>
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["AdminAccount:Email"] = "admin@test.com",
                ["AdminAccount:Password"] = "Password123!",
                ["AdminAccount:Name"] = "Admin",
                ["AdminAccount:PhoneNumber"] = "0123456789"
            });

        private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
    }
}
