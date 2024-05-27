using System;
using System.Collections;

using Rhino;
using Rhino.Collections;
using Rhino.DocObjects.Tables;

using Grasshopper;
using Grasshopper.Kernel;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public class ProxyDocument
  {
    const double DEFAULT_ABSOLUTE_TOLERANCE = RhinoMath.DefaultDistanceToleranceMillimeters;
    const double DEFAULT_ANGLE_TOLERANCE = RhinoMath.DefaultAngleTolerance;
    const UnitSystem DEFAULT_UNIT_SYSTEM = UnitSystem.Meters;

    public const string DOCUMENT_PARAM_NAME = "ghdoc";
    public const string ENVIRON_PARAM_NAME = "ghenv";

    public RhinoList<Guid> CommitIntoRhinoDocument()
    {
      RhinoList<Guid> newGuids = new RhinoList<Guid>(Objects.Count);

      foreach (var content in this.Objects.AttributedGeometries)
      {
        var geom = content.Geometry;
        var attr = content.Attributes;

        if (geom is IGH_BakeAwareData)
        {
          (geom as IGH_BakeAwareData).BakeGeometry(RhinoDoc.ActiveDoc, attr, out Guid guid);
          if (!guid.Equals(Guid.Empty))
            newGuids.Add(guid);
        }
        else
          throw new ApplicationException("UnexpectedObjectException. Please report this error to tech@mcneel.com");
      }

      return newGuids;
    }

    public IGH_Component Component { get; set; }

    public ProxyViewTable Views { get; } = RhinoDoc.ActiveDoc != null
        ? new ProxyViewTable(() => RhinoDoc.ActiveDoc.Views, false) : null;

    #region Indexers
    public object this[Guid id] => Objects.Contains(id) ? Objects.Find(id).Geometry : null;

    public IEnumerable this[IEnumerable guids]
    {

      get
      {
        if (guids == null)
          throw new ArgumentNullException("guids",
              "Cannot obtain a null item or subset from " + DOCUMENT_PARAM_NAME);

        return SubSet(guids);
      }
    }

    public IEnumerable SubSet(IEnumerable guids)
    {
      if (guids == null)
        throw new ArgumentNullException("guids",
            "Cannot obtain a null item or subset from " + DOCUMENT_PARAM_NAME);

      foreach (var obj in guids)
      {
        if (obj is Guid)
        {
          var id = (Guid)obj;
          if (Objects.Contains(id))
            yield return Objects.Find(id).Geometry;
          else
            yield return null;
        }
        else
          yield return null;
      }
    }
    #endregion

    public ProxyObjectTable Objects { get; } = new ProxyObjectTable();

    public BitmapTable Bitmaps => throw new NotSupportedInGHException();

    public DimStyleTable DimStyles => throw new NotSupportedInGHException();

    public int DistanceDisplayPrecision => RhinoDoc.ActiveDoc.DistanceDisplayPrecision;

    public FontTable Fonts => throw new NotSupportedInGHException();

    public GroupTable Groups => throw new NotSupportedInGHException();

    public InstanceDefinitionTable InstanceDefinitions => throw new NotSupportedInGHException();

    public bool IsLocked => false;

    public bool IsReadOnly => false;

    public bool IsSendingMail => RhinoDoc.ActiveDoc.IsSendingMail;

    public LayerTable Layers => throw new NotSupportedInGHException();

    public MaterialTable Materials => throw new NotSupportedInGHException();

    public double ModelAbsoluteTolerance
    {
      get
      {
        if (RhinoApp.IsRunningHeadless && RhinoDoc.ActiveDoc == null)
          return DEFAULT_ABSOLUTE_TOLERANCE;
        return RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      }
      set => throw new NotSupportedInGHException();
    }

    public double ModelAngleToleranceDegrees
    {
      get
      {
        if (RhinoApp.IsRunningHeadless && RhinoDoc.ActiveDoc == null)
          return RhinoMath.ToDegrees(DEFAULT_ANGLE_TOLERANCE);
        return RhinoDoc.ActiveDoc.ModelAngleToleranceDegrees;
      }
      set => throw new NotSupportedInGHException();
    }

    public double ModelAngleToleranceRadians
    {
      get
      {
        if (RhinoApp.IsRunningHeadless && RhinoDoc.ActiveDoc == null)
          return DEFAULT_ANGLE_TOLERANCE;
        return RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      }
      set => throw new NotSupportedInGHException();
    }

    public double ModelRelativeTolerance
    {
      get => RhinoDoc.ActiveDoc.ModelRelativeTolerance;
      set => throw new NotSupportedInGHException();
    }

    public UnitSystem ModelUnitSystem
    {
      get
      {
        if (RhinoApp.IsRunningHeadless && RhinoDoc.ActiveDoc == null)
          return DEFAULT_UNIT_SYSTEM;
        return RhinoDoc.ActiveDoc.ModelUnitSystem;
      }
      set => throw new NotSupportedInGHException();
    }

    public bool Modified
    {
      get => true;
      set => throw new NotSupportedInGHException();
    }

    public string Name => Instances.DocumentServer[0].DisplayName;

    public NamedConstructionPlaneTable NamedConstructionPlanes => throw new NotSupportedInGHException();

    public NamedViewTable NamedViews => throw new NotSupportedInGHException();

    public string Notes
    {
      get => RhinoDoc.ActiveDoc.Notes;
      set => throw new NotSupportedInGHException();
    }

    public double PageAbsoluteTolerance
    {
      get => RhinoDoc.ActiveDoc.PageAbsoluteTolerance;
      set => throw new NotSupportedInGHException();
    }

    public double PageAngleToleranceDegrees
    {
      get => RhinoDoc.ActiveDoc.PageAngleToleranceDegrees;
      set => throw new NotSupportedInGHException();
    }

    public double PageAngleToleranceRadians
    {
      get => RhinoDoc.ActiveDoc.PageAngleToleranceRadians;
      set => throw new NotSupportedInGHException();
    }

    public double PageRelativeTolerance
    {
      get => RhinoDoc.ActiveDoc.PageRelativeTolerance;
      set => throw new NotSupportedInGHException();
    }

    public UnitSystem PageUnitSystem
    {
      get => RhinoDoc.ActiveDoc.PageUnitSystem;
      set => throw new NotSupportedInGHException();
    }

    public string Path
    {
      get
      {
        // 4 Mar 2020 S. Baer
        // It is common for the Instances.DocumentServer to actually
        // have no documents at all when someone is programatically
        // attempting to execute a grasshopper document. This is the
        // typical case for "Resthopper"
        if (Instances.DocumentServer.DocumentCount < 1)
          return null;

        return Instances.DocumentServer[0].FilePath;
      }
    }

    public StringTable Strings => throw new NotSupportedInGHException();

    public string TemplateFileUsed => RhinoDoc.ActiveDoc.TemplateFileUsed;

    public bool UndoRecordingEnabled
    {
      get => false;
      set
      {
        if (value)
          throw new NotSupportedInGHException();
      }
    }
  }
}
