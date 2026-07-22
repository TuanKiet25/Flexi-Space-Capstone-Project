using CloudinaryDotNet;
using FlexiSpace.Domain.Entities;
using FlexiSpace.Infrastructure;
using FlexiSpace.Web;
using FlexiSpace.Web.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using PayOS;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddScoped<FlexiSpace.Application.IServices.INotificationRealtimeSender, NotificationRealtimeSender>();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}); 
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // (Tùy chọn) Cấu hình tiêu đề cho Swagger UI
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FlexiSpace API", Version = "v1" });
    // 1. Định nghĩa cơ chế bảo mật (Security Definition)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt SecretKey is missing!");

// 1. Lấy cấu hình từ appsettings.json
var cloudinarySection = builder.Configuration.GetSection("CloudinarySettings");
builder.Services.Configure<CloudinarySettings>(cloudinarySection);

// 2. Đăng ký đối tượng Cloudinary vào DI
builder.Services.AddScoped(sp =>
{
    var config = cloudinarySection.Get<CloudinarySettings>();
    if (config == null)
    {
        throw new InvalidOperationException("Cloudinary configuration is missing!");
    }
    var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
    return new Cloudinary(account);
});

IConfiguration configuration = builder.Configuration;
PayOSClient payOS = new PayOSClient(
    configuration["PayOS:ClientId"] ?? throw new InvalidOperationException("PayOS ClientId is missing!"),
    configuration["PayOS:ApiKey"] ?? throw new InvalidOperationException("PayOS ApiKey is missing!"),
    configuration["PayOS:ChecksumKey"] ?? throw new InvalidOperationException("PayOS ChecksumKey is missing!")
);
builder.Services.AddSingleton(payOS);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; 
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Đọc token từ query string nếu request gửi tới /chatHub
                if (!string.IsNullOrEmpty(accessToken)
                    && (path.StartsWithSegments("/chatHub")
                        || path.StartsWithSegments("/notificationHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // 2. (Tùy chọn) Thêm dòng này để khi mở web lên, nó trỏ thẳng vào Swagger luôn 
        // thay vì phải gõ thêm /swagger ở URL
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlexiSpace API v1");
        c.RoutePrefix = string.Empty;
    });
//}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
  
        var context = services.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        await FlexiSpace.Web.Extensions.DataSeeder.SeedAdminAccountAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình tự động khởi tạo Database.");
    }
}
app.Run();
