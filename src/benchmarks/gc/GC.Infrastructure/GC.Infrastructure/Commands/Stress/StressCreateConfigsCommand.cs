using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GC.Infrastructure.Core;
using GC.Infrastructure.Core.Presentation.Stress;

namespace GC.Infrastructure.Commands.Stress
{
    internal sealed class StressCreateConfigsCommand : Command<StressCreateConfigsCommand.StressCreateConfigsSettings>
    {
        private static readonly string _baseSuitePath = Path.Combine("Commands", "RunCommand", "BaseSuite");
        private static readonly string _baseStressConfigPath = Path.Combine(_baseSuitePath, "StressBase.config");

        public sealed class StressCreateConfigsSettings : CommandSettings
        {
            [Description("Path to Output.")]
            [CommandOption("-o|--output")]
            public string? OutputFolder { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] StressCreateConfigsSettings settings)
        {
            AnsiConsole.Write(new Rule("Stress Create configs"));
            AnsiConsole.WriteLine();

            List<bool> stressModeList = new() { true, false };
            List<string> gcModeList = new() { "Datas", "Server", "Workstation" };
            List<string> ridList = new() { "win-x64", "linux-x64" };
            foreach (string rid in ridList)
            {
                string ridFolder = Path.Combine(settings.OutputFolder, rid);
                bool ridFolderCreateResult = Utilities.TryCreateDirectory(ridFolder);
                if (!ridFolderCreateResult)
                {
                    AnsiConsole.MarkupLine($"[red bold] Fail to create folder {ridFolder}  [/]");
                    break;
                }

                foreach (bool enableStress in stressModeList)
                {
                    string stressName = enableStress switch
                    {
                        true => "EnableStress",
                        false => "DiableStress"
                    };
                    string stressModeFolder = Path.Combine(ridFolder, stressName);
                    bool stressModeFolderCreateResult = Utilities.TryCreateDirectory(stressModeFolder);
                    if (!stressModeFolderCreateResult)
                    {
                        AnsiConsole.MarkupLine($"[red bold] Fail to create folder {stressModeFolder}  [/]");
                        break;
                    }

                    foreach (string gcMode in gcModeList)
                    {
                        string gcModeFolder = Path.Combine(stressModeFolder, gcMode);
                        bool gcModeFolderCreateResult = Utilities.TryCreateDirectory(gcModeFolder);
                        if (!gcModeFolderCreateResult)
                        {
                            AnsiConsole.MarkupLine($"[red bold] Fail to create folder {gcModeFolder}  [/]");
                            break;
                        }

                        try
                        {
                            string lohConfigPath = Path.Combine(gcModeFolder, "loh.config");
                            StressCommandBuilder.GenerateLOHConfig(_baseStressConfigPath, rid, enableStress, gcMode, lohConfigPath);

                            string pohConfigPath = Path.Combine(gcModeFolder, "poh.config");
                            StressCommandBuilder.GeneratePOHConfig(_baseStressConfigPath, rid, enableStress, gcMode, pohConfigPath);

                            string nonInducedConfigPath = Path.Combine(gcModeFolder, "non_induced.config");
                            StressCommandBuilder.GenerateNonInducedConfig(_baseStressConfigPath, rid, enableStress, gcMode, nonInducedConfigPath);

                            string finalizationConfigPath = Path.Combine(gcModeFolder, "finalization.config");
                            StressCommandBuilder.GenerateFinalizationConfig(_baseStressConfigPath, rid, enableStress, gcMode, finalizationConfigPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"{ex.Message}\nStack Trace:\n{ex.StackTrace}\nInner Exception:\n{ex.InnerException}");
                            continue;
                        }
                    }
                }
            }
            return 0;
        }
    }
}
