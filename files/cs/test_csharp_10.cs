// #! csharp
using System;

// CONSTANT INTERPOLATED STRINGS ────────────────────────
// In C# 10 you can use interpolated strings in const expressions
// as long as all the "holes" are also const.
#pragma warning disable format
const string AppName    = "CSharp10 Demo";
const string AppVersion = "1.0.0";
const string Banner     = $"{AppName} v{AppVersion}";   // ← new in C# 10
#pragma warning restore format

Console.WriteLine(Banner);
Console.WriteLine(new string('-', Banner.Length));

// RECORD STRUCTS ────────────────────────────────────────
// Value-type records with the same immutability / with-expression
// support that record classes have, but stack-allocated.
readonly record struct Point(double X, double Y)
{
  public double DistanceTo(Point other) => Math.Sqrt(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2));
}

// A mutable (non-readonly) record struct is also allowed.
record struct MutablePoint(double X, double Y);

var a = new Point(0, 0);
var b = new Point(3, 4);
Console.WriteLine($"\n[Record struct] Distance A -> B: {a.DistanceTo(b)}");

Person tony = new("Tony", "Bennett");
var olderTony = tony with { LastName = "Montana" };  // copy, but different last name
Console.WriteLine($"[Tony] {tony}");
Console.WriteLine($"[Other Tony] {olderTony}");

// SEALED RECORD ToString ────────────────────────────────
// Sealing ToString on a record prevents derived records from
// overriding the synthesized representation.
record Person(string FirstName, string LastName)
{
  public sealed override string ToString() => $"{FirstName} {LastName}";  // ← sealed in C# 10
}

record Employee(string FirstName, string LastName, string Role)
	: Person(FirstName, LastName)
{
	// Trying to override ToString() here would be a compile error.
	public string Summary => $"{this} - {Role}";
}

var emp = new Employee("Ada", "Lovelace", "Engineer");
Console.WriteLine($"[Sealed ToString] {emp.Summary}");

// EXTENDED PROPERTY PATTERNS ────────────────────────────
// Nested properties can now be matched inline without extra braces.
record Address(string City, string Country);
record Customer(string Name, Address HomeAddress);

static string ClassifyCustomer(Customer c)
{
  return c switch
  {
      { HomeAddress.Country: "US", HomeAddress.City: "New York" } => "NYC customer",
      { HomeAddress.Country: "US" } => "US customer",
      { HomeAddress.Country: "GB" } => "UK customer",
      _ => "International"
  };
}

var customers = new[]
{
	new Customer("Alice", new Address("New York", "US")),
	new Customer("Bob",   new Address("London",   "GB")),
	new Customer("Carol", new Address("Berlin",   "DE")),
};

foreach (var cust in customers)
{
	Console.WriteLine($"[Ext. pattern] {cust.Name}: {ClassifyCustomer(cust)}");
}

// LAMBDA IMPROVEMENTS ───────────────────────────────────
// Lambdas can now have:
//   • explicit return types        Func<int,int> f = int (x) => x * 2;
//   • attributes                   var g = [Obsolete] () => 42;
//   • natural (inferred) delegate  var h = (string s) => s.Length;  // Func<string,int>
void DemoLambdas()
{
	// Explicit return type on a lambda (new in C# 10)
	var toHex = int (int n) => n * 16;

	// Attribute on a lambda
	var strictLength = [Obsolete("Do not use this")] (string s) => s.Length;

	// Natural-type lambda — compiler infers Func<int,int,double>
	// Hover mouse over 'average'
	var average = (int a, int b) => (a + b) / 2.0;

	Console.WriteLine($"toHex(3)       = {toHex(3)}");
	Console.WriteLine($"strictLength   = {strictLength("hello")}");
	Console.WriteLine($"average(4,7)   = {average(4, 7)}");
}

Console.WriteLine("\n[Lambda improvements]");
DemoLambdas();
result = true;