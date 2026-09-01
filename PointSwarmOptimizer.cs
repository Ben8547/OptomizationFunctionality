//using MathNet.Numerics.LinearAlgebra; // for vectorized math
// was originaly using MathNet for the matrix functionality, but since I need a different random number for each coordinate anyway, it makes more since just to iterate over arrays in this case.


using MathNet.Numerics.Optimization;

namespace OptimizationFunctionality
{
    ///<summary>
    /// This namespace contains the functionality for performing optimization techniques on a given objective function from R^n to R.
    ///</summary>
    namespace ContinuousOptimization
    {
        /// <summary>
        /// Here we implement the basic point swarm optimization technique found in James Kennedy and Russell Eberhart 1995 paper.
        /// This class is derrived from the OptimizationProblem class.
        /// It's mandatory parameter is an object of the OptimizationProblem type.
        /// It additionally takes a set of non-mandatory parameters: the number of points to simulate and the initial state as an array of doubles representing the initial positions of the swarm. The final optional parmaters sets the desired accuracy of the solution. This should be decimal such as 1e-3 meaning that the global solution did not change by more than 1e-3 for a given number of generations before compleation.
        /// A final paramter is a boolean indicating whether to use hardware sourced entropy or not. If not, the default .NET random number generator will be used. By default this is False.
        /// A final note that the intitial state array, if provided must have a number of rows equal to the number of points to be simulated and nuber of columns equal to the dimensions of the solution space.
        /// </summary>
        public class ParticleSwarmOptimizer
        {
            private readonly OptimizationProblem optimizationProblem; // this is passed by reference into the construction since we only need to know the interal value, but we keep it as readonly so as to not change variable outside of the class' scope.
            private readonly int numberOfPoints;
            private readonly double[] lowerBounds; // note that readonly does not make this field immutable
            private readonly double[] upperBounds;
            private double[][] states;
            private double[][] velocities;
            private readonly double stepSize;
            private readonly Random? random = null; // this is used for generating random numbers if hardware entropy is not used.
            private const string optimizationMethod = "PSO"; // short for point swarm optimization.
            /// </summary>
            /// <param name="objectiveFunction"> The function to be optimized</param>
            /// <param name="bounds"> The bounds for the solution space. This should be an array of tuples representing the lower and upper bounds for each dimension</param>
            /// <param name="initialState"> The initial positions of the swarm. Each row is populated by the intial points of a given particle.</param>
            /// <param name="stepSize"> The step size for the optimization</param>
            /// <param name="useHardwareEntropy"> Whether to use hardware-sourced entropy for random number generation</param>
            /// <exception cref="ArgumentException"></exception>
            /// </summary>
            public ParticleSwarmOptimizer(in OptimizationProblem optimizationProblem, in double[][] initialState, in double stepSize = 0.1)
            {
                // constructor for the particle swarm optimizer

                this.optimizationProblem = optimizationProblem;
                this.states = initialState;
                this.stepSize = stepSize;
                this.numberOfPoints = this.states.Length;
                this.velocities = new double[this.numberOfPoints][];
                // initialize the velocities to zero
                for (int i = 0; i < this.numberOfPoints; i++)
                {
                    this.velocities[i] = new double[optimizationProblem.numberOfDimensions];
                }
                if (!optimizationProblem.useHardwareEntropy)
                {
                    this.random = new Random(); // this is used for generating random numbers if hardware entropy is not used.
                }

                // test that the number of dimensions is equal to the length of one of the initial state
                if (optimizationProblem.numberOfDimensions != initialState[0].Length)
                {
                    throw new ArgumentException("The number of dimensions must match the length of the initial state.");
                }

                // set private variables for this class
                this.lowerBounds = new double[optimizationProblem.numberOfDimensions];
                this.upperBounds = new double[optimizationProblem.numberOfDimensions];
                for (int i = 0; i < optimizationProblem.numberOfDimensions; i++)
                {
                    this.lowerBounds[i] = optimizationProblem.bounds[i].Item1;
                    this.upperBounds[i] = optimizationProblem.bounds[i].Item2;
                }
            }

            /// </summary>
            /// <param name="objectiveFunction"> The function to be optimized</param>
            /// <param name="bounds"> The bounds for the solution space. This should be an array of tuples representing the lower and upper bounds for each dimension</param>
            /// <param name="numberOfPoints"> The number of points in the swarm</param>
            /// <param name="stepSize"> The step size for the optimization</param>  
            /// <param name="useHardwareEntropy"> Whether to use hardware-sourced entropy for random number generation</param>
            /// <param name="useQuasirandom"> Whether to use quasi-random number generation for the initial state. If false, uniform random number generation will be used.</param>
            /// <exception cref="ArgumentException"></exception>
            /// </summary>
            public ParticleSwarmOptimizer(in OptimizationProblem optimizationProblem, int numberOfPoints, double stepSize = 0.1, bool useQuasirandom = true)
            {
                // constructor generating a random initial state

                this.numberOfPoints = numberOfPoints;
                this.optimizationProblem = optimizationProblem;
                this.stepSize = stepSize;
                if (!optimizationProblem.useHardwareEntropy)
                {
                    this.random = new Random(); // this is used for generating random numbers if hardware entropy is not used.
                }

                this.lowerBounds = new double[optimizationProblem.numberOfDimensions];
                this.upperBounds = new double[optimizationProblem.numberOfDimensions];
                this.states = new double[this.numberOfPoints][];
                this.velocities = new double[this.numberOfPoints][];
                for (int i = 0; i < optimizationProblem.numberOfDimensions; i++)
                {
                    this.lowerBounds[i] = optimizationProblem.bounds[i].Item1;
                    this.upperBounds[i] = optimizationProblem.bounds[i].Item2;
                }
                for (int j = 0; j < this.numberOfPoints; j++)
                {
                    this.states[j] = new double[optimizationProblem.numberOfDimensions];
                    this.velocities[j] = new double[optimizationProblem.numberOfDimensions];
                    for (int i = 0; i < optimizationProblem.numberOfDimensions; i++)
                    {
                        this.states[j][i] = lowerBounds[i] + (upperBounds[i] - lowerBounds[i]) * GetRandDouble(useQuasirandom);
                        // velocities are initialized to zero by default, so we don't need to set them here.
                    }
                }


            }
            protected double GetRandDouble(bool useQuasirandom = false)
            {
                if (useQuasirandom)
                {
                    return CustomRandom.GetQuasirandom(1)[0];
                }
                else if (random == null) // this is the same as checking if useHardwareEntropy is true, but it is more efficient to check if the random object is null since it is only created if useHardwareEntropy is true.
                {
                    return CustomRandom.GetHardwareRandomDouble();
                }
                return random.NextDouble();
            }

            public ParticleSwarmOptimizer(OptimizationProblem optimizationProblem, double stepSize = 0.1)
                : this(optimizationProblem, 100, stepSize)
            {
                // sets the number of simualted points to 100.
            }
            /// <summary>
            /// Perform point swarm optimization.
            /// </summary>
            /// <returns> The estimated global maximum</returns>
            public OptimizationSolution Optimize()
            {
                //Console.WriteLine($"Optimizing"); // debug
                ObjectiveFunction objectiveFunction;
                MutateProblem(out objectiveFunction); // adjust a min problem to a max problem by negating the objective function if necessary
                double[] particleBestValue = new double[numberOfPoints]; // for each particle, contains its best visited location's score
                double[][] particleBestLocation = new double[numberOfPoints][]; // for each particle, contains its best visited location's coordinates. We need a depp copy because the matrix is a reference type
                double globalBestValue = double.MinValue; // contains the best score of all particles
                double prevBestValue = 0.0;
                double[] globalBestPoint = new double[optimizationProblem.numberOfDimensions];
                // this method will perform the optimization technique
                bool shouldContinue = true;
                byte tracker = 0; // this will track how many iterations have passed without improvement in the global best value
                // now we initialize the particle and global best values
                for (int i = 0; i < numberOfPoints; i++)
                {
                    particleBestLocation[i] = (double[])states[i].Clone(); // we need a deep copy because the matrix is a reference type
                    particleBestValue[i] = objectiveFunction(states[i]);
                    if (particleBestValue[i] > globalBestValue)
                    {
                        globalBestValue = particleBestValue[i];
                        globalBestPoint = (double[])states[i].Clone();
                    }   
                }
                while (shouldContinue)
                {
                    //Console.WriteLine($"Entered Main Loop"); // debug
                    prevBestValue = globalBestValue;
                    for (int i = 0; i < numberOfPoints; i++)
                    {
                        for (int j = 0; j < optimizationProblem.numberOfDimensions; j++)
                        {
                            velocities[i][j] = velocities[i][j] + 2.0 * (GetRandDouble() * (particleBestLocation[i][j] - states[i][j])) + 2.0 * (GetRandDouble() * (globalBestPoint[j] - states[i][j]));
                            states[i][j] = states[i][j] + velocities[i][j] * stepSize; // simple Euler ODE should suffice
                            states[i][j] = Math.Min(Math.Max(states[i][j], lowerBounds[j]), upperBounds[j]); // ensure that the state is within the bounds
                        }
                        // now we determine the best value for each particle and make updates.
                        double objFuncOut = objectiveFunction(states[i]);
                        if (objFuncOut > particleBestValue[i])
                        {
                            particleBestValue[i] = objFuncOut;
                            particleBestLocation[i] = (double[])states[i].Clone();
                            if (particleBestValue[i] > globalBestValue)
                            {
                                globalBestValue = particleBestValue[i];
                                globalBestPoint = (double[])states[i].Clone();
                            }
                        }
                    }
                    if (Math.Abs(prevBestValue - globalBestValue) < optimizationProblem.tolerance)
                    {
                        tracker++;
                    }
                    else
                    {
                        tracker = 0; // reset the tracker since there was a large change in the global optimal value
                    }
                    if (tracker >= 200) // this limit was chosen arbitrarily, but it means that the global best value has not changed by more than the tolerance for 200 iterations, so we can assume convergence.
                    {
                        shouldContinue = false;
                    }
                    //Console.WriteLine($"{tracker}"); // debug
                }

                if (optimizationProblem.optimizationType == "min")
                {
                    globalBestValue = -globalBestValue; // convert back to the original problem's value
                }

                OptimizationSolution solution = new OptimizationSolution(globalBestValue, globalBestPoint, optimizationProblem.tolerance, optimizationProblem.optimizationType, optimizationMethod);
                
                return solution;
            }
            protected void MutateProblem(out ObjectiveFunction objectiveFunction)
            {
                if (optimizationProblem.optimizationType == "min")//convert min problem to max problem by negating the objective function
                {
                    objectiveFunction = (x => -optimizationProblem.objectiveFunction(x));
                }
                else // it is already a max problem
                {
                    objectiveFunction = optimizationProblem.objectiveFunction;
                }
            }

        }

    }
}