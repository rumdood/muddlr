namespace Muddlr.Api.HealthStatus;

internal class MuddlrStatus
{
    public string ApiVersion { get; set; }
    public string CoreVersion { get; set; }
    public HealthStatus Status { get; init; } = HealthStatus.Unknown;
}

public enum HealthStatus
{
    Ok,
    Unknown,
    UnknownError,
}