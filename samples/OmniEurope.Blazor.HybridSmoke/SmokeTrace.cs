namespace OmniEurope.Blazor.HybridSmoke;

// The smoke probe reads this host's standard output: startup markers while it comes up, then the
// self-test result. Every line carries the same prefix so the probe can tell them from anything
// else the runtime prints.
internal static class SmokeTrace
{
    public static void Write(string marker)
    {
        Console.WriteLine($"HYBRID-SMOKE {marker}");
        Console.Out.Flush();
    }
}
