using System;
using System.IO;
using System.Text;
using System.Drawing;

using Grasshopper.Kernel;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public sealed class AssemblyInfo : GH_AssemblyInfo
  {
    internal static readonly string _assemblyIconData = "[[ASSEMBLY-ICON]]";
    internal static readonly string _categoryIconData = "[[ASSEMBLY-CATEGORY-ICON]]";
    internal static readonly Bitmap _assemblyIcon;
    internal static readonly Bitmap _categoryIcon;

    static AssemblyInfo()
    {
      using (var aicon = new MemoryStream(Convert.FromBase64String(_assemblyIconData)))
        _assemblyIcon = new Bitmap(aicon);

      using (var cicon = new MemoryStream(Convert.FromBase64String(_categoryIconData)))
        _categoryIcon = new Bitmap(cicon);
    }

    public override Guid Id { get; } = new Guid("4265ad55-87f7-4499-be84-a0c67b052a35");

    public override string AssemblyName { get; } = "demo icon.GH";
    public override string AssemblyVersion { get; } = "0.1.18446.8848";
    public override string AssemblyDescription { get; } = "";
    public override string AuthorName { get; } = "nk";
    public override string AuthorContact { get; } = "nk@local.com";
    public override GH_LibraryLicense AssemblyLicense { get; } = GH_LibraryLicense.unset;
    public override Bitmap AssemblyIcon { get; } = _assemblyIcon;
  }

  public class ProjectComponentPlugin : GH_AssemblyPriority
  {
    public override GH_LoadingInstruction PriorityLoad()
    {
      Grasshopper.Instances.ComponentServer.AddCategoryIcon("demo icon", AssemblyInfo._categoryIcon);
      Grasshopper.Instances.ComponentServer.AddCategorySymbolName("demo icon", "demo icon"[0]);
      return GH_LoadingInstruction.Proceed;
    }

    public static string DecryptString(string text)
    {
      if (text is null)
        throw new System.ArgumentNullException(nameof(text));

      if (string.IsNullOrWhiteSpace(text))
        return string.Empty;

      return Encoding.UTF8.GetString(Convert.FromBase64String(text));
    }
  }
}
