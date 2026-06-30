using System.Runtime.CompilerServices;

namespace DimonSmart.DbSketch.Tests.Infrastructure;

public sealed class ManualIntegrationFactAttribute : FactAttribute
{
    public ManualIntegrationFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = true;
    }
}
