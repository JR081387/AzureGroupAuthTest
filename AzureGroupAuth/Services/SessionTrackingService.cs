using System.Collections.Concurrent;
using AzureGroupAuth.Models;

namespace AzureGroupAuth.Services;

public class SessionTrackingService
{
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
    private readonly ILogger<SessionTrackingService> _logger;

    public SessionTrackingService(ILogger<SessionTrackingService> logger)
    {
        _logger = logger;
    }

    public void RegisterSession(string objectId, string displayName, string email, string ipAddress, string userAgent)
    {
        var session = new UserSession
        {
            ObjectId = objectId,
            DisplayName = displayName,
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            LoginTime = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        _sessions.AddOrUpdate(objectId, session, (_, _) => session);
        _logger.LogInformation("Registered active session for {DisplayName} ({Email})", displayName, email);
    }

    public void UpdateActivity(string objectId, string ipAddress)
    {
        if (_sessions.TryGetValue(objectId, out var session))
        {
            session.LastActivity = DateTime.UtcNow;
            session.IpAddress = ipAddress;
        }
    }

    public void RemoveSession(string objectId)
    {
        if (_sessions.TryRemove(objectId, out var session))
        {
            _logger.LogInformation("Removed session for {DisplayName} ({Email})", session.DisplayName, session.Email);
        }
    }

    public List<UserSession> GetActiveSessions(int timeoutMinutes = 15)
    {
        PruneStale(timeoutMinutes * 2);
        return _sessions.Values
            .Where(s => s.IsActive(timeoutMinutes))
            .OrderByDescending(s => s.LastActivity)
            .ToList();
    }

    public List<UserSession> GetAllSessions()
    {
        return _sessions.Values
            .OrderByDescending(s => s.LastActivity)
            .ToList();
    }

    public int ActiveCount(int timeoutMinutes = 15)
    {
        return _sessions.Values.Count(s => s.IsActive(timeoutMinutes));
    }

    private void PruneStale(int staleMinutes = 30)
    {
        var staleKeys = _sessions
            .Where(kvp => (DateTime.UtcNow - kvp.Value.LastActivity).TotalMinutes > staleMinutes)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            if (_sessions.TryRemove(key, out var session))
            {
                _logger.LogInformation("Pruned stale session for {DisplayName}", session.DisplayName);
            }
        }
    }
}
