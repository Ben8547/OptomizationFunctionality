using System;

namespace OptimizationFunctionality;

///<summary>
/// this defines an objection function from R^n to R
/// </summary>
public delegate double ContinuousObjectiveFunction(double[] x);
/// <summary>
/// This type of objective functon is meant to model a problem taking a finite number of configurations of the solution.
/// The integer array is meant to represent the most general form of input so that it may be compatable with as many types of problem as possible.
/// It is the objective function's responcibility to map the integer keys to the given problem.
/// An array of integers is allowed in the case that a solution space may contain a set of finite items, and the given set of items determines the objective's value.
/// For example, instead of giveing each configuration of the solution space a unique integer, we give each component of the space an integer and permute the inters within the array
/// to find the optimal configuration of the integer array.
/// </summary>
/// <param name="x"></param>
/// <returns> a real number which is to be optimized under the range of inputs</returns>
public delegate double DiscreteObjectiveFunction(int[] x);


public abstract class OptimizationProblem
{
    public readonly bool useHardwareEntropy;
    public readonly double tolerance; // the tolerance for convergence
    public readonly string optimizationType; // the type of optimization problem, either "min" or "max". This is a read only field of the class. The default is "min".
    protected OptimizationProblem(double tolerance, bool useHardwareEntropy, string optimizationType="min")
    {
        this.tolerance  = tolerance;
        this.useHardwareEntropy = useHardwareEntropy;
        this.optimizationType= optimizationType.ToLower();
        if (this.optimizationType != "min" && this.optimizationType != "max")
        {
            throw new ArgumentException("Optimiztion type must be \"min\" or \"max\"");
        }
    }

}


/// <summary>
/// This class stores all of the data required to perform a given optomization problem.
/// From this class it should be possible to perform and optimization technique.
/// Each Technique will be derrived from this class so that class contains only the functionality common to each technique.
/// </summary>
public class ContinuousOptimizationProblem : OptimizationProblem // not sealed incase a particular optimization technique needs to derrive from this class to implement its own functionality
{
    public readonly int numberOfDimensions; // the dimension of the solution space
    private readonly ContinuousObjectiveFunction objective; // this allows us to privately mutate the objective to convert minimization problems into maximization ones
    public ContinuousObjectiveFunction objectiveFunction { get { return objective; } } // the property pointing to the objective function to be optimized
    private readonly double[] lowerBounds;
    private readonly double[] upperBounds;
    public double[] LowerBounds
    {
        get { return lowerBounds; }
    }
    public double[] UpperBounds
    {
        get { return upperBounds; }
    }
    
    /// <summary>
    /// This constructor allows the user to gives the bounds in two separate arrays of equal length.
    /// </summary>
    /// <param name="objectiveFunction"></param>
    /// <param name="lowerBounds"></param>
    /// <param name="upperBounds"></param>
    /// <param name="tolerance"></param>
    /// <param name="optimizationType"></param>
    /// <param name="useHardwareEntropy"></param>
    /// <exception cref="ArgumentException"></exception>
    public ContinuousOptimizationProblem(ContinuousObjectiveFunction objectiveFunction, double[] lowerBounds, double[] upperBounds, double tolerance = 1e-6, string optimizationType = "min", bool useHardwareEntropy = false)
        :base(tolerance, useHardwareEntropy, optimizationType);
    {
        if (optimizationType.ToLower() != "min" && optimizationType.ToLower() != "max")
        {
            throw new ArgumentException("Optimization type must be either 'min' or 'max'");
        }
        this.objective = objectiveFunction;
        this.numberOfDimensions = lowerBounds.Length;

        this.lowerBounds = lowerBounds;
        this.upperBounds = upperBounds;
    }

    /// <summary>
    /// This constructor is used to create an optimization problem object with the minimal required information.    
    /// </summary>
    /// <param name="objectiveFunction">The objective function to be optimized.</param>
    /// <param name="useHardwareEntropy">A boolean indicating whether to use hardware-sourced entropy for random number generation.</param>
    /// <param name="bounds">Each tuple defines the compact interval over which the solution space should be searched.</param>
    /// <param name="optimizationType">The type of optimization problem, either "min" or "max".</param>
    /// <param name="tolerance">The tolerance for convergence.</param>
    public ContinuousOptimizationProblem(ContinuousObjectiveFunction objectiveFunction, ValueTuple<double, double>[] bounds, double tolerance = 1e-6, string optimizationType = "min", bool useHardwareEntropy = false) // this is called whenever a non-abstract derrved class is created
        : this(objectiveFunction, BoundsTupleSeperate(bounds,"lower"), BoundsTupleSeperate(bounds,"upper"), tolerance, optimizationType, useHardwareEntropy)
    {
        foreach (var bound in bounds)
        {
            if (bound.Item1 >= bound.Item2)
            {
                throw new ArgumentException("Each bound must be a tuple of the form (lowerBound, upperBound) where lowerBound < upperBound");
            }
        }
    }

    private static double[] BoundsTupleSeperate(ValueTuple<double, double>[] bounds, string boundType)
    {
        boundType = boundType.ToLower();
        if (boundType != "upper" && boundType != "lower")
        {
            throw new ArgumentException($"bound type of {boundType} is not a valid parameter");
        }
        int size = bounds.Length;
        double[] tempArray = new double[size];
        if (boundType == "lower")
        {
            for (int i = 0; i < size; i++)
            {
                tempArray[i] = bounds[i].Item1;
            }
        }
        else
        {
            for (int i = 0; i < size; i++)
            {
                tempArray[i] = bounds[i].Item2;
            }
        }
        return tempArray;

    }

    /// <summary>
    /// Returns a double in the range [0,1)
    /// </summary>
    /// <returns> double </returns>
    protected double RandDouble()
    {
        if (this.useHardwareEntropy) { return CustomRandom.GetHardwareRandomDouble(); }
        return Random.Shared.NextDouble(); // this is the faster option though less "random"
    }
    
}
/// <summary>
/// This class represents a solution to an optimization problem.
/// We don't derrive from OptimizationProblem to avoid a duplication of identicle information in memory.
/// It would be consice to have the informaion stored all in one place, however memory conservtion is also important.
/// </summary>
public class OptimizationSolution
{
    public readonly double optimalValue;
    public readonly double[] optimalPoint;
    public readonly string optimizationType;
    public readonly string? optimizationMethod = null;
    public readonly double tolerance;
    /// <summary>
    /// This constructor is used to create an optimization solution object with the minimal required information.
    /// </summary>
    /// <param name="optimalValue">The value of the objective function at the optimal point; this is a read only field of the class</param>
    /// <param name="optimalPoint">The point in the solution space that optimizes the objective function; this is a read only field of the class</param>
    /// <param name="optimizationType">The type of optimization problem (should take only "min" or "max"); this is a read only field of the class</param>
    /// <param name="optimizationMethod">The method used to solve the optimization problem; this is a read only field of the class</param>
    /// <param name="tolerance">The tolerance for convergence. This represents the expected accuracy of the solution.</param>
    public OptimizationSolution(double optimalValue, double[] optimalPoint, double tolerance, string optimizationType, string? optimizationMethod = null)
    {
        this.optimalValue = optimalValue;
        this.optimalPoint = optimalPoint;
        this.tolerance = tolerance;
        this.optimizationType = optimizationType;
        if (optimizationType.ToLower() != "min" && optimizationType.ToLower() != "max")
        {
            throw new ArgumentException("Optimization type must be either 'min' or 'max'");
        }
        this.optimizationMethod = optimizationMethod;
    }
}