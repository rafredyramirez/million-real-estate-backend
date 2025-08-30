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
            var items = new[]
            {
                new Property { Id="1", IdOwner="o1", Name="Casa Norte", Address="Dir", Price=350000m,
                               CodeInternal="P-0001", Year=2015, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow,
                               ImageUrl="https://example.com/img1.jpg" }
            }.ToList();

            PropertyFilterDto? captured = null;

            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                 .Callback<PropertyFilterDto, CancellationToken>((f, _) => captured = f)
                 .ReturnsAsync((items, 1L));

            var result = await _sut.GetPropertiesAsync(null!, _ct);

            captured.Should().NotBeNull();
            captured!.Page.Should().Be(1);
            captured.PageSize.Should().Be(10);
            captured.SortBy.Should().Be("CreatedAt");
            captured.SortDir.Should().Be("desc");

            result.Items.Should().HaveCount(1);
            result.Total.Should().Be(1);
        }

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
            captured!.Page.Should().Be(1);
            captured.PageSize.Should().Be(100);
        }

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
        }

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
        }

        [Test]
        public async Task GetPropertiesAsync_BlankStrings_DoNotBreak()
        {
            _repo.Setup(r => r.GetPagedAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<Property>().ToList(), 0L));

            var filter = new PropertyFilterDto("  ", "", null, null, 1, 10, null, null);
            var result = await _sut.GetPropertiesAsync(filter, _ct);

            result.Total.Should().Be(0);
        }

        [Test]
        public async Task GetPropertiesAsync_WhenMinGreaterThanMax_ThrowsArgumentException()
        {
            var filter = new PropertyFilterDto(null, null, 500m, 100m, 1, 10, "CreatedAt", "desc");
            await FluentActions.Invoking(() => _sut.GetPropertiesAsync(filter, _ct))
                               .Should().ThrowAsync<ArgumentException>();
        }
    }
}