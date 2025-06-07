using System.Text.Json.Serialization;
using Domain.Models;

namespace Application.Dtos;

public class NotificationDto
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required NotificationType Type { get; set; }
}