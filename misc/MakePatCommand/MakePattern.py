#! python3

import System

import Rhino as R
import Rhino.Geometry as G

import rhinoscriptsyntax as rs

import patmaker


doc = R.RhinoDoc.ActiveDoc


def make_pattern(selected, bounds, name):
    lines = []
    obj_table = R.RhinoDoc.ActiveDoc.Objects
    for roid in selected:
        curve: G.Curve = obj_table.Find(roid).Geometry
        segments = patmaker.get_segments(curve)
        lines += segments
    
    if lines:
        hatch = patmaker.make_pattern(name, lines, bounds)
        hatch_index = doc.HatchPatterns.Add(hatch)
        print(hatch_index)


def run_make_pattern_command():
    selected = rs.GetObjects("Select Pattern Lines:", rs.filter.curve)
    if not selected:
        return

    bleft = rs.GetPoint("Select Bottom-Left Corner of Pattern Tile")
    if not bleft:
        return

    tright = rs.GetPoint("Select Top-Right Corner of Pattern Tile")
    if not tright:
        return

    name = rs.GetString("Enter Pattern Name")
    if not name:
        return

    make_pattern(
        selected,
        (
            G.Point2f(bleft.X, bleft.Y),
            G.Point2f(tright.X, tright.Y)
        ),
        name
    )


if __name__ == "__main__":
    run_make_pattern_command()


