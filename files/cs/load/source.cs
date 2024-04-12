// #! csharp
#r "nuget: MathNet.Numerics, 5.0.0"

using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

public static class Test
{
    public static int Foo() => 42;

    public static double Solve()
    {
        var m = Matrix<double>.Build.Random(500, 500);
        var v = Vector<double>.Build.Random(500);
        var y = m.Solve(v);

        return y[0];
    }
}