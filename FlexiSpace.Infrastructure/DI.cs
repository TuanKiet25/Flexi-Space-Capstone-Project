
using FlexiSpace.Application;
using FlexiSpace.Application.IRepositories;
using FlexiSpace.Application.IServices;
using FlexiSpace.Application.Services;
using FlexiSpace.Infrastructure.Helper;
using FlexiSpace.Infrastructure.MappingOptions;
using FlexiSpace.Infrastructure.Repositories;
using FlexiSpace.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlexiSpace.Infrastructure
{
    public static class DI
    {
        public static IServiceCollection AddInfrastructureServices(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly));
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
            services.AddScoped<IProfileRepository, ProfileRepository>();
            services.AddScoped<IUserOTPRepository, UserOTPRepository>();
            services.AddScoped<ISpaceRepository, SpaceRepository>();
            services.AddScoped<ISpaceAllowedCategoryRepository, SpaceAllowedCategoryRepository>();
            services.AddScoped<IBussinessCategoryRepository, BussinessCategoryRepository>();
            services.AddScoped<IAmentityRepository, AmentityRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IContractVerificationRepository, ContractVerificationRepository>();
            services.AddScoped<IPrimaryBookingRequestRepository, PrimaryBookingRequestRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IShareSpaceDetailRepository, ShareSpaceDetailRepository>();
            services.AddScoped<IShareSpaceCategoryRepository, ShareSpaceCategoryRepository>();
            services.AddScoped<IAvailabilitiesTimeRepository, AvailabilitiesTimeRepository>();
            services.AddScoped<ISharedSpaceAmenitiesRepository, SharedSpaceAmenitiesRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IListingReportRepository, ListingReportRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IFavoriteListRepository, FavoriteListRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IUserAiImageHistoryRepository, UserAiImageHistoryRepository>();
            services.AddScoped<IPriorityLevelRepository, PriorityLevelRepository>();
            services.AddScoped(typeof(IInsertAndUpdate<,>), typeof(InsertAndUpdate<,>));
            #endregion
            // Đăng ký services
            #region services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<ISpaceService, SpaceService>();
            services.AddScoped<ISpacePartService, SpacePartService>();
            services.AddScoped<IListingService, ListingService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IPrimaryBookingRequestService, PrimaryBookingRequestService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IBussinessCategoryService, BussinessCategoryService>();
            services.AddHttpClient<ITurnstileService, TurnstileService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IDataSeederService, DataSeederService>();
            services.AddScoped<IPictureURL, PictureURLService>();
            services.AddScoped<IAmenityService, AmenityService>();
            services.AddScoped<IRootEmailService, RootEmailService>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IFavoriteListService, FavoriteListService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IFalAiService, FalAiService>();
            services.AddScoped<IAIToolService, AIToolService>();
            services.AddScoped<IUserAiImageHistoryService, UserAiImageHistoryService>();
            services.AddScoped<IPriorityLevelService, PriorityLevelService>();
            services.AddScoped<INotificationExpoService,NotificationExpoService>(); 
            services.AddScoped<IDashboardService, DashboardService>();
            #endregion
            //Map từ appsettings 
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.Configure<MailOptions>(configuration.GetSection("MailSettings"));
            services.Configure<TurnstileOptions>(configuration.GetSection("Turnstile"));
            services.Configure<ResentEmailOptions>(configuration.GetSection("EmailSettings"));
            services.Configure<FalAiOptions>(configuration.GetSection("FalAiSettings"));
            services.AddHttpClient<IFalAiService, FalAiService>((serviceProvider, client) =>
            {
                var falSettings = serviceProvider.GetRequiredService<IOptions<FalAiOptions>>().Value;
                client.BaseAddress = new Uri(falSettings.BaseUrl);
            });
            services.AddHttpClient<IExpoPushService, ExpoPushService>();
            // Đăng ký CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                                  policy =>
                                  {
                                      // Cho phép origin của frontend được truy cập
                                      policy.SetIsOriginAllowed(origin => true)
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                                            .AllowCredentials();
                                  });
            });
            //đăng ký HttpContextAccessor
            services.AddHttpContextAccessor();
            //đăng ký Redis
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("RedisConnection");
                options.InstanceName = "FlexiSpace_"; 
            });
            return services;
        }
    }
}
