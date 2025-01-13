#! python3
import Rhino
import scriptcontext as sc

import os

Import_File =  r"C:\Users\ein\gits\rhinocodetests\files-ui\test_debug_rhino_obj\testwrite2.3dm"

temp_doc = Rhino.RhinoDoc.CreateHeadless(None)
wait_export_file = Rhino.FileIO.File3dm.Read(Import_File)

for obj in wait_export_file.Objects:
    temp_doc.Objects.Add(obj.Geometry)


file_name, file_extension = os.path.splitext(Import_File)
output_filepath = file_name + '.dwg'

options = Rhino.FileIO.FileDwgWriteOptions()

options.Version = Rhino.FileIO.FileDwgWriteOptions.AutocadVersion.Acad2007
#options.Flatten = 0

options.ExportSurfacesAs = Rhino.FileIO.FileDwgWriteOptions.ExportSurfaceMode.Solids
options.ExportMeshesAs = Rhino.FileIO.FileDwgWriteOptions.ExportMeshMode.Meshes

success = Rhino.FileIO.FileDwg.Write(output_filepath, temp_doc, options)
print("File write was:", success)
