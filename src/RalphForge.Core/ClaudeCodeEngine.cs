using System.Text;
using CliWrap;

namespace RalphForge.Core;

/// Wraps the Claude Code CLI (<c>claude -p</c>) as a subprocess engine — we wrap
/// the binary, we do NOT call the Anthropic API/SDK. The executable is resolved
/// from the RALPH_CLAUDE env var, else from PATH as "claude". On non-interactive
/// SSH (e.g. the codespace loop service) PATH is not loaded, so set RALPH_CLAUDE
/// to the absolute path (~/.local/bin/claude).
public sealed class ClaudeCodeEngine : IEngine
{
    private readonly string _exe;

    public ClaudeCodeEngine(string? executable = null)
        => _exe = executable
            ?? Environment.GetEnvironmentVariable("RALPH_CLAUDE")
            ?? "claude";

    public async Task<EngineResult> RunAsync(EngineRequest request, CancellationToken ct = default)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var cmd = Cli.Wrap(_exe)
            .WithArguments(args => args
                .Add("-p").Add(request.Prompt)
                .Add("--output-format").Add("text"))
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr));

        if (request.WorkingDirectory is { } wd)
            cmd = cmd.WithWorkingDirectory(wd);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (request.Timeout is { } t)
            cts.CancelAfter(t);

        try
        {
            var result = await cmd.ExecuteAsync(cts.Token);
            return new EngineResult(
                Success: result.ExitCode == 0,
                Output: stdout.ToString().Trim(),
                Error: stderr.ToString().Trim(),
                ExitCode: result.ExitCode);
        }
        catch (OperationCanceledException)
        {
            return new EngineResult(false, stdout.ToString().Trim(), "engine timed out", -1);
        }
        catch (Exception ex)
        {
            return new EngineResult(false, stdout.ToString().Trim(), $"engine failed: {ex.Message}", -1);
        }
    }
}
