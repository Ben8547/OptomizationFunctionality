using System;
using System.Security.Cryptography;

namespace OptimizationFunctionality;

internal static class CustomRandom
{
    public static double GetHardwareRandomDouble() // for getting random doubles with hardware sourced entropy. This function is AI generated as this is not the point of the present topic.
    {
        // Allocate 8 bytes on the stack for a 64-bit unsigned integer
        Span<byte> buffer = stackalloc byte[8];

        // Fill the buffer with cryptographically strong hardware-backed random bytes
        RandomNumberGenerator.Fill(buffer);

        // Convert the 8 bytes to a ulong
        ulong ul = BitConverter.ToUInt64(buffer);

        // Shift right to use 53 bits of precision (the mantissa size of an IEEE 754 double)
        return (ul >> 11) * (1.0 / (1ul << 53));
    }

    /// <summary>
    /// This function generates a quasi-random sequence of points in the unit hypercube [0,1]^n using the specified method.
    ///The default method is Sobol, but other methods can be implemented as needed.
    /// </summary>
    /// <param name="count">The number of points to return</param>
    /// <param name="method">The method to use for generating quasi-random points</param>
    /// <returns>An array of quasi-random points</returns>
    /// <exception cref="NotImplementedException"></exception>
    public static double[] GetQuasirandom(int count, string method = "halton")
    {
        if (method.ToLower() == "sobol")
        {
            return GenerateSobolSequence(count);
        }
        if (method.ToLower() == "halton")
        {
            return GenerateHaltonSequence(count);
        }
        else
        {
            throw new NotImplementedException($"Quasi-random method '{method}' is not implemented.");
        }
    }
    /// <summary>
    /// This function generates a Sobol sequence of points in the unit hypercube [0,1]^n.
    /// </summary>
    /// <param name="count">The number of points to generate</param>
    /// <returns>An array of Sobol sequence points</returns>
    private static double[] GenerateSobolSequence(int count)
    {
        return new double[count];
    }
    /// <summary>
    /// This function generates a Sobol sequence of points in the unit hypercube [0,1]^n.
    /// </summary>
    /// <param name="count">The number of points to generate</param>
    /// <returns>An array of Halton sequence points</returns>
    private static double[] GenerateHaltonSequence(int count)
    {
        return new double[count];
    }

    public static int[] SimpleXORShift(int count, long seed = 8475)
    {
        return new int[count];
    }

}
