using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;

using LS = Rhino.Runtime.Code.Languages.LanguageSpec;
using RX = Rhino.Runtime.Code.Registry.RegistryException;

void WithExcept<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T ex)
    {
        Console.WriteLine($"pass: {ex.Message}");
    }
}

void WithTry(string spec)
{
    if (LS.TryParse(spec, out LS ls))
    {
        Console.WriteLine(ls);
    }
    else
    {
        throw new Exception(spec);
    }
}

void WithCompare(LS spec1, LS spec2)
{
    if (spec1 > spec2)
    {
        Console.WriteLine("pass");
    }
    else
    {
        throw new Exception();
    }
}

Console.WriteLine(new LS("python"));
Console.WriteLine(new LS("*.python", "3"));
Console.WriteLine(new LS("*.python", "3.-1"));
Console.WriteLine(new LS("pythonnet.python", "3.9.*"));
Console.WriteLine(new LS("*.*.python", "3.*"));
Console.WriteLine(new LS("mcneel.pythonnet.python", "3.9.10"));
Console.WriteLine(new LS("python", "*"));

WithExcept<RX>(() => new LS("python", "3.*.10"));
WithExcept<RX>(() => new LS("dev.mcneel.pythonnet.python", "3.9.10"));
WithExcept<RX>(() => new LS("pythonnet.python_dev-3.9.*-devjames"));

WithTry("python");
WithTry("python3");
WithTry("python@3");
WithTry("python 3.*");
WithTry("python-3");
WithTry("python: 3.2");
WithTry("python-3_vray");
WithTry("pythonnet.python3.9.*_devjames");

WithCompare(new LS("mcneel.python"), new LS("python"));
WithCompare(new LS("python", "3"), new LS("python", "2"));
WithCompare(new LS("mcneel.python", "3.2"), new LS("python", "3"));
WithCompare(new LS("mcneel.python", "3.9.10"), new LS("python", "3.8.11"));
