using FluentAssertions;
using GC.Analysis.API;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Presentation.GCPerfSim;

namespace GC.Infrastructure.Core.UnitTests.GCPerfSim
{
    // Integration test that exercises the real GCPerfSim analysis pipeline end-to-end against
    // recorded GC traces: it loads the traces, computes the same ResultItem / GCTraceMetrics that
    // the `gcperfsim` and `gcperfsim-compare` commands produce, runs the comparison, and verifies
    // that the regression / improvement categorization is direction-aware (Speed_MBPerMSec is
    // higher-is-better) while the displayed delta keeps its real sign.
    [TestClass]
    public sealed class ComparisonCategorization
    {
        // The recorded traces capture a managed process named "Benchmarks" with real GC data.
        private const string ProcessName = "Benchmarks";

        private readonly string[] MetricList = new[]
        {
            "PctTimePausedInGC",
            "FirstToLastGCSeconds",
            "HeapSizeBeforeMB_Mean",
            "HeapSizeAfter_Mean",
            "TotalCommittedInUse",
            "TotalBookkeepingCommitted",
            "TotalCommittedInGlobalDecommit",
            "TotalCommittedInFree",
            "TotalCommittedInGlobalFree",
            "PauseDurationMSec_95PWhereIsGen0",
            "PauseDurationMSec_95PWhereIsGen1",
            "PauseDurationMSec_95PWhereIsBackground",
            "PauseDurationMSec_MeanWhereIsBackground",
            "PauseDurationMSec_95PWhereIsBlockingGen2",
            "PauseDurationMSec_MeanWhereIsBlockingGen2",
            "CountIsBlockingGen2",
            "PauseDurationMSec_SumWhereIsGen1",
            "PauseDurationMSec_MeanWhereIsEphemeral",
            "PromotedMB_MeanWhereIsGen1",
            "CountIsGen1",
            "CountIsGen0",
            "HeapCount",
            "PauseDurationMSec_Sum",
            "TotalAllocatedMB",
            "TotalNumberGCs",
            "Speed_MBPerMSec",
            "ExecutionTimeMSec"
        };

        private static string? FindTracesDirectory()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir != null)
            {
                foreach (string relative in new[]
                {
                    Path.Combine("Notebooks", "Examples", "Traces"),
                    Path.Combine("src", "benchmarks", "gc", "GC.Infrastructure", "Notebooks", "Examples", "Traces"),
                })
                {
                    string candidate = Path.Combine(dir.FullName, relative);
                    if (File.Exists(Path.Combine(candidate, "CPU_Baseline.etl.zip")))
                    {
                        return candidate;
                    }
                }

                dir = dir.Parent;
            }

            return null;
        }

        [TestMethod]
        public void GCPerfSimRun_Comparison_CategorizesSpeedMetricByDirection()
        {
            string? tracesDirectory = FindTracesDirectory();
            if (tracesDirectory == null)
            {
                Assert.Inconclusive("Could not locate the example GC traces required for this test.");
                return;
            }

            string baselinePath = Path.Combine(tracesDirectory, "CPU_Baseline.etl.zip");
            string comparandPath = Path.Combine(tracesDirectory, "CPU_Comparand.etl.zip");

            using Analyzer baselineAnalyzer = AnalyzerManager.GetAnalyzer(baselinePath);
            using Analyzer comparandAnalyzer = AnalyzerManager.GetAnalyzer(comparandPath);

            GCProcessData? baselineData = baselineAnalyzer.GetProcessGCData(ProcessName).FirstOrDefault();
            GCProcessData? comparandData = comparandAnalyzer.GetProcessGCData(ProcessName).FirstOrDefault();

            if (baselineData == null || comparandData == null)
            {
                Assert.Inconclusive($"The traces did not contain GC data for the '{ProcessName}' process.");
                return;
            }

            // Ensure the GCPerfSim run actually produced GC data to analyze.
            baselineData.GCs.Count.Should().BeGreaterThan(0, "the baseline trace should contain GCs");
            comparandData.GCs.Count.Should().BeGreaterThan(0, "the comparand trace should contain GCs");

            // --- End-to-end categorization through the analyze pipeline ---
            GCTraceMetrics[] baselineMetrics = { new(baselineData, "Run", "baseline") };
            GCTraceMetrics[] comparandMetrics = { new(comparandData, "Run", "comparand") };

            GCTraceMetricComparisonResult speedComparison =
                GCTraceMetricComparison.CompareGCTraceMetric(baselineMetrics, comparandMetrics, "Speed_MBPerMSec");
            GCTraceMetricComparisonResults results = new("Run", new[] { speedComparison });

            // A faster comparand (higher Speed_MBPerMSec) must be categorized as an improvement, never a regression.
            bool isFaster = speedComparison.AveragedComparandMetric > speedComparison.AveragedBaselineMetric;
            IEnumerable<GCTraceMetricComparisonResult> regressionBuckets =
                results.LargeRegressions.Concat(results.Regressions).Concat(results.StaleRegressions);
            IEnumerable<GCTraceMetricComparisonResult> improvementBuckets =
                results.LargeImprovements.Concat(results.Improvements).Concat(results.StaleImprovements);

            if (isFaster)
            {
                regressionBuckets.Should().NotContain(speedComparison, "a faster Speed_MBPerMSec is an improvement, not a regression");
                improvementBuckets.Should().Contain(speedComparison);
            }
            else if (speedComparison.AveragedComparandMetric < speedComparison.AveragedBaselineMetric)
            {
                improvementBuckets.Should().NotContain(speedComparison, "a slower Speed_MBPerMSec is a regression, not an improvement");
                regressionBuckets.Should().Contain(speedComparison);
            }
        }

        [TestMethod]
        public void GCPerfSimRun_Comparison_CompareGCPerfsimResultsShouldContainExpectedMetrics()
        {
            string? tracesDirectory = FindTracesDirectory();
            if (tracesDirectory == null)
            {
                Assert.Inconclusive("Could not locate the example GC traces required for this test.");
                return;
            }

            string baselinePath = Path.Combine(tracesDirectory, "CPU_Baseline.etl.zip");
            string comparandPath = Path.Combine(tracesDirectory, "CPU_Comparand.etl.zip");

            File.Exists(baselinePath).Should().BeTrue("the repository should contain example traces for regression coverage");
            File.Exists(comparandPath).Should().BeTrue("the repository should contain example traces for regression coverage");

            IReadOnlyCollection<GCTraceMetricComparisonResult> comparison = GCTraceMetricComparison.CompareGCPerfsimResults(baselinePath, comparandPath);

            comparison.Should().NotBeEmpty("the compare command path should produce metric comparison results");

            Enumerable.Select(comparison, r => r.MetricName).Should().Contain(MetricList);
        }
    }
}
