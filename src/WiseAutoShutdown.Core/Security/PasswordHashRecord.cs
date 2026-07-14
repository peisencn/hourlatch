namespace WiseAutoShutdown.Core.Security;

public sealed record PasswordHashRecord(
    string Algorithm,
    int Iterations,
    string SaltBase64,
    string HashBase64);
