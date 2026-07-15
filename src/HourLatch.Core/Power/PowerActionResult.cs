namespace HourLatch.Core.Power;

public sealed record PowerActionResult(
    bool Succeeded,
    string? Message = null,
    int? ErrorCode = null)
{
    public static PowerActionResult Success() => new(true);

    public static PowerActionResult Failure(string message, int? errorCode = null) =>
        new(false, message, errorCode);
}
