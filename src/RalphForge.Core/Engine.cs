namespace RalphForge.Core;

/// A request to the coding engine: a prompt plus optional execution context.
public sealed record EngineRequest(
    string Prompt,
    string? WorkingDirectory = null,
    TimeSpan? Timeout = null);

/// The engine's reply. Infrastructure failures (timeout, binary missing) are
/// reported as <c>Success = false</c> results, never thrown — the loop treats
/// them as ordinary failed iterations.
public sealed record EngineResult(
    bool Success,
    string Output,
    string Error,
    int ExitCode);

/// The coding engine abstraction. The loop and any future GUI depend only on this;
/// the concrete engine (Claude Code CLI) is swappable and fakeable for tests.
public interface IEngine
{
    Task<EngineResult> RunAsync(EngineRequest request, CancellationToken ct = default);
}
