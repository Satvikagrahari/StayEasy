using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot + Swagger config
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

// ApiGateway Swagger UI (aggregated downstream Swagger docs)
app.UseSwaggerForOcelotUI(
    options =>
    {
        options.PathToSwaggerGenerator = "/swagger/docs";
    },
    uiOptions =>
    {
        uiOptions.RoutePrefix = "swagger";
    });

await app.UseOcelot();

app.Run();