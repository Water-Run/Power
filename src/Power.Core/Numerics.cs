// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal static class Numeric
{
    public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    public static void Accumulate(double value, ref double sum, ref double correction)
    {
        double adjusted = value - correction;
        double next = sum + adjusted;
        correction = (next - sum) - adjusted;
        sum = next;
    }
    // Series near zero avoids cancellation in exp(x)-1 and log(1+x) for small arguments.
    public static double Expm1(double x) => Math.Abs(x) < 1e-4 ? x * (1 + x * (0.5 + x * (1.0 / 6 + x * (1.0 / 24 + x / 120)))) : Math.Exp(x) - 1;
    public static double Log1p(double x) => Math.Abs(x) < 1e-4 ? x * (1 + x * (-0.5 + x * (1.0 / 3 + x * (-0.25 + x / 5)))) : Math.Log(1 + x);
    public static ulong Hash(ulong hash, ulong value)
    {
        unchecked
        {
            for (int i = 0; i < 8; ++i) { hash = (hash ^ (value & 255)) * 1099511628211; value >>= 8; }
            return hash;
        }
    }
    public static ulong Hash(ulong hash, double value) =>
        Hash(hash, unchecked((ulong)BitConverter.DoubleToInt64Bits(value == 0 ? 0 : value)));
}

internal sealed class Factorization
{
    private readonly double[,] _lu;
    private readonly int[] _pivots;
    private readonly double[] _scales;
    public int Size => _pivots.Length;

    public Factorization(double[,] matrix)
    {
        _lu = matrix;
        int n = matrix.GetLength(0);
        _pivots = new int[n];
        _scales = new double[n];
        if (!Factor()) Fail();
    }

    // Mutable factors are owned exclusively by a Simulation, never the shared compiled model.
    internal Factorization(int size)
    {
        _lu = new double[size, size]; _pivots = new int[size]; _scales = new double[size];
    }

    internal bool Refactor(double[,] matrix)
    {
        Array.Copy(matrix, _lu, matrix.Length);
        return Factor();
    }

    private bool Factor()
    {
        int n = Size; var matrix = _lu; var scales = _scales;
        Array.Clear(scales, 0, n);
        for (int row = 0; row < n; ++row)
        {
            for (int col = 0; col < n; ++col)
            {
                double value = Math.Abs(matrix[row, col]);
                if (!Numeric.Finite(value)) return false;
                scales[row] = Math.Max(scales[row], value);
            }
            if (scales[row] == 0) return false;
        }
        for (int k = 0; k < n; ++k)
        {
            int pivot = k;
            for (int row = k + 1; row < n; ++row)
                if (Math.Abs(matrix[row, k]) / scales[row] > Math.Abs(matrix[pivot, k]) / scales[pivot]) pivot = row;
            if (Math.Abs(matrix[pivot, k]) / scales[pivot] < 64 * 2.2204460492503131e-16) return false;
            _pivots[k] = pivot;
            if (pivot != k)
            {
                (scales[k], scales[pivot]) = (scales[pivot], scales[k]);
                for (int col = 0; col < n; ++col)
                    (matrix[k, col], matrix[pivot, col]) = (matrix[pivot, col], matrix[k, col]);
            }
            for (int row = k + 1; row < n; ++row)
            {
                matrix[row, k] /= matrix[k, k];
                if (!Numeric.Finite(matrix[row, k])) return false;
                for (int col = k + 1; col < n; ++col)
                {
                    matrix[row, col] -= matrix[row, k] * matrix[k, col];
                    if (!Numeric.Finite(matrix[row, col])) return false;
                }
            }
        }
        return true;
    }

    private static void Fail() => throw new ModelCompileException(DiagnosticCode.Solver, 0,
        "solver", "Nonfinite or singular/ill-conditioned discrete system.");

    public bool Solve(Span<double> rhs)
    {
        for (int k = 0; k < Size; ++k) (rhs[k], rhs[_pivots[k]]) = (rhs[_pivots[k]], rhs[k]);
        for (int row = 0; row < Size; ++row)
            for (int col = 0; col < row; ++col) rhs[row] -= _lu[row, col] * rhs[col];
        for (int row = Size - 1; row >= 0; --row)
        {
            for (int col = row + 1; col < Size; ++col) rhs[row] -= _lu[row, col] * rhs[col];
            rhs[row] /= _lu[row, row];
            if (!Numeric.Finite(rhs[row])) return false;
        }
        return true;
    }
}
