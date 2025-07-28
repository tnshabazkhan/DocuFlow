using DocuFlow.Application;
using DocuFlow.Infrastructure;
using DocuFlow.Api.Endpoints;
using Microsoft.Azure.SignalR.Management;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "a_very_long_and_secure_secret_key_for_development_purposes";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "DocuFlow",
        ValidAudience = jwtSettings["Audience"] ?? "DocuFlow-Mobile",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Use Newtonsoft.Json for Minimal APIs to align with Cosmos SDK
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddControllers().AddNewtonsoftJson(); 
// Note: For Minimal APIs, we also need to configure the JSON options specifically
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    // This part is still System.Text.Json for Minimal APIs unless we use a custom result
    // Let's try to keep it simple and just ensure the types in the Domain are clean.
});

// Register Layer Dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR Service Management for Serverless Negotiation
builder.Services.AddSingleton<ServiceHubContext>(sp =>
{
    var connectionString = builder.Configuration["SignalRConnection"];
    var serviceManager = new ServiceManagerBuilder()
        .WithOptions(o => o.ConnectionString = connectionString)
        .BuildServiceManager();
    
    return serviceManager.CreateHubContextAsync("documentUpdates", default).GetAwaiter().GetResult();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API Endpoints
app.MapDocumentEndpoints();
app.MapSignalREndpoints();
app.MapIdentityEndpoints();

app.Run();
