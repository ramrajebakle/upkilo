using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds realistic dummy data for local development and testing.
/// Idempotent: checks for existing dev tenants before inserting.
/// Test credentials: owner@glowbeauty.test / Test@1234! (and similar for other tenants)
/// </summary>
public static class DevDataSeeder
{
    private const string DevPassword = "Test@1234!";

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Tenants.AnyAsync(t => t.Slug == "glow-beauty-dev"))
            return;

        // ── Tenants ──────────────────────────────────────────────────
        var glowTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Glow Beauty Studio",
            Slug = "glow-beauty-dev",
            Industry = "Beauty",
            BusinessType = "Salon",
            Email = "hello@glowbeauty.test",
            Phone = "+1-555-100-0001",
            City = "Austin",
            Country = "US",
            Currency = "USD",
            Timezone = "America/Chicago",
            SubscriptionTier = SubscriptionTier.Starter,
            Status = TenantStatus.Active,
            IsActive = true,
            TrialEndsAt = DateTime.UtcNow.AddDays(7),
            SubscriptionPeriodEnd = DateTime.UtcNow.AddDays(30)
        };

        var fitTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "FitLife Gym",
            Slug = "fitlife-gym-dev",
            Industry = "Fitness",
            BusinessType = "Gym",
            Email = "info@fitlifegym.test",
            Phone = "+1-555-200-0001",
            City = "Denver",
            Country = "US",
            Currency = "USD",
            Timezone = "America/Denver",
            SubscriptionTier = SubscriptionTier.Professional,
            Status = TenantStatus.Active,
            IsActive = true,
            SubscriptionPeriodEnd = DateTime.UtcNow.AddDays(60)
        };

        var pawTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "PawCare Clinic",
            Slug = "pawcare-clinic-dev",
            Industry = "Veterinary",
            BusinessType = "Clinic",
            Email = "care@pawcare.test",
            Phone = "+1-555-300-0001",
            City = "Portland",
            Country = "US",
            Currency = "USD",
            Timezone = "America/Los_Angeles",
            SubscriptionTier = SubscriptionTier.Business,
            Status = TenantStatus.Active,
            IsActive = true,
            SubscriptionPeriodEnd = DateTime.UtcNow.AddDays(90)
        };

        context.Tenants.AddRange(glowTenant, fitTenant, pawTenant);

        // ── Users (owners) ────────────────────────────────────────────
        var glowOwner = MakeUser(glowTenant.Id, "owner@glowbeauty.test", "Sophia", "Carter", UserRole.Owner);
        var fitOwner  = MakeUser(fitTenant.Id,  "owner@fitlifegym.test",  "Marcus", "Reed",   UserRole.Owner);
        var pawOwner  = MakeUser(pawTenant.Id,  "owner@pawcare.test",      "Dr. Elena", "Voss", UserRole.Owner);

        context.Users.AddRange(glowOwner, fitOwner, pawOwner);

        // ── Staff members ─────────────────────────────────────────────
        var (glowStaff1, glowUser1) = MakeStaff(glowTenant.Id, "Emma", "Wilson",  "staff1@glowbeauty.test", "Senior Stylist",   "#F472B6");
        var (glowStaff2, glowUser2) = MakeStaff(glowTenant.Id, "James", "Lee",    "staff2@glowbeauty.test", "Nail Technician",  "#A78BFA");
        var (fitStaff1,  fitUser1)  = MakeStaff(fitTenant.Id,  "Priya", "Sharma", "staff1@fitlifegym.test", "Personal Trainer", "#34D399");
        var (fitStaff2,  fitUser2)  = MakeStaff(fitTenant.Id,  "Mike",  "Torres", "staff2@fitlifegym.test", "Yoga Instructor",  "#60A5FA");
        var (pawStaff1,  pawUser1)  = MakeStaff(pawTenant.Id,  "Dr. Sarah", "Chen", "staff1@pawcare.test",  "Veterinarian",     "#FBBF24");
        var (pawStaff2,  pawUser2)  = MakeStaff(pawTenant.Id,  "Alex",  "Kim",    "staff2@pawcare.test",    "Groomer",          "#F87171");

        context.Users.AddRange(glowUser1, glowUser2, fitUser1, fitUser2, pawUser1, pawUser2);
        context.StaffMembers.AddRange(glowStaff1, glowStaff2, fitStaff1, fitStaff2, pawStaff1, pawStaff2);

        // ── Services ──────────────────────────────────────────────────
        var glowServices = new[]
        {
            MakeService(glowTenant.Id, "Haircut & Style",    45m,  45, "#F472B6", "Hair"),
            MakeService(glowTenant.Id, "Full Color",         120m, 90, "#EC4899", "Hair"),
            MakeService(glowTenant.Id, "Manicure",           35m,  45, "#A78BFA", "Nails"),
            MakeService(glowTenant.Id, "Pedicure",           55m,  60, "#7C3AED", "Nails"),
            MakeService(glowTenant.Id, "Hydrating Facial",   80m,  60, "#FDE68A", "Skin"),
        };

        var fitServices = new[]
        {
            MakeService(fitTenant.Id, "Personal Training Session", 75m, 60, "#34D399", "Training"),
            MakeService(fitTenant.Id, "Group HIIT Class",          25m, 45, "#10B981", "Classes"),
            MakeService(fitTenant.Id, "Yoga Flow",                 30m, 60, "#60A5FA", "Classes"),
            MakeService(fitTenant.Id, "Nutrition Consultation",    60m, 50, "#F59E0B", "Wellness"),
            MakeService(fitTenant.Id, "Recovery & Stretch",        50m, 45, "#FCA5A5", "Wellness"),
        };

        var pawServices = new[]
        {
            MakeService(pawTenant.Id, "Wellness Checkup",    65m,  30, "#FBBF24", "Medical"),
            MakeService(pawTenant.Id, "Vaccination",         45m,  20, "#F59E0B", "Medical"),
            MakeService(pawTenant.Id, "Full Grooming",       55m,  90, "#F87171", "Grooming"),
            MakeService(pawTenant.Id, "Dental Cleaning",     120m, 60, "#EF4444", "Medical"),
            MakeService(pawTenant.Id, "Microchipping",       40m,  15, "#FCA5A5", "Medical"),
        };

        context.Services.AddRange(glowServices);
        context.Services.AddRange(fitServices);
        context.Services.AddRange(pawServices);

        // ── Clients ───────────────────────────────────────────────────
        var glowClients = new[]
        {
            MakeClient(glowTenant.Id, "Olivia", "Martinez", "olivia.m@example.test", "+1-555-111-0001"),
            MakeClient(glowTenant.Id, "Ava",    "Johnson",  "ava.j@example.test",    "+1-555-111-0002"),
            MakeClient(glowTenant.Id, "Mia",    "Brown",    "mia.b@example.test",    "+1-555-111-0003"),
            MakeClient(glowTenant.Id, "Chloe",  "Davis",    "chloe.d@example.test",  "+1-555-111-0004"),
            MakeClient(glowTenant.Id, "Zoe",    "Wilson",   "zoe.w@example.test",    "+1-555-111-0005"),
            MakeClient(glowTenant.Id, "Harper", "Moore",    "harper.m@example.test", "+1-555-111-0006"),
            MakeClient(glowTenant.Id, "Lily",   "Taylor",   "lily.t@example.test",   "+1-555-111-0007"),
            MakeClient(glowTenant.Id, "Emma",   "Anderson", "emma.a@example.test",   "+1-555-111-0008"),
            MakeClient(glowTenant.Id, "Nora",   "Thomas",   "nora.t@example.test",   "+1-555-111-0009"),
            MakeClient(glowTenant.Id, "Isla",   "White",    "isla.w@example.test",   "+1-555-111-0010"),
        };

        var fitClients = new[]
        {
            MakeClient(fitTenant.Id, "Liam",   "Garcia",   "liam.g@example.test",   "+1-555-222-0001"),
            MakeClient(fitTenant.Id, "Noah",   "Miller",   "noah.m@example.test",   "+1-555-222-0002"),
            MakeClient(fitTenant.Id, "Ethan",  "Jones",    "ethan.j@example.test",  "+1-555-222-0003"),
            MakeClient(fitTenant.Id, "Aiden",  "Clark",    "aiden.c@example.test",  "+1-555-222-0004"),
            MakeClient(fitTenant.Id, "Lucas",  "Lewis",    "lucas.l@example.test",  "+1-555-222-0005"),
            MakeClient(fitTenant.Id, "Mason",  "Robinson", "mason.r@example.test",  "+1-555-222-0006"),
            MakeClient(fitTenant.Id, "Logan",  "Walker",   "logan.w@example.test",  "+1-555-222-0007"),
            MakeClient(fitTenant.Id, "Elijah", "Hall",     "elijah.h@example.test", "+1-555-222-0008"),
            MakeClient(fitTenant.Id, "James",  "Allen",    "james.a@example.test",  "+1-555-222-0009"),
            MakeClient(fitTenant.Id, "Oliver", "Young",    "oliver.y@example.test", "+1-555-222-0010"),
        };

        var pawClients = new[]
        {
            MakeClient(pawTenant.Id, "Charlotte", "Scott",    "charlotte.s@example.test", "+1-555-333-0001"),
            MakeClient(pawTenant.Id, "Amelia",    "Green",    "amelia.g@example.test",    "+1-555-333-0002"),
            MakeClient(pawTenant.Id, "Evelyn",    "Adams",    "evelyn.a@example.test",    "+1-555-333-0003"),
            MakeClient(pawTenant.Id, "Abigail",   "Baker",    "abigail.b@example.test",   "+1-555-333-0004"),
            MakeClient(pawTenant.Id, "Emily",     "Nelson",   "emily.n@example.test",     "+1-555-333-0005"),
            MakeClient(pawTenant.Id, "Elizabeth", "Carter",   "elizabeth.c@example.test", "+1-555-333-0006"),
            MakeClient(pawTenant.Id, "Sofia",     "Mitchell", "sofia.m@example.test",     "+1-555-333-0007"),
            MakeClient(pawTenant.Id, "Avery",     "Perez",    "avery.p@example.test",     "+1-555-333-0008"),
            MakeClient(pawTenant.Id, "Scarlett",  "Roberts",  "scarlett.r@example.test",  "+1-555-333-0009"),
            MakeClient(pawTenant.Id, "Grace",     "Turner",   "grace.t@example.test",     "+1-555-333-0010"),
        };

        context.Clients.AddRange(glowClients);
        context.Clients.AddRange(fitClients);
        context.Clients.AddRange(pawClients);

        // Save entities referenced by bookings/invoices before creating them
        await context.SaveChangesAsync();

        // ── Bookings ──────────────────────────────────────────────────
        var now = DateTime.UtcNow;
        var bookings = new List<Booking>();

        // Glow Beauty — 20 bookings spread over past 60 days and next 14 days
        for (int i = 0; i < 10; i++)
        {
            var client  = glowClients[i % glowClients.Length];
            var service = glowServices[i % glowServices.Length];
            var staff   = i % 2 == 0 ? glowStaff1 : glowStaff2;
            var start   = now.AddDays(-60 + i * 6).Date.AddHours(9 + (i % 5));

            bookings.Add(MakeBooking(glowTenant.Id, client, staff, service, start,
                status: BookingStatus.Completed, payment: PaymentStatus.Succeeded));
        }
        for (int i = 0; i < 6; i++)
        {
            var client  = glowClients[i % glowClients.Length];
            var service = glowServices[i % glowServices.Length];
            var staff   = i % 2 == 0 ? glowStaff1 : glowStaff2;
            var start   = now.AddDays(1 + i * 2).Date.AddHours(10 + (i % 4));

            bookings.Add(MakeBooking(glowTenant.Id, client, staff, service, start,
                status: BookingStatus.Confirmed, payment: PaymentStatus.Pending));
        }
        // 2 cancelled
        bookings.Add(MakeBooking(glowTenant.Id, glowClients[0], glowStaff1, glowServices[0],
            now.AddDays(-5).Date.AddHours(14), BookingStatus.Cancelled, PaymentStatus.Pending,
            cancellationReason: "Client rescheduled"));
        bookings.Add(MakeBooking(glowTenant.Id, glowClients[2], glowStaff2, glowServices[2],
            now.AddDays(-2).Date.AddHours(11), BookingStatus.NoShow, PaymentStatus.Pending));

        // FitLife Gym — 20 bookings
        for (int i = 0; i < 10; i++)
        {
            var client  = fitClients[i % fitClients.Length];
            var service = fitServices[i % fitServices.Length];
            var staff   = i % 2 == 0 ? fitStaff1 : fitStaff2;
            var start   = now.AddDays(-45 + i * 4).Date.AddHours(7 + (i % 6));

            bookings.Add(MakeBooking(fitTenant.Id, client, staff, service, start,
                status: BookingStatus.Completed, payment: PaymentStatus.Succeeded));
        }
        for (int i = 0; i < 8; i++)
        {
            var client  = fitClients[i % fitClients.Length];
            var service = fitServices[i % fitServices.Length];
            var staff   = i % 2 == 0 ? fitStaff1 : fitStaff2;
            var start   = now.AddDays(1 + i).Date.AddHours(8 + (i % 5));

            bookings.Add(MakeBooking(fitTenant.Id, client, staff, service, start,
                status: BookingStatus.Confirmed, payment: PaymentStatus.Pending));
        }
        bookings.Add(MakeBooking(fitTenant.Id, fitClients[1], fitStaff1, fitServices[1],
            now.AddDays(-3).Date.AddHours(9), BookingStatus.Cancelled, PaymentStatus.Pending,
            cancellationReason: "Instructor unavailable"));
        bookings.Add(MakeBooking(fitTenant.Id, fitClients[3], fitStaff2, fitServices[2],
            now.AddDays(-1).Date.AddHours(18), BookingStatus.Completed, PaymentStatus.Succeeded));

        // PawCare — 20 bookings
        for (int i = 0; i < 10; i++)
        {
            var client  = pawClients[i % pawClients.Length];
            var service = pawServices[i % pawServices.Length];
            var staff   = i % 2 == 0 ? pawStaff1 : pawStaff2;
            var start   = now.AddDays(-30 + i * 3).Date.AddHours(9 + (i % 4));

            bookings.Add(MakeBooking(pawTenant.Id, client, staff, service, start,
                status: BookingStatus.Completed, payment: PaymentStatus.Succeeded));
        }
        for (int i = 0; i < 8; i++)
        {
            var client  = pawClients[i % pawClients.Length];
            var service = pawServices[i % pawServices.Length];
            var staff   = i % 2 == 0 ? pawStaff1 : pawStaff2;
            var start   = now.AddDays(1 + i * 2).Date.AddHours(10 + (i % 3));

            bookings.Add(MakeBooking(pawTenant.Id, client, staff, service, start,
                status: BookingStatus.Confirmed, payment: PaymentStatus.Pending));
        }
        bookings.Add(MakeBooking(pawTenant.Id, pawClients[0], pawStaff1, pawServices[0],
            now.AddDays(-4).Date.AddHours(11), BookingStatus.Cancelled, PaymentStatus.Pending,
            cancellationReason: "Pet ill"));
        bookings.Add(MakeBooking(pawTenant.Id, pawClients[4], pawStaff2, pawServices[2],
            now.AddDays(-7).Date.AddHours(14), BookingStatus.Completed, PaymentStatus.Succeeded));

        context.Bookings.AddRange(bookings);

        // ── Invoices ──────────────────────────────────────────────────
        var invoices = new List<Invoice>();
        int invoiceSeq = 1000;

        // Pull completed bookings to generate invoices from
        var completedGlow = bookings.Where(b => b.TenantId == glowTenant.Id && b.Status == BookingStatus.Completed).Take(6).ToList();
        var completedFit  = bookings.Where(b => b.TenantId == fitTenant.Id  && b.Status == BookingStatus.Completed).Take(6).ToList();
        var completedPaw  = bookings.Where(b => b.TenantId == pawTenant.Id  && b.Status == BookingStatus.Completed).Take(6).ToList();

        foreach (var b in completedGlow) invoices.Add(MakeInvoice(glowTenant.Id, b, ref invoiceSeq));
        foreach (var b in completedFit)  invoices.Add(MakeInvoice(fitTenant.Id,  b, ref invoiceSeq));
        foreach (var b in completedPaw)  invoices.Add(MakeInvoice(pawTenant.Id,  b, ref invoiceSeq));

        // Add a few overdue invoices (sent but not paid)
        var overdueGlow = MakeInvoice(glowTenant.Id, completedGlow[0], ref invoiceSeq, overdue: true);
        var overdueFit  = MakeInvoice(fitTenant.Id,  completedFit[0],  ref invoiceSeq, overdue: true);
        invoices.Add(overdueGlow);
        invoices.Add(overdueFit);

        context.Invoices.AddRange(invoices);

        await context.SaveChangesAsync();
    }

    // ── Factories ────────────────────────────────────────────────────

    private static User MakeUser(Guid tenantId, string email,
        string firstName, string lastName, UserRole role)
    {
        var user = new User
        {
            TenantId = tenantId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            Status = UserStatus.Active,
            IsActive = true,
            EmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow.AddDays(-30)
        };
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DevPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        return user;
    }

    private static (StaffMember staff, User user) MakeStaff(Guid tenantId,
        string firstName, string lastName, string email, string role, string color)
    {
        var user = MakeUser(tenantId, email, firstName, lastName, UserRole.Staff);

        var staff = new StaffMember
        {
            TenantId = tenantId,
            UserId = user.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            Color = color,
            IsActive = true,
            HourlyRate = 25m,
            EmploymentType = EmploymentType.FullTime,
            DateJoined = DateTime.UtcNow.AddDays(-180)
        };

        return (staff, user);
    }

    private static Service MakeService(Guid tenantId, string name, decimal price, int durationMinutes,
        string color, string category) => new()
    {
        TenantId = tenantId,
        Name = name,
        Price = price,
        DurationMinutes = durationMinutes,
        Duration = durationMinutes,
        Color = color,
        Category = category,
        IsActive = true,
        Currency = "USD"
    };

    private static Client MakeClient(Guid tenantId, string firstName, string lastName,
        string email, string phone) => new()
    {
        TenantId = tenantId,
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Phone = phone,
        IsActive = true,
        MarketingConsent = true,
        Source = "Walk-in",
        LoyaltyTier = "Bronze",
        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(10, 180))
    };

    private static Booking MakeBooking(Guid tenantId, Client client, StaffMember staff,
        Service service, DateTime start, BookingStatus status, PaymentStatus payment,
        string? cancellationReason = null) => new()
    {
        TenantId = tenantId,
        ClientId = client.Id,
        StaffId = staff.Id,
        ServiceId = service.Id,
        CustomerName = client.FullName,
        CustomerEmail = client.Email,
        CustomerPhone = client.Phone,
        ServiceName = service.Name,
        StaffName = staff.FirstName + " " + staff.LastName,
        StartTime = start,
        EndTime = start.AddMinutes(service.DurationMinutes),
        Status = status,
        PaymentStatus = payment,
        Price = service.Price,
        Source = BookingSource.Manual,
        CancellationReason = cancellationReason,
        CancelledAt = cancellationReason != null ? start.AddDays(-1) : null
    };

    private static Invoice MakeInvoice(Guid tenantId, Booking booking, ref int seq, bool overdue = false)
    {
        var price = booking.Price ?? 0;
        var issueDate = booking.StartTime.AddDays(1);
        var dueDate = issueDate.AddDays(14);
        var status = overdue ? InvoiceStatus.Overdue : InvoiceStatus.Paid;
        var paidAt = overdue ? (DateTime?)null : issueDate.AddDays(Random.Shared.Next(1, 10));

        var invoice = new Invoice
        {
            TenantId = tenantId,
            InvoiceNumber = $"INV-{seq++}",
            ClientId = booking.ClientId,
            CustomerName = booking.CustomerName ?? "Guest",
            CustomerEmail = booking.CustomerEmail,
            IssueDate = issueDate,
            DueDate = dueDate,
            TotalAmount = price,
            Currency = "USD",
            Status = status,
            PaidAt = paidAt,
            Type = "Service"
        };

        invoice.Items.Add(new InvoiceItem
        {
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            Description = booking.ServiceName ?? "Service",
            Quantity = 1,
            UnitPrice = price,
            Amount = price,
            TaxRate = 0m,
            TaxAmount = 0m,
            TotalAmount = price
        });

        return invoice;
    }
}
