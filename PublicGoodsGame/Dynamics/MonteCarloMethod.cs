namespace PublicGoodsGame.Dynamics
{
    class MonteCarloMethod
    {
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private readonly int[] _nodeStrategies = new int[Network.NodesCount];
        private readonly StreamWriter _tempFileWriter;
        private readonly double _enhancementFactor;
        private readonly object? _fileLock;
        private int[] _cooperatorsPerGroup;
        private int[] _lonersPerGroup;

        private readonly bool _includeLoners;

        private double _defectorsRatio;
        private double _cooperatorsRatio;
        private double _lonersRatio;

        private int _defectorsCount;
        private int _cooperatorsCount;
        private int _lonersCount;

        public MonteCarloMethod(double enhancementFactor,
                                object? fileLock,
                                bool includeLoners,
                                StreamWriter tempFileWriter)
        {
            _enhancementFactor = enhancementFactor;
            _fileLock = fileLock;
            _includeLoners = includeLoners;
            _tempFileWriter = tempFileWriter;
        }

        public void MCSteps()
        {
            var resultFile = FileManager.ResultFile;
            var dataFile = FileManager.DataFile;

            resultFile.WriteLine("Enhancement factor" + "," +
                                             _enhancementFactor + "\n");
            dataFile.WriteLine("Enhancement factor" + "," +
                                             _enhancementFactor + "\n");

            InitialStrategies();

            resultFile.WriteLine("step," + "ratio of D," + "ratio of C," +
                                 (_includeLoners ? "ratio of L" : ""));

            dataFile.WriteLine("step," + "ratio of D," + "ratio of C," +
                               (_includeLoners ? "ratio of L" : ""));

            Console.WriteLine("step\t" + "ratio of D\t" + "ratio of C\t" +
                               (_includeLoners ? "ratio of L" : ""));

            RatioOfStrategies();

            //TODO: Helper Method
            resultFile.WriteLine(1 + "," + _defectorsRatio + "," + _cooperatorsRatio +
                                 (_includeLoners ? "," + _lonersRatio : ""));

            dataFile.WriteLine(1 + "," + _defectorsRatio + "," + _cooperatorsRatio +
                               (_includeLoners ? "," + _lonersRatio : ""));

            Console.WriteLine(1 + "\t" + _defectorsRatio + "\t" + _cooperatorsRatio +
                              (_includeLoners ? "\t" + _lonersRatio : ""));

            var minimumMCS = PGGDynamicModel.MinMCS;

            var lastNStepsD = new List<double>();
            var lastNStepsC = new List<double>();
            var lastNStepsL = _includeLoners ? new List<double>() : null;

            var sd = PGGDynamicModel.StandardDeviationValue;
            for (int step = 2; ; step++)
            {
                SingleMonteCarloStep();

                RatioOfStrategies();

                resultFile.WriteLine(step + "," + _defectorsRatio + "," + _cooperatorsRatio +
                                     (_includeLoners ? "," + _lonersRatio : ""));

                dataFile.WriteLine(step + "," + _defectorsRatio + "," + _cooperatorsRatio +
                                    (_includeLoners ? "," + _lonersRatio : ""));

                if ((step + 1) % 1000 == 0)
                {
                    Console.WriteLine(step + 1 + "\t" + _defectorsRatio + "\t" + _cooperatorsRatio +
                                      (_includeLoners ? "\t" + _lonersRatio : ""));
                }

                if (step > minimumMCS)
                {
                    lastNStepsD.Add(_defectorsRatio);
                    lastNStepsC.Add(_cooperatorsRatio);
                    lastNStepsL?.Add(_lonersRatio);
                }

                if (step == minimumMCS + PGGDynamicModel.StepsToAverageOver)
                {
                    var standardDeviationD = StandardDeviation(lastNStepsD);
                    var standardDeviationC = StandardDeviation(lastNStepsC);
                    var standardDeviationL = _includeLoners ? StandardDeviation(lastNStepsL!)
                                             : 0;

                    if (standardDeviationD <= sd && standardDeviationC <= sd && standardDeviationL <= sd)
                        break;
                    else
                    {
                        minimumMCS += PGGDynamicModel.StepsToAverageOver;
                        lastNStepsC.Clear();
                    }
                }
            }
        }

        public void EnhancementFactorProcess(int repetition)
        {
            var lastNStepsD = new List<double>();
            var lastNStepsC = new List<double>();
            var lastNStepsL = _includeLoners ? new List<double>() : null;

            var sd = PGGDynamicModel.StandardDeviationValue;

            InitialStrategies();

            // take at least this much steps first
            var minimumMCS = PGGDynamicModel.MinMCS;

            for (int step = 0; ; step++)
            {
                if (step == PGGDynamicModel.MaxMCS)
                {
                    ProcessResults(-1, -1, -1);
                    break;
                }

                SingleMonteCarloStep();

                RatioOfStrategies();

                var earlyStopCondition = _includeLoners
                    ? (_defectorsRatio == 0 && _cooperatorsRatio == 0) ||
                      (_defectorsRatio == 0 && _lonersRatio == 0) ||
                      (_cooperatorsRatio == 0 && _lonersRatio == 0)
                    : _cooperatorsRatio == 0 || _cooperatorsRatio == 1;

                if (earlyStopCondition)
                {
                    ProcessResults(_defectorsRatio, _cooperatorsRatio, _lonersRatio);

                    Console.WriteLine(repetition + ": " + step + "\t" +
                                      _enhancementFactor + "\t" +
                                      _defectorsRatio + "\t" +
                                      _cooperatorsRatio + "\t" +
                                      (_includeLoners ? _lonersRatio : ""));

                    break;
                }

                if (minimumMCS < step &&
                    step <= minimumMCS + PGGDynamicModel.StepsToAverageOver)
                {
                    lastNStepsD.Add(_defectorsRatio);
                    lastNStepsC.Add(_cooperatorsRatio);
                    lastNStepsL?.Add(_lonersRatio);
                }

                if (step == minimumMCS + PGGDynamicModel.StepsToAverageOver)
                {
                    var standardDeviationD = StandardDeviation(lastNStepsD);
                    var standardDeviationC = _includeLoners
                                             ? StandardDeviation(lastNStepsC!) : 0;

                    var standardDeviationL = _includeLoners
                                             ? StandardDeviation(lastNStepsL!) : 0;

                    var stoppingCondition = _includeLoners
                        ? standardDeviationD <= sd &&
                          standardDeviationC <= sd &&
                          standardDeviationL <= sd
                        : standardDeviationD <= sd;

                    if (stoppingCondition)
                    {
                        var dRatioAverageOfNSteps = PGGDynamics.AverageOfValues(lastNStepsD);
                        var cRatioAverageOfNSteps = PGGDynamics.AverageOfValues(lastNStepsC);
                        var lRatioAverageOfNSteps = _includeLoners
                                                    ? PGGDynamics.AverageOfValues(lastNStepsL!) : 0;


                        ProcessResults(dRatioAverageOfNSteps, cRatioAverageOfNSteps,
                                       lRatioAverageOfNSteps);

                        Console.WriteLine(repetition + ": " +
                                          step + "\t" +
                                          _enhancementFactor + "\t" +
                                          dRatioAverageOfNSteps + "\t" +
                                          cRatioAverageOfNSteps + "\t" +
                                          (_includeLoners ? lRatioAverageOfNSteps : ""));

                        break;
                    }
                    else
                    {
                        minimumMCS += PGGDynamicModel.StepsToAverageOver;
                        lastNStepsD.Clear();
                        lastNStepsC.Clear();
                        lastNStepsL?.Clear();
                    }
                }
            }
        }

        private void InitialStrategies()
        {
            _cooperatorsPerGroup = new int[Network.NodesCount];
            _lonersPerGroup = _includeLoners ? new int[Network.NodesCount] : null;

            _defectorsCount = 0;
            _cooperatorsCount = 0;
            _lonersCount = 0;

            for (int node = 0; node < Network.NodesCount; node++)
            {
                if (Network.DegreeOfNodes[node] != 0)
                {
                    // 0: Defector  1: Cooperator
                    var randomStrategy = _random.Next(_includeLoners ? 3 : 2);
                    _nodeStrategies[node] = randomStrategy;

                    if (randomStrategy == 0)
                        _defectorsCount++;
                    else if (randomStrategy == 1)
                    {
                        _cooperatorsCount++;
                        var nodeMemberships = Network.NodeMemberships[node];
                        for (int i = 0; i < nodeMemberships.Length; i++)
                        {
                            var groupIndex = nodeMemberships[i];
                            _cooperatorsPerGroup[groupIndex]++;
                        }
                    }
                    else
                    {
                        _lonersCount++;
                        var nodeMembershipsLoner = Network.NodeMemberships[node];
                        for (int i = 0; i < nodeMembershipsLoner.Length; i++)
                        {
                            var groupIndex = nodeMembershipsLoner[i];
                            _lonersPerGroup![groupIndex]++;
                        }
                    }
                }
            }
        }

        private void SingleMonteCarloStep()
        {
            for (int i = 0; i < Network.NodesCount; i++)
            {
                var randomNode = _random.Next(Network.NodesCount);
                var randomNodeDegree = Network.DegreeOfNodes[randomNode];

                //To skip nodes with zero links
                //TODO: Change this not to include zero degree nodes in the first place
                while (randomNodeDegree == 0)
                {
                    randomNode = _random.Next(Network.NodesCount);
                    randomNodeDegree = Network.DegreeOfNodes[randomNode];
                }

                var randomNumber = _random.Next(randomNodeDegree);
                var randomNeighbor = Network.LinksOfNodes[randomNode][randomNumber];

                var randomNodeStrategy = _nodeStrategies[randomNode];
                var randomNeighborStrategy = _nodeStrategies[randomNeighbor];

                if (randomNodeStrategy == randomNeighborStrategy)
                    continue;

                var randomNodeMemberships = Network.NodeMemberships[randomNode];
                var randomNodePayoff =
                    PayoffOfNode(randomNode, randomNodeStrategy, randomNodeMemberships);

                var randomNeighborMemberships = Network.NodeMemberships[randomNeighbor];
                var randomNeighborPayoff =
                    PayoffOfNode(randomNeighbor, randomNeighborStrategy, randomNeighborMemberships);

                //TODO: Explain
                var probability = 1 /
                    (1 + Math.Exp((randomNodePayoff - randomNeighborPayoff) / PGGDynamicModel.K));

                var modify = _random.NextDouble();
                if (modify < probability)
                {
                    _nodeStrategies[randomNode] = randomNeighborStrategy;

                    UpdateStrategyCounts(randomNodeStrategy, randomNode, -1);
                    UpdateStrategyCounts(randomNeighborStrategy, randomNode, +1);
                }
            }
        }

        private double PayoffOfNode(int node, int nodeStrategy, int[] nodeMemberships)
        {
            if (_includeLoners && nodeStrategy == 2)
                return nodeMemberships.Length * PGGDynamicModel.LonerPayoff;

            var totalPayoff = .0;
            var membershipsCount = nodeMemberships.Length;
            var totalGamesPlayed = membershipsCount;
            for (var i = 0; i < membershipsCount; i++)
            {
                var groupIndex = nodeMemberships[i];

                var cooperatorsCount = _cooperatorsPerGroup[groupIndex];
                var groupSize = Network.GroupSizes[groupIndex] -
                                (_includeLoners ? _lonersPerGroup[groupIndex] : 0);

                double groupPayoff;
                if (_includeLoners && groupSize == 1)
                {
                    groupPayoff = PGGDynamicModel.LonerPayoff;
                    if (nodeStrategy == 1)
                        totalGamesPlayed--;
                }
                else
                    groupPayoff = _enhancementFactor * cooperatorsCount *
                                  PGGDynamicModel.ContributionCost / groupSize;

                totalPayoff += groupPayoff;
            }

            if (nodeStrategy == 1)
                totalPayoff -= PGGDynamicModel.ContributionCost * totalGamesPlayed;

            return totalPayoff;
        }

        private void RatioOfStrategies()
        {
            var activeNodesCount = Network.NodesCount - Network.ZeroDegreeNodesCount;

            _defectorsRatio = _defectorsCount * 1.0 / activeNodesCount;
            _cooperatorsRatio = _cooperatorsCount * 1.0 / activeNodesCount;

            if (_includeLoners)
                _lonersRatio = _lonersCount * 1.0 / activeNodesCount;
        }

        private void UpdateStrategyCounts(int strategy, int node, int adjustment)
        {
            switch (strategy)
            {
                case 0: // Defector
                    _defectorsCount += adjustment;
                    break;
                case 1: // Cooperator
                    _cooperatorsCount += adjustment;
                    foreach (var groupIndex in Network.NodeMemberships[node])
                    {
                        _cooperatorsPerGroup[groupIndex] += adjustment;
                    }
                    break;
                case 2: // Loner
                    _lonersCount += adjustment;
                    foreach (var groupIndex in Network.NodeMemberships[node])
                    {
                        _lonersPerGroup[groupIndex] += adjustment;
                    }
                    break;
            }
        }

        private double StandardDeviation(List<double> sample)
        {
            var average = .0;
            foreach (var s in sample)
            {
                average += s;
            }
            average /= sample.Count;

            var standardDeviation = .0;
            foreach (var s in sample)
            {
                standardDeviation += (s - average) * (s - average);
            }

            standardDeviation /= sample.Count;
            standardDeviation = Math.Sqrt(standardDeviation);
            return standardDeviation;
        }

        private void ProcessResults(double dRatio, double cRatio, double lRatio)
        {
            var ratios = new List<double> { dRatio, cRatio };
            if (_includeLoners)
            {
                ratios.Add(lRatio);
            }

            lock (_fileLock)
            {
                if (dRatio == -1)
                    _tempFileWriter.Write("Over," + "Over," + (_includeLoners ? "Over," : ""));
                else
                    _tempFileWriter.Write(dRatio + "," + cRatio + "," +
                                          (_includeLoners ? lRatio + "," : ""));
            }


            _tempFileWriter.Flush();
            PGGDynamics.AddResult(_enhancementFactor, ratios);
        }
    }
}
