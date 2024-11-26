using System;

namespace Test_Library_CompileGuard
{
  public class Test
  {
    public int TestLibrary()
    {
#if LIBRARY
      return 42;
#else
      return -1;
#endif
    }
  }
}
