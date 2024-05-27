using System;
using System.Collections;
using System.Collections.Generic;

using Rhino.Display;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;

using Grasshopper.Kernel.Types;

#pragma warning disable IDE0060

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public class ProxyObjectTable : IEnumerable<ProxyRhinoObject>
  {
    readonly Dictionary<Guid, ProxyRhinoObject> _storage = new Dictionary<Guid, ProxyRhinoObject>();

    #region Proxy ObjectTable API

    public Guid AddArc(Arc arc) => AddArc(arc, null);

    public Guid AddArc(Arc arc, ObjectAttributes attributes)
    {
      if (!arc.IsValid)
        return Guid.Empty;

      if (attributes is null)
        attributes = new ObjectAttributes();

      Guid guid = Guid.NewGuid();
      _storage.Add(guid, new ProxyRhinoObject(new GH_Curve(new ArcCurve(arc)), attributes));
      return guid;
    }

    public Guid AddBrep(Brep brep) => AddBrep(brep, null);

    public Guid AddBrep(Brep brep, ObjectAttributes attributes) => GenericAdd(new GH_Brep(brep), attributes);

    public Guid AddBox(Box box) => AddBox(box, null);

    public Guid AddBox(Box box, ObjectAttributes attributes)
    {
      if (!box.IsValid)
        throw new ArgumentException("Box is invalid.");

      return AddExtrusion(box.ToExtrusion(), attributes);
    }

    public Guid AddCircle(Circle circle) => AddCircle(circle, null);

    public Guid AddCircle(Circle circle, ObjectAttributes attributes)
    {
      if (!circle.IsValid)
        return Guid.Empty;

      if (attributes is null)
        attributes = new ObjectAttributes();

      Guid guid = Guid.NewGuid();
      _storage.Add(guid, new ProxyRhinoObject(new GH_Curve(new ArcCurve(circle)), attributes));
      return guid;
    }

    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, Guid clippedViewportId) => throw new NotSupportedInGHException();

    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds) => throw new NotSupportedInGHException();

    public Guid AddClippingPlane(Plane plane, double uMagnitude, double vMagnitude, IEnumerable<Guid> clippedViewportIds, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddCurve(Curve curve) => GenericAdd(new GH_Curve(curve), null);

    public Guid AddCurve(Curve curve, ObjectAttributes attributes) => GenericAdd(new GH_Curve(curve), attributes);

    public Guid AddEllipse(Ellipse ellipse) => AddEllipse(ellipse, null);

    public Guid AddEllipse(Ellipse ellipse, ObjectAttributes attributes)
    {
      if (!ellipse.IsValid)
        return Guid.Empty;

      if (attributes is null)
        attributes = new ObjectAttributes();

      Guid guid = Guid.NewGuid();
      _storage.Add(guid, new ProxyRhinoObject(new GH_Curve(ellipse.ToNurbsCurve()), attributes));
      return guid;
    }

    public Guid AddExtrusion(Extrusion extrusion) => AddExtrusion(extrusion, null);

    public Guid AddExtrusion(Extrusion extrusion, ObjectAttributes attributes) => GenericAdd(new GH_Surface(extrusion), attributes);

    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform) => AddInstanceObject(instanceDefinitionIndex, instanceXform, null);

    public Guid AddInstanceObject(int instanceDefinitionIndex, Transform instanceXform, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddLeader(IEnumerable<Point3d> points) => throw new NotSupportedInGHException();

    public Guid AddLeader(Plane plane, IEnumerable<Point2d> points) => throw new NotSupportedInGHException();

    public Guid AddLeader(string text, IEnumerable<Point3d> points) => throw new NotSupportedInGHException();

    public Guid AddLeader(Plane plane, IEnumerable<Point2d> points, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points) => throw new NotSupportedInGHException();

    public Guid AddLeader(string text, Plane plane, IEnumerable<Point2d> points, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddLine(Line line) => AddLine(line, null);

    public Guid AddLine(Line line, ObjectAttributes attributes)
    {
      if (!line.IsValid)
        return Guid.Empty;

      if (attributes is null)
        attributes = new ObjectAttributes();

      Guid guid = Guid.NewGuid();
      _storage.Add(guid, new ProxyRhinoObject(new GH_Curve(new LineCurve(line)), attributes));
      return guid;
    }

    public Guid AddLine(Point3d from, Point3d to) => AddLine(new Line(from, to), null);

    public Guid AddLine(Point3d from, Point3d to, ObjectAttributes attributes) => AddLine(new Line(from, to), attributes);

    public Guid AddLinearDimension(LinearDimension dimension) => AddLinearDimension(dimension, null);

    public Guid AddLinearDimension(LinearDimension dimension, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddMesh(Mesh mesh) => AddMesh(mesh, null);

    public Guid AddMesh(Mesh mesh, ObjectAttributes attributes) => GenericAdd(new GH_Mesh(mesh), attributes);

    public Guid AddPoint(Point3d point) => AddPoint(point, null);

    public Guid AddPoint(Point3f point) => AddPoint(new Point3d(point));

    public Guid AddPoint(Point3d point, ObjectAttributes attributes)
    {
      if (!point.IsValid)
        return Guid.Empty;

      Guid guid = Guid.NewGuid();
      if (attributes is null)
        attributes = new ObjectAttributes();

      _storage.Add(guid, new ProxyRhinoObject(new GH_Point(point), attributes));
      return guid;
    }

    public Guid AddPoint(Point3f point, ObjectAttributes attributes) => AddPoint(new Point3d(point), attributes);

    public Guid AddPoint(double x, double y, double z) => AddPoint(new Point3d(x, y, z), null);

    public Guid AddPointCloud(IEnumerable<Point3d> points) => throw new NotSupportedInGHException();

    public Guid AddPointCloud(PointCloud cloud) => throw new NotSupportedInGHException();

    public Guid AddPointCloud(IEnumerable<Point3d> points, ObjectAttributes attributes) => AddPointCloud(new PointCloud(points), attributes);

    public Guid AddPointCloud(PointCloud cloud, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public RhinoList<Guid> AddPoints(IEnumerable<Point3d> points) => AddPoints(points, null);

    public RhinoList<Guid> AddPoints(IEnumerable<Point3f> points) => AddPoints(points, null);

    public RhinoList<Guid> AddPoints(IEnumerable<Point3d> points, ObjectAttributes attributes)
    {
      RhinoList<Guid> list = new RhinoList<Guid>(InferLengthOrGuess(points));
      foreach (Point3d p in points)
      {
        Guid id = AddPoint(p, attributes);
        if (!id.Equals(Guid.Empty))
          list.Add(id);
      }
      return list;
    }

    public RhinoList<Guid> AddPoints(IEnumerable<Point3f> points, ObjectAttributes attributes)
    {
      RhinoList<Guid> list = new RhinoList<Guid>(InferLengthOrGuess(points));
      foreach (Point3f p in points)
      {
        Guid id = AddPoint(p, attributes);
        if (!id.Equals(Guid.Empty))
          list.Add(id);
      }
      return list;
    }

    public Guid AddPolyline(IEnumerable<Point3d> points) => AddPolyline(points, null);

    public Guid AddPolyline(IEnumerable<Point3d> points, ObjectAttributes attributes)
    {
      if (points == null)
        return Guid.Empty;

      return GenericAdd(new GH_Curve(new PolylineCurve(points)), attributes);
    }

    public Guid AddRectangle(Rectangle3d rectangle) => AddRectangle(rectangle, null);

    public Guid AddRectangle(Rectangle3d rectangle, ObjectAttributes attributes)
    {
      if (!rectangle.IsValid)
        throw new ArgumentException("Rectangle is invalid.");

      return AddPolyline(rectangle.ToPolyline(), attributes);
    }

    public Guid AddSphere(Sphere sphere) => AddSphere(sphere, null);

    public Guid AddSphere(Sphere sphere, ObjectAttributes attributes)
    {
      if (!sphere.IsValid)
        return Guid.Empty;

      return AddSurface(sphere.ToRevSurface(), attributes);
    }

    public Guid AddSurface(Surface surface) => AddSurface(surface, null);

    public Guid AddSurface(Surface surface, ObjectAttributes attributes)
    {
      if (surface == null)
        return Guid.Empty;

      return GenericAdd(new GH_Surface(surface), attributes);
    }

    public Guid AddSubD(SubD subd) => AddSubD(subd, null);

    public Guid AddSubD(SubD subd, ObjectAttributes attributes)
    {
      if (subd == null)
        return Guid.Empty;

      return GenericAdd(new GH_SubD(subd), attributes);
    }

    public Guid AddText(Text3d text3d) => throw new NotSupportedInGHException();

    public Guid AddText(Text3d text3d, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic) => throw new NotSupportedInGHException();

    public Guid AddText(string text, Plane plane, double height, string fontName, bool bold, bool italic, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddTextDot(TextDot dot) => throw new NotSupportedInGHException();

    public Guid AddTextDot(string text, Point3d location) => throw new NotSupportedInGHException();

    public Guid AddTextDot(TextDot dot, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public Guid AddTextDot(string text, Point3d location, ObjectAttributes attributes) => throw new NotSupportedInGHException();

    public bool Delete(Guid objectId, bool quiet)
    {
      bool success = _storage.Remove(objectId);

      if (!success && !quiet)
        throw new KeyNotFoundException("The Guid provided is not in the document");

      return success;
    }

    public Guid Duplicate(Guid objectId)
    {
      if (!_storage.ContainsKey(objectId))
        return Guid.Empty;

      ProxyRhinoObject ProxyRhinoObject = _storage[objectId];

      ObjectAttributes attrDup = null;
      if (ProxyRhinoObject.Attributes != null)
        attrDup = ProxyRhinoObject.Attributes.Duplicate();

      return GenericAdd((IGH_GeometricGoo)ProxyRhinoObject.GhGeometry.Duplicate(), attrDup);
    }

    public ProxyRhinoObject FindId(Guid objectId)
    {
      _storage.TryGetValue(objectId, out ProxyRhinoObject ag);
      return ag;
    }

    public bool TryFindPoint(Guid id, out Point3d point)
    {
      bool rc = false;
      if (FindId(id).GhGeometry is GH_Point gh_p)
      {
        point = gh_p.Value;
        rc = true;
      }
      else
        point = Point3d.Unset;
      return rc;
    }

    public ProxyRhinoObject Find(Guid objectId) => FindId(objectId);

    public GeometryBase FindGeometry(Guid objectId) => FindId(objectId).Geometry;

    public bool ModifyAttributes(Guid objectId, ObjectAttributes newAttributes, bool quiet)
    {
      if (!_storage.ContainsKey(objectId))
        return false;

      if (newAttributes == null)
        newAttributes = new ObjectAttributes();
      _storage[objectId] = new ProxyRhinoObject(_storage[objectId].GhGeometry, newAttributes);
      return true;
    }

    public int ObjectCount(ObjectEnumeratorSettings filter) => throw new NotImplementedException();

    public bool Replace(Guid guid, Arc arc)
    {
      if (!arc.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Curve(new ArcCurve(arc)), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Brep brep)
    {
      if (!brep.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Brep(brep), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Circle circle)
    {
      if (!circle.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Curve(new ArcCurve(circle)), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Curve curve)
    {
      if (!curve.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Curve(curve), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Line line)
    {
      if (!line.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Curve(new LineCurve(line)), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Mesh mesh)
    {
      if (!mesh.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Mesh(mesh), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Point3d point)
    {
      if (!point.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Point(point), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Polyline polyline)
    {
      if (!polyline.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Curve(new PolylineCurve(polyline)), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, Surface surface)
    {
      if (!surface.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_Surface(surface), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, SubD subD)
    {
      if (!subD.IsValid || !Contains(guid))
        return false;

      _storage[guid] = new ProxyRhinoObject(new GH_SubD(subD), _storage[guid].Attributes);
      return true;
    }

    public bool Replace(Guid guid, TextDot dot) => throw new NotSupportedInGHException();

    public bool Replace(Guid guid, TextEntity text) => throw new NotSupportedInGHException();

    public bool Show(Guid objectId, bool ignoreLayerMode) => throw new NotImplementedException();

    public bool Show(ObjRef objref, bool ignoreLayerMode) => throw new NotImplementedException();

    public bool Show(ProxyRhinoObject obj, bool ignoreLayerMode) => throw new NotImplementedException();

    public Guid Transform(Guid objectId, Transform xform, bool deleteOriginal)
    {
      if (!_storage.ContainsKey(objectId))
        return Guid.Empty;

      ProxyRhinoObject obj = _storage[objectId];
      IGH_GeometricGoo newObj = deleteOriginal
                                  ? obj.GhGeometry.Transform(xform)
                                  : obj.GhGeometry.DuplicateGeometry().Transform(xform);

      if (newObj == null)
        return Guid.Empty;

      if (deleteOriginal)
      {
        obj.GhGeometry = newObj;
        _storage[objectId] = obj; // ProxyRhinoObject is a ValueType
        return objectId;
      }
      var newId = Guid.NewGuid();
      ObjectAttributes attrClone = obj.Attributes?.Duplicate();
      _storage.Add(newId, new ProxyRhinoObject(newObj, attrClone));
      return newId;
    }

    public bool Unlock(Guid objectId, bool ignoreLayerMode) => throw new NotImplementedException();

    public int UnselectAll() => throw new NotImplementedException();

    public int UnselectAll(bool ignorePersistentSelections) => throw new NotImplementedException();

    #endregion

    #region Proxy ObjectTable API (Python Friendly)
    public Guid Add(Arc arc) => AddArc(arc);

    public Guid Add(Box box) => AddBox(box);

    public Guid Add(Brep brep) => AddBrep(brep);

    public Guid Add(Circle circle) => AddCircle(circle);

    public Guid Add(Curve curve) => AddCurve(curve);

    public Guid Add(Ellipse ellipse) => AddEllipse(ellipse);

    public Guid Add(Extrusion extrusion) => AddExtrusion(extrusion);

    public Guid Add(Line line) => AddLine(line);

    public Guid Add(LinearDimension dimension) => AddLinearDimension(dimension);

    public Guid Add(Mesh mesh) => AddMesh(mesh);

    public Guid Add(Point point) => AddPoint(point.Location);

    public Guid Add(Point3d point) => AddPoint(point);

    public Guid Add(Point3f point) => AddPoint(point);

    public Guid Add(PointCloud cloud) => AddPointCloud(cloud);

    public Guid Add(Polyline polyline) => AddPolyline(polyline);

    public Guid Add(Rectangle3d rectangle) => AddRectangle(rectangle);

    public Guid Add(Sphere sphere) => AddSphere(sphere);

    public Guid Add(Surface surface) => AddSurface(surface);

    public Guid Add(SubD subD) => AddSubD(subD);

    #endregion

    #region Enumerators
    public IEnumerable<IGH_GeometricGoo> GhGeometries
    {
      get
      {
        foreach (ProxyRhinoObject v in _storage.Values)
          yield return v.GhGeometry;
      }
    }

    public IEnumerable Geometries
    {
      get
      {
        foreach (ProxyRhinoObject v in _storage.Values)
          yield return v.Geometry;
      }
    }

    public IEnumerable<ProxyRhinoObject> AttributedGeometries => _storage.Values;

    public bool Contains(Guid item) => _storage.ContainsKey(item);

    public void Clear()
    {
      if (_storage.Count != 0)
        _storage.Clear();
    }

    public bool Contains(ProxyRhinoObject item) => _storage.ContainsValue(item);

    public void CopyTo(ProxyRhinoObject[] array, int arrayIndex)
    {
      foreach (ProxyRhinoObject attr in _storage.Values)
        array[arrayIndex++] = attr;
    }

    public int Count => _storage.Count;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<ProxyRhinoObject> GetEnumerator() => _storage.Values.GetEnumerator();
    #endregion

    const int LIST_INFER_START = 4;

    static int InferLengthOrGuess<T>(IEnumerable<T> points)
    {
      int inferredLength;

      if (points == null)
        inferredLength = 0;
      else
        inferredLength = points is ICollection<T> col ? col.Count : LIST_INFER_START;

      return inferredLength;
    }

    Guid GenericAdd<T>(T obj, ObjectAttributes attributes)
      where T : IGH_GeometricGoo
    {
      if (obj == null || !obj.IsValid)
        return Guid.Empty;

      Guid guid = Guid.NewGuid();
      if (attributes is null)
        attributes = new ObjectAttributes();

      _storage.Add(guid, new ProxyRhinoObject(obj, attributes));
      return guid;
    }
  }
}
