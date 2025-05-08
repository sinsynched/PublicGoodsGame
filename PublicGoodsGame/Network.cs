namespace PublicGoodsGame
{
    enum NetworkTypes
    {
        SquareLattice,
        ErdosRenyi,
        RingGraph,
        BarabasiAlbert,
        TriangularLattice,
        Honeycomb
    }

    enum RewiringType
    {
        SqareLattice,
        RingGraph,
        Regular
    }

    static class Network
    {
        public static bool LoadNetwork { get; set; }
        public static NetworkTypes NetworkType { get; set; }
        public static bool Rewiring { get; set; }
        public static RewiringType RewiringType { get; set; }
        public static int RingGraphDegree { get; set; }
        public static double RewiringProbability { get; set; }
        public static double PortionOfLinksToRewire { get; set; }
        public static double ErdosRenyiProbability { get; set; }
        public static int NodesCount { get; set; }
        public static int LatticeLength { get; set; }
        public static int LinksCount { get; set; }
        public static int ZeroDegreeNodesCount { get; set; }

        public static int[][] LinksOfNodes { get; set; }
        public static List<int>[] LinksOfNodesBeforeRewiring { get; set; }
        public static int[] DegreeOfNodes { get; set; }
        public static int[][][] GroupsOfNeighbors { get; set; }
        public static int[] StrategyOfNodes { get; set; }

        //new properties
        //TODO : why properties?! do you need them?
        public static int[][] NodeMemberships { get; set; }
        public static int[][] GroupMembers { get; set; }
        public static int[] GroupSizes { get; set; }


        private static readonly Random random = new();

        public static void Initialize(bool loadNetwork,
                                      NetworkTypes networkToCreate,
                                      double erdosRenyiProbability,
                                      int nodesCount,
                                      int ringGraphDegree,
                                      bool rewiring,
                                      RewiringType rewiringType,
                                      int portionOfLinksToRewire,
                                      double rewiringProbability,
                                      int latticeLength)
        {
            LoadNetwork = loadNetwork;
            NetworkType = networkToCreate;
            ErdosRenyiProbability = erdosRenyiProbability;
            NodesCount = nodesCount;
            RingGraphDegree = ringGraphDegree;
            Rewiring = rewiring;
            RewiringType = rewiringType;
            PortionOfLinksToRewire = portionOfLinksToRewire;
            RewiringProbability = rewiringProbability;
            LatticeLength = latticeLength;
        }

        public static void Create()
        {
            if (NetworkType == NetworkTypes.SquareLattice ||
                NetworkType == NetworkTypes.TriangularLattice ||
                NetworkType == NetworkTypes.Honeycomb)
            {
                NodesCount = LatticeLength * LatticeLength;
            }

            if (!LoadNetwork)
            {
                LinksOfNodes = new int[NodesCount][];

                switch (NetworkType)
                {
                    case NetworkTypes.SquareLattice:
                        SimpleSquareLattice();
                        break;
                    case NetworkTypes.ErdosRenyi:
                        ErdosRenyiNetwork(ErdosRenyiProbability);
                        break;
                    case NetworkTypes.RingGraph:
                        RegularRingGraph(RingGraphDegree);
                        break;
                    case NetworkTypes.BarabasiAlbert:
                        BarabasiAlbertNetwork();
                        break;
                    case NetworkTypes.TriangularLattice:
                        TriangularLattice();
                        break;
                    case NetworkTypes.Honeycomb:
                        HoneycombNetwork();
                        break;
                }

                SaveNetwork();
            }
            else
                LoadNetworkProcess();

            if (Rewiring)
            {
                switch (RewiringType)
                {
                    case RewiringType.SqareLattice:
                        SquareLatticeRewiring(RewiringProbability);
                        break;
                    case RewiringType.RingGraph:
                        RingGraphRewiring(RewiringProbability, RingGraphDegree);
                        break;
                    case RewiringType.Regular:
                        RegularRewiring(PortionOfLinksToRewire);
                        break;
                }
            }

            DegreeOfNodes = new int[NodesCount];
            GroupsOfNeighbors = new int[NodesCount][][];
            StrategyOfNodes = new int[NodesCount];

            SetDegreeOfNodes();
            DegreeDistribution();
            AssignNodesToGroups();
            CountNumberOfLinks();
            CountZeroDegreeNodes();
        }

        public static string GetNetworkType()
        {
            return NetworkType switch
            {
                NetworkTypes.SquareLattice => "Square Lattice",
                NetworkTypes.ErdosRenyi => "Erdos-Renyi" + "," + "p = " + ErdosRenyiProbability,
                NetworkTypes.RingGraph => "Ring Graph" + "," + RingGraphDegree,
                NetworkTypes.BarabasiAlbert => "Barabasi-Albert",
                NetworkTypes.TriangularLattice => "Triangular Lattice",
                NetworkTypes.Honeycomb => "Honeycomb Network",
                _ => throw new NotImplementedException() //TODO: add exception handling
            };
        }

        private static void SimpleSquareLattice()
        {
            LinksOfNodesBeforeRewiring = new List<int>[NodesCount];

            for (int node = 0; node < NodesCount; node++)
            {
                var linksOfEachNode = new List<int>();

                //left link
                if (node % LatticeLength == 0)
                    linksOfEachNode.Add(node + LatticeLength - 1);
                else
                    linksOfEachNode.Add(node - 1);

                //top link
                if (node - LatticeLength < 0)
                    linksOfEachNode.Add(node + LatticeLength * (LatticeLength - 1));
                else
                    linksOfEachNode.Add(node - LatticeLength);

                //right link
                if ((node + 1) % LatticeLength == 0)
                    linksOfEachNode.Add(node - LatticeLength + 1);
                else
                    linksOfEachNode.Add(node + 1);

                //bottom link
                if (node + LatticeLength >= LatticeLength * LatticeLength)
                    linksOfEachNode.Add(node + LatticeLength * (1 - LatticeLength));
                else
                    linksOfEachNode.Add(node + LatticeLength);

                LinksOfNodesBeforeRewiring[node] = linksOfEachNode;
            }

            if (!Rewiring)
            {
                for (int node = 0; node < NodesCount; node++)
                {
                    LinksOfNodes[node] = LinksOfNodesBeforeRewiring[node].ToArray();
                }
            }
        }

        private static void ErdosRenyiNetwork(double p)
        {
            var tmpLinks = new List<int>[NodesCount];
            for (int i = 0; i < NodesCount; i++)
            {
                tmpLinks[i] = new List<int>();
            }

            float rndNum;
            for (int i = 0; i < NodesCount; i++)
            {
                for (int j = i + 1; j < NodesCount; j++)
                {
                    rndNum = random.NextSingle();
                    if (rndNum < p)
                    {
                        tmpLinks[i].Add(j);
                        tmpLinks[j].Add(i);
                    }
                }
            }

            for (int node = 0; node < NodesCount; node++)
            {
                LinksOfNodes[node] = tmpLinks[node].ToArray();
            }
        }

        private static void RegularRingGraph(int degree)
        {
            LinksOfNodesBeforeRewiring = new List<int>[NodesCount];

            for (int node = 0; node < NodesCount; node++)
            {
                var linksOfEachNode = new List<int>();

                var nodeIncreament = 1;
                while (nodeIncreament <= degree / 2)
                {

                    linksOfEachNode.Add(PeriodicCondition(node + nodeIncreament));
                    linksOfEachNode.Add(PeriodicCondition(node - nodeIncreament));

                    nodeIncreament++;
                }

                LinksOfNodesBeforeRewiring[node] = linksOfEachNode;
            }

            if (!Rewiring)
            {
                for (int node = 0; node < NodesCount; node++)
                {
                    LinksOfNodes[node] = LinksOfNodesBeforeRewiring[node].ToArray();
                }
            }

            int PeriodicCondition(int value)
            {
                if (value < 0)
                    return NodesCount + value;
                else if (value >= NodesCount)
                    return value - NodesCount;
                else
                    return value;
            }
        }

        private static void TriangularLattice()
        {
            LinksOfNodesBeforeRewiring = new List<int>[NodesCount];

            var condition = 1;
            for (int j = 0; j < LatticeLength; j++)
            {
                for (int i = 0; i < LatticeLength; i++)
                {
                    var linksOfEachNode = new List<int>();
                    int nodeToLink;

                    //left link
                    nodeToLink = FindTheTargetNode(PeriodicCondition(i - 1), j);
                    linksOfEachNode.Add(nodeToLink);

                    //right link
                    nodeToLink = FindTheTargetNode(PeriodicCondition(i + 1), j);
                    linksOfEachNode.Add(nodeToLink);

                    //top link
                    nodeToLink = FindTheTargetNode(i, PeriodicCondition(j - 1));
                    linksOfEachNode.Add(nodeToLink);

                    //bottom link
                    nodeToLink = FindTheTargetNode(i, PeriodicCondition(j + 1));
                    linksOfEachNode.Add(nodeToLink);

                    nodeToLink = FindTheTargetNode
                        (PeriodicCondition(i - condition), PeriodicCondition(j - 1));
                    linksOfEachNode.Add(nodeToLink);

                    nodeToLink = FindTheTargetNode
                        (PeriodicCondition(i - condition), PeriodicCondition(j + 1));
                    linksOfEachNode.Add(nodeToLink);

                    LinksOfNodesBeforeRewiring[FindTheTargetNode(i, j)] =
                        linksOfEachNode;
                }
                condition = -condition;
            }

            if (!Rewiring)
            {
                for (int node = 0; node < NodesCount; node++)
                {
                    LinksOfNodes[node] = LinksOfNodesBeforeRewiring[node].ToArray();
                }
            }

            int FindTheTargetNode(int i, int j)
            {
                var targetNode = j * LatticeLength + i;
                return targetNode;
            }

            int PeriodicCondition(int value)
            {
                if (value < 0)
                    return LatticeLength + value;
                else if (value >= LatticeLength)
                    return value - LatticeLength;
                else
                    return value;
            }
        }

        private static void HoneycombNetwork()
        {
            LinksOfNodesBeforeRewiring = new List<int>[NodesCount];

            var bottomLink = true;
            for (int j = 0; j < LatticeLength; j++)
            {
                for (int i = 0; i < LatticeLength; i++)
                {
                    var linksOfEachNode = new List<int>();
                    int nodeToLink;

                    //left link
                    nodeToLink = FindTheTargetNode(PeriodicCondition(i - 1), j);
                    linksOfEachNode.Add(nodeToLink);

                    //right link
                    nodeToLink = FindTheTargetNode(PeriodicCondition(i + 1), j);
                    linksOfEachNode.Add(nodeToLink);

                    if (!bottomLink)
                    {
                        //top link
                        nodeToLink = FindTheTargetNode(i, PeriodicCondition(j - 1));
                        linksOfEachNode.Add(nodeToLink);
                    }
                    else
                    {
                        //bottom link
                        nodeToLink = FindTheTargetNode(i, PeriodicCondition(j + 1));
                        linksOfEachNode.Add(nodeToLink);
                    }

                    bottomLink = !bottomLink;

                    LinksOfNodesBeforeRewiring[FindTheTargetNode(i, j)] = linksOfEachNode;
                }

                bottomLink = !bottomLink;
            }

            if (!Rewiring)
            {
                for (int node = 0; node < NodesCount; node++)
                {
                    LinksOfNodes[node] = LinksOfNodesBeforeRewiring[node].ToArray();
                }
            }

            int FindTheTargetNode(int i, int j)
            {
                var targetNode = j * LatticeLength + i;
                return targetNode;
            }

            int PeriodicCondition(int value)
            {
                if (value < 0)
                    return LatticeLength + value;
                else if (value >= LatticeLength)
                    return value - LatticeLength;
                else
                    return value;
            }
        }

        private static void BarabasiAlbertNetwork()
        {
            var tmpLinks = new List<int>[NodesCount];

            tmpLinks[0] = new List<int> { 1 };
            tmpLinks[1] = new List<int> { 0 };

            var tickets = 2;

            for (int node = 2; node < NodesCount; node++)
            {
                var rndNumber = random.Next(tickets);
                int winnerNode = 0;

                for (int i = 0; i < tmpLinks.Length; i++)
                {
                    rndNumber -= tmpLinks[i].Count;
                    if (rndNumber <= 0)
                    {
                        winnerNode = i;
                        tickets += 2;
                        break;
                    }
                }

                tmpLinks[node] = new List<int> { winnerNode };
                tmpLinks[winnerNode].Add(node);
            }


            for (int node = 0; node < NodesCount; node++)
            {
                LinksOfNodes[node] = tmpLinks[node].ToArray();
            }
        }

        private static void SquareLatticeRewiring(double probability)
        {
            var linksAfterRewiring = new List<int>[NodesCount];

            for (int node = 0; node < NodesCount; node++)
            {
                linksAfterRewiring[node] = new List<int>();
            }

            for (int node = 0; node < NodesCount; node++)
            {
                var neighborNodes = LinksOfNodesBeforeRewiring[node];

                for (int i = 0; i < neighborNodes.Count;)
                {
                    var neighborToRewire = neighborNodes[i];

                    var modify = random.NextSingle();
                    if (modify < probability)
                    {
                        // because unlike the ring graph, we don't know which link
                        // shoud be rewired here so we need two arrays, before and 
                        // after
                        var updatedNeighbors = neighborNodes
                            .Concat(linksAfterRewiring[node])
                            .ToArray();

                        int rndNode;
                        while (true)
                        {
                            rndNode = random.Next(NodesCount);
                            if (rndNode == node)
                                continue;

                            var duplicateLink = false;
                            foreach (var n in updatedNeighbors)
                            {
                                if (n == neighborToRewire)
                                    continue;

                                if (n == rndNode)
                                {
                                    duplicateLink = true;
                                    break;
                                }
                            }

                            if (!duplicateLink)
                            {
                                linksAfterRewiring[node].Add(rndNode);
                                linksAfterRewiring[rndNode].Add(node);
                                break;
                            }
                        }
                    }
                    else
                    {
                        linksAfterRewiring[node].Add(neighborToRewire);
                        linksAfterRewiring[neighborToRewire].Add(node);
                    }

                    LinksOfNodesBeforeRewiring[neighborToRewire].Remove(node);
                    LinksOfNodesBeforeRewiring[node].Remove(neighborToRewire);
                }
            }

            for (int i = 0; i < NodesCount; i++)
            {
                LinksOfNodes[i] = linksAfterRewiring[i].ToArray();
            }
        }

        private static void RegularRewiring(double portionOfLinksToRewire)
        {
            var linksCountBeforeRewiring = CountNumberOfLinksBeforeRewiring();

            var numOfLinksToRewire = portionOfLinksToRewire * linksCountBeforeRewiring;

            var linksAfterRewiring = new List<int>[NodesCount];

            for (int node = 0; node < NodesCount; node++)
            {
                linksAfterRewiring[node] = new List<int>();
            }

            var firstNodeToBreakLink = random.Next(NodesCount);
            var lastNodeToRecieveLink = firstNodeToBreakLink;

            var nextNodeToWire = BreakLink(firstNodeToBreakLink);

            while (numOfLinksToRewire > 0)
            {
                if (numOfLinksToRewire != 1)
                    nextNodeToWire = WireAndBreakProcess(nextNodeToWire);

                else
                {
                    if (nextNodeToWire == lastNodeToRecieveLink)
                        throw new Exception("Rewiring Failed: The first and the last node are the same!");
                    else if (
                   linksAfterRewiring[nextNodeToWire].Contains(lastNodeToRecieveLink) ||
                   LinksOfNodesBeforeRewiring[nextNodeToWire].Contains(lastNodeToRecieveLink))
                        throw new Exception("Rewiring Failed: Last link would be a duplicate!");
                    else
                    {
                        linksAfterRewiring[nextNodeToWire].Add(lastNodeToRecieveLink);
                        linksAfterRewiring[lastNodeToRecieveLink].Add(nextNodeToWire);
                    }
                }

                numOfLinksToRewire--;
            }

            for (int i = 0; i < NodesCount; i++)
            {
                LinksOfNodes[i] = LinksOfNodesBeforeRewiring[i]
                    .Concat(linksAfterRewiring[i])
                    .ToArray();
            }

            int WireAndBreakProcess(int node)
            {
                var linksAfter = linksAfterRewiring[node];
                var updatedNeighbors = LinksOfNodesBeforeRewiring[node]
                                      .Concat(linksAfter);

                while (true)
                {
                    var randomNodeToWire = random.Next(NodesCount);

                    if (randomNodeToWire == node)
                        continue;

                    if (LinksOfNodesBeforeRewiring[randomNodeToWire].Count == 0)
                        continue;

                    var duplicateLink = false;
                    foreach (var n in updatedNeighbors)
                    {
                        if (n == randomNodeToWire)
                        {
                            duplicateLink = true;
                            break;
                        }
                    }

                    if (!duplicateLink)
                    {
                        linksAfterRewiring[node].Add(randomNodeToWire);

                        linksAfterRewiring[randomNodeToWire].Add(node);

                        var nextNodeToWire = BreakLink(randomNodeToWire);

                        return nextNodeToWire;
                    }
                }
            }

            int BreakLink(int node)
            {
                var linksBefore = LinksOfNodesBeforeRewiring[node];

                var randomNumber = random.Next(linksBefore.Count);

                var randomNeighbor = linksBefore[randomNumber];

                linksBefore.Remove(randomNeighbor);
                LinksOfNodesBeforeRewiring[randomNeighbor].Remove(node);

                return randomNeighbor;
            }
        }

        private static void RingGraphRewiring(double rewiringProbability, int degree)
        {
            for (int node = 0; node < NodesCount; node++)
            {
                var nthRightNeighbor = 1;
                while (nthRightNeighbor <= 2)
                {
                    var linkedNodeToRewire = PeriodicCondition(node + nthRightNeighbor);

                    var modify = random.NextSingle();
                    if (modify < rewiringProbability)
                    {
                        while (true)
                        {
                            var rndNode = random.Next(NodesCount);
                            if (rndNode == node)
                                continue;

                            var duplicateLink = false;
                            foreach (var n in LinksOfNodesBeforeRewiring[node])
                            {
                                if (n == linkedNodeToRewire)
                                    continue;

                                if (n == rndNode)
                                {
                                    duplicateLink = true;
                                    break;
                                }
                            }

                            if (!duplicateLink)
                            {
                                LinksOfNodesBeforeRewiring[node].Remove(linkedNodeToRewire);
                                LinksOfNodesBeforeRewiring[linkedNodeToRewire].Remove(node);

                                LinksOfNodesBeforeRewiring[node].Add(rndNode);
                                LinksOfNodesBeforeRewiring[rndNode].Add(node);

                                break;
                            }
                        }
                    }

                    nthRightNeighbor++;
                }
            }

            for (int node = 0; node < NodesCount; node++)
            {
                LinksOfNodes[node] = LinksOfNodesBeforeRewiring[node].ToArray();
            }

            int PeriodicCondition(int value)
            {
                if (value < 0)
                    return NodesCount + value;
                else if (value >= NodesCount)
                    return value - NodesCount;
                else
                    return value;
            }

        }

        private static void SetDegreeOfNodes()
        {
            for (int node = 0; node < NodesCount; node++)
            {
                DegreeOfNodes[node] = LinksOfNodes[node].Length;
            }
        }

        private static void AssignNodesToGroups()
        {
            GroupMembers = new int[NodesCount][];
            GroupSizes = new int[NodesCount];

            var nodeMemberships = new List<int>[NodesCount];
            for (int groupIndex = 0; groupIndex < NodesCount; groupIndex++)
            {
                var groupMembers = new List<int>() { groupIndex };
                groupMembers.AddRange(LinksOfNodes[groupIndex]);
                GroupMembers[groupIndex] = groupMembers.ToArray();
                GroupSizes[groupIndex] = groupMembers.Count;

                foreach (var n in groupMembers)
                {
                    if (nodeMemberships[n] == null)
                        nodeMemberships[n] = new List<int>();
                    nodeMemberships[n].Add(groupIndex);
                }
            }

            NodeMemberships = new int[NodesCount][];
            for (int node = 0; node < NodesCount; node++)
            {
                if (nodeMemberships[node] == null)
                    NodeMemberships[node] = Array.Empty<int>();
                else
                    NodeMemberships[node] = nodeMemberships[node].ToArray();
            }
        }

        private static void CountNumberOfLinks()
        {
            var numberOfLinks = 0;

            for (int node = 0; node < NodesCount; node++)
            {
                numberOfLinks += DegreeOfNodes[node];
            }
            numberOfLinks /= 2;

            LinksCount = numberOfLinks;
        }

        private static int CountNumberOfLinksBeforeRewiring()
        {
            var numberOfLinks = 0;

            for (int node = 0; node < NodesCount; node++)
            {
                numberOfLinks += LinksOfNodesBeforeRewiring[node].Count;
            }

            numberOfLinks /= 2;

            return numberOfLinks;
        }

        private static void CountZeroDegreeNodes()
        {
            for (int node = 0; node < NodesCount; node++)
            {
                if (DegreeOfNodes[node] == 0)
                {
                    ZeroDegreeNodesCount++;
                }
            }
        }

        private static void DegreeDistribution()
        {
            var degreeDistribution = new Dictionary<int, int>();

            for (int node = 0; node < NodesCount; node++)
            {
                var nodeDegree = DegreeOfNodes[node];

                if (!degreeDistribution.ContainsKey(nodeDegree))
                    degreeDistribution.Add(nodeDegree, 1);
                else
                    degreeDistribution[nodeDegree] += 1;
            }

            var sw = FileManager.ResultFile;
            sw.WriteLine("#Degree Distribution" + "\n" +
                         "Degree,Density");
            foreach (var node in degreeDistribution)
            {
                sw.WriteLine(node.Key + "," +
                             node.Value * 1.0 / NodesCount);
            }

            sw.WriteLine();
            sw.Flush();
        }

        private static void SaveNetwork()
        {
            FileManager.NetworkFile = new("Network.csv");
            var sw = FileManager.NetworkFile;

            sw.WriteLine("#Network Info" + "\n" +
                         "Network Type," + GetNetworkType() + "\n" +
                         "Number of Nodes," + NodesCount + "\n");

            sw.WriteLine("#Load Network Info" + "\n" +
                         "NetworkType," + NetworkType + "\n" +
                         "NodesCount," + NodesCount + "\n\n" +
                         "#Links of Each Node");

            for (int i = 0; i < NodesCount; i++)
            {
                if (LinksOfNodes[i].Length == 0)
                    sw.Write("-1"); // to indicate that the node has no links
                else
                {
                    foreach (var item in LinksOfNodes[i])
                    {
                        if (item != LinksOfNodes[i].Last())
                            sw.Write(item + ",");
                        else
                            sw.Write(item);
                    }
                }

                if (i != NodesCount - 1)
                    sw.WriteLine();
            }
            sw.Flush();
            sw.Close();
        }

        private static void LoadNetworkProcess()
        {
            var loadedLinksOfNodes = new List<int[]>();

            var readingNetworkParameters = false;
            var skipLines = true;
            foreach (var line in File.ReadLines("Network.csv"))
            {
                if (line.StartsWith("#Load Network Info"))
                {
                    skipLines = false;
                    continue;
                }
                else if (line.StartsWith("#Links of Each Node"))
                {
                    readingNetworkParameters = true;
                    continue;
                }
                else if (skipLines)
                    continue;

                var parts = line.Split(",");

                if (readingNetworkParameters)
                {
                    var linksOfEachnode = Array.ConvertAll(parts, int.Parse);

                    if (linksOfEachnode[0] == -1)
                        loadedLinksOfNodes.Add(Array.Empty<int>());
                    else
                        loadedLinksOfNodes.Add(linksOfEachnode);
                }
                else
                {
                    if (parts.Length == 2 && parts[0].StartsWith("NodesCount"))
                    {
                        NodesCount = int.Parse(parts[1]);
                    }
                    else if (parts.Length == 2 && parts[0].StartsWith("NetworkType"))
                    {
                        NetworkType = (NetworkTypes)Enum.Parse(typeof(NetworkTypes), parts[1]);
                    }
                }
            }

            LinksOfNodes = new int[NodesCount][];

            if (Rewiring)
            {
                LinksOfNodesBeforeRewiring = new List<int>[NodesCount];

                for (int n = 0; n < NodesCount; n++)
                {
                    LinksOfNodesBeforeRewiring[n] = loadedLinksOfNodes[n].ToList();
                }
            }
            else
            {
                for (int n = 0; n < NodesCount; n++)
                {
                    LinksOfNodes[n] = loadedLinksOfNodes[n];
                }
            }
        }
    }
}
