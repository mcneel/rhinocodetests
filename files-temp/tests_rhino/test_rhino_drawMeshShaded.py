import rhinoscriptsyntax as rs

import Rhino
import Eto
from System.Drawing import Color


class MyGetPoint(Rhino.Input.Custom.GetPoint):
    def __init__(self, texture_file_path):
        super().__init__()

        self.InputPlane = Rhino.Geometry.Plane.WorldXY
        self.Bitmap = Eto.Drawing.Bitmap(texture_file_path)

        self.MeshQuad = self.CreateUnitMeshQuad()
        self.Material = Rhino.Display.DisplayMaterial()
        self.Material.SetBitmapTexture(texture_file_path, True)

        self.Plane = Rhino.Geometry.Plane.Unset
        self.Rectangle = Rhino.Geometry.Rectangle3d.Unset

        # getter options
        self.Constrain(self.InputPlane, False)
        self.SetCommandPrompt("Pick orientation point")
        self.SetBasePoint(self.InputPlane.Origin, True)

    def CreateUnitMeshQuad(self):
        """creates a quad mesh in unit square with tx coordinates"""

        mesh = Rhino.Geometry.Mesh()
        mesh.Vertices.Add(0.0, 0.0, 0.0)
        mesh.Vertices.Add(1.0, 0.0, 0.0)
        mesh.Vertices.Add(1.0, 1.0, 0.0)
        mesh.Vertices.Add(0.0, 1.0, 0.0)

        mesh.TextureCoordinates.Add(Rhino.Geometry.Point2f(0.0, 0.0))
        mesh.TextureCoordinates.Add(Rhino.Geometry.Point2f(1.0, 0.0))
        mesh.TextureCoordinates.Add(Rhino.Geometry.Point2f(1.0, 1.0))
        mesh.TextureCoordinates.Add(Rhino.Geometry.Point2f(0.0, 1.0))

        mesh.Faces.AddFace(0, 1, 2, 3)

        mesh.Normals.ComputeNormals()
        mesh.Compact()

        return mesh

    def ComputeDisplayData(self, current_point):
        try:
            x_axis = current_point - self.InputPlane.Origin
            y_axis = Rhino.Geometry.Vector3d.CrossProduct(self.InputPlane.ZAxis, x_axis)

            self.Plane = Rhino.Geometry.Plane(self.InputPlane.Origin, x_axis, y_axis)
            if not self.Plane.IsValid:
                return

            # image proportional factor -> height / width
            f = self.Bitmap.Height / self.Bitmap.Width

            rc, x, y = self.Plane.ClosestParameter(current_point)
            if not rc:
                return

            # create rectangle and polyline from current mouse point
            x_interval = Rhino.Geometry.Interval(-x, x)
            y_interval = Rhino.Geometry.Interval(-x * f, x * f)

            self.Rectangle = Rhino.Geometry.Rectangle3d(
                self.Plane, x_interval, y_interval
            )
            if self.Rectangle.IsValid:

                # update mesh vertices of mesh quad
                self.MeshQuad.Vertices.SetVertex(0, self.Rectangle.Corner(0))
                self.MeshQuad.Vertices.SetVertex(1, self.Rectangle.Corner(1))
                self.MeshQuad.Vertices.SetVertex(2, self.Rectangle.Corner(2))
                self.MeshQuad.Vertices.SetVertex(3, self.Rectangle.Corner(3))

                self.MeshQuad.Normals.ComputeNormals()

            return True

        except Exception as ex:
            print(ex)

    def OnMouseMove(self, e):
        try:
            self.ComputeDisplayData(e.Point)
        except Exception as ex:
            print(ex)
        finally:
            Rhino.Input.Custom.GetPoint.OnMouseMove(self, e)

    def OnDynamicDraw(self, e):
        try:
            if self.Rectangle.IsValid:
                # draws the mesh quad with decal image
                e.Display.DrawMeshShaded(self.MeshQuad, self.Material)
                e.Display.DrawMeshWires(self.MeshQuad, Color.Black, 1)

        except Exception as ex:
            print(ex)
        finally:
            Rhino.Input.Custom.GetPoint.OnDynamicDraw(self, e)


def DoSomething():

    # get an image from Rhino's system folder
    file_path = rs.FindFile("MediumTile.png")
    if not file_path:
        print("Image not found: {}".format(file_path))
        return

    # start point picker
    gp = MyGetPoint(file_path)
    gp.Get()


DoSomething()
