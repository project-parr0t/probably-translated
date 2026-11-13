using System;

[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("AquaSense")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("SmartInventoryArrange.Mono")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("ProbablyStolenCrateReveal")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("VisualTestDraft")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("ItemDurabilityBars")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("BetterLiquidScanner")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("AgriExpansion")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("RealTimeClock")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("CS Weapon Case")]
[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("ProbablyStolenInGameModSettings")]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}
