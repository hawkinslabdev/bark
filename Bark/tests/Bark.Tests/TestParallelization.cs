// LayoutProvider caches generated assets in process-wide statics keyed by locale, so parallel test hosts serve each other's scripts.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
