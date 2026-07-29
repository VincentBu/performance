using GC.Analysis.API;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Presentation.GCPerfSim;
using System.Collections.Concurrent;

namespace GC.Infrastructure.Core.Analysis
{
    public static class AnalyzeTrace
    {
        public static GCProcessData? GetGCProcessDataForGCPerfSim(Analyzer analyzer)
        {
            GCProcessData? p = null;

            // The crank traces are targeted to the particular process.
            if (analyzer.TraceLogPath.EndsWith("nettrace"))
            {
                p = analyzer.AllGCProcessData.First().Value.First();
            }

            else // ETL* traces.
            {
                p = analyzer.GetProcessGCData("corerun").FirstOrDefault();
                if (p == null)
                {
                    p = analyzer.GetProcessGCData("GCPerfSim").FirstOrDefault();
                    if (p == null)
                    {
                        p = analyzer.AllGCProcessData.Count == 1
                            ? analyzer.AllGCProcessData.First().Value.FirstOrDefault()
                            : null;
                    }
                }
            }

            return p;
        }
    }
}
