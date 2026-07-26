using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private readonly AppDbContext _context;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger, ISecretProvider secretProvider, AppDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        
        // Load from secret provider or config — NO mock fallback
        _clientId = secretProvider.GetSecret("Google--ClientId") ?? _configuration["Authentication:Google:ClientId"] ?? "";
        _clientSecret = secretProvider.GetSecret("Google--ClientSecret") ?? _configuration["Authentication:Google:ClientSecret"] ?? "";

        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            _logger.LogWarning("Google Calendar integration is NOT configured. Set Google--ClientId and Google--ClientSecret in Key Vault or appsettings.");
        }
    }

    /// <summary>Returns true if Google Calendar credentials are properly configured.</summary>
    public bool IsConfigured => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public string GetAuthUrl(string provider, Guid staffId)
    {
        var redirectUri = _configuration["Authentication:Google:RedirectUri"];
        return $"https://accounts.google.com/o/oauth2/v2/auth?client_id={_clientId}&redirect_uri={redirectUri}&response_type=code&scope=https://www.googleapis.com/auth/calendar.events https://www.googleapis.com/auth/calendar.readonly&access_type=offline&prompt=consent&state={staffId}";
    }

    public async Task<CalendarSyncToken> ConnectAsync(string provider, Guid staffId, string code)
    {
        var redirectUri = _configuration["Authentication:Google:RedirectUri"];
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret }
        });

        var tokenResponse = await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);

        var token = new CalendarSyncToken
        {
            Id = Guid.NewGuid(),
            StaffId = staffId,
            Provider = "google",
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3599),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SyncDirection = "TwoWay"
        };

        _context.CalendarSyncTokens.Add(token);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task SyncBookingsAsync(Guid staffId)
    {
        var token = await _context.CalendarSyncTokens
            .FirstOrDefaultAsync(t => t.StaffId == staffId && t.Provider == "google" && t.IsActive);
        
        if (token == null) return;

        var accessToken = await GetValidAccessTokenAsync(token);
        
        // Find bookings to push
        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Service)
            .Where(b => b.StaffId == staffId && b.StartTime >= DateTime.UtcNow)
            .ToListAsync();

        foreach (var booking in bookings)
        {
            var eventId = await PushBookingAsync(accessToken, booking, booking.ExternalId);
            if (booking.ExternalId != eventId)
            {
                booking.ExternalId = eventId;
            }
        }

        token.LastSyncAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetValidAccessTokenAsync(CalendarSyncToken token)
    {
        if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return token.AccessToken;
        }

        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            throw new InvalidOperationException("No refresh token available for Google Calendar.");
        }

        var result = await RefreshAccessTokenAsync(token.RefreshToken);
        token.AccessToken = result.AccessToken;
        token.ExpiresAt = result.ExpiresAt;
        await _context.SaveChangesAsync();

        return token.AccessToken;
    }

    public async Task<(string AccessToken, DateTime ExpiresAt)> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                }
            });

            var credential = new UserCredential(flow, "user", new TokenResponse
            {
                RefreshToken = refreshToken
            });

            var success = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!success)
            {
                throw new Exception("Failed to refresh Google OAuth token.");
            }

            // Expiration is typically in seconds, but TokenResponse usually tracks IssuedUtc + ExpiresInSeconds
            // We approximate adding 3500 just to be safe if ExpiresInSeconds is not strictly populated
            var expiresIn = credential.Token.ExpiresInSeconds ?? 3599; 
            return (credential.Token.AccessToken, DateTime.UtcNow.AddSeconds(expiresIn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Google Access Token.");
            throw;
        }
    }

    public async Task<string> PushBookingAsync(string accessToken, Booking booking, string? existingEventId = null)
    {
        var service = CreateCalendarService(accessToken);

        var calendarEvent = new Event
        {
            Summary = $"Booking: {booking.Client?.FirstName} {booking.Client?.LastName}",
            Location = booking.LocationId.ToString(),
            Description = $"Service: {booking.Service?.Name}\nPrice: {booking.Price}\nStatus: {booking.Status}",
            Start = new EventDateTime { DateTimeDateTimeOffset = booking.StartTime },
            End = new EventDateTime { DateTimeDateTimeOffset = booking.EndTime },
            ExtendedProperties = new Event.ExtendedPropertiesData
            {
                Private__ = new Dictionary<string, string>
                {
                    { "upkilo_booking_id", booking.Id.ToString() }
                }
            }
        };

        try
        {
            if (!string.IsNullOrEmpty(existingEventId))
            {
                var request = service.Events.Update(calendarEvent, "primary", existingEventId);
                var updatedEvent = await request.ExecuteAsync();
                return updatedEvent.Id;
            }
            else
            {
                var request = service.Events.Insert(calendarEvent, "primary");
                var createdEvent = await request.ExecuteAsync();
                return createdEvent.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push booking {BookingId} to Google Calendar.", booking.Id);
            throw;
        }
    }

    public async Task DeleteBookingAsync(string accessToken, string eventId)
    {
        var service = CreateCalendarService(accessToken);
        try
        {
            await service.Events.Delete("primary", eventId).ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete event {EventId} from Google Calendar.", eventId);
        }
    }

    public async Task<IEnumerable<GoogleCalendarEvent>> PullEventsAsync(string accessToken, DateTime from, DateTime to)
    {
        var service = CreateCalendarService(accessToken);
        try
        {
            var request = service.Events.List("primary");
            request.TimeMinDateTimeOffset = from;
            request.TimeMaxDateTimeOffset = to;
            request.ShowDeleted = false;
            request.SingleEvents = true; // Flattens recurring events
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            var result = new List<GoogleCalendarEvent>();

            foreach (var evt in events.Items)
            {
                // Skip if this is an event pushed by Upkilo
                if (evt.ExtendedProperties?.Private__ != null && evt.ExtendedProperties.Private__.ContainsKey("upkilo_booking_id"))
                {
                    continue; // Do not pull back our own bookings as block-offs
                }

                if (evt.Start?.DateTimeDateTimeOffset == null || evt.End?.DateTimeDateTimeOffset == null)
                    continue; // Skip all-day events for now or parse them if needed

                result.Add(new GoogleCalendarEvent
                {
                    Id = evt.Id,
                    Title = evt.Summary ?? "Busy",
                    StartTime = evt.Start.DateTimeDateTimeOffset.Value.UtcDateTime,
                    EndTime = evt.End.DateTimeDateTimeOffset.Value.UtcDateTime,
                    Status = evt.Status
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pull events from Google Calendar.");
            throw;
        }
    }

    private CalendarService CreateCalendarService(string accessToken)
    {
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName = "Upkilo"
        });
    }
}
