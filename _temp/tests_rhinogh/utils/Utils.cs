using System;

namespace TestCSUtils
{
    public static class TestCS
    {
        public static void WriteLine(string message)
        {
            Console.WriteLine($"<< {message} >>");
        }
    }
}