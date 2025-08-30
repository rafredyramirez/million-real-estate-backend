using FluentAssertions;
using FluentValidation.Results;
using FluentValidation;
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
        private Mock<IValidator<PropertyFilterDto>> _validator = null!;
        private PropertiesController _controller = null!;
        private readonly CancellationToken _ct = CancellationToken.None;

        [SetUp]
        public void SetUp()
        {
            _service = new Mock<IPropertyService>(MockBehavior.Strict);
            _validator = new Mock<IValidator<PropertyFilterDto>>(MockBehavior.Strict);
            _controller = new PropertiesController(_service.Object, _validator.Object);
        }

        private static ValidationResult Valid() => new ValidationResult();

        private static ValidationResult Invalid(params (string prop, string msg)[] errs) =>
            new ValidationResult(errs.Select(e => new ValidationFailure(e.prop, e.msg)));

        [Test]
        public async Task Get_MapsQueryParams_ToFilter_AndCallsService_ReturnsOk()
        {
            // Arrange
            PropertyFilterDto? capturedByValidator = null;
            PropertyFilterDto? capturedByService = null;

            _validator.Setup(v => v.ValidateAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                .Callback<PropertyFilterDto, CancellationToken>((f, _) => capturedByValidator = f)
                .ReturnsAsync(Valid());

            var paged = new PagedResult<PropertyDto>(
                Items: new[] { new PropertyDto("1", "o1", "Casa Norte", "Dir", 123m, "img") },
                Page: 2, PageSize: 5, Total: 1, TotalPages: 1);

            _service.Setup(s => s.GetPropertiesAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                .Callback<PropertyFilterDto, CancellationToken>((f, _) => capturedByService = f)
                .ReturnsAsync(paged);

            // Act
            var result = await _controller.Get(
                name: "casa", address: "cedritos",
                minPrice: 200000, maxPrice: 600000,
                page: 2, pageSize: 5,
                sortBy: "Price", sortDir: "asc",
                ct: _ct);

            // Assert (validator recibió el filtro esperado)
            capturedByValidator.Should().NotBeNull();
            capturedByValidator!.Name.Should().Be("casa");
            capturedByValidator.Address.Should().Be("cedritos");
            capturedByValidator.MinPrice.Should().Be(200000);
            capturedByValidator.MaxPrice.Should().Be(600000);
            capturedByValidator.Page.Should().Be(2);
            capturedByValidator.PageSize.Should().Be(5);
            capturedByValidator.SortBy.Should().Be("Price");
            capturedByValidator.SortDir.Should().Be("asc");

            // Assert (service recibió lo mismo)
            capturedByService.Should().NotBeNull();
            capturedByService!.Should().BeEquivalentTo(capturedByValidator);

            // Assert (HTTP 200)
            var ok = result.Result as OkObjectResult;
            ok.Should().NotBeNull();
            var body = ok!.Value as PagedResult<PropertyDto>;
            body!.Items.Should().HaveCount(1);
            body.Items.First().Name.Should().Be("Casa Norte");

            _validator.VerifyAll();
            _service.VerifyAll();
        }

        [Test]
        public void Get_WhenValidationFails_ThrowsArgumentException()
        {
            // Arrange
            _validator.Setup(v => v.ValidateAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Invalid(("PageSize", "PageSize must be between 1 and 100.")));

            // El servicio NO debe llamarse si la validación falla
            _service.Invocations.Clear();

            // Act
            Func<Task> act = () => _controller.Get(
                name: null, address: null,
                minPrice: null, maxPrice: null,
                page: 1, pageSize: 0, // inválido
                sortBy: null, sortDir: null,
                ct: _ct);

            // Assert
            act.Should().ThrowAsync<ArgumentException>()
               .WithMessage("*PageSize*");

            _service.Verify(s => s.GetPropertiesAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()),
                            Times.Never);
        }

        [Test]
        public async Task GetById_WhenFound_ReturnsOk()
        {
            _validator.Setup(v => v.ValidateAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Valid()); 

            var dto = new PropertyDto("000000000000000000000001", "o1", "Casa", "Dir", 100m, "img");
            _service.Setup(s => s.GetByIdAsync(dto.Id, _ct)).ReturnsAsync(dto);

            var action = await _controller.GetById(dto.Id, _ct);

            var ok = action.Result as OkObjectResult;
            ok.Should().NotBeNull();
            (ok!.Value as PropertyDto)!.Id.Should().Be(dto.Id);

            _service.VerifyAll();
        }

        [Test]
        public async Task GetById_WhenNotFound_ReturnsNotFound()
        {
            const string id = "000000000000000000000001";
            _service.Setup(s => s.GetByIdAsync(id, _ct)).ReturnsAsync((PropertyDto?)null);

            var action = await _controller.GetById(id, _ct);
            action.Result.Should().BeOfType<NotFoundResult>();

            _service.VerifyAll();
        }

        [Test]
        public void GetById_WithInvalidIdFormat_ThrowsArgumentException()
        {
            // id no-ObjectId (no 24 hex)
            Func<Task> act = () => _controller.GetById("abc", _ct);
            act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid id format*");
        }
    }
}
