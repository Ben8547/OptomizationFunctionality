***Work in progress***

# OptimizationFunctionality

A C# framework for experimenting with continuous optimization algorithms using a common `OptimizationProblem` abstraction.

The project currently focuses on **Particle Swarm Optimization (PSO)**, with utilities for optimization test functions, random-number generation, and visualization.

## Features

* Common `OptimizationProblem` class for defining optimization problems
* Support for both minimization and maximization
* Arbitrary-dimensional continuous search spaces
* Configurable search-space bounds
* Configurable convergence tolerance
* Particle Swarm Optimization
* Random and quasi-random particle initialization
* Optional hardware-sourced entropy
* Standard optimization test functions
* Streaming of PSO optimization states using `System.Threading.Channels`
* Two-dimensional PSO visualization
* FFmpeg-based MP4 generation

## Project Structure

```text
OptimizationFunctionality/
│
├── OptimizationSetup.cs
│   └── OptimizationProblem and OptimizationSolution
│
├── PointSwarmOptimizer.cs
│   └── Particle Swarm Optimization implementation
│
├── TestFunctions.cs
│   └── Optimization benchmark functions
│
├── RandomUtils.cs
│   └── Random-number utilities
│
├── AnimationUtilities.cs
│   └── PSO visualization and FFmpeg integration
│
├── Program.cs
│   └── Example entry point
│
├── PSO_Anim.mp4
│   └── Example PSO animation
│
└── OptomizationFunctionality.csproj
    └── Project configuration
```

## Defining an Optimization Problem

Optimization problems are represented by `OptimizationProblem`.

An objective function is represented by the `ObjectiveFunction` delegate:

```csharp
double f(double[] x)
```

For example, the following defines the two-dimensional sphere function:

```csharp
using OptimizationFunctionality;

ObjectiveFunction objectiveFunction = (double[] x) =>
    x[0] * x[0] + x[1] * x[1];

ValueTuple<double, double>[] bounds =
{
    (-5.0, 5.0),
    (-5.0, 5.0)
};

OptimizationProblem problem = new OptimizationProblem(
    objectiveFunction,
    bounds,
    tolerance: 1e-6,
    optimizationType: "min"
);
```

The tuple-based constructor accepts one `(lowerBound, upperBound)` tuple for each dimension. The optimization type must be either `"min"` or `"max"`.

The example above defines:

$$
\min_{\mathbf{x}\in[-5,5]^2} x_1^2+x_2^2
$$

whose global minimum is at `(0, 0)`.

## Using Benchmark Functions

The repository provides test functions through the `TestFunction` abstraction.

For example, the Rosenbrock function can be used as follows:

```csharp
using OptimizationFunctionality;
using OptimizationFunctionality.ContinuousOptimization.TestFunctions;

TestFunction testFunction = new Rosenbrock();

ObjectiveFunction objectiveFunction =
    (double[] x) => testFunction.Evaluate(x);

ValueTuple<double, double>[] bounds =
{
    (-10.0, 10.0),
    (-10.0, 10.0)
};

OptimizationProblem problem = new OptimizationProblem(
    objectiveFunction,
    bounds,
    tolerance: 1e-10,
    optimizationType: "min"
);
```

This follows the same pattern used by the repository's `Program.cs`: instantiate the test function, wrap its `Evaluate` method in an `ObjectiveFunction`, and then construct an `OptimizationProblem`.

### Rosenbrock

The two-dimensional Rosenbrock function is commonly written as

$$
f(x,y)=(1-x)^2+100(y-x^2)^2.
$$

Its global minimum is at

$$
(x,y)=(1,1)
$$

with

$$
f(1,1)=0.
$$

### Rastrigin

The Rastrigin function is another common benchmark for continuous optimization. It is particularly useful for testing an optimizer's ability to search a multimodal objective landscape.

```csharp
TestFunction testFunction = new Rastrigin();

ObjectiveFunction objectiveFunction =
    (double[] x) => testFunction.Evaluate(x);
```

## Particle Swarm Optimization

The main optimization algorithm currently implemented is **Particle Swarm Optimization**.

The implementation is contained in:

```text
PointSwarmOptimizer.cs
```

The optimizer accepts an `OptimizationProblem` and can either generate an initial swarm automatically or accept an explicitly supplied initial state. The generated-swarm constructor is:

```csharp
ParticleSwarmOptimizer(
    in OptimizationProblem optimizationProblem,
    int numberOfPoints,
    double stepSize = 0.1,
    bool useQuasirandom = true
)
```

There is also a simpler constructor that uses 100 particles by default:

```csharp
ParticleSwarmOptimizer optimizer =
    new ParticleSwarmOptimizer(problem);
```

### Basic PSO Example

```csharp
using OptimizationFunctionality;
using OptimizationFunctionality.ContinuousOptimization;

ObjectiveFunction objectiveFunction = (double[] x) =>
    x[0] * x[0] + x[1] * x[1];

ValueTuple<double, double>[] bounds =
{
    (-5.0, 5.0),
    (-5.0, 5.0)
};

OptimizationProblem problem = new OptimizationProblem(
    objectiveFunction,
    bounds,
    tolerance: 1e-6,
    optimizationType: "min"
);

ParticleSwarmOptimizer optimizer =
    new ParticleSwarmOptimizer(
        problem,
        numberOfPoints: 100,
        useQuasirandom: false
    );

OptimizationSolution solution = optimizer.Optimize();

Console.WriteLine($"Optimal value: {solution.optimalValue}");
Console.WriteLine(
    $"Optimal point: ({string.Join(", ", solution.optimalPoint)})"
);
```

`Optimize()` returns an `OptimizationSolution` containing the estimated optimal value and the point at which it was found.

## Random and Quasi-Random Initialization

When a swarm is created without explicitly supplying an initial state, the optimizer initializes particle positions inside the problem's bounds.

The `useQuasirandom` parameter controls how the initial positions are generated:

```csharp
ParticleSwarmOptimizer optimizer =
    new ParticleSwarmOptimizer(
        problem,
        numberOfPoints: 100,
        useQuasirandom: true
    );
```

With `useQuasirandom: false`, the optimizer uses ordinary random initialization.

The default for the full constructor is `true`.

## Custom Initial Swarm

An initial swarm can also be supplied directly.

Each row of the `double[][]` represents one particle, and each column represents one dimension:

```csharp
double[][] initialState =
{
    new double[] { -2.0, -1.0 },
    new double[] {  0.0,  2.0 },
    new double[] {  1.0, -3.0 },
    new double[] {  3.0,  1.0 }
};

ParticleSwarmOptimizer optimizer =
    new ParticleSwarmOptimizer(
        problem,
        initialState,
        stepSize: 0.1
);
```

The number of dimensions in each particle must match the dimensionality of the `OptimizationProblem`.

## Minimization and Maximization

`OptimizationProblem` supports both minimization and maximization:

```csharp
OptimizationProblem minimizationProblem =
    new OptimizationProblem(
        objectiveFunction,
        bounds,
        optimizationType: "min"
    );
```

or:

```csharp
OptimizationProblem maximizationProblem =
    new OptimizationProblem(
        objectiveFunction,
        bounds,
        optimizationType: "max"
    );
```

The PSO implementation internally converts minimization problems into the maximization form used by its optimization logic and returns the result in the original optimization convention.

## Visualization

The repository includes utilities for visualizing PSO in a **two-dimensional** solution space.

The visualization entry point is:

```csharp
AnimationPSO.AnimatePSOwith2DSolutionSpace(...)
```

Its current signature is:

```csharp
public static async Task AnimatePSOwith2DSolutionSpace(
    OptimizationProblem problem,
    int numPoints = 100,
    bool useQuasirandom = false,
    int frameRate = 30,
    string outFilePath = "./PSO_Animation.mp4",
    double stepSize = 0.1
)
```

For example:

```csharp
using OptimizationFunctionality;
using OptimizationFunctionality.ContinuousOptimization;
using OptimizationFunctionality.ContinuousOptimization.AnmimationUtilities;
using OptimizationFunctionality.ContinuousOptimization.TestFunctions;

TestFunction testFunction = new Rosenbrock();

ObjectiveFunction objectiveFunction =
    (double[] x) => testFunction.Evaluate(x);

ValueTuple<double, double>[] bounds =
{
    (-10.0, 10.0),
    (-10.0, 10.0)
};

OptimizationProblem problem = new OptimizationProblem(
    objectiveFunction,
    bounds,
    tolerance: 1e-10,
    optimizationType: "min"
);

await AnimationPSO.AnimatePSOwith2DSolutionSpace(
    problem,
    numPoints: 100,
    useQuasirandom: false,
    frameRate: 30,
    outFilePath: "./PSO_Anim.mp4",
    stepSize: 1e-3
);
```

The animation method requires a two-dimensional optimization problem. It throws an `ArgumentException` if the problem has any other dimensionality.

## How Visualization Works

The PSO animation pipeline streams optimization states through a bounded `Channel<StreamPackagePSO>`.

Conceptually:

```text
ParticleSwarmOptimizer
        │
        │ optimization states
        ▼
Channel<StreamPackagePSO>
        │
        ▼
ScottPlot frame generation
        │
        │ PNG bytes
        ▼
FFmpeg
        │
        ▼
PSO_Anim.mp4
```

The optimizer and animation renderer run concurrently. Each optimization state is converted into a plot frame and passed directly to FFmpeg rather than requiring every frame to be stored first.

## FFmpeg Requirement

The animation utility launches an `ffmpeg` process directly:

```csharp
ffmpeg.StartInfo.FileName = "ffmpeg";
```

Therefore, FFmpeg must be installed and available through the system's executable search path when using the animation functionality.

The generated video uses PNG frames piped into FFmpeg and encoded as H.264 with the `yuv420p` pixel format.

## Running the Project

Clone the repository:

```bash
git clone https://github.com/Ben8547/OptomizationFunctionality.git
cd OptomizationFunctionality
```

Build the project:

```bash
dotnet build
```

Run it:

```bash
dotnet run
```

The repository currently targets `.NET 10`, and the checked-in `Program.cs` demonstrates a Rosenbrock PSO animation.

## Current Example

The repository's `Program.cs` currently creates a two-dimensional Rosenbrock optimization problem:

```csharp
TestFunction testFunction = new Rosenbrock();

ObjectiveFunction objectiveFunction =
    (double[] x) => testFunction.Evaluate(x);

ValueTuple<double, double>[] bounds =
{
    (-10.0, 10.0),
    (-10.0, 10.0)
};

OptimizationProblem optimizationProblem =
    new OptimizationProblem(
        objectiveFunction,
        bounds,
        tolerance: 1e-10,
        optimizationType: "min",
        useHardwareEntropy: false
    );
```

It then passes that problem to the animation utility.

## Dependencies

The project uses several NuGet packages for numerical computation and visualization, including:

* ScottPlot
* FFMpegCore
* MathNet.Numerics
* Microsoft.Extensions.Configuration.UserSecrets

FFmpeg is additionally required by the animation pipeline itself.

## Design

The project separates the definition of an optimization problem from the algorithm used to solve it.

```text
OptimizationProblem
        │
        ├── Objective Function
        ├── Search Bounds
        ├── Optimization Type
        ├── Convergence Tolerance
        └── Randomness Configuration
                │
                ▼
      Optimization Algorithm
                │
                └── ParticleSwarmOptimizer
```

This allows optimization algorithms to operate against a common problem representation rather than embedding objective-function-specific logic into the optimizer.

## Current Status

The repository is primarily an experimental framework for continuous optimization.

The main implemented optimizer is Particle Swarm Optimization, supported by benchmark functions, configurable initialization, random-number utilities, and two-dimensional visualization.

Potential future additions include:

* Additional optimization algorithms
* Additional benchmark functions
* More optimization diagnostics
* Improved visualization
* Automated benchmarking
* Additional tests
* Performance improvements

## License

No license is currently specified for the repository.

If the project is intended for public reuse, consider adding an open-source license to clearly define how others may use, modify, and redistribute the code.
