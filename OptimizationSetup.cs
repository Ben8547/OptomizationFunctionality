using System;

namespace OptimizationFunctionality;

///<summary>
/// this defines an objection function from R^n to R
/// </summary>
public delegate double ObjectiveFunction(double[] x);

/// <summary>
/// This class stores all of the data required to perform a given optomization problem.
/// From this class it should be possible to perform and optimization technique.
/// Each Technique will be derrived from this class so that class contains only the functionality common to each technique.
/// </summary>
public class OptimizationProblem // not sealed incase a particular optimization technique needs to derrive from this class to implement its own functionality
{
    public readonly bool useHardwareEntropy;
    public readonly double tolerance; // the tolerance for convergence
    public readonly int numberOfDimensions; // the dimension of the solution space
    private readonly ObjectiveFunction objective; // this allows us to privately mutate the objective to convert minimization problems into maximization ones
    public ObjectiveFunction objectiveFunction { get { return objective; } } // the property pointing to the objective function to be optimized
    public readonly ValueTuple<double, double>[] bounds; // the bounds for each dimension
    public readonly string optimizationType; // the type of optimization problem, either "min" or "max". This is a read only field of the class. The default is "min".
    /// <summary>
    /// This constructor is used to create an optimization problem object with the minimal required information.    
    /// </summary>
    /// <param name="objectiveFunction">The objective function to be optimized.</param>
    /// <param name="useHardwareEntropy">A boolean indicating whether to use hardware-sourced entropy for random number generation.</param>
    /// <param name="bounds">Each tuple defines the compact interval over which the solution space should be searched.</param>
    /// <param name="optimizationType">The type of optimization problem, either "min" or "max".</param>
    /// <param name="tolerance">The tolerance for convergence.</param>
    public OptimizationProblem(ObjectiveFunction objectiveFunction, ValueTuple<double, double>[] bounds, double tolerance = 1e-6, string optimizationType = "min", bool useHardwareEntropy = false) // this is called whenever a non-abstract derrved class is created
    {
        if (optimizationType.ToLower() != "min" && optimizationType.ToLower() != "max")
        {
            throw new ArgumentException("Optimization type must be either 'min' or 'max'");
        }
        this.objective = objectiveFunction;
        this.useHardwareEntropy = useHardwareEntropy;
        this.bounds = bounds;
        foreach(var bound in bounds)
        {
            if (bound.Item1 >= bound.Item2)
            {
                throw new ArgumentException("Each bound must be a tuple of the form (lowerBound, upperBound) where lowerBound < upperBound");
            }
        }
        this.numberOfDimensions = bounds.Length;
        this.tolerance = tolerance;
        this.optimizationType = optimizationType;
    }

    public virtual void Animate() { } // This will animate the optimization process if the objective function is R^2 -> R or R -> R.

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

public class HistoricalOptimizationSolution : OptimizationSolution
{
    readonly double[] historicalValues;
    readonly double[] historicalPoints;
    /// <summary>
    /// This constructor is used to create an optimization solution object with the minimal required information.
    /// </summary>
    /// <param name="optimalValue">The value of the objective function at the optimal point; this is a read only field of the class</param>
    /// <param name="optimalPoint">The point in the solution space that optimizes the objective function; this is a read only field of the class</param>
    /// <param name="optimizationType">The type of optimization problem (should take only "min" or "max"); this is a read only field of the class</param>
    /// <param name="optimizationMethod">The method used to solve the optimization problem; this is a read only field of the class</param>
    /// <param name="historicalValues">An array of historical values of the objective function during the optimization process; this is a read only field of the class.
    /// This array is 1D, but may represent a flattened 2D array depending on the type of optimization method. Additional functionalities should use the optimizationMethod field when treating this parameter.</param>
    /// <param name="historicalPoints">An array of historical points in the solution space during the optimization process; this is a read only field of the class
    /// Note that although this is a 1D array, this is only because it is flattened to allow discrepencies in dimension between techniques as needed.
    /// Additional functionalities may use the optimizationType field to determine how to treat this array.</param>
    /// <note>When flattening the aforementioned arrays, to ensure consistency within this functionality, please ensure that all entires are listed so as to minimize in order, the index of dimension 0, then dimension 1 , then...,then dimension n,.</note>
    HistoricalOptimizationSolution(double optimalValue, double[] optimalPoint, double[] historicalValues, double[] historicalPoints,double tolerance = 1e-6, string optimizationType = "min", string? optimizationMethod = null)
        : base(optimalValue, optimalPoint,tolerance, optimizationType, optimizationMethod)
    {
        this.historicalValues = historicalValues;
        this.historicalPoints = historicalPoints;
    }
}
