// #! csharp
using System;
using System.Globalization;

var culture = new CultureInfo("en-US");

bool res = true;

double number = 12345.6789;
res &= number.ToString(CultureInfo.InvariantCulture) == number.ToString(culture);

DateTime now = DateTime.Now;
// '12/10/2024 15:07:50' != '12/10/2024 3:07:50 PM'
res &= now.ToString(CultureInfo.InvariantCulture) != now.ToString(culture);

decimal money = 1234.56m;
res &= money.ToString(CultureInfo.InvariantCulture) == money.ToString(culture);

string str1 = "encyclopedia";
string str2 = "Encyclopedia";
res &= string.Compare(str1, str2, false, CultureInfo.InvariantCulture) == string.Compare(str1, str2, false, culture);


string[] words1 = { "apple", "Banana", "cherry" };
Array.Sort(words1, StringComparer.Create(CultureInfo.InvariantCulture, false));
res &= words1[0] == "apple";

string[] words2 = { "apple", "Banana", "cherry" };
Array.Sort(words2, StringComparer.Create(culture, false));
res &= words2[0] == "apple";


// Console.WriteLine(res);
result = res;