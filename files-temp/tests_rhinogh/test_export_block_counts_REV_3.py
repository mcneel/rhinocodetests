"""
export_block_counts.py -- November 2023
If this code works, it was written by Dale Fugier.
If not, I don't know who wrote it.
Works with Rhino 7.
"""

import Rhino
import System
import scriptcontext as sc
import clr

# Get the name of the file to save
def get_filename():
    folder = None
    filename = None
    
    # build some defaults
    if not sc.doc.Path:
        folder = Rhino.ApplicationSettings.FileSettings.WorkingFolder
        filename = 'Untitled'
    else:
        folder = System.IO.Path.GetDirectoryName(sc.doc.Path)
        filename = System.IO.Path.GetFileNameWithoutExtension(sc.doc.Path)
        
    # Prompt the user for a file to save
    fd = Rhino.UI.SaveFileDialog()
    fd.Title = 'Save Block Counts'
    fd.Filter = 'TXT files|*.txt||'
    fd.InitialDirectory = folder
    fd.FileName = filename
    if fd.ShowSaveDialog(): 
        return fd.FileName
    return None

# Get a list of fields to show in a dialog
def get_fields():
    # Build a list of tuples
    fields = []
    fields.append(('Name', True))
    fields.append(('Description', True))
    fields.append(('Top-level Count', True))
    fields.append(('Nested Count', True))
    fields.append(('Total Count', True))
    fields.append(('Definition Type', False))
    fields.append(('File Name', False))
    fields.append(('Layer Style', False))
    fields.append(('Link URL', False))
    fields.append(('Link Description', False))
    return fields

# Prompt the user to select some fields
def select_fields(fields):
    str0 = 'Block Fields'
    str1 = 'Check the blocks fields to export.'
    keys = [item[0] for item in fields]
    values = [item[1] for item in fields]
    return Rhino.UI.Dialogs.ShowCheckListBox(str0, str1, keys, values) 

# Get a field string from an instance definition
def get_idef_string(idef, field):
    if not idef:
        return None
    rc = None
    # Name
    if field == 0:
        rc = idef.Name
    # Description
    elif field == 1:
        rc = idef.Description
    # Definition Type
    elif field == 5:
        ut = int(idef.UpdateType)
        if ut == 0 or ut == 1:
            rc = 'Embedded'
        elif ut == 2:
            rc = 'Linked and Embedded'
        else:
            rc = 'Linked'
    # File Name
    elif field == 6:
        rc = idef.SourceArchive
    # Layer Style
    elif field == 7:
        ls = int(idef.LayerStyle)
        if ls == 0:
            rc = 'None'
        elif ls == 1:
            rc = 'Active'
        else:
            rc = 'Reference'
    # Link URL
    elif field == 8:
        rc = idef.Url
    # Link Description
    elif field == 9:
        rc = idef.UrlDescription
    # Sanitize
    if not rc:
        rc = '<none>'
    if rc:
        rc = rc.replace('\r', '')
        rc = rc.replace('\n', ' ')
        rc = rc.replace(';', ' ')
    return rc

# Exports block counts, ala BlockManager
def export_block_counts():
    
    # If there are not instance definitions, bail
    idef_list = sc.doc.InstanceDefinitions.GetList(True)
    if len(idef_list) == 0:
        print('No blocks to count.')
        return
    
    # Prompt the user for a filename
    filename = get_filename()
    if not filename:
        return
    
    # Prompt for the fields to export
    items = get_fields()
    fields = select_fields(items)
    if not fields:
        return
    if sum(fields) == 0:
        return
    
    # Open a stream for writing
    with System.IO.StreamWriter(filename) as writer:
        
        # Write the file header
        rc = None
        for i in range(len(fields)):
            if fields[i]:
                if rc:
                    rc = rc + ';'
                else:
                    rc = ""
                rc = rc + items[i][0]
        writer.WriteLine(rc)
        
        # Process each instance definition
        for idef in idef_list:
            
            # Get counts, might need them later
            top = clr.Reference[System.Int32](0)
            nested = clr.Reference[System.Int32](0)
            total = idef.UseCount(top, nested)
            
            # Write the selected fields
            rc = None
            for i in range(len(fields)):
                if fields[i]:
                    if rc:
                        rc = rc + ';'
                    else:
                        rc = ""
                    if i == 2:
                        rc = rc + str(top.Value)
                    elif i == 3:
                        rc = rc + str(nested.Value)
                    elif i == 4:
                        rc = rc + str(total)
                    else:
                        rc = rc + get_idef_string(idef, i)
            writer.WriteLine(rc)
        
        # Finish up
        writer.Flush()
        writer.Close()
        print('Block counts saved to \'{0}\'.'.format(filename))


# Check to see if this file is being executed as the 'main' python
# script instead of being used as a module by some other python script
# This allows us to use the module which ever way we want.
if __name__ == '__main__':
    export_block_counts()
