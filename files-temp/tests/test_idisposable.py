#! python3

import rhinoscriptsyntax as rs
import scriptcontext as sc
import math

import System
import System.Collections.Generic
import Rhino


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


filename = get_filename()
with System.IO.StreamWriter(filename) as writer:
    writer.WriteLine("Test")
    writer.Flush()
    writer.Close()
