using System.Runtime.CompilerServices;

// Allow our test assemblies to share internal data
[assembly: InternalsVisibleTo("Bossy.Tests.Editor")]
[assembly: InternalsVisibleTo("Bossy.Tests.Runtime")]
[assembly: InternalsVisibleTo("Bossy.Tests.Utils")]

namespace Bossy.Global
{
    /// <summary>
    /// A container holding constant Bossy data.
    /// </summary>
    internal static class BossyData
    {
        /// <summary>
        /// The name of this project.
        /// </summary>
        public const string Name = "Bossy";
    }
}