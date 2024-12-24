using System.Xml.Linq;

namespace GC.Infrastructure.Core.Presentation.Stress
{
    public static class StressCommandBuilder
    {
        private static Dictionary<string, Dictionary<string, string>> Windows { get; } = new()
        {
            { "loh", new() { { "Server", "00:10:00"}, { "Workstation", "01:45:00"}, { "Datas", "00:10:00"} } },
            { "poh", new() { { "Server", "00:05:00"}, { "Workstation", "00:50:00" }, { "Datas", "00:05:00" } } },
            { "non_induced", new() { { "Server", "00:30:00"}, { "Workstation", "03:30:00"}, { "Datas", "00:20:00"} } },
            { "finalization", new() { { "Server", "00:05:00"}, { "Workstation", "00:50:00"}, { "Datas", "00:15:00" } } }
        };

        private static Dictionary<string, Dictionary<string, string>> WindowsStress { get; } = new()
        {
            { "loh", new() { { "Server", "00:02:00"}, { "Workstation", "00:10:00"}, { "Datas", "00:05:00"} } },
            { "poh", new() { { "Server", "00:02:00"}, { "Workstation", "00:10:00" }, { "Datas", "00:05:00" } } },
            { "non_induced", new() { { "Server", "00:25:00"}, { "Workstation", "04:00:00"}, { "Datas", "00:35:00"} } },
            { "finalization", new() { { "Server", "00:05:00"}, { "Workstation", "00:40:00"}, { "Datas", "01:40:00" } } }
        };

        private static Dictionary<string, Dictionary<string, string>> Linux { get; } = new()
        {
            { "loh", new() { { "Server", "00:25:00"}, { "Workstation", "00:20:00"}, { "Datas", "00:20:00"} } },
            { "poh", new() { { "Server", "00:10:00"}, { "Workstation", "00:10:00" }, { "Datas", "00:10:00" } } },
            { "non_induced", new() { { "Server", "00:25:00"}, { "Workstation", "00:55:00"}, { "Datas", "00:15:00"} } },
            { "finalization", new() { { "Server", "00:20:00"}, { "Workstation", "01:30:00"}, { "Datas", "00:25:00" } } }
        };

        private static Dictionary<string, Dictionary<string, string>> LinuxStress { get; } = new()
        {
            { "loh", new() { { "Server", "00:10:00"}, { "Workstation", "00:25:00"}, { "Datas", "00:08:00"} } },
            { "poh", new() { { "Server", "00:05:00"}, { "Workstation", "00:15:00" }, { "Datas", "00:05:00" } } },
            { "non_induced", new() { { "Server", "00:25:00"}, { "Workstation", "01:10:00"}, { "Datas", "00:30:00"} } },
            { "finalization", new() { { "Server", "00:20:00"}, { "Workstation", "00:50:00"}, { "Datas", "04:00:00" } } }
        };

        private static string GetMaximumWaitTime(string configName, string rid, bool enableStress, string gcMode)
        {
            string osName = rid.Split("-")
                    .FirstOrDefault("");

            string maximumWaitTime =
                (osName, enableStress) switch
                {
                    ("win", false) => Windows[configName][gcMode],
                    ("win", true) => WindowsStress[configName][gcMode],
                    ("linux", false) => Linux[configName][gcMode],
                    ("linux", true) => LinuxStress[configName][gcMode],
                    _ => throw new Exception($"{nameof(StressCommandBuilder)}: Get unknown OS {osName} for maximumWaitTime")
                };

            return maximumWaitTime;
        }

        private static string GetConcurrentCopies(string rid)
        {
            string osName = rid.Split("-")
                    .FirstOrDefault("");
            string concurrentCopies =
                osName switch
                {
                    "linux" => "1",
                    "win" => "3",
                    _ => throw new Exception($"{nameof(StressCommandBuilder)}: Get unknown OS {osName} for concurrentCopies")
                };
            return concurrentCopies;
        }

        private static string GenerateThresholdPassPercent(string configName)
        {
            string thresholdPassPercent =
                configName switch
                {
                    "loh" => "85",
                    "poh" => "85",
                    "non_induced" => "100",
                    "finalization" => "85",
                    _ => throw new Exception($"{nameof(StressCommandBuilder)}: Get unknown config {configName} for percentPassIsPass")
                };
            return thresholdPassPercent;
        }

        private static XElement GenerateBaseConfig(string baseConfigPath,
                                                   string configName,
                                                   string rid,
                                                   bool enableStress,
                                                   string gcMode)
        {
            try
            {
                string maximumWaitTime = GetMaximumWaitTime(configName, rid, enableStress, gcMode);
                string thresholdPassPercent = GenerateThresholdPassPercent(configName);

                string baseConfigContent = File.ReadAllText(baseConfigPath);
                XElement config = XElement.Parse(baseConfigContent);
                config.SetAttributeValue("maximumWaitTime", maximumWaitTime);
                config.SetAttributeValue("maximumExecutionTime", "24:00:00");
                config.SetAttributeValue("maximumTestRuns", "-1");
                config.SetAttributeValue("percentPassIsPass", thresholdPassPercent);
                return config;
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(StressCommandBuilder)}: Fail to generate base config: {ex.Message}\nStack trace:\n{ex.StackTrace}");
            }
        }

        public static void GenerateLOHConfig(string baseConfigPath, string rid, bool enableStress, string gcMode, string outputPath)
        {
            string configName = "loh";
            try
            {
                XElement config = GenerateBaseConfig(baseConfigPath, configName, rid, enableStress, gcMode);
                string concurrentCopies = GetConcurrentCopies(rid);
                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - LOH No Live Data."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 0 -lohar 1000 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - LOH Some Live Data."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 1000 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 50 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - LOH A Lot Of Live Data."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 5 -lohar 1000 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 30 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - POH Scenario."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 900 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 0 -pohsi 50 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));


                config.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(StressCommandBuilder)}: Fail to generate test config for loh.config: {ex.Message}\nStack trace:\n{ex.StackTrace}");
            }
        }

        public static void GeneratePOHConfig(string baseConfigPath, string rid, bool enableStress, string gcMode, string outputPath)
        {
            string configName = "poh";
            try
            {
                XElement config = GenerateBaseConfig(baseConfigPath, configName, rid, enableStress, gcMode);
                string concurrentCopies = GetConcurrentCopies(rid);

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - POH No Live Data."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 0 -lohar 0 -pohar 1000 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - POH Some Live Data."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 1000 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 0 -pohsi 50 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - POH A Lot Of Live Data."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 5 -lohar 0 -pohar 1000 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 0 -pohsi 30 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - LOH Scenario."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 1000 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 50 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(StressCommandBuilder)}: Fail to generate test config for poh.config: {ex.Message}\nStack trace:\n{ex.StackTrace}");
            }
        }

        public static void GenerateNonInducedConfig(string baseConfigPath, string rid, bool enableStress, string gcMode, string outputPath)
        {
            string configName = "non_induced";
            try
            {
                XElement config = GenerateBaseConfig(baseConfigPath, configName, rid, enableStress, gcMode);
                string concurrentCopies = GetConcurrentCopies(rid);

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "SingLinkStay.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "SingLinkStay.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "StressAllocator.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "StressAllocator.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "StressAllocator.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "StressAllocator.dll"),
                        new XAttribute("arguments", "-pinned 50 -usepoh true"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "bestfit-finalize.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "bestfit-finalize.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "LeakGenThrd.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "LeakGenThrd.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "DirectedGraph.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "DirectedGraph.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "ThdTreeGrowingObj.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "ThdTreeGrowingObj.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "LargeObjectAllocPinned.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "LargeObjectAllocPinned.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "LargeObjectAlloc.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "LargeObjectAlloc.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "pinstress.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "pinstress.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCSimulator.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCSimulator.dll"),
                        new XAttribute("arguments", "-dp 0.05 -t 8"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "plug.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "plug.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "573277.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "573277.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "MulDimJagAry.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "MulDimJagAry.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "concurrentspin2.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "concurrentspin2.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "allocationwithpins.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "allocationwithpins.dll"),
                        new XAttribute("arguments", "500"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "LargeObjectAlloc1.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "LargeObjectAlloc1.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "LargeObjectAlloc4.dll"),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "LargeObjectAlloc4.dll"),
                        new XAttribute("arguments", ""),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(StressCommandBuilder)}: Fail to generate test config for non_induced.config: {ex.Message}\nStack trace:\n{ex.StackTrace}");
            }
        }

        public static void GenerateFinalizationConfig(string baseConfigPath, string rid, bool enableStress, string gcMode, string outputPath)
        {
            string configName = "finalization";
            try
            {
                XElement config = GenerateBaseConfig(baseConfigPath, configName, rid, enableStress, gcMode);
                string concurrentCopies = GetConcurrentCopies(rid);
                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - SOH Allocations Finalizable Objects."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 50 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 50 -lohfi 0 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - LOH Allocations Finalizable Objects."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 1000 -pohar 0 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 50 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 50 -pohfi 0 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));

                config.Add(
                    new XElement(
                        "Assembly",
                        new XAttribute("id", "GCPerfSim - POH Allocations Finalizable Objects."),
                        new XAttribute("successCode", "100"),
                        new XAttribute("filename", "GCPerfSim.dll"),
                        new XAttribute("arguments", "-tc 28 -tagb 540 -tlgb 2 -lohar 0 -pohar 1000 -sohsr 100-4000 -lohsr 102400-204800 -pohsr 100-204800 -sohsi 0 -lohsi 0 -pohsi 50 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 50 -allocType reference -testKind time"),
                        new XAttribute("concurrentCopies", concurrentCopies)));


                config.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(StressCommandBuilder)}: Fail to generate test config for finalization.config: {ex.Message}\nStack trace:\n{ex.StackTrace}");
            }
        }
    }
}
