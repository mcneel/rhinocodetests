#! python3
import ifcopenshell
import os.path as op

print(ifcopenshell.version)
model = ifcopenshell.open(op.join(op.dirname(__file__), "test_pip_openifcshell_example.ifc"))

print(model.schema) # May return IFC2X3, IFC4, or IFC4X3.

print(model.by_id(1))

# print(model.by_guid('0EI0MSHbX9gg8Fxwar7lL8'))

walls = model.by_type('IfcWall')
print(len(walls))

wall = model.by_type('IfcWall')[0]
print(wall.id())

print(wall.is_a()) # Returns 'IfcWall'

print(wall[0]) # The first attribute is the GlobalId
print(wall[2]) # The third attribute is the Name
