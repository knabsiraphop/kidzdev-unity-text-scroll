using System.Runtime.CompilerServices;

// Exposes internal helpers (TextScrollDriver, ITickable, TextScroller's cached-behavior accessor) to the
// test assemblies so the shared tick driver and behavior caching can be exercised directly.
[assembly: InternalsVisibleTo("KidzDev.Unity.TextScroll.Tests.Editor")]
[assembly: InternalsVisibleTo("KidzDev.Unity.TextScroll.Tests.Runtime")]

