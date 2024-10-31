import rhinoscriptsyntax as rs
import scriptcontext as sc

# rs.MessageBox("Hello World")

centerpoint = [1.0, 2.0, 4.0]
rs.AddCircle(centerpoint, 5.0)

print(sc.doc)

startPoint = [1.0, 2.0, 0.0]
endPoint = [4.0, 5.0, 0.0]
line1 = [startPoint, endPoint]

line1ID = rs.AddLine(
    line1[0], line1[1]
)  # Adds a line to the Rhino Document and returns an ObjectID

startPoint2 = [1.0, 4.0, 0.0]
endPoint2 = [4.0, 2.0, 0.0]
line2 = [startPoint2, endPoint2]

line2ID = rs.AddLine(line2[0], line2[1])  # Returns another ObjectID

int1 = rs.LineLineIntersection(
    line1ID, line2ID
)  # passing the ObjectIDs to the function.

print(int)
