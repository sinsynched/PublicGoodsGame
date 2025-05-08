using System.Diagnostics;

namespace PublicGoodsGame
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            FileManager.ReadParametersFromConfigFile();

            Network.Create();

            FileManager.Initialize();

            await PGGDynamics.RunAsync();

            stopWatch.Stop();
            var timePast = stopWatch.Elapsed.ToString("dd\\.hh\\:mm\\:ss");

            Console.WriteLine("\nRuntime: " + timePast);
            FileManager.ResultFile.WriteLine("\nRuntime," + timePast);

            FileManager.Finalize();

            Console.ReadLine();
        }
    }
}
