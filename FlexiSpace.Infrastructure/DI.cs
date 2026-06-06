using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FlexiSpace.Application;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Infrastructure.Helper;
using FlexiSpace.Infrastructure.MappingOptions;
using FlexiSpace.Infrastructure.Repositories;
using FlexiSpace.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlexiSpace.Infrastructure
{
    public static class DI
    {
        public static IServiceCollection AddInfrastructureServices(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            // Đăng ký AppDbContext
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            });

            // Đăng ký repositiries
            #region Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();  
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserOTPRepository, UserOTPRepository>();
            services.AddScoped<ISpaceRepository, SpaceRepository>();
            services.AddScoped<ISpaceAllowedCategoryRepository, SpaceAllowedCategoryRepository>();
            services.AddScoped<IBussinessCategoryRepository, BussinessCategoryRepository>();
            services.AddScoped<IAmentityRepository, AmentityRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped(typeof(IInsertAndUpdate<,>), typeof(InsertAndUpdate<,>));
            #endregion
            // Đăng ký services
            #region services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<ISpaceService, SpaceService>();
            services.AddScoped<IListingService, ListingService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IBussinessCategoryService, BussinessCategoryService>();
            services.AddHttpClient<ITurnstileService, TurnstileService>();
            #endregion
            //Đăng ký auto mapper
            //services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            // Đăng ký JWT authentication

            //Map từ appsettings 
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.Configure<MailOptions>(configuration.GetSection("MailSettings"));
            services.Configure<TurnstileOptions>(configuration.GetSection("Turnstile"));
            // Đăng ký CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                                  policy =>
                                  {
                                      // Cho phép origin của frontend được truy cập
                                      policy.AllowAnyOrigin()
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                                  });
            });
            //đăng ký HttpContextAccessor
            services.AddHttpContextAccessor();
            return services;
        }
    }
}
