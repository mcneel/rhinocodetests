#! python 3
"""
- create MeshSphere
- run and select the mesh
"""
# from Rhino.NodeInCode import Components as ncc
import ghpythonlib.components as ghcomp

import Rhino

def get_meshes():
    go = Rhino.Input.Custom.GetObject()
    go.SetCommandPrompt("Select Mesh")
    go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh  # Filter to curves
    go.EnablePreSelect(True, True)
    go.SubObjectSelect = False
    go.DeselectAllBeforePostSelect = False
    res = go.GetMultiple(1, 0)
    
    if go.CommandResult() == Rhino.Commands.Result.Success:
        selected_meshes = [go.Object(i).Mesh() for i in range(go.ObjectCount) if go.Object(i).Mesh()]
        return selected_meshes

meshes = get_meshes()

if meshes:
    mesh = meshes[0]
    # T,D,C = ncc.NodeInCodeFunctions.TriRemesh(mesh, None, None, None, 1, 25)
    T,D,C = ghcomp.TriRemesh(mesh, None, None, None, 1, 25)
    print(D)
    Rhino.RhinoDoc.ActiveDoc.Objects.AddMesh(T)