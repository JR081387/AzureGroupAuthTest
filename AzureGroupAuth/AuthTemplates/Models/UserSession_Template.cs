namespace SOWTracker.Models;

public class UserSession
{
    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public bool IsActive(int timeoutMinutes = 15)
    {
        return (DateTime.UtcNow - LastActivity).TotalMinutes <= timeoutMinutes;
    }
}
