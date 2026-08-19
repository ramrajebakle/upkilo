namespace Upkilo.Core.Entities;

/// <summary>
/// Vehicle size class. This is the pricing axis for auto detailing: the same service takes
/// materially longer and costs more on a three-row SUV than on a coupe, so quoting one price per
/// service would either underprice large vehicles or overprice small ones.
///
/// Deliberately a small closed set rather than free text. It is a join key — ServiceVehiclePrice
/// rows hang off it — and free text would let "SUV", "suv" and "S.U.V." become three
/// unpriceable variants of the same thing.
/// </summary>
public enum VehicleClass
{
    Sedan = 0,
    Coupe = 1,
    Hatchback = 2,
    SUV = 3,
    Truck = 4,
    Van = 5,
    Motorcycle = 6,
    /// <summary>Oversized, exotic or otherwise non-standard — priced by quote.</summary>
    Other = 7,
}

/// <summary>
/// A vehicle belonging to a client.
///
/// Auto detailing is the one vertical Upkilo serves where the subject of the work is not the
/// person who booked it. Without this, a detailer has no way to record that the SUV serviced in
/// March and the coupe booked in June belong to the same customer — which is what makes both
/// accurate quoting and "your SUV is due again" reactivation possible.
/// </summary>
public class Vehicle : TenantEntity
{
    public Guid ClientId { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }

    /// <summary>Drives price and duration via ServiceVehiclePrice.</summary>
    public VehicleClass Class { get; set; } = VehicleClass.Sedan;

    /// <summary>
    /// Registration/plate. Stored to tell two otherwise-identical vehicles apart on the same
    /// account; it is personal data, so it is never required and never surfaced publicly.
    /// </summary>
    public string? LicensePlate { get; set; }

    public string? Color { get; set; }

    /// <summary>Condition notes carried between visits — existing swirls, trim damage, pet use.</summary>
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual Client? Client { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public string DisplayName =>
        string.Join(" ", new[] { Year?.ToString(), Make, Model }.Where(p => !string.IsNullOrWhiteSpace(p)))
        is { Length: > 0 } named ? named : Class.ToString();
}

/// <summary>
/// Price and duration for one service on one vehicle class.
///
/// Rows are an override layer, not a replacement: a service with no row for a given class falls
/// back to its own Price and DurationMinutes. That keeps the feature invisible to the verticals
/// that do not need it — a hair salon never creates one — while letting a detailer price the
/// full matrix without a service per vehicle size.
/// </summary>
public class ServiceVehiclePrice : TenantEntity
{
    public Guid ServiceId { get; set; }
    public VehicleClass VehicleClass { get; set; }

    /// <summary>Price for this service on this vehicle class, in the service's currency.</summary>
    public decimal Price { get; set; }

    /// <summary>
    /// How long the job actually takes on this class. This is the field that stops a calendar
    /// from being overbooked: an 8-hour paint correction booked into a 3-hour slot costs the
    /// business the rest of the day.
    /// </summary>
    public int DurationMinutes { get; set; }

    // Navigation
    public virtual Tenant? Tenant { get; set; }
    public virtual Service? Service { get; set; }
}
