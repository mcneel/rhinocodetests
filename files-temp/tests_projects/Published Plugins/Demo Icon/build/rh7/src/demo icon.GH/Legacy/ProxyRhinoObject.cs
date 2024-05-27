using System;

using Rhino.DocObjects;
using Rhino.Geometry;

using Grasshopper.Kernel.Types;

#pragma warning disable IDE0060

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public struct ProxyRhinoObject : IEquatable<ProxyRhinoObject>
  {
    ObjectAttributes _attributes;

    public ProxyRhinoObject(IGH_GeometricGoo item, ObjectAttributes attr)
    {
      _attributes = attr;
      GhGeometry = item;
    }

    internal IGH_GeometricGoo GhGeometry { get; set; }

    public bool IsDefault => GhGeometry == null;

    public Guid Id => _attributes.ObjectId;

    public string Name
    {
      get => _attributes?.Name;
      set => _attributes.Name = value;
    }

    public GeometryBase Geometry
    {
      get
      {
        if (GhGeometry is null)
          return null;

        object toReturn = GhGeometry.ScriptVariable();

        if (toReturn is Point3d)
          toReturn = new Point((Point3d)toReturn);

        return (GeometryBase)toReturn;
      }
    }

    public ObjectAttributes Attributes
    {
      get
      {
        return _attributes;
      }
      set
      {
        if (_attributes == null)
          throw new ArgumentNullException();

        _attributes = value;
      }
    }

    public override string ToString()
    {
      return string.Format("{0}, {1}", Geometry, (object)_attributes ?? "(default)");
    }

    public override int GetHashCode()
    {
      int val;
      if (Geometry is null)
        val = 0;
      else if (_attributes is null)
        val = Geometry.GetHashCode();
      else
        val = Geometry.GetHashCode() ^ (_attributes.GetHashCode() << 5);
      return val;
    }

    public bool Equals(ProxyRhinoObject other) => Geometry == other.Geometry && _attributes == other._attributes;

    public override bool Equals(object obj) => (obj is ProxyRhinoObject robj) && Equals(robj);

    public static bool operator ==(ProxyRhinoObject one, ProxyRhinoObject other) => one.Equals(other);

    public static bool operator !=(ProxyRhinoObject one, ProxyRhinoObject other) => !one.Equals(other);

    #region Proxy RhinoObject API
    public bool CommitChanges() => true;
    public int IsSelected(bool checkSubObjects) => 0;
    public bool IsSubObjectSelected(ComponentIndex componentIndex) => false;
    public ComponentIndex[] GetSelectedSubObjects() => Array.Empty<ComponentIndex>();
    public bool IsSelectable(bool ignoreSelectionState, bool ignoreGripsState, bool ignoreLayerLocking, bool ignoreLayerVisibility) => false;
    public bool IsSelectable() => IsSelectable(false, false, false, false);
    public bool IsSubObjectSelectable(ComponentIndex componentIndex, bool ignoreSelectionState) => false;
    public int Select(bool on, bool syncHighlight, bool persistentSelect, bool ignoreGripsState, bool ignoreLayerLocking, bool ignoreLayerVisibility) => 0;
    public int Select(bool on) => 0;
    public int Select(bool on, bool syncHighlight) => 0;
    public int SelectSubObject(ComponentIndex componentIndex, bool select, bool syncHighlight) => 0;
    public int SelectSubObject(ComponentIndex componentIndex, bool select, bool syncHighlight, bool persistentSelect) => 0;
    public int UnselectAllSubObjects() => 0;
    public int IsHighlighted(bool checkSubObjects) => 0;
    public bool Highlight(bool enable) => false;
    public bool IsSubObjectHighlighted(ComponentIndex componentIndex) => false;
    public ComponentIndex[] GetHighlightedSubObjects() => Array.Empty<ComponentIndex>();
    public bool HighlightSubObject(ComponentIndex componentIndex, bool highlight) => false;
    public int UnhighlightAllSubObjects() => 0;
    public bool GripsOn { get => false; set { } }
    public bool GripsSelected => false;
    #endregion
  }
}
