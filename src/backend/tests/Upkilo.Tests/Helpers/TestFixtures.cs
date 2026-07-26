using Bogus;
using Upkilo.Core.Entities;

namespace Upkilo.Tests.Helpers;

/// <summary>
/// Provides reusable test entity factories with realistic data.
/// All factory methods return detached entities suitable for seeding DbContext.
/// </summary>
public static class TestFixtures
{
    private static readonly Faker _faker = new("en");

    // ── Tenant ────────────────────────────────────────────────────────

    public static Tenant CreateTenant(Guid? id = null, string? name = null, TenantStatus status = TenantStatus.Active)
    {
        var tenantId = id ?? Guid.NewGuid();
        var tenantName = name ?? _faker.Company.CompanyName();
        return new Tenant
        {
            Id = tenantId,
            Name = tenantName,
            Slug = tenantName.ToLower().Replace(" ", "-").Replace(".", "").Replace(",", "") + "-" + tenantId.ToString()[..6],
            Status = status,
            Email = _faker.Internet.Email(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── User ──────────────────────────────────────────────────────────

    public static User CreateUser(Guid tenantId, Guid? id = null, string? email = null,
        UserRole role = UserRole.Admin, UserStatus status = UserStatus.Active, string password = "TestP@ss1!")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Email = email ?? _faker.Internet.Email(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Role = role,
            Status = status,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Staff ─────────────────────────────────────────────────────────

    public static StaffMember CreateStaff(Guid tenantId, Guid? id = null, string? email = null)
    {
        return new StaffMember
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = email ?? _faker.Internet.Email(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Client ────────────────────────────────────────────────────────

    public static Client CreateClient(Guid tenantId, Guid? id = null, string? email = null)
    {
        return new Client
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = email ?? _faker.Internet.Email(),
            Phone = _faker.Phone.PhoneNumber(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Service ───────────────────────────────────────────────────────

    public static Service CreateService(Guid tenantId, Guid? id = null, string? name = null,
        int durationMinutes = 30, decimal price = 50.00m)
    {
        return new Service
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = name ?? _faker.Commerce.ProductName(),
            DurationMinutes = durationMinutes,
            Price = price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Booking ───────────────────────────────────────────────────────

    public static Booking CreateBooking(Guid tenantId, Guid serviceId, Guid? staffId = null,
        Guid? clientId = null, BookingStatus status = BookingStatus.Confirmed, decimal price = 50.00m)
    {
        var start = DateTime.UtcNow.AddDays(1);
        return new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            StaffId = staffId,
            ClientId = clientId,
            StartTime = start,
            EndTime = start.AddMinutes(30),
            Status = status,
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Pricing Plan ──────────────────────────────────────────────────

    public static PricingPlan CreatePlan(Guid? id = null, string name = "Professional")
    {
        return new PricingPlan
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            IsActive = true,
            TrialDays = 14
        };
    }

    // ── Subscription ──────────────────────────────────────────────────

    public static Subscription CreateSubscription(Guid tenantId, Guid pricingPlanId, PricingPlan? plan = null)
    {
        return new Subscription
        {
            TenantId = tenantId,
            PricingPlanId = pricingPlanId,
            PricingPlan = plan,
            Status = SubscriptionStatus.Active,
            BillingInterval = BillingInterval.Monthly,
            StripeSubscriptionId = "sub_test_" + Guid.NewGuid().ToString()[..8],
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(15),
            BookingsUsed = 50,
            SmsUsed = 10,
            AiCreditsUsed = 5,
            AiMonthlyBudget = 25m
        };
    }

    // ── Marketing Funnel ──────────────────────────────────────────────

    public static MarketingFunnel CreateFunnel(Guid tenantId, Guid? id = null, string status = "draft")
    {
        return new MarketingFunnel
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = _faker.Commerce.ProductAdjective() + " Funnel",
            Status = status
        };
    }

    // ── PromoCode ─────────────────────────────────────────────────────

    public static PromoCode CreatePromoCode(string code = "TEST10", decimal discount = 10m,
        bool isActive = true, int? usageLimit = null, DateTime? expiresAt = null)
    {
        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            DiscountValue = discount,
            DiscountType = PromoType.Percentage,
            IsActive = isActive,
            UsageLimit = usageLimit,
            TimesUsed = 0,
            ExpiresAt = expiresAt
        };
    }

    // ── Notification ──────────────────────────────────────────────────

    public static Notification CreateNotification(Guid tenantId, Guid? userId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId ?? Guid.NewGuid(),
            Title = _faker.Lorem.Sentence(3),
            Message = _faker.Lorem.Sentence(10),
            Type = "info",
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Inventory Item ────────────────────────────────────────────────

    public static InventoryItem CreateInventoryItem(Guid tenantId, Guid? id = null)
    {
        return new InventoryItem
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = _faker.Commerce.ProductName(),
            Sku = _faker.Commerce.Ean8(),
            Quantity = _faker.Random.Int(1, 100),
            ReorderLevel = 5,
            CostPrice = _faker.Random.Decimal(1, 100),
            CreatedAt = DateTime.UtcNow
        };
    }

    // ── Workflow ───────────────────────────────────────────────────────

    public static Upkilo.Core.Entities.Workflow CreateWorkflow(Guid tenantId, Guid? id = null)
    {
        return new Upkilo.Core.Entities.Workflow
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = _faker.Commerce.ProductAdjective() + " Workflow",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
