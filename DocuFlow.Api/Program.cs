using DocuFlow.Application;
using DocuFlow.Infrastructure;
using DocuFlow.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Map Minimal API Endpoints
app.MapDocumentEndpoints();

app.Run();
