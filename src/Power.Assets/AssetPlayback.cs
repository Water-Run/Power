using Power.Core;

namespace Power.Assets;

/// <summary>Executes imported experiment events at exact ticks with no managed allocation during Advance.</summary>
public sealed class AssetPlayback
{
    private readonly Simulation _simulation;
    private readonly PowerAsset _asset;
    private int _nextInput, _busy;
    private ulong _time;
    public CompiledModel Model => _asset.Model;
    public ulong TimeNanoseconds => _time;
    public bool Completed => _time == _asset.DurationNanoseconds;

    internal AssetPlayback(PowerAsset asset)
    {
        _asset = asset;
        _simulation = asset.Model.CreateSimulation();
        while (_nextInput < asset.InputArray.Length && asset.InputArray[_nextInput].TimeNanoseconds == 0) ++_nextInput;
        var initial = new Scalar[_nextInput];
        for (int i = 0; i < initial.Length; ++i) initial[i] = new(asset.InputArray[i].Channel, asset.InputArray[i].Value);
        if (_simulation.SubmitInputs(initial) != SimulationStatus.Ok) throw new InvalidOperationException("Invalid compiled initial event.");
    }

    public SimulationStatus Advance(ulong deltaNanoseconds, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return SimulationStatus.Busy;
        try
        {
            if (deltaNanoseconds == 0 || deltaNanoseconds > _asset.DurationNanoseconds - _time)
                return SimulationStatus.InvalidTimeStep;
            ulong end = _time + deltaNanoseconds;
            int next = _nextInput;
            while (next < _asset.InputArray.Length && _asset.InputArray[next].TimeNanoseconds <= end) ++next;
            var status = _simulation.Step(deltaNanoseconds, _asset.InputArray.AsSpan(_nextInput, next - _nextInput), cancellationToken);
            if (status == SimulationStatus.Ok) { _nextInput = next; _time = end; }
            return status;
        }
        finally { Volatile.Write(ref _busy, 0); }
    }

    public SnapshotInfo ReadSnapshot(Span<Scalar> destination)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) throw new InvalidOperationException("Playback is busy on another thread.");
        try { return _simulation.ReadSnapshot(destination); }
        finally { Volatile.Write(ref _busy, 0); }
    }

    public Simulation ForkSimulation()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) throw new InvalidOperationException("Playback is busy on another thread.");
        try { return _simulation.Fork(); }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
