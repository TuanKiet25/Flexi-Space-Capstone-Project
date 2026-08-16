using AutoMapper;
using FluentAssertions;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
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
    public class FavoriteListServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IFavoriteListRepository> _mockFavoriteListRepository;
        private readonly Mock<IListingRepository> _mockListingRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly FavoriteListService _sut;

        public FavoriteListServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockFavoriteListRepository = new Mock<IFavoriteListRepository>();
            _mockListingRepository = new Mock<IListingRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.SetupGet(u => u.favoriteListRepository).Returns(_mockFavoriteListRepository.Object);
            _mockUnitOfWork.SetupGet(u => u.listingRepository).Returns(_mockListingRepository.Object);

            _sut = new FavoriteListService(_mockUnitOfWork.Object, _mockMapper.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task AddListingsAsync_MissingUserId_ReturnsFailedResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns(string.Empty);

            // 2. ACT
            var result = await _sut.AddListingsAsync(new[] { 1L });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("UserId");
            _mockListingRepository.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task AddListingsAsync_MissingListing_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing> { new() { Id = 1 } });

            // 2. ACT
            var result = await _sut.AddListingsAsync(new[] { 1L, 2L });

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Contain("2");
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddListingsAsync_NewFavoriteList_CreatesListAndReturnsSuccessResult()
        {
            // 1. ARRANGE
            var mapped = new FavoriteListResponse { Id = 10, UserId = "user-1" };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockListingRepository
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Listing, bool>>>()))
                .ReturnsAsync(new List<Listing> { new() { Id = 1 }, new() { Id = 2 } });
            _mockFavoriteListRepository
                .SetupSequence(r => r.GetAsync(
                    It.IsAny<Expression<Func<FavoriteList, bool>>>(),
                    It.IsAny<Func<IQueryable<FavoriteList>, IIncludableQueryable<FavoriteList, object>>>()))
                .ReturnsAsync((FavoriteList)null!)
                .ReturnsAsync(new FavoriteList
                {
                    Id = 10,
                    UserId = "user-1",
                    FavoriteListings = new List<FavoriteListing>
                    {
                        new() { ListingId = 1 },
                        new() { ListingId = 2 }
                    }
                });
            _mockFavoriteListRepository
                .Setup(r => r.AddAsync(It.IsAny<FavoriteList>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<FavoriteListResponse>(It.IsAny<FavoriteList>()))
                .Returns(mapped);

            // 2. ACT
            var result = await _sut.AddListingsAsync(new[] { 1L, 1L, 2L });

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(mapped);
            result.Message.Should().Contain("Đã thêm");
            _mockFavoriteListRepository.Verify(r => r.AddAsync(It.Is<FavoriteList>(f =>
                f.UserId == "user-1" &&
                f.FavoriteListings.Select(x => x.ListingId).OrderBy(x => x).SequenceEqual(new[] { 1L, 2L }))), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByUserIdAsync_FavoriteListMissing_ReturnsNotFoundResult()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockFavoriteListRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<FavoriteList, bool>>>(),
                    It.IsAny<Func<IQueryable<FavoriteList>, IIncludableQueryable<FavoriteList, object>>>()))
                .ReturnsAsync((FavoriteList)null!);

            // 2. ACT
            var result = await _sut.GetByUserIdAsync();

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
            result.Message.Should().Contain("chưa có danh sách");
        }

        [Fact]
        public async Task GetListingDetailAsync_FavoriteExists_ReturnsMappedListing()
        {
            // 1. ARRANGE
            var listing = new Listing { Id = 7 };
            var favoriteList = new FavoriteList
            {
                UserId = "user-1",
                FavoriteListings = new List<FavoriteListing>
                {
                    new() { ListingId = 7, Listing = listing }
                }
            };
            var response = new ListingResponse
            {
                Id = 7,
                CreatorId = "owner-1",
                LessorName = "Owner",
                SpaceAddress = "Address",
                SpaceCity = "City"
            };

            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockFavoriteListRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<FavoriteList, bool>>>(),
                    It.IsAny<Func<IQueryable<FavoriteList>, IIncludableQueryable<FavoriteList, object>>>()))
                .ReturnsAsync(favoriteList);
            _mockMapper
                .Setup(m => m.Map<ListingResponse>(listing))
                .Returns(response);

            // 2. ACT
            var result = await _sut.GetListingDetailAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(response);
        }

        [Fact]
        public async Task GetListingDetailAsync_FavoriteMissing_ReturnsNotFound()
        {
            // 1. ARRANGE
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockFavoriteListRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<FavoriteList, bool>>>(),
                    It.IsAny<Func<IQueryable<FavoriteList>, IIncludableQueryable<FavoriteList, object>>>()))
                .ReturnsAsync(new FavoriteList { UserId = "user-1", FavoriteListings = new List<FavoriteListing>() });

            // 2. ACT
            var result = await _sut.GetListingDetailAsync(7);

            // 3. ASSERT
            result.IsSuccess.Should().BeFalse();
            result.IsNotFound.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveListingAsync_ExistingFavorite_RemovesListingAndReturnsUpdatedIds()
        {
            // 1. ARRANGE
            var favorite = new FavoriteListing { ListingId = 2 };
            var favoriteList = new FavoriteList
            {
                Id = 10,
                UserId = "user-1",
                FavoriteListings = new List<FavoriteListing>
                {
                    new() { ListingId = 1 },
                    favorite
                }
            };
            var mapped = new FavoriteListIdsResponse { Id = 10, UserId = "user-1", ListingIds = new List<long> { 1 } };
            _mockCurrentUserService.SetupGet(s => s.UserId).Returns("user-1");
            _mockFavoriteListRepository
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<FavoriteList, bool>>>(),
                    It.IsAny<Func<IQueryable<FavoriteList>, IIncludableQueryable<FavoriteList, object>>>()))
                .ReturnsAsync(favoriteList);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockMapper
                .Setup(m => m.Map<FavoriteListIdsResponse>(favoriteList))
                .Returns(mapped);

            // 2. ACT
            var result = await _sut.RemoveListingAsync(2);

            // 3. ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(mapped);
            favoriteList.FavoriteListings.Should().NotContain(favorite);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
