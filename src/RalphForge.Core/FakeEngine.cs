namespace RalphForge.Core;

/// Deterministic engine for tests: replies with a fixed, ordered list of responses
/// (and empty once exhausted). No network, no auth — exercises the loop logic alone.
public sealed class FakeEngine : IEngine
{
    private readonly Queue<string> _responses;

    public FakeEngine(params string[] responses) => _responses = new Queue<string>(responses);

    public Task<EngineResult> RunAsync(EngineRequest request, CancellationToken ct = default)
    {
        var reply = _responses.Count > 0 ? _responses.Dequeue() : "";
        return Task.FromResult(new EngineResult(true, reply, "", 0));
    }
}
