import scriptcontext as sc

from rhinoscript.application import *
from rhinoscript.curve import *
from rhinoscript.document import *
from rhinoscript.geometry import *
from rhinoscript.layer import *
from rhinoscript.object import *
from rhinoscript.plane import *
from rhinoscript.selection import *
from rhinoscript.surface import *
from rhinoscript.userinterface import *
from rhinoscript.view import *
from rhinoscript.utility import *
from rhinoscript.block import *
from rhinoscript.group import *
from rhinoscript.mesh import *
from rhinoscript.line import *
from rhinoscript.transformation import *
from rhinoscript.grips import *
from rhinoscript.pointvector import *
from rhinoscript.userdata import *
from rhinoscript.material import *
from rhinoscript.dimension import *
from rhinoscript.light import *
from rhinoscript.hatch import *
from rhinoscript.linetype import *
from rhinoscript.toolbar import *

import rhinoscriptsyntax as rs

r = sc.escape_test(reset=True)
print(r)
