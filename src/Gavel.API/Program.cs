using Gavel.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationCore(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseApplicationPipeline();
app.Run();