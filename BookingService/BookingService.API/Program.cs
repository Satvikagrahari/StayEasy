using BookingService.Application.Interfaces.Services;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================= DB =================
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ================= DI - Application Services =================
builder.Services.AddScoped<IBookingService, BookingService.Infrastructure.Services.BookingService>();
builder.Services.AddHttpClient<ICartService, CartService>();

// ================= JWT Configuration =================
var jwtSettings = builder.Configuration.GetSection("Jwt");

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

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"])
        )
    };
});

builder.Services.AddAuthorization();

// ================= Controllers =================
builder.Services.AddControllers();

// ================= Swagger/OpenAPI =================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Booking API",
        Version = "v1"
    });

    // JWT Swagger Support
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer <your_token>"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ================= RabbitMQ Configuration =================
builder.Services.AddSingleton<IBookingPublisher, RabbitMQPublisher>();
builder.Services.AddHostedService<RabbitMQConsumerService>();

// ================= Build Application =================
var app = builder.Build();

// ================= Middleware =================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ================= Run Application =================
app.Run();







//using BookingService.Application.Interfaces.Services;
//using BookingService.Infrastructure.Data;
//using BookingService.Infrastructure.Messaging;
//using BookingService.Infrastructure.Services;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//// ================= DB =================
//builder.Services.AddDbContext<BookingDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

//// ================= DI =================
//builder.Services.AddScoped<IBookingService, BookingService.Infrastructure.Services.BookingService>();
//builder.Services.AddHttpClient<ICartService, CartService>();

//// ================= JWT =================
//var jwtSettings = builder.Configuration.GetSection("Jwt");

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,

//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(
//            Encoding.UTF8.GetBytes(jwtSettings["Key"])
//        )
//    };
//});

//builder.Services.AddAuthorization();

//// ================= Controllers =================
//builder.Services.AddControllers();

//// ================= Swagger =================
//builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "Booking API",
//        Version = "v1"
//    });

//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "Enter: Bearer <your_token>"
//    });

//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            new string[] {}
//        }
//    });
//});

//// ================= RabbitMQ =================
//builder.Services.AddSingleton<RabbitMQPublisher>();
//builder.Services.AddSingleton<RabbitMQConsumer>();

//var app = builder.Build();

//// ================= Middleware =================
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//// ================= Start RabbitMQ Consumer =================
//try
//{
//    var consumer = app.Services.GetRequiredService<RabbitMQConsumer>();
//    consumer.StartListening();
//    Console.WriteLine("✓ RabbitMQ consumer started successfully");
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"✗ Failed to start RabbitMQ consumer: {ex.Message}");
//}

//app.Run();