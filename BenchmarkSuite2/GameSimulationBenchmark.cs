using BenchmarkDotNet.Attributes;
using MapProcessing;
using Microsoft.VSDiagnostics;

namespace MapProcessing.Benchmarks
{
    [CPUUsageDiagnoser]
    public class GameSimulationBenchmark
    {
        private Map _map = null!;
        private const int SmallMapSize = 40;
        private const int LargeMapSize = 600;
        private const int SimulationTerm = 100;

        [GlobalSetup]
        public void Setup()
        {
            _map = new Map(SmallMapSize, SmallMapSize);
        }

        [Benchmark]
        public void SmallMap_Sequential()
        {
            var map = new Map(SmallMapSize, SmallMapSize);
            var frames = Proccessor.ProcessMapFrames(map, SimulationTerm, Proccessor.GameMode.Simple);
            foreach (var frame in frames) { }
        }

        [Benchmark]
        public void LargeMap_Sequential()
        {
            var map = new Map(LargeMapSize, LargeMapSize);
            var frames = Proccessor.ProcessMapFrames(map, SimulationTerm, Proccessor.GameMode.Simple);
            foreach (var frame in frames) { }
        }
    }
}