#! python3

from Rhino.Commands import Result
from Rhino.Runtime.Code.Execution import ExitException

raise ExitException(int(Result.Cancel));