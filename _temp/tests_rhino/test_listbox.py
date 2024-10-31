import rhinoscriptsyntax as rs

print(
    rs.ListBox(
        ["test 0", "test 1", "test 2"],
        "test message",
        "test title",
        "test 2"
    )
)


idx = 3
formats = ("3dm", "obj", "stp", "stl", "igs", "3ds", "x_t", "fbx", "dwg", "dxf", "ai", "3mf","sat",)
fFormat = rs.ListBox(formats, "Choose export format", "Export by layer", formats[idx])
