//using MathNet.Numerics.LinearAlgebra; // for vectorized math
// was originaly using MathNet for the matrix functionality, but since I need a different random number for each coordinate anyway, it makes more since just to iterate over arrays in this case.
using System.Threading.Channels; // used only for streaming data out of optomization for for tasks such as animation

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

                this.states = new double[this.numberOfPoints][];
                this.velocities = new double[this.numberOfPoints][];
                for (int j = 0; j < this.numberOfPoints; j++)
                {
                    this.states[j] = new double[optimizationProblem.numberOfDimensions];
                    this.velocities[j] = new double[optimizationProblem.numberOfDimensions];
                    for (int i = 0; i < optimizationProblem.numberOfDimensions; i++)
                    {
                        this.states[j][i] = optimizationProblem.LowerBounds[i] + (optimizationProblem.UpperBounds[i] - optimizationProblem.LowerBounds[i]) * GetRandDouble(useQuasirandom);
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
                double multiplier = MutateProblem(out objectiveFunction); // adjust a min problem to a max problem by negating the objective function if necessary
                double[] particleBestValue = new double[numberOfPoints]; // for each particle, contains its best visited location's score
                double[][] particleBestLocation = new double[numberOfPoints][]; // for each particle, contains its best visited location's coordinates. We need a depp copy because the matrix is a reference type
                double globalBestValue = double.MinValue; // contains the best score of all particles
                double prevBestValue = 0.0;
                double[] globalBestPoint = new double[optimizationProblem.numberOfDimensions];
                // this method will perform the optimization technique
                bool shouldContinue = true;
                byte tracker = 0; // this will track how many iterations have passed without improvement in the global best value
                // now we initialize the particle and global best values
                PopulateOptimalArrays(in objectiveFunction, ref particleBestLocation, ref particleBestValue, ref globalBestValue, ref globalBestPoint);

                while (shouldContinue)
                {
                    UpdateStates(in objectiveFunction, ref particleBestLocation, ref particleBestValue, ref globalBestValue, ref globalBestPoint, ref prevBestValue);
                    TrackerLogic(ref tracker, in globalBestValue, in prevBestValue, ref shouldContinue);
                }

                OptimizationSolution solution = new OptimizationSolution(multiplier * globalBestValue, globalBestPoint, optimizationProblem.tolerance, optimizationProblem.optimizationType, optimizationMethod);

                return solution;
            }
            public async Task StreamOptimization(Channel<StreamPackagePSO> channel)
            {

                ObjectiveFunction objectiveFunction;
                double multiplier = MutateProblem(out objectiveFunction); // adjust a min problem to a max problem by negating the objective function if necessary

                double prevBestValue = 0.0d;
                double globalBestValue = double.MinValue; // contains the best score of all particles
                double[] globalBestPoint = new double[optimizationProblem.numberOfDimensions];
                double[][] particleBestLocation = new double[numberOfPoints][]; // for each particle, contains its best visited location's coordinates. We need a deep copy because the matrix is a reference type
                double[] particleBestValue = new double[numberOfPoints];
                double[][] velocities = new double[numberOfPoints][];
                bool shouldContinue = true;
                byte tracker = 0; // this will track how many iterations have passed without improvement in the global best value
                // now we initialize the particle and global best values
                PopulateOptimalArrays(in objectiveFunction, ref particleBestLocation, ref particleBestValue, ref globalBestValue, ref globalBestPoint);

                while (shouldContinue)
                {
                    StreamPackagePSO packet = new StreamPackagePSO(numberOfPoints, optimizationProblem.numberOfDimensions); //make a new stuct each iteration because it contains a reference type. When we push it into the channel, we don't want to overwrite references prematurely if the channel is backlogged.

                    UpdateStates(in objectiveFunction, ref particleBestLocation, ref particleBestValue, ref globalBestValue, ref globalBestPoint, ref prevBestValue);
                    packet.correctedGlobalBestValue = multiplier * globalBestValue;
                    packet.SetStates(states);
                    packet.globalBestPoint = (double[])globalBestPoint.Clone();
                    await channel.Writer.WriteAsync(packet); // add the packet to the channel
                    TrackerLogic(ref tracker, in globalBestValue, in prevBestValue, ref shouldContinue);
                };
                channel.Writer.Complete(); // close the input stream to the channel
            }
            /// <summary>
            /// This method mutates the optimization problem's objective function if it is a minimization problem. It negates the objective function to convert it into a maximization problem, which is required for the particle swarm optimization algorithm. If the optimization problem is already a maximization problem, it simply assigns the original objective function to the output parameter.
            /// </summary>
            /// <param name="objectiveFunction"> The mutated objective function </param>
            /// <returns> The multiplier for correcting the global best value </returns>
            protected double MutateProblem(out ObjectiveFunction objectiveFunction)
            {
                if (optimizationProblem.optimizationType == "min")//convert min problem to max problem by negating the objective function
                {
                    objectiveFunction = (x => -optimizationProblem.objectiveFunction(x));
                    return -1.0d;
                }
                else // it is already a max problem
                {
                    objectiveFunction = optimizationProblem.objectiveFunction;
                    return 1.0d;
                }
            }
            /// <summary>
            /// This method populates the best known locations and values for each particle in the swarm, as well as the global best location and value. It iterates through each particle, evaluates the objective function at its current state, and updates the best known values and locations accordingly.
            /// </summary>
            /// <param name="objectiveFunction"> The objective function to optimize </param>
            /// <param name="particleBestLocation"> The best known locations for each particle </param>
            /// <param name="particleBestValue"> The best known values for each particle </param>
            /// <param name="globalBestValue"> The global best value </param>
            /// <param name="globalBestPoint"> The point yielding the global best value </param>
            protected void PopulateOptimalArrays(in ObjectiveFunction objectiveFunction, ref double[][] particleBestLocation, ref double[] particleBestValue, ref double globalBestValue, ref double[] globalBestPoint)
            {
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
            }
            /// <summary>
            /// This method updates the states of the particles in the swarm based on their velocities and the best known positions of the particles and the global best position. It also updates the best known positions and values for each particle and the global best position and value.
            /// </summary>
            /// <param name="objectiveFunction"> The objective function to optimize </param>
            /// <param name="particleBestLocation"> The best known locations for each particle </param>
            /// <param name="particleBestValue"> The best known values for each particle </param>
            /// <param name="globalBestValue"> The global best value </param>
            /// <param name="globalBestPoint"> The global best point </param>
            /// <param name="prevBestValue"> The previous global best value </param>
            protected void UpdateStates(in ObjectiveFunction objectiveFunction, ref double[][] particleBestLocation, ref double[] particleBestValue, ref double globalBestValue, ref double[] globalBestPoint, ref double prevBestValue)
            {
                //Console.WriteLine($"Entered Main Loop"); // debug
                prevBestValue = globalBestValue;
                for (int i = 0; i < numberOfPoints; i++)
                {
                    for (int j = 0; j < optimizationProblem.numberOfDimensions; j++)
                    {
                        velocities[i][j] = velocities[i][j] + 2.0 * (GetRandDouble() * (particleBestLocation[i][j] - states[i][j])) + 2.0 * (GetRandDouble() * (globalBestPoint[j] - states[i][j]));
                        states[i][j] = states[i][j] + velocities[i][j] * stepSize; // simple Euler ODE should suffice
                        states[i][j] = Math.Min(Math.Max(states[i][j], optimizationProblem.LowerBounds[j]), optimizationProblem.UpperBounds[j]); // ensure that the state is within the bounds
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
            }
            /// <summary>
            /// This method tracks the number of iterations that have passed without improvement in the global best value. If the number of iterations exceeds a certain threshold, the optimization process will stop.
            /// </summary>
            /// <param name="tracker"> tracks the number of iterations without improvement </param>
            /// <param name="globalBestValue"> the current global best value </param>
            /// <param name="prevBestValue"> the previous global best value </param>
            /// <param name="shouldContinue"> indicates whether the optimization should continue </param>
            /// <param name="threshhold"> the threshold for the number of iterations without improvement </param>
            protected void TrackerLogic(ref byte tracker, in double globalBestValue, in double prevBestValue, ref bool shouldContinue, byte threshhold = 200)
            {
                if (Math.Abs(prevBestValue - globalBestValue) < optimizationProblem.tolerance)
                {
                    tracker++;
                }
                else
                {
                    tracker = 0; // reset the tracker since there was a large change in the global optimal value
                }
                if (tracker >= threshhold) // this limit was chosen arbitrarily, but it means that the global best value has not changed by more than the tolerance for 200 iterations, so we can assume convergence.
                {
                    shouldContinue = false;
                }
                //Console.WriteLine($"{tracker}"); // debug
            }
        }
        /// <summary>
        /// A structure for cleaning encapselating the optimazation progress streamed out of the Particle Swarm Optimizer
        /// </summary>
        public struct StreamPackagePSO // needs to be public so that the channel can be created to accept this type
        {
            public double[][] states;
            public double correctedGlobalBestValue; // the global best value after correcting for minimization problems
            public double[] globalBestPoint;
            public StreamPackagePSO(int numParticles, int numDimensions)
            {
                this.states = new double[numParticles][];
                for (int i = 0; i < numParticles; i++)
                {
                    this.states[i] = new double[numDimensions];
                }
                this.correctedGlobalBestValue = double.MinValue;
                this.globalBestPoint = new double[numDimensions];
            }
            /// <summary>
            /// Makes a deep copy of another jagged array
            /// </summary>
            /// <param name="state"> The jagged array to copy </param>
            public void SetStates(double[][] state)
            {
                for (int i = 0; i < state.Length; i++)
                {
                    states[i] = (double[])state[i].Clone();
                }
            }
        }
    }
}