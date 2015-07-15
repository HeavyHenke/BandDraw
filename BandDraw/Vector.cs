using System;

namespace BandDraw
{
    struct Vector
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double Value => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector Add(Vector other)
        {
            return new Vector(X + other.X, Y + other.Y, Z + other.Z);
        }

        public Vector Add(double x, double y, double z)
        {
            return new Vector(X + x, Y + y, Z + z);
        }

        public Vector Multiply(double val)
        {
            return new Vector(X * val, Y * val, Z * val);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public Vector Max(Vector gyroReading)
        {
            return new Vector(Math.Max(X, gyroReading.X), Math.Max(Y, gyroReading.Y), Math.Max(Z, gyroReading.Z));
        }

        public Vector Min(Vector gyroReading)
        {
            return new Vector(Math.Min(X, gyroReading.X), Math.Min(Y, gyroReading.Y), Math.Min(Z, gyroReading.Z));
        }
    }
}