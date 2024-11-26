using System;

namespace Test_Library_CompileGuard_Not
{
  public class Test
  {
    public int TestNotLibrary()
    {
#if LIBRARY
      return -1;
#else
      return 42;
#endif
    }
  }
}
