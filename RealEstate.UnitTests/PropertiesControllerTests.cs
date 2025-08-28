using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using RealEstate.Api.Controllers;
using RealEstate.Application.Interfaces;
using RealEstate.Contracts.Dtos;

namespace RealEstate.UnitTests
{
    [TestFixture]
    public class PropertiesControllerTests
    {
        private Mock<IPropertyService> _service = null!;
        private PropertiesController _controller = null!;
        private readonly CancellationToken _ct = CancellationToken.None;

        [SetUp]
        public void SetUp()
        {
            _service = new Mock<IPropertyService>(MockBehavior.Strict);
            _controller = new PropertiesController(_service.Object);
        }

        [Test]
        public async Task Get_List_ReturnsOkWithPagedResult()
        {
            // Arrange
            var paged = new PagedResult<PropertyDto>(
                Items: new[] { new PropertyDto("1", "owner1", "Casa", "Dir", 350000m, "img") },
                Page: 1, PageSize: 10, Total: 1, TotalPages: 1);

            _service.Setup(s => s.GetPropertiesAsync(
                    It.Is<PropertyFilterDto>(f => f.Page == 1 && f.PageSize == 10),
                    _ct))
                .ReturnsAsync(paged);

            // Act
            var action = await _controller.Get(
                name: null, address: null,
                minPrice: null, maxPrice: null,
                page: 1, pageSize: 10,
                sortBy: "CreatedAt", sortDir: "desc",
                ct: _ct);

            // Assert
            var ok = action.Result as OkObjectResult;
            ok.Should().NotBeNull();
            var body = ok!.Value as PagedResult<PropertyDto>;
            body.Should().NotBeNull();
            body!.Total.Should().Be(1);

            _service.VerifyAll();
        }

        [Test]
        public async Task GetById_WhenNotFound_Returns404()
        {
            _service.Setup(s => s.GetByIdAsync("nope", _ct))
                    .ReturnsAsync((PropertyDto?)null);

            var action = await _controller.GetById("nope", _ct);
            action.Result.Should().BeOfType<NotFoundResult>();

            _service.VerifyAll();
        }

        [Test]
        public async Task GetById_WhenFound_ReturnsOkWithDto()
        {
            var dto = new PropertyDto("1", "o1", "Casa", "Dir", 100m, "img");
            _service.Setup(s => s.GetByIdAsync("1", _ct)).ReturnsAsync(dto);

            var action = await _controller.GetById("1", _ct);
            var ok = action.Result as OkObjectResult;
            ok.Should().NotBeNull();
            (ok!.Value as PropertyDto)!.Id.Should().Be("1");

            _service.VerifyAll();
        }
        //new
        // Helper: configura el mock para capturar el filtro y devolver un PagedResult “fake”
        private void SetupListResult(Action<PropertyFilterDto?> capture, int total = 1)
        {
            var result = new PagedResult<PropertyDto>(
                Items: new[] { new PropertyDto("1", "o1", "Casa Norte", "Dir", 123m, "img") },
                Page: 1, PageSize: 10, Total: total, TotalPages: (total + 9) / 10
            );

            _service.Setup(s => s.GetPropertiesAsync(
                    It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                .Callback<PropertyFilterDto, CancellationToken>((f, _) => capture(f))
                .ReturnsAsync(result);
        }

        [Test]
        public async Task Get_List_MapsQueryParamsToFilter_AndReturnsOk()
        {
            // Arrange
            PropertyFilterDto? captured = null;
            SetupListResult(f => captured = f);

            // Act
            var action = await _controller.Get(
                name: "casa", address: "cedritos",
                minPrice: 200000, maxPrice: 600000,
                page: 2, pageSize: 5,
                sortBy: "Price", sortDir: "asc",
                ct: _ct);

            // Assert: mapping correcto hacia el service
            captured.Should().NotBeNull();
            captured!.Name.Should().Be("casa");
            captured.Address.Should().Be("cedritos");
            captured.MinPrice.Should().Be(200000);
            captured.MaxPrice.Should().Be(600000);
            captured.Page.Should().Be(2);
            captured.PageSize.Should().Be(5);
            captured.SortBy.Should().Be("Price");
            captured.SortDir.Should().Be("asc");

            // Assert: HTTP 200 + body esperado
            var ok = action.Result as OkObjectResult;
            ok.Should().NotBeNull();
            var body = ok!.Value as PagedResult<PropertyDto>;
            body.Should().NotBeNull();
            body!.Items.Should().HaveCount(1);
            body.Items.First().Name.Should().Be("Casa Norte");

            _service.VerifyAll();
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("   ", "   ")]
        public async Task Get_List_AllowsNullOrBlankFilters(string? name, string? address)
        {
            // Arrange
            PropertyFilterDto? captured = null;
            SetupListResult(f => captured = f, total: 0);

            // Act
            var action = await _controller.Get(
                name, address,
                minPrice: null, maxPrice: null,
                page: 1, pageSize: 10,
                sortBy: null, sortDir: null,
                ct: _ct);

            // Assert: controller pasa los mismos valores (el service ya normaliza defaults)
            captured.Should().NotBeNull();
            captured!.Name.Should().Be(name);
            captured.Address.Should().Be(address);

            var ok = action.Result as OkObjectResult;
            ok.Should().NotBeNull();
            var body = ok!.Value as PagedResult<PropertyDto>;
            body!.Total.Should().Be(0);

            _service.VerifyAll();
        }

        [TestCase("Price", "asc")]
        [TestCase("Price", "desc")]
        [TestCase("CreatedAt", "desc")]
        [TestCase("Name", "asc")]
        public async Task Get_List_RespectsSortByAndSortDir(string sortBy, string sortDir)
        {
            // Arrange
            PropertyFilterDto? captured = null;
            SetupListResult(f => captured = f);

            // Act
            var action = await _controller.Get(
                name: null, address: null,
                minPrice: null, maxPrice: null,
                page: 1, pageSize: 10,
                sortBy, sortDir,
                ct: _ct);

            // Assert
            captured.Should().NotBeNull();
            captured!.SortBy.Should().Be(sortBy);
            captured.SortDir.Should().Be(sortDir);

            var ok = action.Result as OkObjectResult;
            ok.Should().NotBeNull();

            _service.VerifyAll();
        }
        [Test]
        public void Get_List_WhenServiceThrows_PropagatesArgumentException()
        {
            // Nota: en unit test de controller puro, no hay middleware global.
            // Por lo tanto, si el service lanza ArgumentException, se propaga.
            _service.Setup(s => s.GetPropertiesAsync(
                    It.IsAny<PropertyFilterDto>(), _ct))
                .ThrowsAsync(new System.ArgumentException("MinPrice no puede ser mayor que MaxPrice."));

            var act = async () => await _controller.Get(null, null, 1000, 100, 1, 10, "CreatedAt", "desc", _ct);

            act.Should().ThrowAsync<System.ArgumentException>();
            _service.VerifyAll();
        }
    }
}
