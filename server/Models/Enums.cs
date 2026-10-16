using System.Text.Json.Serialization;

namespace CampusRent.Server.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    STUDENT,
    LANDLORD,
    ADMIN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RoomStatus
{
    AVAILABLE,
    RENTED,
    HIDDEN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    PENDING,
    RESOLVED,
    DISMISSED
}
