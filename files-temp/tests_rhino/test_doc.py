import Rhino


def get_docname():
    if Rhino.RhinoDoc.ActiveDoc:
        name = Rhino.RhinoDoc.ActiveDoc.Name
        if name:
            return name
    return "Untitled.3dm"


if d := get_docname():
    print(f"Document Name: {d}")
