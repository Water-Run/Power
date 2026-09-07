using System.Text.Json;
using Power.Assets;
using Power.Core;
using Power.Experiments;
using Power.Tests;
using static Power.Tests.CoreChecks;

string sample = Path.Combine(AppContext.BaseDirectory, "electrothermal.power.json");
string source = File.ReadAllText(sample);
var checks = CoreChecks.All.Concat(AssetChecks.All).Concat(AgentChecks.All).Concat(new (string, Action)[]
{
    ("JSON experiment / replay / reference values", () =>
    {
        var document = ModelDocument.Parse(source);
        var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 11);
        Require(report.Model.Fingerprint == CompiledModel.Compile(SampleModels.Electrothermal()).Fingerprint.ToString("x16"));
        double Output(uint id, Field field) => report.Samples[^1].Values[Channels.Output(id, field).ToString()];
        // Independent native prototype, compared numerically rather than by runtime-specific state hash.
        Near(Output(1, Field.Speed), 29.741824420739064, 1e-8);
        Near(Output(2, Field.Speed), 9.91394147378988, 1e-8);
        Near(Output(3, Field.Temperature), 302.4760662920842, 1e-6);
        var finer = ExperimentRunner.Evaluate(document with { Model = document.Model with { StepNanoseconds = 50_000 } });
        Require(finer.Passed && finer.Model.Fingerprint != report.Model.Fingerprint);
        Near(finer.Checks[0].Value, report.Checks[0].Value, 1e-7);
        Near(finer.Checks[2].Value, report.Checks[2].Value, 2e-6);
        Require(!ExperimentRunner.Evaluate(document with { Checks = [new(1, Field.Speed, 100, null, null)] }).Passed);
    }),
    ("JSON to portable asset / all report boundaries / sub-presentation events", () =>
    {
        var original = ModelDocument.Parse(source);
        foreach (var document in new[] { original, original with { Events = [new(0, [new(100, 12)]),
            new(5_000_100_000, [new(100, 4)]), new(6_013_300_000, [new(100, 24)])] } })
        {
            var report = ExperimentRunner.Evaluate(document);
            var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Boundary replay")));
            Require(asset.SourceSha256 == document.SourceSha256 && asset.Model.Fingerprint.ToString("x16") == report.Model.Fingerprint);
            var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
            foreach (var boundary in report.Samples)
            {
                while (playback.TimeNanoseconds < boundary.TimeNs)
                    Require(playback.Advance(Math.Min(20_000_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
                var snapshot = playback.ReadSnapshot(values);
                Require(snapshot.TimeNanoseconds == boundary.TimeNs && snapshot.StateHash.ToString("x16") == boundary.StateHash);
                Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            }
            Require(playback.Completed);
        }
    }),
    ("thermal JSON / analytic exchange / non-presentation tick / conservation", () =>
    {
        var document = ModelDocument.Load(Path.Combine(AppContext.BaseDirectory, "thermal-network.power.json"));
        var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 11);
        double hot = report.Checks[0].Value, cold = report.Checks[1].Value;
        double expectedDiscrete = 320 + 40 / Math.Pow(1 + 0.007 * (5.0 / 100 + 5.0 / 200), 1000);
        Near(hot, expectedDiscrete, 1e-9);
        Near(hot, 320 + 40 * Math.Exp(-0.075 * 7), 0.004);
        Near(100 * hot + 200 * cold, 100 * 360 + 200 * 300, 1e-7);
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Thermal exchange")));
        Require(!asset.Model.Channels.Any(c => c.IsInput) && asset.Model.StepNanoseconds == 7_000_000);
        var playback = asset.CreatePlayback();
        while (!playback.Completed)
            Require(playback.Advance(Math.Min(14_000_000, asset.DurationNanoseconds - playback.TimeNanoseconds)) == SimulationStatus.Ok);
        Require(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash.ToString("x16") == report.Samples[^1].StateHash);
    }),
    ("strict JSON / unknown fields / duplicate fields / finite quantities", () =>
    {
        Throws<ArgumentException>(() => ModelDocument.Parse(source.Replace("\"description\":", "\"typo\":")));
        Throws<ArgumentException>(() => ModelDocument.Parse(source.Replace("\"step_ns\": 100000,", "\"step_ns\": 100000, \"step_ns\": 100000,")));
        Throws<ArgumentException>(() => ModelDocument.Parse(source.Replace("\"kg_m2\"", "\"banana\"")));
        Throws<ArgumentException>(() => ModelDocument.Parse(source.Replace("\"value\": 0.2", "\"value\": 1e999")));
        Throws<ArgumentException>(() => ModelDocument.Parse(source.Replace("\"power.model.v1\"", "\"power.model.v2\"")));
        Throws<ArgumentException>(() => ModelDocument.Parse(new string(' ', 1_048_577)));
        Throws<JsonException>(() => ModelDocument.Parse("{"));
    }),
    ("experiment boundaries / event order / check validation", () =>
    {
        var d = ModelDocument.Parse(source);
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { DurationNanoseconds = 1 }));
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { Events = [d.Events[1], d.Events[0]] }));
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { Events = [d.Events[0], d.Events[0]] }));
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { Events = [new(3, [new(100, 10)])] }));
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { Events = [new(0, [new(99, 10)])] }));
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { Checks = [new(1, Field.Speed, 100, 1, null)] }));
        Throws<ArgumentException>(() => ExperimentRunner.Evaluate(d with { Checks = [new(1, Field.Current, 0, null, null)] }));
        var atZero = ExperimentRunner.Evaluate(d with { Events = [new(0, [new(100, 24)])] });
        Require(atZero.Passed && atZero.Samples[0].TimeNs == 0);
    })
});

int failed = 0, total = 0;
foreach (var (name, run) in checks)
{
    ++total;
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { ++failed; Console.Error.WriteLine($"FAIL {name}: {error}"); }
}
Console.WriteLine($"{total - failed}/{total} checks passed (.NET 10).");
return failed == 0 ? 0 : 1;
