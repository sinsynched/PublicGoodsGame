
using PublicGoodsGame.Dynamics;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace PublicGoodsGame
{
    static class FileManager
    {
        public static StreamWriter ResultFile = new("Results" +
            DateTime.Now.ToString(" - yyyy-MM-dd-HH-mm-ss-ff") + ".csv", false, new UTF8Encoding(true));

        public static StreamWriter DataFile = new("Data.csv");

        public static StreamWriter NetworkFile;

        private static ConcurrentBag<string> _tempFiles = new();
        private static readonly object _writeLock = new();

        public static void Initialize()
        {
            EmptyTempFolder();
            WriteParametersToFile();
        }

        public static void ReadParametersFromConfigFile()
        {
            var jsonFilePath = "Config.json";
            var jsonContent = File.ReadAllText(jsonFilePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var jsonDocument = JsonDocument.Parse(jsonContent);
            var root = jsonDocument.RootElement;

            var networkParameters = root.GetProperty(nameof(Network));
            Network.Initialize(
                networkParameters.GetProperty(nameof(Network.LoadNetwork)).GetBoolean(),
                (NetworkTypes)networkParameters.GetProperty(nameof(Network.NetworkType)).GetInt32(),
                networkParameters.GetProperty(nameof(Network.ErdosRenyiProbability)).GetDouble(),
                networkParameters.GetProperty(nameof(Network.NodesCount)).GetInt32(),
                networkParameters.GetProperty(nameof(Network.RingGraphDegree)).GetInt32(),
                networkParameters.GetProperty(nameof(Network.Rewiring)).GetBoolean(),
                (RewiringType)networkParameters.GetProperty(nameof(Network.RewiringType)).GetInt32(),
                networkParameters.GetProperty(nameof(Network.PortionOfLinksToRewire)).GetInt32(),
                networkParameters.GetProperty(nameof(Network.RewiringProbability)).GetDouble(),
                networkParameters.GetProperty(nameof(Network.LatticeLength)).GetInt32()
            );

            var pggDynamicModelParameters = root.GetProperty(nameof(PGGDynamicModel));
            PGGDynamicModel.Initialize(
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.WithLoners)).GetBoolean(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.WithSatisfaction)).GetBoolean(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.SatisfactionUpdateProbability)).GetDouble(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.EnhancementRange)).GetBoolean(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.EnhancementFactor)).GetDouble(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.MaxEnhancementFactor)).GetDouble(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.RepetitionsCount)).GetInt32(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.MinMCS)).GetInt32(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.StepsToAverageOver)).GetInt32(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.K)).GetDouble(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.EnhancementFactorTick)).GetDouble(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.MinorEnhancementFactorTickInterval)).EnumerateArray().Select(e => e.GetDouble()).ToArray(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.MinorEnhancementFactorTick)).GetDouble(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.MaxMCS)).GetInt32(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.ContributionCost)).GetInt32(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.LonerPayoff)).GetInt32(),
                pggDynamicModelParameters.GetProperty(nameof(PGGDynamicModel.StandardDeviationValue)).GetDouble()
            );
        }

        public static void Finalize()
        {
            ResultFile.Close();
            DataFile.Close();
            DeleteTempFiles();
        }

        private static void WriteParametersToFile()
        {
            ResultFile.WriteLine("Network Type," + Network.GetNetworkType());

            if (Network.Rewiring)
            {
                if (Network.RewiringType == RewiringType.Regular)
                {
                    ResultFile.WriteLine("Portion of Rewired Links" + "," +
                                    Network.PortionOfLinksToRewire);
                }
                else
                    ResultFile.WriteLine("Rewiring Probability" + "," +
                    Network.RewiringProbability);
            }


            if (PGGDynamicModel.WithSatisfaction)
                ResultFile.WriteLine("Satisfaction Update Probability" + "," +
                                      PGGDynamicModel.SatisfactionUpdateProbability);

            if (PGGDynamicModel.WithLoners)
                ResultFile.WriteLine("Loner's Payoff" + "," +
                                      PGGDynamicModel.LonerPayoff);


            ResultFile.Write("Number of nodes" + "," +
                             "Number of links" + "," +
                             "Number of nodes with zero degree" + "," +
                             "Repetitions for each r" + "," +
                             "Min MCS" + "," +
                             "Steps to average over" + "," +
                             "k" + "," +
                             "Max MCS" +
                             "\n");

            ResultFile.Write(Network.NodesCount + "," +
                             Network.LinksCount + "," +
                             Network.ZeroDegreeNodesCount + "," +
                             PGGDynamicModel.RepetitionsCount + "," +
                             PGGDynamicModel.MinMCS + "," +
                             PGGDynamicModel.StepsToAverageOver + "," +
                             PGGDynamicModel.K + "," +
                             PGGDynamicModel.MaxMCS +
                             "\n\n");

            ResultFile.Write("r" + ",");
            for (var i = 1; i <= PGGDynamicModel.RepetitionsCount; i++)
            {
                ResultFile.Write($"Rep {i}: ρ(D)" + "," + $"Rep {i}: ρ(C)" + ",");
                if (PGGDynamicModel.WithLoners)
                    ResultFile.Write($"Rep {i}: ρ(L)" + ",");
            }

            ResultFile.Write("Mean ρ(D)" + "," + "Mean ρ(C)" + ",");

            if (PGGDynamicModel.WithLoners)
                ResultFile.Write("Mean ρ(L)" + ",");

            ResultFile.Write("\n");

            ResultFile.Flush();
        }

        public static StreamWriter ThreadTempFile(string fileName)
        {
            var programDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var tempFolderPath = Path.Combine(programDirectory, "Temp");

            Directory.CreateDirectory(tempFolderPath);

            var tempFile = Path.Combine(tempFolderPath, $"{fileName}.csv");
            _tempFiles.Add(tempFile);

            return new StreamWriter(tempFile);
        }

        public static void WriteToResultFile(double enhancementFactor, List<double> results)
        {
            lock (_writeLock)
            {
                ResultFile.Write(enhancementFactor + ",");
                DataFile.Write(enhancementFactor + ",");

                foreach (var result in results)
                {
                    if (result == -1)
                    {
                        ResultFile.Write("OVER,");
                        DataFile.Write("OVER,");
                    }
                    else
                    {
                        ResultFile.Write(result + ",");
                        DataFile.Write(result + ",");
                    }
                }

                ResultFile.Write("\n");
                ResultFile.Flush();

                DataFile.Write("\n");
                DataFile.Flush();
            }
        }

        public static void DeleteTempFiles()
        {
            foreach (var tempFile in _tempFiles)
            {
                File.Delete(tempFile);
            }
            _tempFiles.Clear();
        }

        private static void EmptyTempFolder()
        {
            var programDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var tempFolderPath = Path.Combine(programDirectory, "Temp");

            if (Directory.Exists(tempFolderPath))
            {
                var files = Directory.GetFiles(tempFolderPath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                        // Or log the error
                    }
                }
            }
        }
    }
}
