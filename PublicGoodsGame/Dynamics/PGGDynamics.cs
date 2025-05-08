
using PublicGoodsGame.Dynamics;
using System.Collections.Concurrent;

namespace PublicGoodsGame
{
    static class PGGDynamics
    {
        private static ConcurrentDictionary<double, List<double>> _results = new();
        public static async Task RunAsync()
        {

            if (PGGDynamicModel.EnhancementRange)
            {
                Console.WriteLine((PGGDynamicModel.EnhancementRange ? "Repetition\t" : "Step\t") +
                                  "Enhancement Factor\t" +
                                  "Defectors Ratio\t" +
                                  "Cooperators Ratio" +
                                  (PGGDynamicModel.WithLoners ? "\tLoners Ratio" : ""));

                var enhancementFactors = ListofEnhanceMentFactors();

                var tasks = new List<Task>();
                foreach (var enhancementFactor in enhancementFactors)
                {
                    var eFactor = enhancementFactor; // capture correctly
                    var tempFileWriter = FileManager.ThreadTempFile($"PGGDC_r={eFactor}");
                    tempFileWriter.Write(enhancementFactor + ",");

                    var fileLock = new object();
                    var groupTask = Task.Run(async () =>
                    {
                        var tasksPerEnhancementFactor = new List<Task>();

                        for (int i = 0; i < PGGDynamicModel.RepetitionsCount; i++)
                        {
                            var repetition = i;
                            tasksPerEnhancementFactor.Add(Task.Run(() =>
                            {
                                new MonteCarloMethod(eFactor,
                                                     fileLock,
                                                     PGGDynamicModel.WithLoners,
                                                     tempFileWriter)
                                .EnhancementFactorProcess(repetition);
                            }));
                        }

                        await Task.WhenAll(tasksPerEnhancementFactor);
                        tempFileWriter.Close();
                        MeanOverEachEnhancementFactor(eFactor);
                    });

                    tasks.Add(groupTask);
                }

                await Task.WhenAll(tasks);
            }
            else
                new MonteCarloMethod(PGGDynamicModel.EnhancementFactor, null, PGGDynamicModel.WithLoners, null).MCSteps();
        }

        public static void AddResult(double key, List<double> values)
        {
            var list = _results.GetOrAdd(key, new List<double>());

            lock (list)
            {
                list.AddRange(values);
            }
        }

        private static void MeanOverEachEnhancementFactor(double enhancementFactor)
        {
            var ensembleToAverageOverD = new List<double>();
            var ensembleToAverageOverC = new List<double>();
            var ensembleToAverageOverL = PGGDynamicModel.WithLoners ? new List<double>() : null;

            var ratios = _results[enhancementFactor];

            for (int i = 0; i < ratios.Count; i++)
            {
                if (ratios[i] == -1)
                    continue;

                if (PGGDynamicModel.WithLoners)
                {
                    if (i % 3 == 0)
                        ensembleToAverageOverD.Add(ratios[i]);
                    else if (i % 3 == 1)
                        ensembleToAverageOverC.Add(ratios[i]);
                    else
                        ensembleToAverageOverL!.Add(ratios[i]);
                }
                else
                {
                    if (i % 2 == 0)
                        ensembleToAverageOverD.Add(ratios[i]);
                    else
                        ensembleToAverageOverC.Add(ratios[i]);
                }
            }

            var dRatio = AverageOfValues(ensembleToAverageOverD);
            var cRatio = AverageOfValues(ensembleToAverageOverC);
            var lRatio = PGGDynamicModel.WithLoners
                         ? AverageOfValues(ensembleToAverageOverL!) : 0;

            ratios.Add(dRatio);
            ratios.Add(cRatio);
            if (PGGDynamicModel.WithLoners)
                ratios.Add(lRatio);

            //_tempFileWriter.Write(dRatio + "," + cRatio + (PGGDynamicModel.WithLoners ? "," + lRatio : ""));
            //_tempFileWriter.Flush();
            //_tempFileWriter.Close();

            Console.WriteLine(enhancementFactor + "\t" + dRatio + "\t" + cRatio +
                              (PGGDynamicModel.WithLoners ? "\t" + lRatio : ""));

            FileManager.WriteToResultFile(enhancementFactor, ratios);
        }

        public static double AverageOfValues(List<double> sample)
        {
            var average = .0;

            for (int i = 0; i < sample.Count; i++)
            {
                average += sample[i];
            }

            return average / sample.Count;
        }

        private static List<double> ListofEnhanceMentFactors()
        {
            var enhancementFactors = new List<double>();
            var tempEnhancementFactor = PGGDynamicModel.EnhancementFactor;

            while (tempEnhancementFactor <= PGGDynamicModel.MaxEnhancementFactor)
            {
                enhancementFactors.Add(tempEnhancementFactor);

                if (PGGDynamicModel.MinorEnhancementFactorTickInterval[0]
                    < tempEnhancementFactor &&
                    tempEnhancementFactor <
                    PGGDynamicModel.MinorEnhancementFactorTickInterval[1])

                    tempEnhancementFactor += PGGDynamicModel.MinorEnhancementFactorTick;
                else
                    tempEnhancementFactor += PGGDynamicModel.EnhancementFactorTick;
            }

            return enhancementFactors;
        }
    }
}
