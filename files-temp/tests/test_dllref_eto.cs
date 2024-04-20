# r "Eto"
# r "Eto.macOS"
# r "NetStandard"

using System;
using Eto.Drawing;
using Eto.Forms;


var tf = new TestTree();

// tf.ShowModal();

Console.WriteLine("dfsdf");


var ex = new Size(5, 5);
var r1 = new RectangleF(0, 0, 20, 20);
var r2 = RectangleF.Inflate(r1, ex);
var r3 = r1.Expand(ex);

Console.WriteLine(r2);
Console.WriteLine(r3);

public class TestTree : Eto.Forms.Dialog
{
	public TestTree()
	{
		Content = new TreeGridView();
	}
}


public static class DrawingExtensions
{
    public static RectangleF Expand(this RectangleF rectangle, Size size)
      => new RectangleF(
        x: rectangle.X - size.Width,
        y: rectangle.Y - size.Width,
        width: rectangle.Width + size.Width,
        height: rectangle.Height + size.Height
      );
}