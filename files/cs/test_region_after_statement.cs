// https://mcneel.myjetbrains.com/youtrack/issue/RH-99698
using System;

result = Helper.Answer() == 42;

#region Helpers
static class Helper
{
    public static int Answer() => 42;
}
#endregion
