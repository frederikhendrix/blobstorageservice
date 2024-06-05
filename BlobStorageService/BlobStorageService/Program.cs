using BlobStorageService.Interfaces;
using BlobStorageService.Services;

var builder = WebApplication.CreateBuilder(args);

var blobStorageConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")?.Trim('"');
var virusTotalApiKey = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY")?.Trim('"');

builder.Configuration["BlobStorage:ConnectionString"] = blobStorageConnectionString;
builder.Configuration["BlobStorage:VirusTotalApiKey"] = virusTotalApiKey;

// Add services to the container.
builder.Services.AddScoped<IBlobService, BlobService>();
 

builder.Services.AddControllers();
builder.Services.AddLogging();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
