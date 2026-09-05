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
        private readonly Mock<ISpaceRepository> _mockSpaceRepository;

        public DataSeederServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockSpaceRepository = new Mock<ISpaceRepository>();
            _mockUnitOfWork.SetupGet(u => u.userRepository).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
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

        [Fact]
        public async Task SeedAdminAccountAsync_MockAccounts_CreateOwnerSpaceAndRenterWithoutSpace()
        {
            var owner = (User)null!;
            var renter = (User)null!;
            var userLookup = 0;
            var sut = new DataSeederService(_mockUnitOfWork.Object, CreateMockAccountsConfiguration());

            _mockUserRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(() => ++userLookup == 1 ? owner : renter);
            _mockUserRepository
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(user =>
                {
                    if (user.Email == "owner@flexispace.com") owner = user;
                    if (user.Email == "renter@flexispace.com") renter = user;
                })
                .Returns(Task.CompletedTask);
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>()))
                .ReturnsAsync((Space)null!);
            _mockSpaceRepository
                .Setup(r => r.AddAsync(It.IsAny<Space>()))
                .Returns(Task.CompletedTask);

            await sut.SeedAdminAccountAsync();

            _mockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == "owner@flexispace.com" && u.Role == RoleEnum.USER && u.IsActive && u.UserStatus == UserStatus.Active)), Times.Once);
            _mockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == "renter@flexispace.com" && u.Role == RoleEnum.USER && u.IsActive && u.UserStatus == UserStatus.Active)), Times.Once);
            _mockSpaceRepository.Verify(r => r.AddAsync(It.Is<Space>(s =>
                s.OwnerId == owner.UserId && s.IsActive)), Times.Once);
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

        private static IConfiguration CreateMockAccountsConfiguration() =>
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["MockAccounts:Owner:Email"] = "owner@flexispace.com",
                ["MockAccounts:Owner:Password"] = "Password123!",
                ["MockAccounts:Renter:Email"] = "renter@flexispace.com",
                ["MockAccounts:Renter:Password"] = "Password123!"
            });

        private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
    }
}
