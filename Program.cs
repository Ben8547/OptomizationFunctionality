using OptimizationFunctionality;
using OptimizationFunctionality.ContinuousOptimization;
using OptimizationFunctionality.ContinuousOptimization.TestFunctions;
using OptimizationFunctionality.ContinuousOptimization.AnmimationUtilities;

class Program
{
    static async Task Main(string[] args)
    {
        //Console.WriteLine("No Implemenation yet");
        //TestPSO1();
        TestFunction testFunction = new Rosenbrock();
        ContinuousObjectiveFunction objectiveFunction = (double[] x) => (testFunction.Evaluate(x));
        ValueTuple<double, double>[] bounds = { (-10.0, 10.0), (-10.0, 10.0) };
        ContinuousOptimizationProblem optimizationProblem = new ContinuousOptimizationProblem(objectiveFunction, bounds, tolerance: 1e-10, optimizationType: "min", useHardwareEntropy: false);

        string filePath = "C:\\Users\\19738\\Desktop\\Optomization\\OptomizationFunctionality\\OptomizationFunctionality";


        Task animate = AnimateTestPOS1(optimizationProblem,100,false,1e-3,10,filePath+"\\PSO_Anim.mp4");

        await Task.WhenAny(animate);


    }
    static void TestPSO1()
    {
        TestFunction testFunction = new Rastrigin();
        ContinuousObjectiveFunction objectiveFunction = (double[] x) => (testFunction.Evaluate(x));
        ValueTuple<double, double>[] bounds = { (-10.0, 10.0), (-10.0, 10.0) };
        ContinuousOptimizationProblem optimizationProblem = new ContinuousOptimizationProblem(objectiveFunction, bounds, tolerance: 1e-10, optimizationType: "min", useHardwareEntropy: false);
        ParticleSwarmOptimizer optimizer = new ParticleSwarmOptimizer(optimizationProblem, numberOfPoints: 100, useQuasirandom: false);
        OptimizationSolution solution = optimizer.Optimize();
        Console.Write($"Optimal value: {solution.optimalValue} achieved at (");
        for (int i = 0; i < solution.optimalPoint.Length; i++)
        {
            if (i == solution.optimalPoint.Length - 1)
            {
                Console.Write($"{solution.optimalPoint[i]}");
            }
            else
            {
                Console.Write($"{solution.optimalPoint[i]}, ");
            }
        }
        Console.Write(")");
    }
    static async Task AnimateTestPOS1(ContinuousOptimizationProblem problem, int numPoints, bool useQuasirandom = false, double stepSize = 0.1, int frameRate = 30, string outFilePath = "./PSO_Anim.mp4")
    {
        // create a channel through which to recieve each frame of the animation
        Task animate = AnimationPSO.AnimatePSOwith2DSolutionSpace(problem,numPoints: numPoints, useQuasirandom: useQuasirandom, frameRate: frameRate, outFilePath: outFilePath, stepSize: stepSize);

        await Task.WhenAny(animate);
    }
}