using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using RealEstate.Contracts.Dtos;

namespace RealEstate.UnitTests;

[TestFixture]
public class PropertiesApiIntegrationTests
{
    private const string BASE = "api/Properties";

    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("http://localhost") });

        _factory.ServiceMock.Reset();
        _factory.ServiceMock
            .Setup(s => s.GetPropertiesAsync(It.IsAny<PropertyFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertyDto>(Enumerable.Empty<PropertyDto>(), 1, 10, 0, 0));
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Root_RedirectsToSwagger()
    {
        // sanity check: la app realmente está levantada
        var resp = await _client.GetAsync("/");
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/swagger");
    }

    [Test]
    public async Task Healthz_ReturnsOk()
    {
        var resp = await _client.GetAsync("/healthz");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Validation_MinGreaterThanMax_Returns400_ProblemDetails()
    {
        var resp = await _client.GetAsync($"{BASE}?minPrice=1000&maxPrice=100");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!.Should().ContainKey("title");
        body["title"].ToString().Should().Be("Bad Request");
        body.Should().ContainKey("detail");
    }

    [Test]
    public async Task Validation_PageSizeZero_Returns400_ProblemDetails()
    {
        var resp = await _client.GetAsync($"{BASE}?pageSize=0");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task GetById_InvalidIdFormat_Returns400()
    {
        var resp = await _client.GetAsync($"{BASE}/abc");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["detail"].ToString().Should().Contain("Invalid id format");
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        const string id = "000000000000000000000001";
        _factory.ServiceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDto?)null);

        var resp = await _client.GetAsync($"{BASE}/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetById_Ok_Returns200_AndDto()
    {
        var dto = new PropertyDto("000000000000000000000002", "o1", "Casa", "Dir", 100m, "img");
        _factory.ServiceMock
            .Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var resp = await _client.GetAsync($"{BASE}/{dto.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<PropertyDto>();
        body!.Id.Should().Be(dto.Id);
        body.Name.Should().Be("Casa");
    }

    [Test]
    public async Task CorrelationId_EchoesHeader_WhenProvided()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, BASE);
        req.Headers.TryAddWithoutValidation("X-Correlation-ID", "test-123");

        var resp = await _client.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        resp.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.Single().Should().Be("test-123");
    }

    [Test]
    public async Task CorrelationId_IsGenerated_WhenMissing()
    {
        var resp = await _client.GetAsync(BASE);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        resp.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.Single().Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Cors_BlocksUnconfiguredOrigin()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, BASE);
        req.Headers.TryAddWithoutValidation("Origin", "http://evil.local");

        var resp = await _client.SendAsync(req);

        resp.Headers.TryGetValues("Access-Control-Allow-Origin", out var _).Should().BeFalse();
    }
}
