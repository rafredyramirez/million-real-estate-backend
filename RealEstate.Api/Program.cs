using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using RealEstate.Application.Interfaces;
using RealEstate.Application.Services;
using RealEstate.Infraestructure.Mongo;
using RealEstate.Api.Middlewares;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

// ========= Serilog =========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithCorrelationId()
       .WriteTo.Console());

// ========= Services =========
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyService, PropertyService>();

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========= CORS =========
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)    
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-ID");
    });
});

var app = builder.Build();

app.UseCors("Frontend");

app.Use(async (ctx, next) =>
{
    const string HeaderName = "X-Correlation-ID";
    var cid = ctx.Request.Headers.TryGetValue(HeaderName, out var h) && !string.IsNullOrWhiteSpace(h)
        ? h.ToString()
        : ctx.TraceIdentifier;

    ctx.Response.Headers[HeaderName] = cid;

    using (LogContext.PushProperty("CorrelationId", cid))
    {
        await next();
    }
});

// ====== Request logging ======
app.UseSerilogRequestLogging(options =>
{
    options.IncludeQueryInRequestPath = true;
    options.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("RequestHost", httpCtx.Request.Host.Value);
        diagCtx.Set("RequestScheme", httpCtx.Request.Scheme);
        diagCtx.Set("UserAgent", httpCtx.Request.Headers.UserAgent.ToString());
        diagCtx.Set("RemoteIp", httpCtx.Connection.RemoteIpAddress?.ToString());
        diagCtx.Set("TraceIdentifier", httpCtx.TraceIdentifier);
    };
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ====== Health ======
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/readyz", async (MongoContext ctx, CancellationToken ct) =>
{
    try
    {
        await ctx.Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1), cancellationToken: ct);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "mongo unavailable", detail: ex.Message, statusCode: 503);
    }
});

// ====== Raíz → Swagger ======
app.MapGet("/", () => Results.Redirect("/swagger"));

// ====== Controllers ======
app.MapControllers();

app.Run();
