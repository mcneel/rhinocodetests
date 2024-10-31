import rhinoscriptsyntax as rs
point1 = rs.GetPoint("First point")
if point1:
    rs.AddPoint(point1)
point2 = rs.GetPoint("Next point")
if point2:
    rs.AddPoint(point2)
point3 = rs.GetPoint("Third point")
if point3:
    rs.AddPoint(point3)
vector12 = rs.VectorUnitize(rs.VectorCreate(point2, point1))
vector23 = rs.VectorUnitize(rs.VectorCreate(point3, point2))
vector13 = rs.VectorUnitize(rs.VectorCreate(point3, point1))
N=rs.GetInteger("Number Of Circle?")

for i in range(1,N+1):
    rs.AddCircle(rs.CopyObject(point1,vector12*(i)),i)
    rs.AddCircle(rs.CopyObject(point2,vector12*(-i)),i)
    
    rs.AddCircle(rs.CopyObject(point2,vector23*(i)),i)
    rs.AddCircle(rs.CopyObject(point3,vector23*(-i)),i)
    
    rs.AddCircle(rs.CopyObject(point1,vector13*(i)),i)
    rs.AddCircle(rs.CopyObject(point3,vector13*(-i)),i)

