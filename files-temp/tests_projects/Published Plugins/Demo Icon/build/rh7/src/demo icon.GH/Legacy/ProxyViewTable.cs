using System;
using System.Collections;
using System.Collections.Generic;

using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public class ProxyViewTable : IEnumerable<RhinoView>
  {
    readonly Func<ViewTable> _tableFunc;
    readonly bool _redraws;

    public ProxyViewTable(Func<ViewTable> tableFunc, bool redraws)
    {
      _tableFunc = tableFunc;
      _redraws = redraws;
    }

    public RhinoView ActiveView
    {
      get => _tableFunc().ActiveView;
      set => _tableFunc().ActiveView = value;
    }

    public object Document => _tableFunc().Document;

    public bool RedrawEnabled
    {
      get => _tableFunc().RedrawEnabled;
      set => _tableFunc().RedrawEnabled = value;
    }

    public RhinoPageView AddPageView(string title) => _tableFunc().AddPageView(title);

    public RhinoPageView AddPageView(string title, double pageWidth, double pageHeight) => _tableFunc().AddPageView(title, pageWidth, pageHeight);

    public void DefaultViewLayout() => _tableFunc().DefaultViewLayout();

    public RhinoView Find(Guid mainViewportId) => Find(mainViewportId);

    public RhinoView Find(string mainViewportName, bool compareCase) => _tableFunc().Find(mainViewportName, compareCase);

    public void FlashObjects(IEnumerable<RhinoObject> list, bool useSelectionColor) => _tableFunc().FlashObjects(list, useSelectionColor);

    public void FourViewLayout(bool useMatchingViews) => _tableFunc().FourViewLayout(useMatchingViews);

    public RhinoPageView[] GetPageViews() => _tableFunc().GetPageViews();

    public RhinoView[] GetStandardRhinoViews() => _tableFunc().GetStandardRhinoViews();

    public RhinoView[] GetViewList(bool includeStandardViews, bool includePageViews) => _tableFunc().GetViewList(includeStandardViews, includePageViews);

    public void Redraw()
    {
      if (_redraws)
        _tableFunc().Redraw();
    }

    public void ThreeViewLayout(bool useMatchingViews) => _tableFunc().ThreeViewLayout(useMatchingViews);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<RhinoView> GetEnumerator() => _tableFunc().GetEnumerator();
  }
}
