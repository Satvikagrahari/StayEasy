using Microsoft.OpenApi;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("auth", new OpenApiInfo { Title = "Auth Service", Version = "v1" });
    options.SwaggerDoc("catalog", new OpenApiInfo { Title = "Catalog Service", Version = "v1" });
    options.SwaggerDoc("booking", new OpenApiInfo { Title = "Booking Service", Version = "v1" });
    options.SwaggerDoc("admin", new OpenApiInfo { Title = "Admin Service", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/auth/swagger.json", "Auth Service");
    options.SwaggerEndpoint("/swagger/catalog/swagger.json", "Catalog Service");
    options.SwaggerEndpoint("/swagger/booking/swagger.json", "Booking Service");
    options.SwaggerEndpoint("/swagger/admin/swagger.json", "Admin Service");
});

await app.UseOcelot();
app.Run();


