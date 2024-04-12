using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;

using LS = Rhino.Runtime.Code.Languages.LanguageSpec;
using RX = Rhino.Runtime.Code.Registry.RegistryException;

bool WithExcept<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T ex)
    {
        return true;
    }

    return false;
}

bool WithTry(string spec)
{
    if (LS.TryParse(spec, out LS ls))
    {
        // Console.WriteLine(ls);
        return true;
    }

    return false;
}

bool WithCompare(LS spec1, LS spec2)
{
    if (spec1 > spec2)
    {
        // Console.WriteLine("pass");
        return true;
    }

    return false;
}

// Console.WriteLine(new LS("python"));
// Console.WriteLine(new LS("*.python", "3"));
// Console.WriteLine(new LS("*.python", "3.-1"));
// Console.WriteLine(new LS("pythonnet.python", "3.9.*"));
// Console.WriteLine(new LS("*.*.python", "3.*"));
// Console.WriteLine(new LS("mcneel.pythonnet.python", "3.9.10"));
// Console.WriteLine(new LS("python", "*"));

bool test = true;

test &= WithExcept<RX>(() => new LS("python", "3.*.10"));
test &= WithExcept<RX>(() => new LS("dev.mcneel.pythonnet.python", "3.9.10"));
test &= WithExcept<RX>(() => new LS("pythonnet.python_dev-3.9.*-devjames"));

test &= WithTry("python");
test &= WithTry("python3");
test &= WithTry("python@3");
test &= WithTry("python 3.*");
test &= WithTry("python-3");
test &= WithTry("python: 3.2");
test &= WithTry("python-3_vray");
test &= WithTry("pythonnet.python3.9.*_devjames");

test &= WithCompare(new LS("mcneel.python"), new LS("python"));
test &= WithCompare(new LS("python", "3"), new LS("python", "2"));
test &= WithCompare(new LS("mcneel.python", "3.2"), new LS("python", "3"));
test &= WithCompare(new LS("mcneel.python", "3.9.10"), new LS("python", "3.8.11"));


result = test;