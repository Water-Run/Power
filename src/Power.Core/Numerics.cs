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
    public int Size => _pivots.Length;

    public Factorization(double[,] matrix)
    {
        _lu = matrix;
        int n = matrix.GetLength(0);
        _pivots = new int[n];
        double[] scales = new double[n];
        for (int row = 0; row < n; ++row)
        {
            for (int col = 0; col < n; ++col)
            {
                double value = Math.Abs(matrix[row, col]);
                if (!Numeric.Finite(value)) Fail();
                scales[row] = Math.Max(scales[row], value);
            }
            if (scales[row] == 0) Fail();
        }
        for (int k = 0; k < n; ++k)
        {
            int pivot = k;
            for (int row = k + 1; row < n; ++row)
                if (Math.Abs(matrix[row, k]) / scales[row] > Math.Abs(matrix[pivot, k]) / scales[pivot]) pivot = row;
            if (Math.Abs(matrix[pivot, k]) / scales[pivot] < 64 * 2.2204460492503131e-16) Fail();
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
                if (!Numeric.Finite(matrix[row, k])) Fail();
                for (int col = k + 1; col < n; ++col)
                {
                    matrix[row, col] -= matrix[row, k] * matrix[k, col];
                    if (!Numeric.Finite(matrix[row, col])) Fail();
                }
            }
        }
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
