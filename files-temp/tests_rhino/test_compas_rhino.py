#! python3
# env: C:\Users\ein\AppData\Roaming\McNeel\Rhinoceros\7.0\scripts
# env: C:\Users\ein\.conda\envs\research\Lib\site-packages

import compas

from compas.geometry import (
    Frame,
    Plane,
    Vector,
    Geometry,
    Transformation,
    Polyline,
    Polygon,
    Point,
    Box,
    Line,
    Pointcloud,
    bounding_box,
    convex_hull,
    distance_point_point,
    cross_vectors,
    centroid_points,
    distance_point_plane_signed,
    intersection_plane_plane_plane,
    centroid_polyhedron,
    volume_polyhedron,
    transform_points,
)

print(compas.__version__)