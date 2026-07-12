using FlexiSpace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlexiSpace.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserOTP> UserOTPs { get; set; }
        public virtual DbSet<UserProfile> Profiles { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<Space> Spaces { get; set; }
        public virtual DbSet<Listing> Listings { get; set; }
        public virtual DbSet<PrimaryBookingRequest> PrimaryBookingRequests { get; set; }
        public virtual DbSet<Contract> Contracts { get; set; }
        public virtual DbSet<Amentity> Amenities { get; set; }
        public virtual DbSet<BussinessCategory> BussinessCategories { get; set; }
        public virtual DbSet<OperatingHour> OperatingHours { get; set; }
        public virtual DbSet<PictureURL> PictureURLs { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<SpaceAllowedCategory> SpaceAllowedCategories { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<AvailabilitiesTime> AvailabilitiesTimes { get; set; }
        public virtual DbSet<SharedSpaceAmenities> SharedSpaceAmenities { get; set; }
        public virtual DbSet<ShareSpaceDetail> ShareSpaceDetails { get; set; }
        public virtual DbSet<ContractVerification> ContractVerifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
