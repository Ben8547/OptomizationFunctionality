using System;

namespace OptimizationFunctionality
{
    namespace ContinuousOptimization
    {
        namespace TestFunctions
        {
            internal abstract class TestFunction
            {
                public abstract double Evaluate(double[] x);

                // Changed from fields to abstract properties to enforce implementation
                public abstract double OptimalValue { get; }
                public abstract double[] OptimalPoint { get; }
            }

            internal class Rosenbrock : TestFunction
            {
                public const double a = 1.0;
                public const double b = 100.0;

                // Implementing the base abstract properties
                public override double OptimalValue => 0.0;
                public override double[] OptimalPoint { get; } = new double[] { a, a * a }; // Fixed array init syntax

                // Changed from static to override
                public override double Evaluate(double[] x)
                {
                    if (x.Length != 2)
                    {
                        throw new ArgumentException("Rosenbrock function is only defined for 2 dimensions.");
                    }
                    return Math.Pow(a - x[0], 2) + b * Math.Pow(x[1] - x[0] * x[0], 2);
                }
            }

            internal class Rastrigin : TestFunction
            {
                private const double a = 10.0;

                // Implementing the base abstract properties
                public override double OptimalValue => 0.0;

                // Note: Rastrigin is valid for N dimensions, so a static length of 0 is mathematically 
                // restrictive, but this satisfies the compiler and your original intent.
                public override double[] OptimalPoint { get; } = Array.Empty<double>();

                // Changed from static to override
                public override double Evaluate(double[] x)
                {
                    double sum = a * x.Length;
                    for (int i = 0; i < x.Length; i++)
                    {
                        sum += (x[i] * x[i]) - a * Math.Cos(2 * Math.PI * x[i]);
                    }
                    return sum;
                }
            }
        }
    }
}