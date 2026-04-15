// #! csharp
#r "lib: Ed.Core.dll"
#r "lib: Ed.Eto.dll"
using System;
using Rhino.Runtime;
using Eto.Forms;
using Ed.Core;
using Ed.Eto;

using var d = new Dialog { Width = 600, Height = 400, Resizable = true };

using var diff = new EdDiff(
  "python",
  "def hello():\n\t print('Hello;')\n",
  "def hello(name):\n\t print(f'Hello {name};')\n");
var b = new Button { Text = "Show Developer Tools"};
b.Click += (s, e) => diff.ShowDevTools();
d.Content = new TableLayout { Rows = { new TableRow{ ScaleHeight = true, Cells = { diff }}, b }};
diff.SetThemeAsync(isDark: Rhino.Runtime.HostUtils.RunningInDarkMode);

d.ShowModal();
