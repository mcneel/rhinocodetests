using System;
using System.Threading;
using System.Globalization;


printCulture(Thread.CurrentThread.CurrentCulture);
// printCulture(Thread.CurrentThread.CurrentUICulture);

void printCulture(CultureInfo c)
{
    Console.WriteLine(c.Name);
    Console.WriteLine(c.DisplayName);
    Console.WriteLine(c.EnglishName);
    Console.WriteLine(c.NativeName);
    Console.WriteLine(c.LCID);
}
