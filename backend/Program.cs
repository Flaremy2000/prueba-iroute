using IRoute.Api.Configuration;
using IRoute.Application;
using IRoute.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseApiPipeline();
app.Run();

public partial class Program;
