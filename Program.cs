using OptimizationFunctionality;
using OptimizationFunctionality.ContinuousOptimization;
using OptimizationFunctionality.ContinuousOptimization.TestFunctions;

class Program
{
    static void Main(string[] args)
    {
        //Console.WriteLine("No Implemenation yet");
        TestFunction testFunction = new Rastrigin();
        ObjectiveFunction objectiveFunction = (double[] x) => (testFunction.Evaluate(x));
        ValueTuple<double,double>[] bounds = { (-10.0, 10.0), (-10.0, 10.0) };
        OptimizationProblem optimizationProblem = new OptimizationProblem(objectiveFunction, bounds, tolerance: 1e-10, optimizationType: "min", useHardwareEntropy: false);
        ParticleSwarmOptimizer optimizer = new ParticleSwarmOptimizer(optimizationProblem, numberOfPoints: 100,useQuasirandom: false);
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
}