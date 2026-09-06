using System.Threading.Channels;
using ScottPlot; // To facilitate plotting
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;// to run ffmpeg

namespace OptimizationFunctionality
{
    namespace ContinuousOptimization
    {
        namespace AnmimationUtilities // some utility functions to animate certain types of continuous optimization problems when the dimensions are ammenable
        {
            /// <summary>
            /// Contains some utilites for visualizing the time evolution for low dimensional PSO tasks
            /// </summary>
            public static class AnimationPSO
            {
                /// <summary>
                /// This function intakes an optimiztion problem with continuous objective function from R^2 -> R.
                /// It solves the optimization problem, piping the particle data generated at each step into a channel.
                /// This data is then converted into frames visualizing the movement of prticles through solution space.
                /// The global optimum at each phase is also recorded.
                /// </summary>
                /// <param name="problem"></param>
                /// <param name="numPoints"></param>
                /// <param name="useQuasirandom"></param>
                /// <param name="frameRate"></param>
                /// <param name="outFilePath"></param>
                /// <param name="stepSize"></param>
                /// <returns></returns>
                /// <exception cref="ArgumentException"></exception>
                public static async Task AnimatePSOwith2DSolutionSpace(ContinuousOptimizationProblem problem, int numPoints = 100, bool useQuasirandom = false, int frameRate = 30,string outFilePath="./PSO_Animation.mp4", double stepSize=0.1)
                {
                    ParticleSwarmOptimizer optimizer = new ParticleSwarmOptimizer(problem, numberOfPoints: numPoints, useQuasirandom: useQuasirandom, stepSize: stepSize);
                    
                    Channel<StreamPackagePSO> channel = Channel.CreateBounded<StreamPackagePSO>(100);

                    double[] FrameLBounds = problem.LowerBounds;
                    double[] FrameUBounds = problem.UpperBounds; // these are the physical bounds of the scatter plot
                    if (FrameLBounds.Length != 2 || FrameUBounds.Length != 2)
                    {
                        throw new ArgumentException("Dimension of the solution space must be 2 to use this method.");
                    }
                    string? problemType = problem.optimizationType == "min"
                        ? "minimum"
                        : "maximum";


                    // Kick off the paralell process
                    Task optimize = optimizer.StreamOptimization(channel);
                    Task animate = CreateAnimationPSO(channel, FrameLBounds, FrameUBounds, outFilePath, frameRate, problemType);

                    await Task.WhenAll(optimize, animate);

                }
                static async Task CreateAnimationPSO(Channel<StreamPackagePSO> channel, double[] frameLB, double[] frameUB,string outputFilePath, int frameRate=30, string? problemType=null)
                {

                    //int framenumber = 0; // debug

                    Process ffmpeg = StartFFmpegProcess(outputFilePath, frameRate);

                    await foreach(StreamPackagePSO packet  in channel.Reader.ReadAllAsync())
                    {
                        //framenumber++; //debug
                        //Console.WriteLine($"Rendering frame {framenumber}"); //debug

                        byte[] frame = RenderFramePSO2D(packet, frameLB, frameUB, problemType);

                        //Console.WriteLine($"Frame {framenumber}: {frame.Length} bytes"); //devug

                        // pump the frame into ffmpeg
                        await ffmpeg.StandardInput.BaseStream.WriteAsync(frame);
                    }
                    //Console.WriteLine($"Total frames rendered: {framenumber}"); //debug

                    ffmpeg.StandardInput.Close();
                    await ffmpeg.WaitForExitAsync();
                }
                static Process StartFFmpegProcess(string fileOutputPath, int frameRate = 30)
                {
                    Process ffmpeg = new Process();

                    ffmpeg.StartInfo.FileName = "ffmpeg";

                    ffmpeg.StartInfo.Arguments =
                        $"-f image2pipe " + // feeding image files into the process
                        $"-vcodec png " + // this tells ffmpeg what the file format is. Without this ffmpeg treats the entire stream of byte arrays as a single file
                        $"-framerate {frameRate} " + // frame rate of the animation
                        $"-i pipe:0 " + // to mt current understanding this is the channel though which we are assinged to acces ffmpeg through
                        $"-c:v libx264 " + // to my current understanding this is the encoder that ffmpeg is using to generate the mp4
                        $"-pix_fmt yuv420p " + // I don't know what this is
                        $"\"{fileOutputPath}\""; // output file path

                    // some settings
                    ffmpeg.StartInfo.UseShellExecute = false; //do not launch through windows shell
                    ffmpeg.StartInfo.RedirectStandardInput = true; // Give C# access to the pipe
                    ffmpeg.StartInfo.RedirectStandardError = false; // I don't want to deal with the error data

                    ffmpeg.Start();

                    return ffmpeg;
                }
                static byte[] RenderFramePSO2D(StreamPackagePSO package, double[] frameLB, double[] frameUB, string? problemType=null)
                {
                    // extract the particle coordinates from the packet in the channel
                    double[] x = package.states // x coordinates in solution space
                        .Where(row => row != null)
                        .Select(row => row[0])
                        .ToArray();
                    double[] y = package.states // y coordinates in solution space
                        .Where(row => row != null)
                        .Select(row => row[1])
                        .ToArray();
                    double optimalValue = package.correctedGlobalBestValue;
                    double[] optimalPoint = package.globalBestPoint;

                    // create the current frame
                    Plot currentFrame = new();
                    var scatter = currentFrame.Add.Scatter(x, y);
                    currentFrame.Axes.SetLimits(frameLB[0], frameUB[0], frameLB[1], frameUB[1]);
                    scatter.LineWidth = 0; // remove connecting lines from the plot
                    scatter.MarkerColor = ScottPlot.Colors.Blue;
                    scatter.MarkerSize = 5;
                    // add the global value
                    string message = problemType != null
                        ? problemType
                        : "optimum";
                    
                    var text = currentFrame.Add.Text($"Global {message} is estimated to be at ({optimalPoint[0]},{optimalPoint[1]}) and has value {optimalValue}", x: -10, y: 10);
                    text.LabelFontSize = 26;
                    text.LabelBold = true;
                    // add a point representing the global optimal point
                    var globalPointScatter = currentFrame.Add.Scatter(optimalPoint[0], optimalPoint[1]);
                    globalPointScatter.MarkerColor = ScottPlot.Colors.Red;
                    globalPointScatter.MarkerSize = 15;

                    // convert the plot to a byte array
                    byte[] frame = currentFrame.GetImage(1920, 1080).GetImageBytes();
                    return frame;
                }
            }
        }
    }
}