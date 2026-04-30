using System.Runtime.CompilerServices;

// Allow our test assemblies to share internal data
[assembly: InternalsVisibleTo("Bossy.Tests.Editor")]
[assembly: InternalsVisibleTo("Bossy.Tests.Runtime")]
[assembly: InternalsVisibleTo("Bossy.Tests.Utils")]