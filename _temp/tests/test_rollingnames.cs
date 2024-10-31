using System;

var m = new RollingNames("xyzuvwst");
for (int i = 0; i < 100; i++) {
    string next = m.Next();
    Console.WriteLine($"{next} (Current: {m.ConvertFromBase(next)}");
}


public class RollingNames
{
    readonly char[] _alphabet;
    readonly int _radix;

    public int Current { get; set; } = 0;

    public RollingNames(string alphabet) : this(alphabet.ToCharArray()) { }
    public RollingNames(char[] alphabet)
    {
        _alphabet = alphabet;
        _radix = _alphabet.Length;
    }

    //https://stackoverflow.com/q/34574203/2350244
    public string ConvertBase(int value)
    {
        string result = string.Empty;

        if (value < _radix)
            result = _alphabet[value].ToString();
        else
        {
            long index;
            while (value != 0)
            {
                index = value % _radix;
                value = Convert.ToInt32(Math.Floor(value / (float)_radix));
                result += string.Concat(_alphabet[index].ToString());
            }
        }

        return result;
    }

    public int ConvertFromBase(string repr)
    {
        int result = 0;
        int mult = 1;
        foreach (char c in repr.ToCharArray())
        {
            int charValue = 0;
            foreach (char ac in _alphabet)
            {
                if (ac == c)
                    break;
                charValue++;
            }
            result += mult * charValue;
            mult *= _radix;
        }

        return result;
    }


    public string Next()
    {
        string result = ConvertBase(Current);

        unchecked
        {
            Current++;
        }

        return result;
    }
}