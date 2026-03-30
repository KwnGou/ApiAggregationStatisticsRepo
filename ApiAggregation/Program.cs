using ApiAggregation.Models;
using ApiAggregation.Services.Github;
using ApiAggregation.Services.News;
using ApiAggregation.Services.Spotify;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Serialization;


var MyCorsPolicy = "_MyCorsPolicy";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    

    options.AddPolicy(name: MyCorsPolicy,
        policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("CORSorigins").Get<string[]>());
            policy.AllowAnyMethod();
            policy.AllowAnyHeader();
        });

});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Include XML comments (enable <GenerateDocumentationFile>true</GenerateDocumentationFile> in csproj)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Remove or comment out the following lines as OpenApiSchema is not available:
    // options.MapType<DateTime>(() => new OpenApiSchema { Type = "string", Format = "date" });
    // options.MapType<DateTime?>(() => new OpenApiSchema { Type = "string", Format = "date" });
});

// Register HttpClient services
builder.Services.AddHttpClient<SpotifyAuthService>();
builder.Services.AddTransient<SpotifyAuthHandler>();
builder.Services.AddHttpClient<SpotifyApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.spotify.com/");
})
.AddHttpMessageHandler<SpotifyAuthHandler>();
builder.Services.AddHttpClient<GNewsApiService>();
builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
});

builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(MyCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
