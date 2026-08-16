# FlexiSpace Capstone Project

FlexiSpace is a .NET 8 Web API for managing rental spaces, listings, booking requests, contracts, shared-space permissions, payments, chat, notifications, reviews, and dashboard reporting.

The project follows a layered architecture with separated Domain, Application, Infrastructure, Web API, and test projects.

## Solution Structure

```text
FlexiSpace_CapstoneProject.sln
+-- FlexiSpace.Domain              # Entities, enums, core domain models
+-- FlexiSpace.Application         # Service contracts, request/response models, business services
+-- FlexiSpace.Infrastructure      # EF Core, repositories, service implementations, mappings, integrations
+-- FlexiSpace.Web                 # ASP.NET Core API, controllers, SignalR hubs, background workers
+-- FlexiSpace.*.Tests             # Unit test projects
+-- coveragereport                 # Generated coverage report output
```

## Main Technologies

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core with PostgreSQL
- Redis distributed cache
- JWT Bearer authentication
- SignalR for realtime chat and notifications
- AutoMapper
- MediatR notifications
- PayOS payment integration
- Cloudinary image storage
- Email services and OTP flows
- Cloudflare Turnstile captcha
- Expo push notifications

## Main Modules

### Authentication and Users

- User registration, login, OTP, password reset, and JWT generation.
- User profile verification and profile management.
- Role and status handling through domain enums.

### Spaces

- Parent spaces represent the main rental space.
- Space parts represent child spaces split from a parent space.
- Spaces support amenities, operating hours, allowed business categories, pictures, location, and area.

Implemented space-part rules:

- A logged-in user is required to create, update, delete, or read their managed space parts.
- Parent-space owner can manage parts.
- A user with active share permission can split a parent space.
- Space parts store the actual creator in `CreatedBy`.
- Space-part listing/management is scoped by `CreatedBy`, so owners do not automatically see parts created by renters.
- Total child-space area cannot exceed parent-space area.
- Cannot split a parent space after its listing is `Occupied` or it already has an active contract.
- Child spaces rented individually do not block other sibling child spaces from being listed.

### Listings

- Supports normal entire-space listings and shared-space listings.
- Listing statuses include `Available`, `Occupied`, `Hidden`, `Ban`, and `Expired`.
- Listings include rental period, price, price unit, priority level, view count, and pictures.
- Background worker expires listings after their configured duration.

Important listing rules:

- A user must own the space or have valid share rights to create listings.
- Users with valid share permission can create normal listings for child spaces.
- Listing period must be valid and cannot be in the past where not allowed.
- Price must be greater than zero.
- Price unit must match the listing duration.
- Parent-space listings cannot overlap active child-space listings.
- Child-space listings cannot overlap active parent-space listings.
- A rented child space does not block listing other child spaces under the same parent.

### Shared Space and Usage Rights

- `SpaceUsageRight` records who can use or share a space.
- Rights include:
  - `CanShare`
  - `CanGrantSharePermission`
  - Valid date range
  - Right type: `Owner`, `PrimaryRenter`, `SubRenter`
- Primary renters can share when granted permission.
- Sub renters cannot grant or continue sharing permission.

### Booking Requests

- Booking requests connect renters, lessors, spaces, and listings.
- Supports booking status changes and notification events.
- Booking events can trigger notifications to lessor/lessee.

### Contracts

- Contracts can be platform-created or external.
- Contract statuses include `Draft`, `Signing`, `Active`, `Expired`, `Cancelled`, and `PendingExternalVerification`.
- Supports OTP-based signing and verification metadata.
- After both sides sign, the contract becomes active and the related listing is marked occupied.
- Active contracts create or update usage rights for the renter where applicable.
- Background worker expires active contracts after their end date.

### Payments and Wallet

- Wallet balance is used for paid actions such as listing creation.
- Transaction and transaction-history modules record payment activity.
- PayOS integration is registered in the Web project.

### Chat and Notifications

- SignalR hubs:
  - `/chatHub`
  - `/notificationHub`
- Messages support multiple message types, including contract proposal flows.
- Notifications support app-level and Expo push delivery.

### Media and AI Tools

- Cloudinary is used for uploading and storing pictures.
- Fal AI integration supports AI image generation/history.

### Dashboard and Reports

- Dashboard aggregates listing activity, booking requests, contracts, and trends.
- Listing reports allow users/admins to manage reported listings.
- Daily listing view statistics are tracked.

## API Entry Points

The Web API controllers are in `FlexiSpace.Web/Controllers`.

Common controller groups:

- `AuthController`
- `UserController`
- `ProfileController`
- `SpaceController`
- `SpacePartController`
- `SpaceUsageRightController`
- `ListingController`
- `PrimaryBookingRequestController`
- `ContractController`
- `ExternalContractController`
- `ConversationController`
- `MessageController`
- `NotificationController`
- `WalletController`
- `TransactionController`
- `ReviewController`
- `DashboardController`
- `BannerController`

Swagger is enabled at the application root when the API runs.

## Configuration

Configuration is loaded from `FlexiSpace.Web/appsettings.json` and environment-specific appsettings files.

Required configuration groups:

- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:RedisConnection`
- `Jwt`
- `CloudinarySettings`
- `EmailSettings`
- `TurnstileSettings`
- `PayOS`
- `FalAiSettings`

Do not commit production secrets. Use environment variables, user secrets, or deployment secret storage for real credentials.

## Database

The API uses EF Core migrations from the Infrastructure project. On startup, the Web project calls:

```csharp
await context.Database.MigrateAsync();
```

This applies pending migrations automatically, then seeds the admin account.

## Running Locally

Restore and build:

```bash
dotnet restore FlexiSpace_CapstoneProject.sln
dotnet build FlexiSpace_CapstoneProject.sln
```

Run the API:

```bash
dotnet run --project FlexiSpace.Web/FlexiSpace.Web.csproj
```

Open Swagger:

```text
https://localhost:<port>/
```

## Tests

Run all tests:

```bash
dotnet test FlexiSpace_CapstoneProject.sln
```

Test projects are split by layer:

- `FlexiSpace.Domain.Tests`
- `FlexiSpace.Application.Tests`
- `FlexiSpace.Infrastructure.Tests`
- `FlexiSpace.Web.Tests`

## Notes

- Some generated coverage files are present under `coveragereport`.
- The project currently reports a NuGet vulnerability warning for `AutoMapper 12.0.1` (`NU1903`). Review package upgrades before production release.
- Several source files contain Vietnamese user-facing messages. Keep messages consistent when adding new validations.
