using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.Services;
using FlexiSpace.Application.ViewModels.Responses;
using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FlexiSpace.Application.Tests
{
    public class AmenityServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ISpaceRepository> _mockSpaceRepository;
        private readonly Mock<IAmentityRepository> _mockAmenityRepository;
        private readonly IMapper _mapper;
        private readonly AmenityService _sut;

        public AmenityServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSpaceRepository = new Mock<ISpaceRepository>();
            _mockAmenityRepository = new Mock<IAmentityRepository>();

            _mockUnitOfWork.SetupGet(u => u.spaceRepository).Returns(_mockSpaceRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.amenityRepository).Returns(_mockAmenityRepository.Object);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Amentity, AmenityResponse>();
            });
            _mapper = mapperConfig.CreateMapper();

            _sut = new AmenityService(_mockUnitOfWork.Object, _mapper);
        }

        [Fact]
        public async Task GetAllAmenitiesBySpaceIdAsync_SpaceNotFound_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            var spaceId = 123L;
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ReturnsAsync((Space)null!);

            // 2. ACT
            var result = await _sut.GetAllAmenitiesBySpaceIdAsync(spaceId);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Contain("Không tìm thấy mặt bằng với ID đã cho.");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAmenitiesBySpaceIdAsync_SpaceFoundButNoAmenities_ReturnsEmptyList()
        {
            // 1. ARRANGE
            var spaceId = 123L;
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ReturnsAsync(new Space { Id = spaceId });
            _mockAmenityRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Amentity, bool>>>() ))
                .ReturnsAsync(new List<Amentity>());

            // 2. ACT
            var result = await _sut.GetAllAmenitiesBySpaceIdAsync(spaceId);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Message.Should().Contain("Không có tiện ích nào cho mặt bằng này.");
        }

        [Fact]
        public async Task GetAllAmenitiesBySpaceIdAsync_SpaceFoundWithAmenities_ReturnsAmenityResponses()
        {
            // 1. ARRANGE
            var spaceId = 123L;
            var amenities = new List<Amentity>
            {
                new Amentity { Id = 1, SpaceId = spaceId, Quantity = 2 },
                new Amentity { Id = 2, SpaceId = spaceId, Quantity = 5 }
            };

            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ReturnsAsync(new Space { Id = spaceId });
            _mockAmenityRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Amentity, bool>>>() ))
                .ReturnsAsync(amenities);

            // 2. ACT
            var result = await _sut.GetAllAmenitiesBySpaceIdAsync(spaceId);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data!.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1L, 2L });
            result.Data.Select(x => x.SpaceId).Should().OnlyContain(id => id == spaceId);
            result.Data.Select(x => x.Quantity).Should().BeEquivalentTo(new[] { 2, 5 });
        }

        [Fact]
        public async Task GetAllAmenitiesBySpaceIdAsync_SpaceFoundAndAmenityRepositoryReturnsNull_ReturnsEmptyList()
        {
            // 1. ARRANGE
            var spaceId = 123L;
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ReturnsAsync(new Space { Id = spaceId });
            _mockAmenityRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Amentity, bool>>>() ))
                .ReturnsAsync((List<Amentity>)null!);

            // 2. ACT
            var result = await _sut.GetAllAmenitiesBySpaceIdAsync(spaceId);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Message.Should().Contain("Không có tiện ích nào cho mặt bằng này.");
        }

        [Fact]
        public async Task GetAllAmenitiesBySpaceIdAsync_SpaceFound_CallsAmenityRepositoryWithCorrectFilter()
        {
            // 1. ARRANGE
            var spaceId = 123L;
            var amenities = new List<Amentity>
            {
                new Amentity { Id = 1, SpaceId = spaceId, Quantity = 2 }
            };

            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ReturnsAsync(new Space { Id = spaceId });
            _mockAmenityRepository
                .Setup(r => r.GetAllAsync(It.Is<Expression<Func<Amentity, bool>>>(expr => expr.Compile()(new Amentity { SpaceId = spaceId }))))
                .ReturnsAsync(amenities);

            // 2. ACT
            var result = await _sut.GetAllAmenitiesBySpaceIdAsync(spaceId);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().SpaceId.Should().Be(spaceId);
        }

        [Fact]
        public async Task GetAllAmenitiesBySpaceIdAsync_RepositoryThrowsException_ReturnsFailedResult()
        {
            // 1. ARRANGE
            var spaceId = 123L;
            _mockSpaceRepository
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Space, bool>>>() ))
                .ThrowsAsync(new InvalidOperationException("Db failure"));

            // 2. ACT
            var result = await _sut.GetAllAmenitiesBySpaceIdAsync(spaceId);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Lỗi khi lấy danh sách tiện ích: Db failure");
            result.Data.Should().BeNull();
        }
    }
}
