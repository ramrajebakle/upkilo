using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire job for seeding sample/demo data during onboarding.
/// </summary>
public class SampleDataSeedJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<SampleDataSeedJob> _logger;

    public SampleDataSeedJob(AppDbContext context, ILogger<SampleDataSeedJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(Guid tenantId, string templateId)
    {
        _logger.LogInformation("Seeding sample data '{Template}' for tenant {TenantId}", templateId, tenantId);

        try
        {
            var (serviceNames, staffNames, clientCount, bookingCount) = templateId switch
            {
                "spa" => (new[] { "Swedish Massage", "Deep Tissue", "Hot Stone", "Facial", "Body Wrap" },
                          new[] { "Emma Stone", "James Park", "Lily Chen" }, 20, 15),
                "salon" => (new[] { "Haircut", "Color", "Blowout", "Highlights", "Trim", "Keratin", "Balayage", "Updo" },
                            new[] { "Maria G.", "Alex R.", "Jordan T.", "Sam K." }, 25, 20),
                "dental" => (new[] { "Cleaning", "Filling", "Whitening", "Root Canal", "Crown", "Exam" },
                             new[] { "Dr. Smith", "Dr. Patel" }, 15, 10),
                "fitness" => (new[] { "HIIT", "Yoga", "Pilates", "Spin", "Boxing", "CrossFit", "Stretching", "Meditation", "Strength", "Cardio" },
                              new[] { "Coach Mike", "Coach Sarah", "Coach Raj", "Coach Amy", "Coach Dan" }, 30, 25),
                "consulting" => (new[] { "Strategy Session", "Financial Review", "Legal Consultation", "IT Assessment" },
                                 new[] { "John D.", "Sarah M.", "Mike R." }, 12, 8),
                _ => (new[] { "Service A", "Service B", "Service C" }, new[] { "Staff 1", "Staff 2" }, 10, 5)
            };

            // Seed services
            var services = new List<Service>();
            foreach (var name in serviceNames)
            {
                var svc = new Service
                {
                    TenantId = tenantId,
                    Name = name,
                    Duration = Random.Shared.Next(3, 12) * 15, // 45-180 min
                    Price = Random.Shared.Next(25, 200),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Services.Add(svc);
                services.Add(svc);
            }

            // Seed staff
            var staff = new List<StaffMember>();
            foreach (var name in staffNames)
            {
                var parts = name.Split(' ', 2);
                var member = new StaffMember
                {
                    TenantId = tenantId,
                    FirstName = parts[0],
                    LastName = parts.Length > 1 ? parts[1] : "",
                    Email = $"{parts[0].ToLower()}@demo.upkilo.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.StaffMembers.Add(member);
                staff.Add(member);
            }

            // Seed clients
            var clients = new List<Client>();
            var firstNames = new[] { "Alice", "Bob", "Carol", "David", "Eva", "Frank", "Grace", "Henry", "Iris", "Jack",
                                     "Kate", "Leo", "Mia", "Nathan", "Olivia", "Paul", "Quinn", "Ruby", "Sam", "Tina",
                                     "Uma", "Victor", "Wendy", "Xavier", "Yara", "Zane", "Aiden", "Bella", "Chris", "Diana" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };

            for (int i = 0; i < clientCount && i < firstNames.Length; i++)
            {
                var client = new Client
                {
                    TenantId = tenantId,
                    FirstName = firstNames[i],
                    LastName = lastNames[i % lastNames.Length],
                    Email = $"{firstNames[i].ToLower()}.{lastNames[i % lastNames.Length].ToLower()}@example.com",
                    Phone = $"+1555{Random.Shared.Next(1000000, 9999999)}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Clients.Add(client);
                clients.Add(client);
            }

            await _context.SaveChangesAsync();

            // Seed bookings
            for (int i = 0; i < bookingCount; i++)
            {
                var service = services[Random.Shared.Next(services.Count)];
                var client = clients[Random.Shared.Next(clients.Count)];
                var staffMember = staff[Random.Shared.Next(staff.Count)];
                var startTime = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30)).Date
                    .AddHours(Random.Shared.Next(9, 17));

                var booking = new Booking
                {
                    TenantId = tenantId,
                    ClientId = client.Id,
                    ServiceId = service.Id,
                    StaffId = staffMember.Id,
                    StartTime = startTime,
                    EndTime = startTime.AddMinutes(service.Duration),
                    Status = BookingStatus.Confirmed,
                    Source = BookingSource.Website,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Bookings.Add(booking);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sample data seeded for tenant {TenantId}: {Services} services, {Staff} staff, {Clients} clients, {Bookings} bookings",
                tenantId, services.Count, staff.Count, clients.Count, bookingCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed sample data for tenant {TenantId}", tenantId);
            throw;
        }
    }
}
