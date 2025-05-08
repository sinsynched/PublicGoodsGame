namespace PublicGoodsGame.Dynamics
{
    static class PGGDynamicModel
    {
        public static bool WithLoners { get; set; }
        public static bool EnhancementRange { get; set; }
        public static bool WithSatisfaction { get; set; }
        public static double SatisfactionUpdateProbability { get; set; }
        public static double EnhancementFactor { get; set; }
        public static double MaxEnhancementFactor { get; set; }
        public static int RepetitionsCount { get; set; }
        public static int MinMCS { get; set; }
        public static int MaxMCS { get; set; }
        public static int StepsToAverageOver { get; set; }
        public static double EnhancementFactorTick { get; set; }
        //TODO: change for parsing issue
        public static double[]? MinorEnhancementFactorTickInterval { get; set; }
        public static double MinorEnhancementFactorTick { get; set; }
        public static double ContributionCost { get; set; }
        public static double LonerPayoff { get; set; }
        public static double StandardDeviationValue { get; set; }
        public static double K { get; set; }

        // for saving final results


        public static void Initialize(bool withLoners,
                                      bool withSatisfaction,
                                      double satisfactionUpdateProbability,
                                      bool enhancementRange,
                                      double enhancementFactor,
                                      double maxEnhancementFactor,
                                      int repetitionsCount,
                                      int minMCS,
                                      int stepsToAverageOver,
                                      double k,
                                      double enhancementFactorTick,
                                      double[] smallerEnhancementFactorTickInterval,
                                      double smallerEnhancementFactorTick,
                                      int maxMCS,
                                      int contributionCost,
                                      int lonerPayoff,
                                      double standardDeviationValue)
        {
            WithLoners = withLoners;
            WithSatisfaction = withSatisfaction;
            SatisfactionUpdateProbability = satisfactionUpdateProbability;
            EnhancementRange = enhancementRange;
            EnhancementFactor = enhancementFactor;
            MaxEnhancementFactor = maxEnhancementFactor;
            RepetitionsCount = repetitionsCount;
            MinMCS = minMCS;
            StepsToAverageOver = stepsToAverageOver;
            K = k;
            EnhancementFactorTick = enhancementFactorTick;
            MinorEnhancementFactorTickInterval = smallerEnhancementFactorTickInterval;
            MinorEnhancementFactorTick = smallerEnhancementFactorTick;
            MaxMCS = maxMCS;
            ContributionCost = contributionCost;
            LonerPayoff = lonerPayoff;
            StandardDeviationValue = standardDeviationValue;
        }
    }
}
