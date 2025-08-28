using FluentAssertions;
using Moq;
using NUnit.Framework;
using RealEstate.Application.Interfaces;
using RealEstate.Application.Services;
using RealEstate.Contracts.Dtos;
using RealEstate.Domain.Entities;

namespace RealEstate.UnitTests
{
    [TestFixture]
    public class PropertyServiceTests
    {
        private Mock<IPropertyRepository> _repo = null!;
        private PropertyService _sut = null!;
        private readonly CancellationToken _ct = CancellationToken.None;

        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<IPropertyRepository>(MockBehavior.Strict);
            _sut = new PropertyService(_repo.Object);
        }

        [Test]
        public async Task GetPropertiesAsync_WhenFilterIsNull_NormalizesAndCallsRepo()
        {
            // Arrange
            var items = new[]
            {
            new Property
            {
                Id = "666aaa000000000000000001",
                IdOwner = "666bbb000000000000000001",
                Name = "Casa Norte",
                Address = "Calle 10 #5-20",
                Price = 350000m,
                CodeInternal = "P-0001",
                Year = 2015,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ImageUrl = "https://example.com/img1.jpg"
            }
        }.ToList();

            _repo.Setup(r => r.GetPagedAsync(
                    It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((items, total: 1L));

            // Act
            var result = await _sut.GetPropertiesAsync(filter: null!, _ct);

            // Assert
            _repo.Verify(r => r.GetPagedAsync(
                It.Is<PropertyFilterDto>(f =>
                    f.Page == 1 &&
                    f.PageSize == 10 &&
                    string.Equals(f.SortBy, "CreatedAt", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()), Times.Once);

            result.Items.Should().HaveCount(1);
            result.Total.Should().Be(1);
            result.Items.First().Name.Should().Be("Casa Norte");
            result.Items.First().ImageUrl.Should().Be("https://example.com/img1.jpg");
        }

        [Test]
        public async Task GetPropertiesAsync_WhenMinGreaterThanMax_ThrowsArgumentException()
        {
            var filter = new PropertyFilterDto(
                Name: null, Address: null,
                MinPrice: 500m, MaxPrice: 100m,
                Page: 1, PageSize: 10,
                SortBy: "CreatedAt", SortDir: "desc");

            Func<Task> act = () => _sut.GetPropertiesAsync(filter, _ct);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*MinPrice*");
        }

        [Test]
        public async Task GetPropertiesAsync_ComputesTotalPagesCorrectly()
        {
            // Arrange
            var filter = new PropertyFilterDto(null, null, null, null, 2, 10, "CreatedAt", "desc");

            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Property>().ToList(), total: 25L));

            // Act
            var result = await _sut.GetPropertiesAsync(filter, _ct);

            // Assert
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.Total.Should().Be(25);
            result.TotalPages.Should().Be(3); // 25/10 -> 3
        }

        [Test]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetByIdAsync("does-not-exist", _ct))
                 .ReturnsAsync((Property?)null);

            var dto = await _sut.GetByIdAsync("does-not-exist", _ct);

            dto.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_WhenFound_MapsToDto()
        {
            var entity = new Property
            {
                Id = "666aaa000000000000000002",
                IdOwner = "666bbb000000000000000002",
                Name = "Apto Cedritos",
                Address = "Cl 140 # 19-40",
                Price = 380000m,
                CodeInternal = "P-0010",
                Year = 2019,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ImageUrl = "https://example.com/pic.jpg"
            };

            _repo.Setup(r => r.GetByIdAsync(entity.Id, _ct))
                 .ReturnsAsync(entity);

            var dto = await _sut.GetByIdAsync(entity.Id, _ct);

            dto.Should().NotBeNull();
            dto!.Id.Should().Be(entity.Id);
            dto.Name.Should().Be("Apto Cedritos");
            dto.ImageUrl.Should().Be("https://example.com/pic.jpg");
        }

        //new
        // A2) Clamping de paginación
        [Test]
        public async Task GetPropertiesAsync_ClampsPageAndPageSize()
        {
            PropertyFilterDto? captured = null;
            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                .Callback<PropertyFilterDto, CancellationToken>((f, _) => captured = f)
                .ReturnsAsync((Enumerable.Empty<Property>().ToList(), 0L));

            var filter = new PropertyFilterDto(null, null, null, null, 0, 1000, null, null);
            await _sut.GetPropertiesAsync(filter, _ct);

            captured.Should().NotBeNull();
            captured!.Page.Should().Be(1);     // 0 → 1
            captured.PageSize.Should().Be(100); // 1000 → 100 (máximo)
            _repo.VerifyAll();
        }

        // A3) Ordenamiento: Price asc
        [Test]
        public async Task GetPropertiesAsync_SortByPriceAsc_MapsCorrectly()
        {
            PropertyFilterDto? captured = null;
            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                .Callback<PropertyFilterDto, CancellationToken>((f, _) => captured = f)
                .ReturnsAsync((Enumerable.Empty<Property>().ToList(), 0L));

            var filter = new PropertyFilterDto(null, null, null, null, 1, 10, "Price", "asc");
            await _sut.GetPropertiesAsync(filter, _ct);

            captured.Should().NotBeNull();
            captured!.SortBy.Should().Be("Price");
            captured.SortDir.Should().Be("asc");
            _repo.VerifyAll();
        }

        // A4) Filtros combinados name+address (no deben romper; se pasan al repo)
        [Test]
        public async Task GetPropertiesAsync_WithNameAndAddress_PassesFilterToRepo()
        {
            PropertyFilterDto? captured = null;
            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                .Callback<PropertyFilterDto, CancellationToken>((f, _) => captured = f)
                .ReturnsAsync((Enumerable.Empty<Property>().ToList(), 0L));

            var filter = new PropertyFilterDto("casa", "cedritos", null, null, 1, 10, "CreatedAt", "desc");
            await _sut.GetPropertiesAsync(filter, _ct);

            captured.Should().NotBeNull();
            captured!.Name.Should().Be("casa");
            captured.Address.Should().Be("cedritos");
            _repo.VerifyAll();
        }

        // A5) Strings vacíos → se tratan como null (no debe crashear ni forzar filtros)
        [Test]
        public async Task GetPropertiesAsync_BlankStrings_DoNotBreak()
        {
            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Property>().ToList(), 0L));

            var filter = new PropertyFilterDto("   ", "", null, null, 1, 10, null, null);
            var result = await _sut.GetPropertiesAsync(filter, _ct);

            result.Total.Should().Be(0);
            _repo.VerifyAll();
        }
    }
}