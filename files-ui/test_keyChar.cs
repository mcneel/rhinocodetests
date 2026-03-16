// #! csharp
#r "lib: Microsoft.macOS.dll"
using System;
using System.Windows.Forms;
using AppKit;
using Foundation;
using Eto.Forms;

string GetKeySymbol(ushort keyCode)
{
    var ctx = NSGraphicsContext.CurrentContext;
	var keyEvent = NSEvent.KeyEvent(NSEventType.KeyDown, CoreGraphics.CGPoint.Empty, 0, 0, 0, ctx, "A", string.Empty, false, keyCode);
	return keyEvent.Characters;
}

ushort keyCode = 1;
string symbol = GetKeySymbol(keyCode);
Console.WriteLine($"Key Code: {keyCode}, Symbol: {symbol}");

KeysConverter kc = new KeysConverter();
Console.WriteLine(kc.ConvertToString((int)Eto.Forms.Keys.A));
Console.WriteLine(kc.ConvertToString((int)System.Windows.Forms.Keys.A));
Console.WriteLine(kc.ConvertToString((int)System.Windows.Forms.Keys.M));

