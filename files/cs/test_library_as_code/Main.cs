#r "nuget: MathNet.Numerics, 5.0.0"

using System;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

using TestAssembly.Math.Types;

#if !LIBRARY
var m = new TestAssembly.Math.DoMath();
_add_ = m.Add(21, 21);
_solve_ = m.Solve();
#endif

namespace TestAssembly.Math
{
  public sealed class DoMath
  {
    public int Add(int x, int y)
    {
#if DEBUG
        return 500;
#else
        return x + y + 10;
#endif
    }

    public double Solve() 
    {
        return 42;
    }
  }
}
