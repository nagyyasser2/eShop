using eShop.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCorsPolicy();
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJobServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddControllerConfiguration();
builder.Services.AddCachingServices();

var app = builder.Build();

app.UseApplicationMiddleware(builder.Environment);
app.UseAuthenticationMiddleware();

app.Run();