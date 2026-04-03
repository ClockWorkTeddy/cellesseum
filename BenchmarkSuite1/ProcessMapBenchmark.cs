using BenchmarkDotNet.Attributes;
using MapProcessing;
using Microsoft.VSDiagnostics;
using System.Collections.Generic;

[CPUUsageDiagnoser]
public class ProcessMapBenchmark
{
    private Proccessor _processor;
    [GlobalSetup]
    public void Setup()
    {
        _processor = new Proccessor();
    }

    [Benchmark]
    public List<AreaData> ProcessMap_Size50()
    {
        var map = new Map(50);
        return _processor.ProcessMap(map);
    }
}