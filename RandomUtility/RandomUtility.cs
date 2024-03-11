
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;

public class GraveRandom
{
    private Random random;
    
    public GraveRandom(int seed)
    {
        random = new Random(seed);
    }

      public int Next(int minValue, int maxValue)
    {
        return random.Next(minValue, maxValue);
    }

    public float NextFloat()
    {
        return (float)random.NextDouble();
    }

    // Method to generate a random float within a specified range
    public float NextFloat(float minValue, float maxValue)
    {
        return (float)random.NextDouble() * (maxValue - minValue) + minValue;
    }

    // Method to generate a random boolean with a specified probability of being true
    public bool NextBoolean(float probabilityOfTrue = 0.5f)
    {
        return NextFloat() < probabilityOfTrue;
    }

    // Method to retrieve a random integer across the full int range
    // Corrected method to retrieve a random integer across the full int range
    public int NextFullRangeInt()
    {
        // Generate the lower 31 bits
        int lowerBits = random.Next(int.MinValue, int.MaxValue);

        // Generate the sign bit
        int signBit = random.Next(0, 2) << 31; // Shift left to make it the MSB

        // Combine the two parts, taking advantage of bitwise OR to include the sign bit
        return lowerBits | signBit;
    }
}
    


public static class RandomUtility
{
    // Converts a string to a seed and initializes UnityEngine.Random with this seed.

    public static GraveRandom random;
    
    // Method to initialize the GraveRandom instance with a consistent seed across platforms
    public static void InitializeRandomWithSeed(string seedString)
    {
        int seed = ComputeConsistentSeed(seedString);
        random = new GraveRandom(seed); // Initialize the GraveRandom instance with the consistent seed
    }

     // Helper method to compute a consistent seed from a string input
    private static int ComputeConsistentSeed(string input)
    {
        // Use UTF8 Encoding for consistent byte representation across platforms
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // Convert the first 4 bytes of the hash into an integer to use as a seed
            int seed = BitConverter.ToInt32(hashBytes, 0);

            // Ensure the seed is positive
            return Math.Abs(seed);
        }
    }

// Method to roll a dice and choose a random value between min and max
    public static int ChooseRandomValue(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentException("minValue should be less than or equal to maxValue");
        }

        // Ensure to add 1 to maxValue because Random.Next upper bound is exclusive
        return random.Next(minValue, maxValue + 1);
    }




}