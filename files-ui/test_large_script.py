"""

- Search for __revit__ and find lines with errors
- Select a big chunk of lines and comment in/out and check
  speed and update in problems panel

"""
#pylint: disable=W0703,C0302,C0103,C0413,raise-missing-from
import sys
import os
import os.path as op
from collections import namedtuple
import traceback
import re

import clr  # pylint: disable=E0401

import System
import System.Runtime.InteropServices as SRI

from pyrevit import compat

PYREVIT_ADDON_NAME = 'pyRevit'
PYREVIT_CLI_NAME = 'pyrevit.exe'

# extract version from version file
VERSION_STRING = '0.0.'
with open(op.join(op.dirname(__file__), 'version'), 'r') as version_file:
    VERSION_STRING = version_file.read()
matches = re.findall(r'(\d+)\.(\d+)\.(\d+)\.?(.+)?', VERSION_STRING)[0]
if len(matches) == 4:
    VERSION_MAJOR, VERSION_MINOR, VERSION_PATCH, BUILD_METADATA = matches
else:
    VERSION_MAJOR, VERSION_MINOR, VERSION_PATCH = matches
    BUILD_METADATA = ""

try:
    VERSION_MAJOR = int(VERSION_MAJOR)
    VERSION_MINOR = int(VERSION_MINOR)
    VERSION_PATCH = int(VERSION_PATCH)
except:
    raise Exception('Critical Error. Can not determine pyRevit version.')
# -----------------------------------------------------------------------------
# config environment paths
# -----------------------------------------------------------------------------
# main pyrevit repo folder
try:
    # 3 steps back for <home>/Lib/pyrevit
    HOME_DIR = op.dirname(op.dirname(op.dirname(__file__)))
except NameError:
    raise Exception('Critical Error. Can not find home directory.')

# Determine dotnet runtime
DOTNET_RUNTIME_ID = "netfx"
if System.Environment.Version.Major >= 5 or \
        (".NET Core" in SRI.RuntimeInformation.FrameworkDescription):
    DOTNET_RUNTIME_ID = "netcore"

IS_DOTNET_CORE = DOTNET_RUNTIME_ID == "netcore"

# BIN directory
BIN_DIR = op.join(HOME_DIR, 'bin', DOTNET_RUNTIME_ID)

# main pyrevit lib folders
MAIN_LIB_DIR = op.join(HOME_DIR, 'pyrevitlib')
MISC_LIB_DIR = op.join(HOME_DIR, 'site-packages')

# path to pyrevit module
MODULE_DIR = op.join(MAIN_LIB_DIR, 'pyrevit')

# loader directory
LOADER_DIR = op.join(MODULE_DIR, 'loader')

# runtime directory
RUNTIME_DIR = op.join(MODULE_DIR, 'runtime')

# addin directory
ADDIN_DIR = op.join(LOADER_DIR, 'addin')

# if loader module is available means pyRevit is being executed by Revit.
import pyrevit.engine as eng
if eng.EngineVersion != 000:
    ENGINES_DIR = op.join(BIN_DIR, 'engines', eng.EngineVersion)
# otherwise it might be under test, or documentation processing.
# so let's keep the symbols but set to None (fake the symbols)
else:
    ENGINES_DIR = None

# add the framework dll path to the search paths
sys.path.append(BIN_DIR)
sys.path.append(ADDIN_DIR)
sys.path.append(ENGINES_DIR)


PYREVIT_CLI_PATH = op.join(HOME_DIR, 'bin', PYREVIT_CLI_NAME)


# now we can start importing stuff
from pyrevit.compat import safe_strtype
from pyrevit.framework import Process
from pyrevit.framework import Windows
from pyrevit.framework import Forms
from pyrevit import api
from pyrevit.api import DB, UI, ApplicationServices, AdWindows

# -----------------------------------------------------------------------------
# Base Exceptions
# -----------------------------------------------------------------------------
TRACEBACK_TITLE = 'Traceback:'


# General Exceptions
class PyRevitException(Exception):
    """Common base class for all pyRevit exceptions.

    Parameters args and message are derived from Exception class.
    """

    @property
    def msg(self):
        """Return exception message."""
        if self.args:
            return self.args[0] #pylint: disable=E1136
        else:
            return ''

    def __repr__(self):
        return str(self)

    def __str__(self):
        """Process stack trace and prepare report for output window."""
        sys.exc_type, sys.exc_value, sys.exc_traceback = sys.exc_info()
        try:
            tb_report = traceback.format_tb(sys.exc_traceback)[0]
            if self.msg:
                return '{}\n\n{}\n{}'.format(self.msg,
                                             TRACEBACK_TITLE,
                                             tb_report)
            else:
                return '{}\n{}'.format(TRACEBACK_TITLE, tb_report)
        except Exception:
            return Exception.__str__(self)


class PyRevitIOError(PyRevitException):
    """Common base class for all pyRevit io-related exceptions."""


class PyRevitCPythonNotSupported(PyRevitException):
    """Exception for features not supported under CPython."""
    def __init__(self, feature_name):
        super(PyRevitCPythonNotSupported, self).__init__()
        self.feature_name = feature_name

    def __str__(self):
        return self.msg

    @property
    def msg(self):
        """Return exception message."""
        return '\"{}\" is not currently supported under CPython' \
                .format(self.feature_name)


# -----------------------------------------------------------------------------
# Wrapper for __revit__ builtin parameter set in scope by C# Script Executor
# -----------------------------------------------------------------------------
# namedtuple for passing information about a PostableCommand
_HostAppPostableCommand = namedtuple('_HostAppPostableCommand',
                                     ['name', 'key', 'id', 'rvtobj'])
"""Private namedtuple for passing information about a PostableCommand

Attributes:
    name (str): Postable command name
    key (str): Postable command key string
    id (int): Postable command id
    rvtobj (``RevitCommandId``): Postable command Id Object
"""


class _HostApplication(object):
    """Private Wrapper for Current Instance of Revit.

    Provides version info and comparison functionality, alongside providing
    info on the active screen, active document and ui-document, available
    postable commands, and other functionality.

    Examples:
            ```python
            hostapp = _HostApplication()
            hostapp.is_newer_than(2017)
            ```
    """

    def __init__(self):
        self._postable_cmds = []

    # @property
    # def uiapp(self):
    #     """Return UIApplication provided to the running command."""
    #     if isinstance(__revit__, UI.UIApplication):  #pylint: disable=undefined-variable
    #         return __revit__  #pylint: disable=undefined-variable

    # @property
    # def app(self):
    #     """Return Application provided to the running command."""
    #     if self.uiapp:
    #         return self.uiapp.Application
    #     elif isinstance(__revit__, ApplicationServices.Application):  #pylint: disable=undefined-variable
    #         return __revit__  #pylint: disable=undefined-variable

    @property
    def addin_id(self):
        """Return active addin id."""
        return self.app.ActiveAddInId

    @property
    def has_api_context(self):
        """Determine if host application is in API context."""
        return self.app.ActiveAddInId is not None

    @property
    def uidoc(self):
        """Return active UIDocument."""
        return getattr(self.uiapp, 'ActiveUIDocument', None)

    @property
    def doc(self):
        """Return active Document."""
        return getattr(self.uidoc, 'Document', None)

    @property
    def active_view(self):
        """Return view that is active (UIDocument.ActiveView)."""
        return getattr(self.uidoc, 'ActiveView', None)

    @active_view.setter
    def active_view(self, value):
        """Set the active view in user interface."""
        setattr(self.uidoc, 'ActiveView', value)

    @property
    def docs(self):
        """Return :obj:`list` of open :obj:`Document` objects."""
        return getattr(self.app, 'Documents', None)

    @property
    def available_servers(self):
        """Return :obj:`list` of available Revit server names."""
        return list(self.app.GetRevitServerNetworkHosts())

    @property
    def version(self):
        """str: Return version number (e.g. '2018')."""
        return self.app.VersionNumber

    @property
    def subversion(self):
        """str: Return subversion number (e.g. '2018.3')."""
        if hasattr(self.app, 'SubVersionNumber'):
            return self.app.SubVersionNumber
        else:
            return '{}.0'.format(self.version)

    @property
    def version_name(self):
        """str: Return version name (e.g. 'Autodesk Revit 2018')."""
        return self.app.VersionName

    @property
    def build(self):
        """str: Return build number (e.g. '20170927_1515(x64)')."""
        if int(self.version) >= 2021:
            # uses labs module that is imported later in this code
            return labs.extract_build_from_exe(self.proc_path)
        else:
            return self.app.VersionBuild

    @property
    def serial_no(self):
        """str: Return serial number number (e.g. '569-09704828')."""
        return api.get_product_serial_number()

    @property
    def pretty_name(self):
        """Returns the pretty name of the host.

        Examples:
            Autodesk Revit 2019.2 build: 20190808_0900(x64)

        Returns:
            (str): Pretty name of the host
        """
        host_name = self.version_name
        if self.is_newer_than(2017):
            host_name = host_name.replace(self.version, self.subversion)
        return "%s build: %s" % (host_name, self.build)

    @property
    def is_demo(self):
        """bool: Determine if product is using demo license."""
        return api.is_product_demo()

    @property
    def language(self):
        """str: Return language type (e.g. 'LanguageType.English_USA')."""
        return self.app.Language

    @property
    def username(self):
        """str: Return the username from Revit API (Application.Username)."""
        uname = self.app.Username
        uname = uname.split('@')[0]  # if username is email
        # removing dots since username will be used in file naming
        uname = uname.replace('.', '')
        return uname

    @property
    def proc(self):
        """System.Diagnostics.Process: Return current process object."""
        return Process.GetCurrentProcess()

    @property
    def proc_id(self):
        """int: Return current process id."""
        return Process.GetCurrentProcess().Id

    @property
    def proc_name(self):
        """str: Return current process name."""
        return Process.GetCurrentProcess().ProcessName

    @property
    def proc_path(self):
        """str: Return file path for the current process main module."""
        return Process.GetCurrentProcess().MainModule.FileName

    @property
    def proc_window(self):
        """``intptr``: Return handle to current process window."""
        if self.is_newer_than(2019, or_equal=True):
            return self.uiapp.MainWindowHandle
        else:
            return AdWindows.ComponentManager.ApplicationWindow

    @property
    def proc_screen(self):
        """``intptr``: Return handle to screen hosting current process."""
        return Forms.Screen.FromHandle(self.proc_window)

    @property
    def proc_screen_workarea(self):
        """``System.Drawing.Rectangle``: Return screen working area."""
        screen = HOST_APP.proc_screen
        if screen:
            return screen.WorkingArea

    @property
    def proc_screen_scalefactor(self):
        """float: Return scaling for screen hosting current process."""
        screen = HOST_APP.proc_screen
        if screen:
            actual_wdith = Windows.SystemParameters.PrimaryScreenWidth
            scaled_width = screen.PrimaryScreen.WorkingArea.Width
            return abs(scaled_width / actual_wdith)

    def is_newer_than(self, version, or_equal=False):
        """bool: Return True if host app is newer than provided version.

        Args:
            version (str or int): version to check against.
            or_equal (bool): Whether to include `version` in the comparison
        """
        if or_equal:
            return int(self.version) >= int(version)
        else:
            return int(self.version) > int(version)

    def is_older_than(self, version):
        """bool: Return True if host app is older than provided version.

        Args:
            version (str or int): version to check against.
        """
        return int(self.version) < int(version)

    def is_exactly(self, version):
        """bool: Return True if host app is equal to provided version.

        Args:
            version (str or int): version to check against.
        """
        return int(self.version) == int(version)

    def get_postable_commands(self):
        """Return list of postable commands.

        Returns:
            (list[_HostAppPostableCommand]): postable commands.
        """
        # if list of postable commands is _not_ already created
        # make the list and store in instance parameter
        if not self._postable_cmds:
            for pc in UI.PostableCommand.GetValues(UI.PostableCommand):
                try:
                    rcid = UI.RevitCommandId.LookupPostableCommandId(pc)
                    self._postable_cmds.append(
                        # wrap postable command info in custom namedtuple
                        _HostAppPostableCommand(name=safe_strtype(pc),
                                                key=rcid.Name,
                                                id=rcid.Id,
                                                rvtobj=rcid)
                        )
                except Exception:
                    # if any error occured when querying postable command
                    # or its info, pass silently
                    pass

        return self._postable_cmds

    def post_command(self, command_id):
        """Request Revit to run a command.

        Args:
            command_id (str): command identifier e.g. ID_REVIT_SAVE_AS_TEMPLATE
        """
        command_id = UI.RevitCommandId.LookupCommandId(command_id)
        self.uiapp.PostCommand(command_id)



try:
    # Create an intance of host application wrapper
    # making sure __revit__ is available
    HOST_APP = _HostApplication()
except Exception:
    raise Exception('Critical Error: Host software is not supported. '
                    '(__revit__ handle is not available)')


# -----------------------------------------------------------------------------
# Wrapper to access builtin parameters set in scope by C# Script Executor
# -----------------------------------------------------------------------------
class _ExecutorParams(object):
    """Private Wrapper that provides runtime environment info."""

    @property   # read-only
    def exec_id(self):
        """Return execution unique id."""
        try:
            return __execid__
        except NameError:
            pass

    @property   # read-only
    def exec_timestamp(self):
        """Return execution timestamp."""
        try:
            return __timestamp__
        except NameError:
            pass

    @property   # read-only
    def engine_id(self):
        """Return engine id."""
        try:
            return __cachedengineid__
        except NameError:
            pass

    @property   # read-only
    def engine_ver(self):
        """str: Return PyRevitLoader.ScriptExecutor hardcoded version."""
        if eng.ScriptExecutor:
            return eng.ScriptExecutor.EngineVersion

    @property  # read-only
    def cached_engine(self):
        """bool: Check whether pyrevit is running on a cached engine."""
        try:
            return __cachedengine__
        except NameError:
            return False

    @property  # read-only
    def first_load(self):
        """bool: Check whether pyrevit is not running in pyrevit command."""
        # if no output window is set by the executor, it means that pyRevit
        # is loading at Revit startup (not reloading)
        return True if self.window_handle is None else False

    @property   # read-only
    def script_runtime(self):
        """``PyRevitLabs.PyRevit.Runtime.ScriptRuntime``: Return command."""
        try:
            return __scriptruntime__
        except NameError:
            return None

    @property   # read-only
    def output_stream(self):
        """Return ScriptIO."""
        if self.script_runtime:
            return self.script_runtime.OutputStream

    @property   # read-only
    def script_data(self):
        """Return ScriptRuntime.ScriptData."""
        if self.script_runtime:
            return self.script_runtime.ScriptData

    @property   # read-only
    def script_runtime_cfgs(self):
        """Return ScriptRuntime.ScriptRuntimeConfigs."""
        if self.script_runtime:
            return self.script_runtime.ScriptRuntimeConfigs

    @property   # read-only
    def engine_cfgs(self):
        """Return ScriptRuntime.ScriptRuntimeConfigs."""
        if self.script_runtime:
            return self.script_runtime.EngineConfigs

    @property
    def command_mode(self):
        """bool: Check if pyrevit is running in pyrevit command context."""
        return self.script_runtime is not None

    @property
    def event_sender(self):
        """``Object``: Return event sender object."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.EventSender

    @property
    def event_args(self):
        """``DB.RevitAPIEventArgs``: Return event arguments object."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.EventArgs

    @property
    def event_doc(self):
        """``DB.Document``: Return document set in event args if available."""
        if self.event_args:
            if hasattr(self.event_args, 'Document'):
                return getattr(self.event_args, 'Document')
            elif hasattr(self.event_args, 'ActiveDocument'):
                return getattr(self.event_args, 'ActiveDocument')
            elif hasattr(self.event_args, 'CurrentDocument'):
                return getattr(self.event_args, 'CurrentDocument')
            elif hasattr(self.event_args, 'GetDocument'):
                return self.event_args.GetDocument()

    @property   # read-only
    def needs_refreshed_engine(self):
        """bool: Check if command needs a newly refreshed IronPython engine."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.RefreshEngine
        else:
            return False

    @property   # read-only
    def debug_mode(self):
        """bool: Check if command is in debug mode."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.DebugMode
        else:
            return False

    @property   # read-only
    def config_mode(self):
        """bool: Check if command is in config mode."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.ConfigMode
        else:
            return False

    @property   # read-only
    def executed_from_ui(self):
        """bool: Check if command was executed from ui."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.ExecutedFromUI
        else:
            return False

    @property   # read-only
    def needs_clean_engine(self):
        """bool: Check if command needs a clean IronPython engine."""
        if self.engine_cfgs:
            return self.engine_cfgs.CleanEngine
        else:
            return False

    @property   # read-only
    def needs_fullframe_engine(self):
        """bool: Check if command needs a full-frame IronPython engine."""
        if self.engine_cfgs:
            return self.engine_cfgs.FullFrameEngine
        else:
            return False

    @property   # read-only
    def needs_persistent_engine(self):
        """bool: Check if command needs a persistent IronPython engine."""
        if self.engine_cfgs:
            return self.engine_cfgs.PersistentEngine
        else:
            return False

    @property   # read
    def window_handle(self):
        """Output window handle."""
        if self.script_runtime:
            return self.script_runtime.OutputWindow

    @property
    def command_data(self):
        """ExternalCommandData: Return current command data."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.CommandData

    @property
    def command_elements(self):
        """``DB.ElementSet``: Return elements passed to by Revit."""
        if self.script_runtime_cfgs:
            return self.script_runtime_cfgs.SelectedElements

    @property   # read-only
    def command_path(self):
        """str: Return current command path."""
        if '__commandpath__' in __builtins__ \
                and __builtins__['__commandpath__']:
            return __builtins__['__commandpath__']
        elif self.script_runtime:
            return op.dirname(self.script_runtime.ScriptData.ScriptPath)

    @property   # read-only
    def command_config_path(self):
        """str: Return current command config script path."""
        if '__configcommandpath__' in __builtins__ \
                and __builtins__['__configcommandpath__']:
            return __builtins__['__configcommandpath__']
        elif self.script_runtime:
            return op.dirname(self.script_runtime.ScriptData.ConfigScriptPath)

    @property   # read-only
    def command_name(self):
        """str: Return current command name."""
        if '__commandname__' in __builtins__ \
                and __builtins__['__commandname__']:
            return __builtins__['__commandname__']
        elif self.script_runtime:
            return self.script_runtime.ScriptData.CommandName

    @property   # read-only
    def command_bundle(self):
        """str: Return current command bundle name."""
        if '__commandbundle__' in __builtins__ \
                and __builtins__['__commandbundle__']:
            return __builtins__['__commandbundle__']
        elif self.script_runtime:
            return self.script_runtime.ScriptData.CommandBundle

    @property   # read-only
    def command_extension(self):
        """str: Return current command extension name."""
        if '__commandextension__' in __builtins__ \
                and __builtins__['__commandextension__']:
            return __builtins__['__commandextension__']
        elif self.script_runtime:
            return self.script_runtime.ScriptData.CommandExtension

    @property   # read-only
    def command_uniqueid(self):
        """str: Return current command unique id."""
        if '__commanduniqueid__' in __builtins__ \
                and __builtins__['__commanduniqueid__']:
            return __builtins__['__commanduniqueid__']
        elif self.script_runtime:
            return self.script_runtime.ScriptData.CommandUniqueId

    @property   # read-only
    def command_controlid(self):
        """str: Return current command control id."""
        if '__commandcontrolid__' in __builtins__ \
                and __builtins__['__commandcontrolid__']:
            return __builtins__['__commandcontrolid__']
        elif self.script_runtime:
            return self.script_runtime.ScriptData.CommandControlId

    @property   # read-only
    def command_uibutton(self):
        """str: Return current command ui button."""
        if '__uibutton__' in __builtins__ \
                and __builtins__['__uibutton__']:
            return __builtins__['__uibutton__']

    @property
    def doc_mode(self):
        """bool: Check if pyrevit is running by doc generator."""
        try:
            return __sphinx__
        except NameError:
            return False

    @property
    def result_dict(self):
        """``Dictionary<String, String>``: Return results dict for logging."""
        if self.script_runtime:
            return self.script_runtime.GetResultsDictionary()


# create an instance of _ExecutorParams wrapping current runtime.
EXEC_PARAMS = _ExecutorParams()


# -----------------------------------------------------------------------------
# type to safely get document instance from app or event args
# -----------------------------------------------------------------------------

class _DocsGetter(object):
    """Instance to safely get document from HOST_APP instance or EXEC_PARAMS."""

    @property
    def doc(self):
        """Active document."""
        return HOST_APP.doc or EXEC_PARAMS.event_doc

    @property
    def docs(self):
        """List of active documents."""
        return HOST_APP.docs


DOCS = _DocsGetter()

# -----------------------------------------------------------------------------
# config user environment paths
# -----------------------------------------------------------------------------
# user env paths
if EXEC_PARAMS.doc_mode:
    ALLUSER_PROGRAMDATA = USER_ROAMING_DIR = USER_SYS_TEMP = USER_DESKTOP = \
    EXTENSIONS_DEFAULT_DIR = THIRDPARTY_EXTENSIONS_DEFAULT_DIR = ' '
else:
    ALLUSER_PROGRAMDATA = os.getenv('programdata')
    USER_ROAMING_DIR = os.getenv('appdata')
    USER_SYS_TEMP = os.getenv('temp')
    USER_DESKTOP = op.expandvars('%userprofile%\\desktop')

    # verify directory per issue #369
    if not USER_DESKTOP or not op.exists(USER_DESKTOP):
        USER_DESKTOP = USER_SYS_TEMP

    # default extensions directory
    EXTENSIONS_DEFAULT_DIR = op.join(HOME_DIR, 'extensions')
    THIRDPARTY_EXTENSIONS_DEFAULT_DIR = \
        op.join(USER_ROAMING_DIR, PYREVIT_ADDON_NAME, 'Extensions')

# create paths for pyrevit files
if EXEC_PARAMS.doc_mode:
    PYREVIT_ALLUSER_APP_DIR = PYREVIT_APP_DIR = PYREVIT_VERSION_APP_DIR = ' '
else:
    # pyrevit file directory
    PYREVIT_ALLUSER_APP_DIR = op.join(ALLUSER_PROGRAMDATA, PYREVIT_ADDON_NAME)
    PYREVIT_APP_DIR = op.join(USER_ROAMING_DIR, PYREVIT_ADDON_NAME)
    PYREVIT_VERSION_APP_DIR = op.join(PYREVIT_APP_DIR, HOST_APP.version)

    # add runtime paths to sys.paths
    # this will allow importing any dynamically compiled DLLs that
    # would be placed under this paths.
    for pyrvt_app_dir in [PYREVIT_APP_DIR,
                          PYREVIT_VERSION_APP_DIR,
                          THIRDPARTY_EXTENSIONS_DEFAULT_DIR]:
        if not op.isdir(pyrvt_app_dir):
            try:
                os.mkdir(pyrvt_app_dir)
                sys.path.append(pyrvt_app_dir)
            except Exception as err:
                raise PyRevitException('Can not access pyRevit '
                                       'folder at: {} | {}'
                                       .format(pyrvt_app_dir, err))
        else:
            sys.path.append(pyrvt_app_dir)


# -----------------------------------------------------------------------------
# standard prefixes for naming pyrevit files (config, appdata and temp files)
# -----------------------------------------------------------------------------
if EXEC_PARAMS.doc_mode:
    PYREVIT_FILE_PREFIX_UNIVERSAL = PYREVIT_FILE_PREFIX = \
        PYREVIT_FILE_PREFIX_STAMPED = None
    PYREVIT_FILE_PREFIX_UNIVERSAL_USER = PYREVIT_FILE_PREFIX_USER = \
        PYREVIT_FILE_PREFIX_STAMPED_USER = None
else:
    # e.g. pyRevit_
    PYREVIT_FILE_PREFIX_UNIVERSAL = '{}_'.format(PYREVIT_ADDON_NAME)
    PYREVIT_FILE_PREFIX_UNIVERSAL_REGEX = \
        r'^' + PYREVIT_ADDON_NAME + r'_(?P<fname>.+)'

    # e.g. pyRevit_2018_
    PYREVIT_FILE_PREFIX = '{}_{}_'.format(PYREVIT_ADDON_NAME,
                                          HOST_APP.version)
    PYREVIT_FILE_PREFIX_REGEX = \
        r'^' + PYREVIT_ADDON_NAME + r'_(?P<version>\d{4})_(?P<fname>.+)'

    # e.g. pyRevit_2018_14422_
    PYREVIT_FILE_PREFIX_STAMPED = '{}_{}_{}_'.format(PYREVIT_ADDON_NAME,
                                                     HOST_APP.version,
                                                     HOST_APP.proc_id)
    PYREVIT_FILE_PREFIX_STAMPED_REGEX = \
        r'^' + PYREVIT_ADDON_NAME \
        + r'_(?P<version>\d{4})_(?P<pid>\d+)_(?P<fname>.+)'

    # e.g. pyRevit_eirannejad_
    PYREVIT_FILE_PREFIX_UNIVERSAL_USER = '{}_{}_'.format(PYREVIT_ADDON_NAME,
                                                         HOST_APP.username)
    PYREVIT_FILE_PREFIX_UNIVERSAL_USER_REGEX = \
        r'^' + PYREVIT_ADDON_NAME + r'_(?P<user>.+)_(?P<fname>.+)'

    # e.g. pyRevit_2018_eirannejad_
    PYREVIT_FILE_PREFIX_USER = '{}_{}_{}_'.format(PYREVIT_ADDON_NAME,
                                                  HOST_APP.version,
                                                  HOST_APP.username)
    PYREVIT_FILE_PREFIX_USER_REGEX = \
        r'^' + PYREVIT_ADDON_NAME \
        + r'_(?P<version>\d{4})_(?P<user>.+)_(?P<fname>.+)'

    # e.g. pyRevit_2018_eirannejad_14422_
    PYREVIT_FILE_PREFIX_STAMPED_USER = '{}_{}_{}_{}_'.format(PYREVIT_ADDON_NAME,
                                                             HOST_APP.version,
                                                             HOST_APP.username,
                                                             HOST_APP.proc_id)
    PYREVIT_FILE_PREFIX_STAMPED_USER_REGEX = \
        r'^' + PYREVIT_ADDON_NAME \
        + r'_(?P<version>\d{4})_(?P<user>.+)_(?P<pid>\d+)_(?P<fname>.+)'

# -----------------------------------------------------------------------------
# config labs modules
# -----------------------------------------------------------------------------
from pyrevit import labs


"""Provide access to DotNet Framework.

Examples:
    ```python
    from pyrevit.framework import Assembly, Windows
    ```
"""

#pylint: disable=W0703,C0302,C0103,W0614,E0401,W0611,C0413,ungrouped-imports
import os.path as op
from pyrevit.compat import PY3, PY2

import clr
import System


# netcore init
if int(__revit__.Application.VersionNumber) >= 2025:
    clr.AddReference('System.Runtime')
    clr.AddReference('System.Text.RegularExpressions')
    clr.AddReference('System.Diagnostics.Process')
    clr.AddReference('System.IO.FileSystem.DriveInfo')
    clr.AddReference('System.Net.WebClient')
    clr.AddReference('System.Net.Requests')
    clr.AddReference('System.Net.WebProxy')
    clr.AddReference('System.Runtime.Serialization.Formatters')
    clr.AddReference('System.Reflection.Emit')
    clr.AddReference('Lokad.ILPack')
    clr.AddReference('System.ComponentModel')
    clr.AddReference('System.ObjectModel')
    clr.AddReference('System.Diagnostics.FileVersionInfo')

clr.AddReference('System.Core')
clr.AddReference('System.Management')
clr.AddReference('System.Windows.Forms')
clr.AddReference('System.Drawing')
clr.AddReference('PresentationCore')
clr.AddReference('PresentationFramework')
clr.AddReference('System.Xml.Linq')
clr.AddReference('WindowsBase')

# add linq extensions?
if PY2:
    clr.ImportExtensions(System.Linq)

from System import AppDomain, Version
from System import Type
from System import Uri, UriKind, Guid
from System import EventHandler
from System import Array, IntPtr, Enum, Byte
from System import Convert
from System.Text import Encoding
from System.Text.RegularExpressions import Regex
from System.Collections import ObjectModel
from System.Collections.ObjectModel import ObservableCollection
from System.Collections import IEnumerator, IEnumerable
from System.Collections.Generic import List, Dictionary
from System.Collections.Generic import IList, IDictionary
from System.Collections.Generic import KeyValuePair
from System import DateTime, DateTimeOffset

from System import Diagnostics
from System.Diagnostics import Process
from System.Diagnostics import Stopwatch

from System import Reflection
from System.Reflection import Assembly, AssemblyName
from System.Reflection import TypeAttributes, MethodAttributes
from System.Reflection import CallingConventions
from System.Reflection import BindingFlags
from System.Reflection.Emit import AssemblyBuilderAccess
from System.Reflection.Emit import CustomAttributeBuilder, OpCodes

from System import IO
from System.IO import IOException, DriveInfo, Path, StringReader, File

from System import Net
from System.Net import WebClient, WebRequest, WebProxy

from System import ComponentModel
from System import Drawing
from System import Windows
from System.Windows import Forms
from System.Windows.Forms import Clipboard
from System.Windows import Controls
from System.Windows import Documents
from System.Windows import Media
from System.Windows import Threading
from System.Windows import Interop
from System.Windows import Input
from System.Windows import Data
from System.Windows import ResourceDictionary
from System.Windows.Media import Imaging, SolidColorBrush, Color
from System import Math
from System.Management import ManagementObjectSearcher
from System.Runtime.Serialization import FormatterServices
from System.Linq import Enumerable

import pyrevit.engine as eng


ASSEMBLY_FILE_TYPE = 'dll'
ASSEMBLY_FILE_EXT = '.dll'


ipy_assmname = '{prefix}IronPython'.format(prefix=eng.EnginePrefix)
ipy_dllpath = op.join(eng.EnginePath, ipy_assmname + ASSEMBLY_FILE_EXT)
if PY3:
    clr.AddReference(ipy_dllpath)
else:
    clr.AddReferenceToFileAndPath(ipy_dllpath)

import IronPython

# WPF
wpf = None
wpf_assmname = '{prefix}IronPython.Wpf'.format(prefix=eng.EnginePrefix)
wpf_dllpath = op.join(eng.EnginePath, wpf_assmname + ASSEMBLY_FILE_EXT)
try:
    clr.AddReference(wpf_assmname)
    if PY3:
        wpf = IronPython.Modules.Wpf
    else:
        import wpf
except Exception:
    clr.AddReferenceToFileAndPath(wpf_dllpath)
    import wpf


# SQLite
sqlite3 = None
sqlite3_assmname = '{prefix}IronPython.SQLite'.format(prefix=eng.EnginePrefix)
sqlite3_dllpath = op.join(eng.EnginePath, sqlite3_assmname + ASSEMBLY_FILE_EXT)
try:
    clr.AddReference(sqlite3_assmname)
    if PY3:
        sqlite3 = IronPython.SQLite
    else:
        import sqlite3
except Exception:
    clr.AddReferenceToFileAndPath(sqlite3_dllpath)
    import sqlite3


CPDialogs = None
try:
    clr.AddReference('Microsoft.WindowsAPICodePack')
    clr.AddReference('Microsoft.WindowsAPICodePack.Shell')
    import Microsoft.WindowsAPICodePack.Dialogs as CPDialogs #pylint: disable=ungrouped-imports
except Exception:
    pass


# try loading some utility modules shipped with revit
NSJson = None
try:
    clr.AddReference('pyRevitLabs.Json')
    import pyRevitLabs.Json as NSJson
except Exception:
    pass

clr.AddReference('pyRevitLabs.Emojis')
import pyRevitLabs.Emojis as Emojis


# do not import anything from pyrevit before this
from pyrevit import BIN_DIR


def get_type(fw_object):
    """Return CLR type of an object."""
    return clr.GetClrType(fw_object)


def get_dll_file(assembly_name):
    """Return path to given assembly name."""
    addin_file = op.join(BIN_DIR, assembly_name + '.dll')
    if op.exists(addin_file):
        return addin_file


def get_current_thread_id():
    """Return manageed thread id of current thread."""
    return System.Threading.Thread.CurrentThread.ManagedThreadId

"""Handle reading and parsing, writin and saving of all user configurations.

This module handles the reading and writing of the pyRevit configuration files.
It's been used extensively by pyRevit sub-modules. user_config is
set up automatically in the global scope by this module and can be imported
into scripts and other modules to access the default configurations.

All other modules use this module to query user config.

Examples:
    ```python
    from pyrevit.userconfig import user_config
    user_config.add_section('newsection')
    user_config.newsection.property = value
    user_config.newsection.get_option('property', default_value)
    user_config.save_changes()
    ```


The user_config object is also the destination for reading and writing
configuration by pyRevit scripts through :func:`get_config` of
:mod:`pyrevit.script` module. Here is the function source:

.. literalinclude:: ../../pyrevitlib/pyrevit/script.py
    :pyobject: get_config

Examples:
    ```python
    from pyrevit import script
    cfg = script.get_config()
    cfg.property = value
    cfg.get_option('property', default_value)
    script.save_config()
    ```
"""
#pylint: disable=C0103,C0413,W0703
import os
import os.path as op

from pyrevit import EXEC_PARAMS, HOME_DIR, HOST_APP, IS_DOTNET_CORE
from pyrevit import PyRevitException
from pyrevit import EXTENSIONS_DEFAULT_DIR, THIRDPARTY_EXTENSIONS_DEFAULT_DIR
from pyrevit import PYREVIT_ALLUSER_APP_DIR, PYREVIT_APP_DIR
from pyrevit.compat import winreg as wr

from pyrevit.labs import PyRevit

from pyrevit import coreutils
from pyrevit.coreutils import appdata
from pyrevit.coreutils import configparser
from pyrevit.coreutils import logger
from pyrevit.versionmgr import upgrade


DEFAULT_CSV_SEPARATOR = ','


mlogger = logger.get_logger(__name__)


CONSTS = PyRevit.PyRevitConsts


class PyRevitConfig(configparser.PyRevitConfigParser):
    """Provide read/write access to pyRevit configuration.

    Args:
        cfg_file_path (str): full path to config file to be used.
        config_type (str): type of config file

    Examples:
        ```python
        cfg = PyRevitConfig(cfg_file_path)
        cfg.add_section('sectionname')
        cfg.sectionname.property = value
        cfg.sectionname.get_option('property', default_value)
        cfg.save_changes()
        ```
    """

    def __init__(self, cfg_file_path=None, config_type='Unknown'):
        """Load settings from provided config file and setup parser."""
        # try opening and reading config file in order.
        super(PyRevitConfig, self).__init__(cfg_file_path=cfg_file_path)

        # set log mode on the logger module based on
        # user settings (overriding the defaults)
        self._update_env()
        self._admin = config_type == 'Admin'
        self.config_type = config_type

    def _update_env(self):
        # update the debug level based on user config
        mlogger.reset_level()

        try:
            # first check to see if command is not in forced debug mode
            if not EXEC_PARAMS.debug_mode:
                if self.core.debug:
                    mlogger.set_debug_mode()
                    mlogger.debug('Debug mode is enabled in user settings.')
                elif self.core.verbose:
                    mlogger.set_verbose_mode()

            logger.set_file_logging(self.core.filelogging)
        except Exception as env_update_err:
            mlogger.debug('Error updating env variable per user config. | %s',
                          env_update_err)

    @property
    def config_file(self):
        """Current config file path."""
        return self._cfg_file_path

    @property
    def environment(self):
        """Environment section."""
        if not self.has_section(CONSTS.EnvConfigsSectionName):
            self.add_section(CONSTS.EnvConfigsSectionName)
        return self.get_section(CONSTS.EnvConfigsSectionName)

    @property
    def core(self):
        """Core section."""
        if not self.has_section(CONSTS.ConfigsCoreSection):
            self.add_section(CONSTS.ConfigsCoreSection)
        return self.get_section(CONSTS.ConfigsCoreSection)

    @property
    def routes(self):
        """Routes section."""
        if not self.has_section(CONSTS.ConfigsRoutesSection):
            self.add_section(CONSTS.ConfigsRoutesSection)
        return self.get_section(CONSTS.ConfigsRoutesSection)

    @property
    def telemetry(self):
        """Telemetry section."""
        if not self.has_section(CONSTS.ConfigsTelemetrySection):
            self.add_section(CONSTS.ConfigsTelemetrySection)
        return self.get_section(CONSTS.ConfigsTelemetrySection)

    @property
    def bin_cache(self):
        """"Whether to use the cache for extensions."""
        return self.core.get_option(
            CONSTS.ConfigsBinaryCacheKey,
            default_value=CONSTS.ConfigsBinaryCacheDefault,
        )

    @bin_cache.setter
    def bin_cache(self, state):
        self.core.set_option(
            CONSTS.ConfigsBinaryCacheKey,
            value=state
        )

    @property
    def check_updates(self):
        """Whether to check for updates."""
        return self.core.get_option(
            CONSTS.ConfigsCheckUpdatesKey,
            default_value=CONSTS.ConfigsCheckUpdatesDefault,
        )

    @check_updates.setter
    def check_updates(self, state):
        self.core.set_option(
            CONSTS.ConfigsCheckUpdatesKey,
            value=state
        )

    @property
    def auto_update(self):
        """Whether to automatically update pyRevit."""
        return self.core.get_option(
            CONSTS.ConfigsAutoUpdateKey,
            default_value=CONSTS.ConfigsAutoUpdateDefault,
        )

    @auto_update.setter
    def auto_update(self, state):
        self.core.set_option(
            CONSTS.ConfigsAutoUpdateKey,
            value=state
        )

    @property
    def rocket_mode(self):
        """Whether to enable rocket mode."""
        return self.core.get_option(
            CONSTS.ConfigsRocketModeKey,
            default_value=CONSTS.ConfigsRocketModeDefault,
        )

    @rocket_mode.setter
    def rocket_mode(self, state):
        self.core.set_option(
            CONSTS.ConfigsRocketModeKey,
            value=state
        )

    @property
    def log_level(self):
        """Logging level."""
        if self.core.get_option(
                CONSTS.ConfigsDebugKey,
                default_value=CONSTS.ConfigsDebugDefault,
            ):
            return PyRevit.PyRevitLogLevels.Debug
        elif self.core.get_option(
                CONSTS.ConfigsVerboseKey,
                default_value=CONSTS.ConfigsVerboseDefault,
            ):
            return PyRevit.PyRevitLogLevels.Verbose
        return PyRevit.PyRevitLogLevels.Quiet

    @log_level.setter
    def log_level(self, state):
        if state == PyRevit.PyRevitLogLevels.Debug:
            self.core.set_option(CONSTS.ConfigsDebugKey, True)
            self.core.set_option(CONSTS.ConfigsVerboseKey, True)
        elif state == PyRevit.PyRevitLogLevels.Verbose:
            self.core.set_option(CONSTS.ConfigsDebugKey, False)
            self.core.set_option(CONSTS.ConfigsVerboseKey, True)
        else:
            self.core.set_option(CONSTS.ConfigsDebugKey, False)
            self.core.set_option(CONSTS.ConfigsVerboseKey, False)

    @property
    def file_logging(self):
        """Whether to enable file logging."""
        return self.core.get_option(
            CONSTS.ConfigsFileLoggingKey,
            default_value=CONSTS.ConfigsFileLoggingDefault,
        )

    @file_logging.setter
    def file_logging(self, state):
        self.core.set_option(
            CONSTS.ConfigsFileLoggingKey,
            value=state
        )

    @property
    def startuplog_timeout(self):
        """Timeout for the startup log."""
        return self.core.get_option(
            CONSTS.ConfigsStartupLogTimeoutKey,
            default_value=CONSTS.ConfigsStartupLogTimeoutDefault,
        )

    @startuplog_timeout.setter
    def startuplog_timeout(self, timeout):
        self.core.set_option(
            CONSTS.ConfigsStartupLogTimeoutKey,
            value=timeout
        )

    @property
    def required_host_build(self):
        """Host build required to run the commands."""
        return self.core.get_option(
            CONSTS.ConfigsRequiredHostBuildKey,
            default_value="",
        )

    @required_host_build.setter
    def required_host_build(self, buildnumber):
        self.core.set_option(
            CONSTS.ConfigsRequiredHostBuildKey,
            value=buildnumber
        )

    @property
    def min_host_drivefreespace(self):
        """Minimum free space for running the commands."""
        return self.core.get_option(
            CONSTS.ConfigsMinDriveSpaceKey,
            default_value=CONSTS.ConfigsMinDriveSpaceDefault,
        )

    @min_host_drivefreespace.setter
    def min_host_drivefreespace(self, freespace):
        self.core.set_option(
            CONSTS.ConfigsMinDriveSpaceKey,
            value=freespace
        )

    @property
    def load_beta(self):
        """Whether to load commands in beta."""
        return self.core.get_option(
            CONSTS.ConfigsLoadBetaKey,
            default_value=CONSTS.ConfigsLoadBetaDefault,
        )

    @load_beta.setter
    def load_beta(self, state):
        self.core.set_option(
            CONSTS.ConfigsLoadBetaKey,
            value=state
        )

    @property
    def cpython_engine_version(self):
        """CPython engine version to use."""
        return self.core.get_option(
            CONSTS.ConfigsCPythonEngineKey,
            default_value=CONSTS.ConfigsCPythonEngineDefault,
        )

    @cpython_engine_version.setter
    def cpython_engine_version(self, version):
        self.core.set_option(
            CONSTS.ConfigsCPythonEngineKey,
            value=int(version)
        )

    @property
    def user_locale(self):
        """User locale."""
        return self.core.get_option(
            CONSTS.ConfigsLocaleKey,
            default_value="",
        )

    @user_locale.setter
    def user_locale(self, local_code):
        self.core.set_option(
            CONSTS.ConfigsLocaleKey,
            value=local_code
        )

    @property
    def output_stylesheet(self):
        """Stylesheet used for output."""
        return self.core.get_option(
            CONSTS.ConfigsOutputStyleSheet,
            default_value="",
        )

    @output_stylesheet.setter
    def output_stylesheet(self, stylesheet_filepath):
        if stylesheet_filepath:
            self.core.set_option(
                CONSTS.ConfigsOutputStyleSheet,
                value=stylesheet_filepath
            )
        else:
            self.core.remove_option(CONSTS.ConfigsOutputStyleSheet)

    @property
    def routes_host(self):
        """Routes API host."""
        return self.routes.get_option(
            CONSTS.ConfigsRoutesHostKey,
            default_value=CONSTS.ConfigsRoutesHostDefault,
        )

    @routes_host.setter
    def routes_host(self, routes_host):
        self.routes.set_option(
            CONSTS.ConfigsRoutesHostKey,
            value=routes_host
        )

    @property
    def routes_port(self):
        """API routes port."""
        return self.routes.get_option(
            CONSTS.ConfigsRoutesPortKey,
            default_value=CONSTS.ConfigsRoutesPortDefault,
        )

    @routes_port.setter
    def routes_port(self, port):
        self.routes.set_option(
            CONSTS.ConfigsRoutesPortKey,
            value=port
        )

    @property
    def load_core_api(self):
        """Whether to load pyRevit core api."""
        return self.routes.get_option(
            CONSTS.ConfigsLoadCoreAPIKey,
            default_value=CONSTS.ConfigsConfigsLoadCoreAPIDefault,
        )

    @load_core_api.setter
    def load_core_api(self, state):
        self.routes.set_option(
            CONSTS.ConfigsLoadCoreAPIKey,
            value=state
        )

    @property
    def telemetry_utc_timestamp(self):
        """Whether to use UTC timestamps in telemetry."""
        return self.telemetry.get_option(
            CONSTS.ConfigsTelemetryUTCTimestampsKey,
            default_value=CONSTS.ConfigsTelemetryUTCTimestampsDefault,
        )

    @telemetry_utc_timestamp.setter
    def telemetry_utc_timestamp(self, state):
        self.telemetry.set_option(
            CONSTS.ConfigsTelemetryUTCTimestampsKey,
            value=state
        )

    @property
    def telemetry_status(self):
        """Telemetry status."""
        return self.telemetry.get_option(
            CONSTS.ConfigsTelemetryStatusKey,
            default_value=CONSTS.ConfigsTelemetryStatusDefault,
        )

    @telemetry_status.setter
    def telemetry_status(self, state):
        self.telemetry.set_option(
            CONSTS.ConfigsTelemetryStatusKey,
            value=state
        )

    @property
    def telemetry_file_dir(self):
        """Telemetry file directory."""
        return self.telemetry.get_option(
            CONSTS.ConfigsTelemetryFileDirKey,
            default_value="",
        )

    @telemetry_file_dir.setter
    def telemetry_file_dir(self, filepath):
        self.telemetry.set_option(
            CONSTS.ConfigsTelemetryFileDirKey,
            value=filepath
        )

    @property
    def telemetry_server_url(self):
        """Telemetry server URL."""
        return self.telemetry.get_option(
            CONSTS.ConfigsTelemetryServerUrlKey,
            default_value="",
        )

    @telemetry_server_url.setter
    def telemetry_server_url(self, server_url):
        self.telemetry.set_option(
            CONSTS.ConfigsTelemetryServerUrlKey,
            value=server_url
        )

    @property
    def telemetry_include_hooks(self):
        """Whether to include hooks in telemetry."""
        return self.telemetry.get_option(
            CONSTS.ConfigsTelemetryIncludeHooksKey,
            default_value=CONSTS.ConfigsTelemetryIncludeHooksDefault,
        )

    @telemetry_include_hooks.setter
    def telemetry_include_hooks(self, state):
        self.telemetry.set_option(
            CONSTS.ConfigsTelemetryIncludeHooksKey,
            value=state
        )

    @property
    def apptelemetry_status(self):
        """Telemetry status."""
        return self.telemetry.get_option(
            CONSTS.ConfigsAppTelemetryStatusKey,
            default_value=CONSTS.ConfigsAppTelemetryStatusDefault,
        )

    @apptelemetry_status.setter
    def apptelemetry_status(self, state):
        self.telemetry.set_option(
            CONSTS.ConfigsAppTelemetryStatusKey,
            value=state
        )

    @property
    def apptelemetry_server_url(self):
        """App telemetry server URL."""
        return self.telemetry.get_option(
            CONSTS.ConfigsAppTelemetryServerUrlKey,
            default_value="",
        )

    @apptelemetry_server_url.setter
    def apptelemetry_server_url(self, server_url):
        self.telemetry.set_option(
            CONSTS.ConfigsAppTelemetryServerUrlKey,
            value=server_url
        )

    @property
    def apptelemetry_event_flags(self):
        """Telemetry event flags."""
        return self.telemetry.get_option(
            CONSTS.ConfigsAppTelemetryEventFlagsKey,
            default_value="",
        )

    @apptelemetry_event_flags.setter
    def apptelemetry_event_flags(self, flags):
        self.telemetry.set_option(
            CONSTS.ConfigsAppTelemetryEventFlagsKey,
            value=flags
        )

    @property
    def user_can_update(self):
        """Whether the user can update pyRevit repos."""
        return self.core.get_option(
            CONSTS.ConfigsUserCanUpdateKey,
            default_value=CONSTS.ConfigsUserCanUpdateDefault,
        )

    @user_can_update.setter
    def user_can_update(self, state):
        self.core.set_option(
            CONSTS.ConfigsUserCanUpdateKey,
            value=state
        )

    @property
    def user_can_extend(self):
        """Whether the user can manage pyRevit Extensions."""
        return self.core.get_option(
            CONSTS.ConfigsUserCanExtendKey,
            default_value=CONSTS.ConfigsUserCanExtendDefault,
        )

    @user_can_extend.setter
    def user_can_extend(self, state):
        self.core.set_option(
            CONSTS.ConfigsUserCanExtendKey,
            value=state
        )

    @property
    def user_can_config(self):
        """Whether the user can access the configuration."""
        return self.core.get_option(
            CONSTS.ConfigsUserCanConfigKey,
            default_value=CONSTS.ConfigsUserCanConfigDefault,
        )

    @user_can_config.setter
    def user_can_config(self, state):
        self.core.set_option(
            CONSTS.ConfigsUserCanConfigKey,
            value=state
        )

    @property
    def colorize_docs(self):
        """Whether to enable the document colorizer."""
        return self.core.get_option(
            CONSTS.ConfigsColorizeDocsKey,
            default_value=CONSTS.ConfigsColorizeDocsDefault,
        )

    @colorize_docs.setter
    def colorize_docs(self, state):
        self.core.set_option(
            CONSTS.ConfigsColorizeDocsKey,
            value=state
        )

    @property
    def tooltip_debug_info(self):
        """Whether to append debug info on tooltips."""
        return self.core.get_option(
            CONSTS.ConfigsAppendTooltipExKey,
            default_value=CONSTS.ConfigsAppendTooltipExDefault,
        )

    @tooltip_debug_info.setter
    def tooltip_debug_info(self, state):
        self.core.set_option(
            CONSTS.ConfigsAppendTooltipExKey,
            value=state
        )

    @property
    def routes_server(self):
        """Whether the server routes are enabled."""
        return self.routes.get_option(
            CONSTS.ConfigsRoutesServerKey,
            default_value=CONSTS.ConfigsRoutesServerDefault,
        )

    @routes_server.setter
    def routes_server(self, state):
        self.routes.set_option(
            CONSTS.ConfigsRoutesServerKey,
            value=state
        )

    @property
    def respect_language_direction(self):
        """Whether the system respects the language direction."""
        return False

    @respect_language_direction.setter
    def respect_language_direction(self, state):
        pass

    def get_config_version(self):
        """Return version of config file used for change detection.

        Returns:
            (str): hash of the config file
        """
        return self.get_config_file_hash()

    def get_thirdparty_ext_root_dirs(self, include_default=True):
        """Return a list of external extension directories set by the user.

        Returns:
            (list[str]): External user extension directories.
        """
        dir_list = set()
        if include_default:
            # add default ext path
            dir_list.add(THIRDPARTY_EXTENSIONS_DEFAULT_DIR)
        try:
            dir_list.update([
                op.expandvars(op.normpath(x))
                for x in self.core.get_option(
                    CONSTS.ConfigsUserExtensionsKey,
                    default_value=[]
                )])
        except Exception as read_err:
            mlogger.error('Error reading list of user extension folders. | %s',
                          read_err)

        return [x for x in dir_list if op.exists(x)]

    def get_ext_root_dirs(self):
        """Return a list of all extension directories.

        Returns:
            (list[str]): user extension directories.

        """
        dir_list = set()
        if op.exists(EXTENSIONS_DEFAULT_DIR):
            dir_list.add(EXTENSIONS_DEFAULT_DIR)
        dir_list.update(self.get_thirdparty_ext_root_dirs())
        return list(dir_list)

    def get_ext_sources(self):
        """Return a list of extension definition source files."""
        ext_sources = self.environment.get_option(
            CONSTS.EnvConfigsExtensionLookupSourcesKey,
            default_value=[],
        )
        return list(set(ext_sources))

    def set_thirdparty_ext_root_dirs(self, path_list):
        """Updates list of external extension directories in config file.

        Args:
            path_list (list[str]): list of external extension paths
        """
        for ext_path in path_list:
            if not op.exists(ext_path):
                raise PyRevitException("Path \"%s\" does not exist." % ext_path)

        try:
            self.core.userextensions = \
                [op.normpath(x) for x in path_list]
        except Exception as write_err:
            mlogger.error('Error setting list of user extension folders. | %s',
                          write_err)

    def get_current_attachment(self):
        """Return current pyRevit attachment."""
        try:
            return PyRevit.PyRevitAttachments.GetAttached(int(HOST_APP.version))
        except PyRevitException as ex:
            mlogger.error('Error getting current attachment. | %s',
                          ex)

    def get_active_cpython_engine(self):
        """Return active cpython engine."""
        engines = []
        # try ot find attachment and get engines from the clone
        attachment = self.get_current_attachment()
        if attachment and attachment.Clone:
            engines = attachment.Clone.GetNetCoreEngines() if IS_DOTNET_CORE else attachment.Clone.GetNetFxEngines()
        # if can not find attachment, instantiate a temp clone
        else:
            try:
                clone = PyRevit.PyRevitClone(clonePath=HOME_DIR)
                engines = clone.GetNetCoreEngines() if IS_DOTNET_CORE else clone.GetNetFxEngines()
            except Exception as cEx:
                mlogger.debug('Can not create clone from path: %s', str(cEx))

        # find cpython engines
        cpy_engines_dict = {
            x.Version: x for x in engines
            if 'cpython' in x.KernelName.lower()
            }
        mlogger.debug('cpython engines dict: %s', cpy_engines_dict)

        if cpy_engines_dict:
            # grab cpython engine configured to be used by user
            try:
                cpyengine_ver = int(self.cpython_engine_version)
            except Exception:
                cpyengine_ver = 000

            try:
                return cpy_engines_dict[cpyengine_ver]
            except KeyError:
                # return the latest cpython engine
                return max(
                    cpy_engines_dict.values(), key=lambda x: x.Version.Version
                )
        else:
            mlogger.error('Can not determine cpython engines for '
                          'current attachment: %s', attachment)

    def set_active_cpython_engine(self, pyrevit_engine):
        """Set the active CPython engine.

        Args:
            pyrevit_engine (PyRevitEngine): python engine to set as active
        """
        self.cpython_engine_version = pyrevit_engine.Version

    @property
    def is_readonly(self):
        """bool: whether the config is read only."""
        return self._admin

    def save_changes(self):
        """Save user config into associated config file."""
        if not self._admin and self.config_file:
            try:
                super(PyRevitConfig, self).save()
            except Exception as save_err:
                mlogger.error('Can not save user config to: %s | %s',
                              self.config_file, save_err)

            # adjust environment per user configurations
            self._update_env()
        else:
            mlogger.debug('Config is in admin mode. Skipping save.')

    @staticmethod
    def get_list_separator():
        """Get list separator defined in user os regional settings."""
        intkey = coreutils.get_reg_key(wr.HKEY_CURRENT_USER,
                                       r'Control Panel\International')
        if intkey:
            try:
                return wr.QueryValueEx(intkey, 'sList')[0]
            except Exception:
                return DEFAULT_CSV_SEPARATOR


def find_config_file(target_path):
    """Find config file in target path."""
    return PyRevit.PyRevitConsts.FindConfigFileInDirectory(target_path)


def verify_configs(config_file_path=None):
    """Create a user settings file.

    if config_file_path is not provided, configs will be in memory only

    Args:
        config_file_path (str, optional): config file full name and path

    Returns:
        (pyrevit.userconfig.PyRevitConfig): pyRevit config file handler
    """
    if config_file_path:
        mlogger.debug('Creating default config file at: %s', config_file_path)
        coreutils.touch(config_file_path)

    try:
        parser = PyRevitConfig(cfg_file_path=config_file_path)
    except Exception as read_err:
        # can not create default user config file under appdata folder
        mlogger.warning('Can not create config file under: %s | %s',
                        config_file_path, read_err)
        parser = PyRevitConfig()

    return parser


LOCAL_CONFIG_FILE = ADMIN_CONFIG_FILE = USER_CONFIG_FILE = CONFIG_FILE = ''
user_config = None

# location for default pyRevit config files
if not EXEC_PARAMS.doc_mode:
    LOCAL_CONFIG_FILE = find_config_file(HOME_DIR)
    ADMIN_CONFIG_FILE = find_config_file(PYREVIT_ALLUSER_APP_DIR)
    USER_CONFIG_FILE = find_config_file(PYREVIT_APP_DIR)

    # decide which config file to use
    # check if a config file is inside the repo. for developers config override
    if LOCAL_CONFIG_FILE:
        CONFIG_TYPE = 'Local'
        CONFIG_FILE = LOCAL_CONFIG_FILE

    # check to see if there is any config file provided by admin
    elif ADMIN_CONFIG_FILE \
            and os.access(ADMIN_CONFIG_FILE, os.W_OK) \
            and not USER_CONFIG_FILE:
        # if yes, copy that and use as default
        # if admin config file is writable it means it is provided
        # to bootstrap the first pyRevit run
        try:
            CONFIG_TYPE = 'Seed'
            # make a local copy if one does not exist
            PyRevit.PyRevitConfigs.SetupConfig(ADMIN_CONFIG_FILE)
            CONFIG_FILE = find_config_file(PYREVIT_APP_DIR)
        except Exception as adminEx:
            # if init operation failed, make a new config file
            CONFIG_TYPE = 'New'
            # setup config file name and path
            CONFIG_FILE = appdata.get_universal_data_file(file_id='config',
                                                          file_ext='ini')
            mlogger.warning(
                'Failed to initialize config from seed file at %s\n'
                'Using default config file',
                ADMIN_CONFIG_FILE
            )

    # unless it's locked. then read that config file and set admin-mode
    elif ADMIN_CONFIG_FILE \
            and not os.access(ADMIN_CONFIG_FILE, os.W_OK):
        CONFIG_TYPE = 'Admin'
        CONFIG_FILE = ADMIN_CONFIG_FILE

    # if a config file is available for user use that
    elif USER_CONFIG_FILE:
        CONFIG_TYPE = 'User'
        CONFIG_FILE = USER_CONFIG_FILE

    # if nothing can be found, make a new one
    else:
        CONFIG_TYPE = 'New'
        # setup config file name and path
        CONFIG_FILE = appdata.get_universal_data_file(file_id='config',
                                                      file_ext='ini')

    mlogger.debug('Using %s config file: %s', CONFIG_TYPE, CONFIG_FILE)

    # read config, or setup default config file if not available
    # this pushes reading settings at first import of this module.
    try:
        verify_configs(CONFIG_FILE)
        user_config = PyRevitConfig(cfg_file_path=CONFIG_FILE,
                                    config_type=CONFIG_TYPE)
        upgrade.upgrade_user_config(user_config)
        user_config.save_changes()
    except Exception as cfg_err:
        mlogger.debug('Can not read confing file at: %s | %s',
                      CONFIG_FILE, cfg_err)
        mlogger.debug('Using configs in memory...')
        user_config = verify_configs()

"""Misc Helper functions for pyRevit.

Examples:
    ```python
    from pyrevit import coreutils
    coreutils.cleanup_string('some string')
    ```
"""
#pylint: disable=invalid-name
import os
import os.path as op
import re
import ast
import hashlib
import time
import datetime
import shutil
import random
import stat
import codecs
import math
import socket
from collections import defaultdict

#pylint: disable=E0401
from pyrevit import HOST_APP, PyRevitException
from pyrevit import compat
from pyrevit.compat import PY3, PY2
from pyrevit.compat import safe_strtype
from pyrevit.compat import winreg as wr
from pyrevit import framework
from pyrevit import api

# RE: https://github.com/eirannejad/pyRevit/issues/413
# import uuid
from System import Guid

#pylint: disable=W0703,C0302
DEFAULT_SEPARATOR = ';'

# extracted from
# https://www.fileformat.info/info/unicode/block/general_punctuation/images.htm
UNICODE_NONPRINTABLE_CHARS = [
    u'\u2000', u'\u2001', u'\u2002', u'\u2003', u'\u2004', u'\u2005', u'\u2006',
    u'\u2007', u'\u2008', u'\u2009', u'\u200A', u'\u200B', u'\u200C', u'\u200D',
    u'\u200E', u'\u200F',
    u'\u2028', u'\u2029', u'\u202A', u'\u202B', u'\u202C', u'\u202D', u'\u202E',
    u'\u202F',
    u'\u205F', u'\u2060',
    u'\u2066', u'\u2067', u'\u2068', u'\u2069', u'\u206A', u'\u206B', u'\u206C'
    u'\u206D', u'\u206E', u'\u206F'
    ]


class Timer(object):
    """Timer class using python native time module.

    Examples:
        ```python
        timer = Timer()
        timer.get_time()
        ```
        12
    """

    def __init__(self):
        """Initialize and Start Timer."""
        self.start = time.time()

    def restart(self):
        """Restart Timer."""
        self.start = time.time()

    def get_time(self):
        """Get Elapsed Time."""
        return time.time() - self.start


class ScriptFileParser(object):
    """Parse python script to extract variables and docstrings.

    Primarily designed to assist pyRevit in determining script configurations
    but can work for any python script.

    Examples:
        ```python
        finder = ScriptFileParser('/path/to/coreutils/__init__.py')
        finder.docstring()
        ```
        "Misc Helper functions for pyRevit."
        ```python
        finder.extract_param('SomeValue', [])
        ```
        []
    """

    def __init__(self, file_address):
        """Initialize and read provided python script.

        Args:
            file_address (str): python script file path
        """
        self.ast_tree = None
        self.file_addr = file_address
        with codecs.open(file_address, 'r', 'utf-8') as source_file:
            contents = source_file.read()
            if contents:
                self.ast_tree = ast.parse(contents)

    def extract_node_value(self, node):
        """Manual extraction of values from node."""
        if isinstance(node, ast.Assign):
            node_value = node.value
        else:
            node_value = node

        if isinstance(node_value, ast.Num):
            return node_value.n
        elif PY2 and isinstance(node_value, ast.Name):
            return node_value.id
        elif PY3 and isinstance(node_value, ast.NameConstant):
            return node_value.value
        elif isinstance(node_value, ast.Str):
            return node_value.s
        elif isinstance(node_value, ast.List):
            return node_value.elts
        elif isinstance(node_value, ast.Dict):
            return {self.extract_node_value(k):self.extract_node_value(v)
                    for k, v in zip(node_value.keys, node_value.values)}

    def get_docstring(self):
        """Get global docstring."""
        if self.ast_tree:
            doc_str = ast.get_docstring(self.ast_tree)
            if doc_str:
                return doc_str.decode('utf-8')

    def extract_param(self, param_name, default_value=None):
        """Find variable and extract its value.

        Args:
            param_name (str): variable name
            default_value (any):
                default value to be returned if variable does not exist

        Returns:
            (Any): value of the variable or None
        """
        if self.ast_tree:
            try:
                for node in ast.iter_child_nodes(self.ast_tree):
                    if isinstance(node, ast.Assign):
                        for target in node.targets:
                            if hasattr(target, 'id') \
                                    and target.id == param_name:
                                return ast.literal_eval(node.value)
            except Exception as err:
                raise PyRevitException('Error parsing parameter: {} '
                                       'in script file for : {} | {}'
                                       .format(param_name, self.file_addr, err))
        return default_value


class FileWatcher(object):
    """Simple file version watcher.

    This is a simple utility class to look for changes in a file based on
    its timestamp.

    Examples:
        ```
        watcher = FileWatcher('/path/to/file.ext')
        watcher.has_changed
        ```
        True
    """

    def __init__(self, filepath):
        """Initialize and read timestamp of provided file.

        Args:
            filepath (str): file path
        """
        self._cached_stamp = 0
        self._filepath = filepath
        self.update_tstamp()

    def update_tstamp(self):
        """Update the cached timestamp for later comparison."""
        self._cached_stamp = os.stat(self._filepath).st_mtime

    @property
    def has_changed(self):
        """Compare current file timestamp to the cached timestamp."""
        return os.stat(self._filepath).st_mtime != self._cached_stamp


class SafeDict(dict):
    """Dictionary that does not fail on any key.

    This is a dictionary subclass to help with string formatting with unknown
    key values.

    Examples:
        ```python
        string = '{target} {attr} is {color}.'
        safedict = SafeDict({'target': 'Apple',
                             'attr':   'Color'})
        string.format(safedict)  # will not fail with missing 'color' key
        ```
        'Apple Color is {color}.'
    """

    def __missing__(self, key):
        return '{' + key + '}'


def get_all_subclasses(parent_classes):
    """Return all subclasses of a python class.

    Args:
        parent_classes (list): list of python classes

    Returns:
        (list): list of python subclasses
    """
    sub_classes = []
    # if super-class, get a list of sub-classes.
    # Otherwise use component_class to create objects.
    for parent_class in parent_classes:
        try:
            derived_classes = parent_class.__subclasses__()
            if not derived_classes:
                sub_classes.append(parent_class)
            else:
                sub_classes.extend(derived_classes)
        except AttributeError:
            sub_classes.append(parent_class)
    return sub_classes


def get_sub_folders(search_folder):
    """Get a list of all subfolders directly inside provided folder.

    Args:
        search_folder (str): folder path

    Returns:
        (list[str]): list of subfolder names
    """
    sub_folders = []
    for sub_folder in os.listdir(search_folder):
        if op.isdir(op.join(search_folder, sub_folder)):
            sub_folders.append(sub_folder)
    return sub_folders


def verify_directory(folder):
    """Check if the folder exists and if not create the folder.

    Args:
        folder (str): path of folder to verify

    Returns:
        (str): path of verified folder, equals to provided folder

    Raises:
        OSError: on folder creation error.
    """
    if not op.exists(folder):
        try:
            os.makedirs(folder)
        except OSError as err:
            raise err
    return folder


def join_strings(str_list, separator=DEFAULT_SEPARATOR):
    """Join strings using provided separator.

    Args:
        str_list (list): list of string values
        separator (str): single separator character,
            defaults to DEFAULT_SEPARATOR

    Returns:
        (str): joined string
    """
    if str_list:
        if any(not isinstance(x, str) for x in str_list):
            str_list = [str(x) for x in str_list]
        return separator.join(str_list)
    return ''


# character replacement list for cleaning up file names
SPECIAL_CHARS = {' ': '',
                 '~': '',
                 '!': 'EXCLAM',
                 '@': 'AT',
                 '#': 'SHARP',
                 '$': 'DOLLAR',
                 '%': 'PERCENT',
                 '^': '',
                 '&': 'AND',
                 '*': 'STAR',
                 '+': 'PLUS',
                 ';': '', ':': '', ',': '', '\"': '',
                 '{': '', '}': '', '[': '', ']': '', r'\(': '', r'\)': '',
                 '-': 'MINUS',
                 '=': 'EQUALS',
                 '<': '', '>': '',
                 '?': 'QMARK',
                 '.': 'DOT',
                 '_': 'UNDERS',
                 '|': 'VERT',
                 r'\/': '', '\\': ''}


def cleanup_string(input_str, skip=None):
    """Replace special characters in string with another string.

    This function was created to help cleanup pyRevit command unique names from
    any special characters so C# class names can be created based on those
    unique names.

    ``coreutils.SPECIAL_CHARS`` is the conversion table for this function.

    Args:
        input_str (str): input string to be cleaned
        skip (Container[str]): special characters to keep

    Examples:
        ```python
        src_str = 'TEST@Some*<value>'
        cleanup_string(src_str)
        ```
        "TESTATSomeSTARvalue"
    """
    # remove spaces and special characters from strings
    for char, repl in SPECIAL_CHARS.items():
        if skip and char in skip:
            continue
        input_str = input_str.replace(char, repl)

    return input_str


def get_revit_instance_count():
    """Return number of open host app instances.

    Returns:
        (int): number of open host app instances.
    """
    return len(list(framework.Process.GetProcessesByName(HOST_APP.proc_name)))


def run_process(proc, cwd='C:'):
    """Run shell process silently.

    Args:
        proc (str): process executive name
        cwd (str): current working directory

    Exmaple:
        ```python
        run_process('notepad.exe', 'c:/')
        ```
    """
    import subprocess
    return subprocess.Popen(proc,
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                            cwd=cwd, shell=True)


def inspect_calling_scope_local_var(variable_name):
    """Trace back the stack to find the variable in the caller local stack.

    PyRevitLoader defines __revit__ in builtins and __window__ in locals.
    Thus, modules have access to __revit__ but not to __window__.
    This function is used to find __window__ in the caller stack.

    Args:
        variable_name (str): variable name to look up in caller local scope
    """
    import inspect

    frame = inspect.stack()[1][0]
    while variable_name not in frame.f_locals:
        frame = frame.f_back
        if frame is None:
            return None
    return frame.f_locals[variable_name]


def inspect_calling_scope_global_var(variable_name):
    """Trace back the stack to find the variable in the caller global stack.

    Args:
        variable_name (str): variable name to look up in caller global scope
    """
    import inspect

    frame = inspect.stack()[1][0]
    while variable_name not in frame.f_globals:
        frame = frame.f_back
        if frame is None:
            return None
    return frame.f_locals[variable_name]


def make_canonical_name(*args):
    """Join arguments with dot creating a unique id.

    Args:
        *args (str): Variable length argument list

    Returns:
        (str): dot separated unique name

    Examples:
        ```python
        make_canonical_name('somename', 'someid', 'txt')
        ```
        "somename.someid.txt"
    """
    return '.'.join(args)


def get_canonical_parts(canonical_string):
    """Splots argument using dot, returning all composing parts.

    Args:
        canonical_string (str): Source string e.g. "Config.SubConfig"

    Returns:
        (list[str]): list of composing parts

    Examples:
        ```python
        get_canonical_parts("Config.SubConfig")
        ```
        ['Config', 'SubConfig']
    """
    return canonical_string.split('.')


def get_file_name(file_path):
    """Return file basename of the given file.

    Args:
        file_path (str): file path
    """
    return op.splitext(op.basename(file_path))[0]


def get_str_hash(source_str):
    """Calculate hash value of given string.

    Current implementation uses :func:`hashlib.md5` hash function.

    Args:
        source_str (str): source str

    Returns:
        (str): hash value as string
    """
    return hashlib.md5(source_str.encode('utf-8', 'ignore')).hexdigest()


def calculate_dir_hash(dir_path, dir_filter, file_filter):
    r"""Create a unique hash to represent state of directory.

    Args:
        dir_path (str): target directory
        dir_filter (str): exclude directories matching this regex
        file_filter (str): exclude files matching this regex

    Returns:
        (str): hash value as string

    Examples:
        ```python
        calculate_dir_hash(source_path, '\.extension', '\.json')
        ```
        "1a885a0cae99f53d6088b9f7cee3bf4d"
    """
    mtime_sum = 0
    for root, dirs, files in os.walk(dir_path): #pylint: disable=W0612
        if re.search(dir_filter, op.basename(root), flags=re.IGNORECASE):
            mtime_sum += op.getmtime(root)
            for filename in files:
                if re.search(file_filter, filename, flags=re.IGNORECASE):
                    modtime = op.getmtime(op.join(root, filename))
                    mtime_sum += modtime
    return get_str_hash(safe_strtype(mtime_sum))


def prepare_html_str(input_string):
    """Reformat html string and prepare for pyRevit output window.

    pyRevit output window renders html content. But this means that < and >
    characters in outputs from python (e.g. <class at xxx>) will be treated
    as html tags. To avoid this, all <> characters that are defining
    html content need to be replaced with special phrases. pyRevit output
    later translates these phrases back in to < and >. That is how pyRevit
    distinquishes between <> printed from python and <> that define html.

    Args:
        input_string (str): input html string

    Examples:
        ```python
        prepare_html_str('<p>Some text</p>')
        ```
        "&clt;p&cgt;Some text&clt;/p&cgt;"
    """
    return input_string.replace('<', '&clt;').replace('>', '&cgt;')


def reverse_html(input_html):
    """Reformat codified pyRevit output html string back to normal html.

    pyRevit output window renders html content. But this means that < and >
    characters in outputs from python (e.g. <class at xxx>) will be treated
    as html tags. To avoid this, all <> characters that are defining
    html content need to be replaced with special phrases. pyRevit output
    later translates these phrases back in to < and >. That is how pyRevit
    distinquishes between <> printed from python and <> that define html.

    Args:
        input_html (str): input codified html string

    Examples:
        ```python
        prepare_html_str('&clt;p&cgt;Some text&clt;/p&cgt;')
        ```
        "<p>Some text</p>"
    """
    return input_html.replace('&clt;', '<').replace('&cgt;', '>')


def escape_for_html(input_string):
    return input_string.replace('<', '&lt;').replace('>', '&gt;')


# def check_internet_connection():
    # import urllib2
    #
    # def internet_on():
    #     try:
    #         urllib2.urlopen('http://216.58.192.142', timeout=1)
    #         return True
    #     except urllib2.URLError as err:
    #         return False


def can_access_url(url_to_open, timeout=1000):
    """Check if url is accessible within timeout.

    Args:
        url_to_open (str): url to check access for
        timeout (int): timeout in milliseconds

    Returns:
        (bool): true if accessible
    """
    try:
        client = framework.WebRequest.Create(url_to_open)
        client.Method = "HEAD"
        client.Timeout = timeout
        client.Proxy = framework.WebProxy.GetDefaultProxy()
        response = client.GetResponse()
        response.GetResponseStream()
        return True
    except Exception:
        return False


def read_url(url_to_open):
    """Get the url and return response.

    Args:
        url_to_open (str): url to check access for
    """
    client = framework.WebClient()
    return client.DownloadString(url_to_open)


def check_internet_connection(timeout=1000):
    """Check if internet connection is available.

    Pings a few well-known websites to check if internet connection is present.

    Args:
        timeout (int): timeout in milliseconds

    Returns:
        (str): url if internet connection is present, None if no internet.
    """
    solid_urls = ["http://google.com/",
                  "http://github.com/",
                  "http://bitbucket.com/",
                  "http://airtable.com/",
                  "http://todoist.com/",
                  "http://stackoverflow.com/",
                  "http://twitter.com/",
                  "http://youtube.com/"]
    random.shuffle(solid_urls)
    for url in solid_urls:
        if can_access_url(url, timeout):
            return url

    return None


def touch(fname, times=None):
    """Update the timestamp on the given file.

    Args:
        fname (str): target file path
        times (int): number of times to touch the file
    """
    with open(fname, 'a'):
        os.utime(fname, times)


def read_source_file(source_file_path):
    """Read text file and return contents.

    Args:
        source_file_path (str): target file path

    Returns:
        (str): file contents

    Raises:
        PyRevitException: on read error
    """
    try:
        with open(source_file_path, 'r') as code_file:
            return code_file.read()
    except Exception as read_err:
        raise PyRevitException('Error reading source file: {} | {}'
                               .format(source_file_path, read_err))


def open_folder_in_explorer(folder_path):
    """Open given folder in Windows Explorer.

    Args:
        folder_path (str): directory path
    """
    import subprocess
    subprocess.Popen(r'explorer /open,"{}"'
                     .format(os.path.normpath(folder_path)))


def show_entry_in_explorer(entry_path):
    """Show given entry in Windows Explorer.

    Args:
        entry_path (str): directory or file path
    """
    import subprocess
    subprocess.Popen(r'explorer /select,"{}"'
                     .format(os.path.normpath(entry_path)))


def fully_remove_dir(dir_path):
    """Remove directory recursively.

    Args:
        dir_path (str): directory path
    """
    def del_rw(action, name, exc):   #pylint: disable=W0613
        """Force delete entry."""
        os.chmod(name, stat.S_IWRITE)
        os.remove(name)

    shutil.rmtree(dir_path, onerror=del_rw)


def cleanup_filename(file_name, windows_safe=False):
    """Cleanup file name from special characters.

    Args:
        file_name (str): file name
        windows_safe (bool): whether to use windows safe characters

    Returns:
        (str): cleaned up file name

    Examples:
        ```python
        cleanup_filename('Myfile-(3).txt')
        ```
        "Myfile(3).txt"

        ```python
        cleanup_filename('Perforations 1/8" (New)')
        ```
        "Perforations 18 (New).txt"
    """
    if windows_safe:
        return re.sub(r'[\/:*?"<>|]', '', file_name)
    else:
        return re.sub(r'[^\w_.() -#]|["]', '', file_name)


def _inc_or_dec_string(str_id, shift, refit=False, logger=None):
    """Increment or decrement identifier.

    Args:
        str_id (str): identifier e.g. A310a
        shift (int): number of steps to change the identifier
        refit (bool): whether to refit the identifier
        logger (logging.Logger): logger

    Returns:
        (str): modified identifier

    Examples:
        ```python
        _inc_or_dec_string('A319z')
        ```
        'A320a'
    """
    # if no shift, return given string
    if shift == 0:
        return str_id

    # otherwise lets process the shift
    next_str = ""
    index = len(str_id) - 1
    carry = shift

    while index >= 0:
        # pick chars from right
        # A1.101a <--
        this_char = str_id[index]

        # determine character range (# of chars, starting index)
        if this_char.isdigit():
            char_range = ('0', '9')
        elif this_char.isalpha():
            # if this_char.isupper()
            char_range = ('A', 'Z')
            if this_char.islower():
                char_range = ('a', 'z')
        else:
            next_str += this_char
            index -= 1
            continue

        # get character range properties
        direction = int(carry / abs(carry)) if carry != 0 else 1
        start_char, end_char = \
            char_range if direction > 0 else char_range[::-1]
        char_steps = abs(ord(end_char) - ord(start_char)) + 1
        # calculate offset

        # positive carry -> start_char=0    end_char=9
        # +----------++        ++          +  char_steps
        # +--------+                          dist
        #          +---------------+          carry (abs)
        # 01234567[8]9012345678901[2]3456789
        # ----------------------+==+          offset

        # negative carry -> start_char=9    end_char=0
        # +----------++        ++          +  char_steps
        # +-------+                           dist
        #         +---------------+           carry (abs)
        # 9876543[2]1098765432109[8]76543210
        # ----------------------+=+           offset

        dist = abs(ord(this_char) - ord(start_char))
        offset = (dist + abs(carry)) % char_steps
        next_char = chr(ord(start_char) + (offset * direction))
        next_str += next_char
        carry = int((dist + abs(carry)) / char_steps) * direction
        if logger:
            logger.debug(
                '\"{}\" index={} start_char=\"{}\" end_char=\"{}\" '
                'next_carry={} direction={} dist={} offset={} next_char=\"{}\"'
                .format(
                    this_char,
                    index,
                    start_char,
                    end_char,
                    carry,
                    direction,
                    dist,
                    offset,
                    next_char))
        index -= 1
        # refit the final value
        # 009 --> 9
        # ZZA --> ZA
        if refit and index == -1:
            if carry > 0:
                str_id = start_char + str_id
                if start_char.isalpha():
                    carry -= 1
                index = 0
            elif direction == -1:
                if next_str.endswith(start_char):
                    next_str = next_str[:-1]
                else:
                    while next_str.endswith(end_char):
                        next_str = next_str[:-1]

    return next_str[::-1]


def increment_str(input_str, step=1, expand=False):
    """Incremenet identifier.

    Args:
        input_str (str): identifier e.g. A310a
        step (int): number of steps to change the identifier
        expand (bool): removes leading zeroes and duplicate letters

    Returns:
        (str): modified identifier

    Examples:
        ```python
        increment_str('A319z')
        ```
        'A320a'
    """
    return _inc_or_dec_string(input_str, abs(step), refit=expand)


def decrement_str(input_str, step=1, shrink=False):
    """Decrement identifier.

    Args:
        input_str (str): identifier e.g. A310a
        step (int): number of steps to change the identifier
        shrink (bool): removes leading zeroes or duplicate letters 

    Returns:
        (str): modified identifier

    Examples:
        ```python
        decrement_str('A310a')
        ```
        'A309z'
    """
    return _inc_or_dec_string(input_str, -abs(step), refit=shrink)


def extend_counter(input_str, upper=True, use_zero=False):
    """Add a new level to identifier. e.g. A310 -> A310A.

    Args:
        input_str (str): identifier e.g. A310
        upper (bool): use UPPERCASE characters for extension
        use_zero (bool): start from 0 for numeric extension

    Returns:
        (str): extended identifier

    Examples:
        ```python
        extend_counter('A310')
        ```
        'A310A'
        ```python
        extend_counter('A310A', use_zero=True)
        ```
        'A310A0'
    """
    if input_str[-1].isdigit():
        return input_str + ("A" if upper else "a")
    else:
        return input_str + ("0" if use_zero else "1")


def filter_null_items(src_list):
    """Remove None items in the given list.

    Args:
        src_list (list[Any]): list of any items

    Returns:
        (list[Any]): cleaned list
    """
    return list(filter(bool, src_list))


def reverse_dict(input_dict):
    """Reverse the key, value pairs.

    Args:
        input_dict (dict): source ordered dict

    Returns:
        (defaultdict): reversed dictionary

    Examples:
        ```python
        reverse_dict({1: 2, 3: 4})
        ```
        defaultdict(<type 'list'>, {2: [1], 4: [3]})
    """
    output_dict = defaultdict(list)
    for key, value in input_dict.items():
        output_dict[value].append(key)
    return output_dict


def timestamp():
    """Return timestamp for current time.

    Returns:
        (str): timestamp in string format

    Examples:
        ```python
        timestamp()
        ```
        '01003075032506808'
    """
    return datetime.datetime.now().strftime("%m%j%H%M%S%f")


def current_time():
    """Return formatted current time.

    Current implementation uses %H:%M:%S to format time.

    Returns:
        (str): formatted current time.

    Examples:
        ```python
        current_time()
        ```
        '07:50:53'
    """
    return datetime.datetime.now().strftime("%H:%M:%S")


def current_date():
    """Return formatted current date.

    Current implementation uses %Y-%m-%d to format date.

    Returns:
        (str): formatted current date.

    Examples:
        ```python
        current_date()
        ```
        '2018-01-03'
    """
    return datetime.datetime.now().strftime("%Y-%m-%d")


def is_blank(input_string):
    """Check if input string is blank (multiple white spaces is blank).

    Args:
        input_string (str): input string

    Returns:
        (bool): True if string is blank

    Examples:
        ```python
        is_blank('   ')
        ```
        True
    """
    if input_string and input_string.strip():
        return False
    return True


def is_url_valid(url_string):
    """Check if given URL is in valid format.

    Args:
        url_string (str): URL string

    Returns:
        (bool): True if URL is in valid format

    Examples:
        ```python
        is_url_valid('https://www.google.com')
        ```
        True
    """
    regex = re.compile(
        r'^(?:http|ftp)s?://'                   # http:// or https://
        r'(?:(?:[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?\.)+'
        r'(?:[A-Z]{2,6}\.?|[A-Z0-9-]{2,}\.?)|'  # domain...
        r'localhost|'                           # localhost...
        r'\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})'  # ...or ip
        r'(?::\d+)?'                            # optional port
        r'(?:/?|[/?]\S+)$', re.IGNORECASE)

    return regex.match(url_string)


def reformat_string(orig_str, orig_format, new_format):
    """Reformat a string into a new format.

    Extracts information from a string based on a given pattern,
    and recreates a new string based on the given new pattern.

    Args:
        orig_str (str): Original string to be reformatted
        orig_format (str): Pattern of the original str (data to be extracted)
        new_format (str): New pattern (how to recompose the data)

    Returns:
        (str): Reformatted string

    Examples:
        ```python
        reformat_string('150 - FLOOR/CEILING - WD - 1 HR - FLOOR ASSEMBLY',
                            '{section} - {loc} - {mat} - {rating} - {name}',
                            '{section}:{mat}:{rating} - {name} ({loc})'))
        ```
        '150:WD:1 HR - FLOOR ASSEMBLY (FLOOR/CEILING)'
    """
    # find the tags
    tag_extractor = re.compile('{(.+?)}')
    tags = tag_extractor.findall(orig_format)

    # replace the tags with regex patterns
    # to create a regex pattern that finds values
    tag_replacer = re.compile('{.+?}')
    value_extractor_pattern = tag_replacer.sub('(.+)', orig_format)
    # find all values
    value_extractor = re.compile(value_extractor_pattern)
    match = value_extractor.match(orig_str)
    values = match.groups()

    # create a dictionary of tags and values
    reformat_dict = {}
    for key, value in zip(tags, values):
        reformat_dict[key] = value

    # use dictionary to reformat the string into new
    return new_format.format(**reformat_dict)


def get_mapped_drives_dict():
    """Return a dictionary of currently mapped network drives."""
    searcher = framework.ManagementObjectSearcher(
        "root\\CIMV2",
        "SELECT * FROM Win32_MappedLogicalDisk"
        )

    return {x['DeviceID']: x['ProviderName'] for x in searcher.Get()}


def dletter_to_unc(dletter_path):
    """Convert drive letter path into UNC path of that drive.

    Args:
        dletter_path (str): drive letter path

    Returns:
        (str): UNC path

    Examples:
        ```python
        # assuming J: is mapped to //filestore/server/jdrive
        dletter_to_unc('J:/somefile.txt')
        ```
        '//filestore/server/jdrive/somefile.txt'
    """
    drives = get_mapped_drives_dict()
    dletter = dletter_path[:2]
    for mapped_drive, server_path in drives.items():
        if dletter.lower() == mapped_drive.lower():
            return dletter_path.replace(dletter, server_path)


def unc_to_dletter(unc_path):
    """Convert UNC path into drive letter path.

    Args:
        unc_path (str): UNC path

    Returns:
        (str): drive letter path

    Examples:
        ```python
        # assuming J: is mapped to //filestore/server/jdrive
        unc_to_dletter('//filestore/server/jdrive/somefile.txt')
        ```
        'J:/somefile.txt'
    """
    drives = get_mapped_drives_dict()
    for mapped_drive, server_path in drives.items():
        if server_path in unc_path:
            return unc_path.replace(server_path, mapped_drive)


def random_color():
    """Return a random color channel value (between 0 and 255)."""
    return random.randint(0, 255)


def random_alpha():
    """Return a random alpha value (between 0 and 1.00)."""
    return round(random.random(), 2)


def random_hex_color():
    """Return a random color in hex format.

    Examples:
        ```python
        random_hex_color()
        ```
        '#FF0000'
    """
    return '#%02X%02X%02X' % (random_color(),
                              random_color(),
                              random_color())


def random_rgb_color():
    """Return a random color in rgb format.

    Examples:
        ```python
        random_rgb_color()
        ```
        'rgb(255, 0, 0)'
    """
    return 'rgb(%d, %d, %d)' % (random_color(),
                                random_color(),
                                random_color())


def random_rgba_color():
    """Return a random color in rgba format.

    Examples:
        ```python
        random_rgba_color()
        ```
        'rgba(255, 0, 0, 0.5)'
    """
    return 'rgba(%d, %d, %d, %.2f)' % (random_color(),
                                       random_color(),
                                       random_color(),
                                       random_alpha())


def extract_range(formatted_str, max_range=500):
    """Extract range from formatted string.

    String must be formatted as below
    A103            No range
    A103-A106       A103 to A106
    A103:A106       A103 to A106
    A103,A105a      A103 and A105a
    A103;A105a      A103 and A105a

    Args:
        formatted_str (str): string specifying range
        max_range (int): maximum number of items to create.

    Returns:
        (list[str]): names in the specified range

    Examples:
        ```python
        exract_range('A103:A106')
        ```
        ['A103', 'A104', 'A105', 'A106']
        ```python
        exract_range('S203-S206')
        ```
        ['S203', 'S204', 'S205', 'S206']
        ```python
        exract_range('M00A,M00B')
        ```
        ['M00A', 'M00B']
    """
    for rchar, rchartype in {'::': 'range', '--': 'range',
                             ',': 'list', ';': 'list'}.items():
        if rchar in formatted_str:
            if rchartype == 'range' \
                    and formatted_str.count(rchar) == 1:
                items = []
                start, end = formatted_str.split(rchar)
                assert len(start) == len(end), \
                    'Range start and end must have same length'
                items.append(start)
                item = increment_str(start, 1)
                safe_counter = 0
                while item != end:
                    items.append(item)
                    item = increment_str(item, 1)
                    safe_counter += 1
                    assert safe_counter < max_range, 'Max range reached.'
                items.append(end)
                return items
            elif rchartype == 'list':
                return [x.strip() for x in formatted_str.split(rchar)]
    return [formatted_str]


def check_encoding_bom(filename, bom_bytes=codecs.BOM_UTF8):
    """Check if given file contains the given BOM bytes at the start.

    Args:
        filename (str): file path
        bom_bytes (bytes, optional): BOM bytes to check
    """
    with open(filename, 'rb') as rtfile:
        return rtfile.read()[:len(bom_bytes)] == bom_bytes


def has_nonprintable(input_str):
    """Check input string for non-printable characters.

    Args:
        input_str (str): input string

    Returns:
        (bool): True if contains non-printable characters
    """
    return any([x in input_str for x in UNICODE_NONPRINTABLE_CHARS])


def get_enum_values(enum_type):
    """Returns enum values."""
    return framework.Enum.GetValues(enum_type)


def get_enum_value(enum_type, value_string):
    """Return enum value matching given value string (case insensitive)."""
    for ftype in get_enum_values(enum_type):
        if str(ftype).lower() == value_string.lower():
            return ftype


def get_enum_none(enum_type):
    """Returns the None value in given Enum."""
    for val in get_enum_values(enum_type):
        if str(val) == 'None':
            return val


def extract_guid(source_str):
    """Extract GUID number from a string."""
    guid_match = re.match(".*([0-9A-Fa-f]{8}"
                          "[-][0-9A-Fa-f]{4}"
                          "[-][0-9A-Fa-f]{4}"
                          "[-][0-9A-Fa-f]{4}"
                          "[-][0-9A-Fa-f]{12}).*", source_str)
    if guid_match:
        return guid_match.groups()[0]


def format_hex_rgb(rgb_value):
    """Formats rgb value as #RGB value string."""
    if isinstance(rgb_value, str):
        if not rgb_value.startswith('#'):
            return '#%s' % rgb_value
        else:
            return rgb_value
    elif isinstance(rgb_value, int):
        return '#%x' % rgb_value


def new_uuid():
    """Create a new UUID (using dotnet Guid.NewGuid)."""
    # RE: https://github.com/eirannejad/pyRevit/issues/413
    # return uuid.uuid1()
    return str(Guid.NewGuid())


def is_box_visible_on_screens(left, top, width, height):
    """Check if given box is visible on any screen."""
    bounds = \
        framework.Drawing.Rectangle(
            framework.Convert.ToInt32(0 if math.isnan(left) else left),
            framework.Convert.ToInt32(0 if math.isnan(top) else top),
            framework.Convert.ToInt32(0 if math.isnan(width) else width),
            framework.Convert.ToInt32(0 if math.isnan(height) else height)
            )
    for scr in framework.Forms.Screen.AllScreens:
        if bounds.IntersectsWith(scr.Bounds):
            return True
    return False


def fuzzy_search_ratio(target_string, sfilter, regex=False):
    """Match target string against the filter and return a match ratio.

    Args:
        target_string (str): target string
        sfilter (str): search term
        regex (bool): treat the sfilter as regular expression pattern

    Returns:
        (int): integer between 0 to 100, with 100 being the exact match
    """
    tstring = target_string

    # process regex here. It's a yes no situation (100 or 0)
    if regex:
        try:
            if re.search(sfilter, tstring):
                return 100
        except Exception:
            pass
        return 0

    # 100 for identical matches
    if sfilter == tstring:
        return 100

    # 98 to 99 reserved (2 scores)

    # 97 for identical non-case-sensitive matches
    lower_tstring = tstring.lower()
    lower_sfilter_str = sfilter.lower()
    if lower_sfilter_str == lower_tstring:
        return 97

    # 95  to 96 reserved (2 scores)

    # 93 to 94 for inclusion matches
    if sfilter in tstring:
        return 94
    if lower_sfilter_str in lower_tstring:
        return 93

    # 91  to 92 reserved (2 scores)

    ## 80 to 90 for parts matches
    tstring_parts = tstring.split()
    sfilter_parts = sfilter.split()
    if all(x in tstring_parts for x in sfilter_parts):
        return 90

    # 88 to 89 reserved (2 scores)

    lower_tstring_parts = [x.lower() for x in tstring_parts]
    lower_sfilter_parts = [x.lower() for x in sfilter_parts]
    # exclude override
    if any(x[0] == '!' for x in sfilter_parts):
        exclude_indices = [
            lower_sfilter_parts.index(i) for i in lower_sfilter_parts
            if i[0] == '!'
        ]
        exclude_indices.reverse()
        exclude_list = [
            lower_sfilter_parts.pop(i) for i in exclude_indices
        ]
        for e in exclude_list:
            # doesn't contain
            if len(e) > 1:
                exclude_string = e[1:]
                if any(
                        [exclude_string in
                        part for part in lower_tstring_parts]
                ):
                    return 0
    if all(x in lower_tstring_parts for x in lower_sfilter_parts):
        return 87

    # 85 to 86 reserved (2 scores)

    if all(x in tstring for x in sfilter_parts):
        return 84

    # 82 to 83 reserved (2 scores)

    if all(x in lower_tstring for x in lower_sfilter_parts):
        return 81

    # 80 reserved

    return 0


def get_exe_version(exepath):
    """Extract Product Version value from EXE file."""
    version_info = framework.Diagnostics.FileVersionInfo.GetVersionInfo(exepath)
    return version_info.ProductVersion


def get_reg_key(key, subkey):
    """Get value of the given Windows registry key and subkey.

    Args:
        key (PyHKEY): parent registry key
        subkey (str): subkey path

    Returns:
        (PyHKEY): registry key if found, None if not found

    Examples:
        ```python
        get_reg_key(wr.HKEY_CURRENT_USER, 'Control Panel/International')
        ```
        <PyHKEY at 0x...>
    """
    try:
        return wr.OpenKey(key, subkey, 0, wr.KEY_READ)
    except Exception:
        return None


def kill_tasks(task_name):
    """Kill running tasks matching task_name.

    Args:
        task_name (str): task name

    Examples:
        ```python
        kill_tasks('Revit.exe')
        ```
    """
    os.system("taskkill /f /im %s" % task_name)


def int2hex_long(number):
    """Integer to hexadecimal string."""
    # python 2 fix of addin 'L' to long integers
    return hex(number).replace('L', '')


def hex2int_long(hex_string):
    """Hexadecimal string to Integer."""
    # python 2 fix of addin 'L' to long integers
    hex_string.replace('L', '')
    return int(hex_string, 16)


def split_words(input_string):
    """Splits given string by uppercase characters.

    Args:
        input_string (str): input string

    Returns:
        (list[str]): split string

    Examples:
        ```python
        split_words("UIApplication_ApplicationClosing")
        ```
        ['UIApplication', 'Application', 'Closing']
    """
    parts = []
    part = ""
    for c in input_string:
        if c.isalpha():
            if c.isupper() and part and part[-1].islower():
                if part:
                    parts.append(part.strip())
                part = c
            else:
                part += c
    parts.append(part)
    return parts


def get_paper_sizes(printer_name=None):
    """Get paper sizes defined on this system.

    Returns:
        (list[]): list of papersize instances
    """
    print_settings = framework.Drawing.Printing.PrinterSettings()
    if printer_name:
        print_settings.PrinterName = printer_name
    return list(print_settings.PaperSizes)


def get_integer_length(number):
    """Return digit length of given number."""
    return 1 if number == 0 else (math.floor(math.log10(number)) + 1)


def get_my_ip():
    """Return local ip address of this machine."""
    return socket.gethostbyname(socket.gethostname())

"""Charts engine for output window."""
# pylint: disable=C0103
from json import JSONEncoder

from pyrevit.coreutils import timestamp, random_rgba_color

# CHARTS_ENGINE = 'Chart.js'
CHARTS_ENGINE = 'Chart.bundle.js'

# chart.js chart types
LINE_CHART = 'line'
BAR_CHART = 'bar'
RADAR_CHART = 'radar'
POLAR_CHART = 'polarArea'
PIE_CHART = 'pie'
DOUGHNUT_CHART = 'doughnut'
BUBBLE_CHART = 'bubble'


CHARTS_JS_PATH = \
    "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/{version}/Chart.min.js"


SCRIPT_TEMPLATE = \
    "var ctx = document.getElementById('{canvas_id}').getContext('2d');" \
    "var chart = new Chart(ctx, {canvas_code});"


class _ChartsDataSetEncode(JSONEncoder):
    """JSON encoder for chart data sets."""
    def default(self, dataset_obj):  # pylint: disable=E0202, W0221
        data_dict = dataset_obj.__dict__.copy()
        for key, value in data_dict.items():
            if key.startswith('_') or value == '' or value == []:
                data_dict.pop(key)

        return data_dict


class PyRevitOutputChartOptions(object):
    """Chart options wrapper object."""
    def __init__(self):
        pass


class PyRevitOutputChartDataset(object):
    """Chart dataset wrapper object."""
    def __init__(self, label):
        self.label = label
        self.data = []
        self.backgroundColor = ''

    def set_color(self, *args):
        """Set dataset color.

        Arguments are expected to be R, G, B, A values.

        Examples:
            ```python
            dataset_obj.set_color(0xFF, 0x8C, 0x8D, 0.8)
            ```
        """
        if len(args) == 4:
            self.backgroundColor = 'rgba({},{},{},{})'.format(args[0],
                                                              args[1],
                                                              args[2],
                                                              args[3])
        elif len(args) == 1:
            self.backgroundColor = '{}'.format(args[0])


class PyRevitOutputChartData(object):
    """Chart data wrapper object."""
    def __init__(self):
        self.labels = ''
        self.datasets = []

    def new_dataset(self, dataset_label):
        """Create new data set.

        Args:
            dataset_label (str): dataset label

        Returns:
            (PyRevitOutputChartDataset): dataset wrapper object

        Examples:
            ```python
            chart.data.new_dataset('set_a')
            ```
        """
        new_dataset = PyRevitOutputChartDataset(dataset_label)
        self.datasets.append(new_dataset)
        return new_dataset


class PyRevitOutputChart(object):
    """Chart wrapper object for output window.

    Attributes:
        output (pyrevit.output.PyRevitOutputWindow):
            output window wrapper object
        chart_type (str): chart type name
    """
    def __init__(self, output, chart_type=LINE_CHART, version=None):
        self._output = output
        self._style = None
        self._width = self._height = None
        self._version = version or '2.8.0'

        self.type = chart_type
        self.data = PyRevitOutputChartData()

        self.options = PyRevitOutputChartOptions()
        # # common chart options and their default values
        # chart.options.responsive = True
        # chart.options.responsiveAnimationDuration = 0
        # chart.options.maintainAspectRatio = True
        #
        # # layout options
        # chart.options.layout = {'padding': 0}
        #
        # # title options
        # # position:
        # # Position of the title. Possible values are 'top',
        # # 'left', 'bottom' and 'right'.
        # chart.options.title = {'display': False,
        #                        'position': 'top',
        #                        'fullWidth': True,
        #                        'fontSize': 12,
        #                        'fontFamily': 'Arial',
        #                        'fontColor': '#666',
        #                        'fontStyle': 'bold',
        #                        'padding': 10,
        #                        'text': ''
        #                        }
        #
        # # legend options
        # chart.options.legend = {'display': True,
        #                         'position': 'top',
        #                         'fullWidth': True,
        #                         'reverse': False,
        #                         'labels': {'boxWidth': 40,
        #                                    'fontSize': 12,
        #                                    'fontStyle': 'normal',
        #                                    'fontColor': '#666',
        #                                    'fontFamily': 'Arial',
        #                                    'padding': 10,
        #                                    'usePointStyle': True
        #                                    }
        #                         }
        #
        # # tooltips options
        # # intersect:
        # # if true, the tooltip mode applies only when the mouse
        # # position intersects with an element.
        # # If false, the mode will be applied at all times
        # chart.options.tooltips = {'enabled': True,
        #                           'intersect': True,
        #                           'backgroundColor': 'rgba(0,0,0,0.8)',
        #                           'caretSize': 5,
        #                           'displayColors': True}

    def _setup_charts(self):
        cur_head = self._output.get_head_html()
        charts_js_path = CHARTS_JS_PATH.format(version=self._version)
        if charts_js_path not in cur_head:
            self._output.inject_script('', {'src': charts_js_path})

    @staticmethod
    def _make_canvas_unique_id():
        return 'chart{}'.format(timestamp())

    def _make_canvas_code(self, canvas_id):
        attribs = ''
        attribs += ' id="{}"'.format(canvas_id)
        if self._style:
            attribs += ' style="{}"'.format(self._style)
        else:
            if self._width:
                attribs += ' width="{}px"'.format(self._width)
            if self._height:
                attribs += ' height="{}px"'.format(self._height)

        return '<canvas {}></canvas>'.format(attribs)

    def _make_charts_script(self, canvas_id):
        return SCRIPT_TEMPLATE.format(
            canvas_id=canvas_id,
            canvas_code=_ChartsDataSetEncode().encode(self))

    def randomize_colors(self):
        """Randomize chart datasets colors."""
        if self.type in [POLAR_CHART, PIE_CHART, DOUGHNUT_CHART]:
            for dataset in self.data.datasets:
                dataset.backgroundColor = [random_rgba_color()
                                           for _ in range(0, len(dataset.data))]
        else:
            for dataset in self.data.datasets:
                dataset.backgroundColor = random_rgba_color()

    def set_width(self, width):
        """Set chart width on output window."""
        self._width = width

    def set_height(self, height):
        """Set chart height on output window."""
        self._height = height

    def set_style(self, html_style):
        """Set chart styling.

        Args:
            html_style (str): inline html css styling string

        Examples:
            ```python
            chart.set_style('height:150px')
            ```
        """
        self._style = html_style

    def draw(self):
        """Request chart to draw itself on output window."""
        self._setup_charts()
        # setup canvas
        canvas_id = self._make_canvas_unique_id()
        canvas_code = self._make_canvas_code(canvas_id)
        self._output.print_html(canvas_code)
        # make the code
        js_code = self._make_charts_script(canvas_id)
        self._output.inject_script(js_code, body=True)

"""Base module for pyRevit config parsing."""
import json
import codecs
from pyrevit.compat import configparser

from pyrevit import PyRevitException, PyRevitIOError
from pyrevit import coreutils

#pylint: disable=W0703,C0302
KEY_VALUE_TRUE = "True"
KEY_VALUE_FALSE = "False"


class PyRevitConfigSectionParser(object):
    """Config section parser object. Handle section options."""
    def __init__(self, config_parser, section_name):
        self._parser = config_parser
        self._section_name = section_name

    def __iter__(self):
        return iter(self._parser.options(self._section_name))

    def __str__(self):
        return self._section_name

    def __repr__(self):
        return '<PyRevitConfigSectionParser object '    \
               'at 0x{0:016x} '                         \
               'config section \'{1}\'>'                \
               .format(id(self), self._section_name)

    def __getattr__(self, param_name):
        try:
            value = self._parser.get(self._section_name, param_name)
            try:
                try:
                    return json.loads(value)  #pylint: disable=W0123
                except Exception:
                    # try fix legacy formats
                    # cleanup python style true, false values
                    if value == KEY_VALUE_TRUE:
                        value = json.dumps(True)
                    elif value == KEY_VALUE_FALSE:
                        value = json.dumps(False)
                    # cleanup string representations
                    value = value.replace('\'', '"').encode('string-escape')
                    # try parsing again
                    try:
                        return json.loads(value)  #pylint: disable=W0123
                    except Exception:
                        # if failed again then the value is a string
                        # but is not encapsulated in quotes
                        # e.g. option = C:\Users\Desktop
                        value = value.strip()
                        if not value.startswith('(') \
                                or not value.startswith('[') \
                                or not value.startswith('{'):
                            value = "\"%s\"" % value
                        return json.loads(value)  #pylint: disable=W0123
            except Exception:
                return value
        except (configparser.NoOptionError, configparser.NoSectionError):
            raise AttributeError('Parameter does not exist in config file: {}'
                                 .format(param_name))

    def __setattr__(self, param_name, value):
        # check agaist used attribute names
        if param_name in ['_parser', '_section_name']:
            super(PyRevitConfigSectionParser, self).__setattr__(param_name,
                                                                value)
        else:
            # if not used by this object, then set a config section
            try:
                return self._parser.set(self._section_name,
                                        param_name,
                                        json.dumps(value,
                                                   separators=(',', ':'),
                                                   ensure_ascii=False))
            except Exception as set_err:
                raise PyRevitException('Error setting parameter value. '
                                       '| {}'.format(set_err))

    @property
    def header(self):
        """Section header."""
        return self._section_name

    @property
    def subheader(self):
        """Section sub-header e.g. Section.SubSection."""
        return coreutils.get_canonical_parts(self.header)[-1]

    def has_option(self, option_name):
        """Check if section contains given option."""
        return self._parser.has_option(self._section_name, option_name)

    def get_option(self, op_name, default_value=None):
        """Get option value or return default."""
        try:
            return self.__getattr__(op_name)
        except Exception as opt_get_err:
            if default_value is not None:
                return default_value
            else:
                raise opt_get_err

    def set_option(self, op_name, value):
        """Set value of given option."""
        self.__setattr__(op_name, value)

    def remove_option(self, option_name):
        """Remove given option from section."""
        return self._parser.remove_option(self._section_name, option_name)

    def has_subsection(self, section_name):
        """Check if section has any subsections."""
        return True if self.get_subsection(section_name) else False

    def add_subsection(self, section_name):
        """Add subsection to section."""
        return self._parser.add_section(
            coreutils.make_canonical_name(self._section_name, section_name)
        )

    def get_subsections(self):
        """Get all subsections."""
        subsections = []
        for section_name in self._parser.sections():
            if section_name.startswith(self._section_name + '.'):
                subsec = PyRevitConfigSectionParser(self._parser, section_name)
                subsections.append(subsec)
        return subsections

    def get_subsection(self, section_name):
        """Get subsection with given name."""
        for subsection in self.get_subsections():
            if subsection.subheader == section_name:
                return subsection


class PyRevitConfigParser(object):
    """Config parser object. Handle config sections and io."""
    def __init__(self, cfg_file_path=None):
        self._cfg_file_path = cfg_file_path
        self._parser = configparser.ConfigParser()
        if self._cfg_file_path:
            try:
                with codecs.open(self._cfg_file_path, 'r', 'utf-8') as cfg_file:
                    self._parser.readfp(cfg_file)
            except (OSError, IOError):
                raise PyRevitIOError()
            except Exception as read_err:
                raise PyRevitException(read_err)

    def __iter__(self):
        return iter([self.get_section(x) for x in self._parser.sections()])

    def __getattr__(self, section_name):
        if self._parser.has_section(section_name):
            # build a section parser object and return
            return PyRevitConfigSectionParser(self._parser, section_name)
        else:
            raise AttributeError(
                'Section \"{}\" does not exist in config file.'
                .format(section_name))

    def get_config_file_hash(self):
        """Get calculated unique hash for this config.

        Returns:
            (str): hash of the config.
        """
        with codecs.open(self._cfg_file_path, 'r', 'utf-8') as cfg_file:
            cfg_hash = coreutils.get_str_hash(cfg_file.read())

        return cfg_hash

    def has_section(self, section_name):
        """Check if config contains given section."""
        try:
            self.get_section(section_name)
            return True
        except Exception:
            return False

    def add_section(self, section_name):
        """Add section with given name to config."""
        self._parser.add_section(section_name)
        return PyRevitConfigSectionParser(self._parser, section_name)

    def get_section(self, section_name):
        """Get section with given name.

        Raises:
            AttributeError: if section is missing
        """
        # check is section with full name is available
        if self._parser.has_section(section_name):
            return PyRevitConfigSectionParser(self._parser, section_name)

        # if not try to match with section_name.subsection
        # if there is a section_name.subsection defined, that should be
        # the sign that the section exists
        # section obj then supports getting all subsections
        for cfg_section_name in self._parser.sections():
            master_section = coreutils.get_canonical_parts(cfg_section_name)[0]
            if section_name == master_section:
                return PyRevitConfigSectionParser(self._parser,
                                                  master_section)

        # if no match happened then raise exception
        raise AttributeError('Section does not exist in config file.')

    def remove_section(self, section_name):
        """Remove section from config."""
        cfg_section = self.get_section(section_name)
        for cfg_subsection in cfg_section.get_subsections():
            self._parser.remove_section(cfg_subsection.header)
        self._parser.remove_section(cfg_section.header)

    def reload(self, cfg_file_path=None):
        """Reload config from original or given file."""
        try:
            with codecs.open(cfg_file_path \
                    or self._cfg_file_path, 'r', 'utf-8') as cfg_file:
                self._parser.readfp(cfg_file)
        except (OSError, IOError):
            raise PyRevitIOError()

    def save(self, cfg_file_path=None):
        """Save config to original or given file."""
        try:
            with codecs.open(cfg_file_path \
                    or self._cfg_file_path, 'w', 'utf-8') as cfg_file:
                self._parser.write(cfg_file)
        except (OSError, IOError):
            raise PyRevitIOError()

"""Base module to interact with Revit ribbon."""
from collections import OrderedDict

#pylint: disable=W0703,C0302,C0103
from pyrevit import HOST_APP, EXEC_PARAMS, PyRevitException
from pyrevit.compat import safe_strtype
from pyrevit import coreutils
from pyrevit.coreutils.logger import get_logger
from pyrevit.coreutils import envvars
from pyrevit.framework import System, Uri, Windows
from pyrevit.framework import IO
from pyrevit.framework import Imaging
from pyrevit.framework import BindingFlags
from pyrevit.framework import Media, Convert
from pyrevit.api import UI, AdWindows, AdInternal
from pyrevit.runtime import types
from pyrevit.revit import ui


mlogger = get_logger(__name__)


PYREVIT_TAB_IDENTIFIER = 'pyrevit_tab'

ICON_SMALL = 16
ICON_MEDIUM = 24
ICON_LARGE = 32

DEFAULT_DPI = 96

DEFAULT_TOOLTIP_IMAGE_FORMAT = '.png'
DEFAULT_TOOLTIP_VIDEO_FORMAT = '.swf'
if not EXEC_PARAMS.doc_mode and HOST_APP.is_newer_than(2019, or_equal=True):
    DEFAULT_TOOLTIP_VIDEO_FORMAT = '.mp4'


def argb_to_brush(argb_color):
    # argb_color is formatted as #AARRGGBB
    a = r = g = b = "FF"
    try:
        b = argb_color[-2:]
        g = argb_color[-4:-2]
        r = argb_color[-6:-4]
        if len(argb_color) > 7:
            a = argb_color[-8:-6]
        return Media.SolidColorBrush(Media.Color.FromArgb(
                Convert.ToInt32("0x" + a, 16),
                Convert.ToInt32("0x" + r, 16),
                Convert.ToInt32("0x" + g, 16),
                Convert.ToInt32("0x" + b, 16)
                )
            )
    except Exception as color_ex:
        mlogger.error("Bad color format %s | %s", argb_color, color_ex)


def load_bitmapimage(image_file):
    """Load given png file.

    Args:
        image_file (str): image file path

    Returns:
        (Imaging.BitmapImage): bitmap image object
    """
    bitmap = Imaging.BitmapImage()
    bitmap.BeginInit()
    bitmap.UriSource = Uri(image_file)
    bitmap.CacheOption = Imaging.BitmapCacheOption.OnLoad
    bitmap.CreateOptions = Imaging.BitmapCreateOptions.IgnoreImageCache
    bitmap.EndInit()
    bitmap.Freeze()
    return bitmap


# Helper classes and functions -------------------------------------------------
class PyRevitUIError(PyRevitException):
    """Common base class for all pyRevit ui-related exceptions."""
    pass


class ButtonIcons(object):
    """pyRevit ui element icon.

    Upon init, this type reads the given image file into an io stream and
    releases the os lock on the file.

    Args:
        image_file (str): image file path to be used as icon

    Attributes:
        icon_file_path (str): icon image file path
        filestream (IO.FileStream): io stream containing image binary data
    """
    def __init__(self, image_file):
        self.icon_file_path = image_file
        self.check_icon_size()
        self.filestream = IO.FileStream(image_file,
                                        IO.FileMode.Open,
                                        IO.FileAccess.Read)

    @staticmethod
    def recolour(image_data, size, stride, color):
        # FIXME: needs doc, and argument types
        # ButtonIcons.recolour(image_data, image_size, stride, 0x8e44ad)
        step = stride / size
        for i in range(0, stride, step):
            for j in range(0, stride, step):
                idx = (i * size) + j
                # R = image_data[idx+2]
                # G = image_data[idx+1]
                # B = image_data[idx]
                # luminance = (0.299*R + 0.587*G + 0.114*B)
                image_data[idx] = color >> 0 & 0xff       # blue
                image_data[idx+1] = color >> 8 & 0xff     # green
                image_data[idx+2] = color >> 16 & 0xff    # red

    def check_icon_size(self):
        """Verify icon size is within acceptable range."""
        image = System.Drawing.Image.FromFile(self.icon_file_path)
        image_size = max(image.Width, image.Height)
        if image_size > 96:
            mlogger.warning('Icon file is too large. Large icons adversely '
                            'affect the load time since they need to be '
                            'processed and adjusted for screen scaling. '
                            'Keep icons at max 96x96 pixels: %s',
                            self.icon_file_path)

    def create_bitmap(self, icon_size):
        """Resamples image and creates bitmap for the given size.

        Icons are assumed to be square.

        Args:
            icon_size (int): icon size (width or height)

        Returns:
            (Imaging.BitmapSource): object containing image data at given size
        """
        mlogger.debug('Creating %sx%s bitmap from: %s',
                      icon_size, icon_size, self.icon_file_path)
        adjusted_icon_size = icon_size * 2
        adjusted_dpi = DEFAULT_DPI * 2
        screen_scaling = HOST_APP.proc_screen_scalefactor

        self.filestream.Seek(0, IO.SeekOrigin.Begin)
        base_image = Imaging.BitmapImage()
        base_image.BeginInit()
        base_image.StreamSource = self.filestream
        base_image.DecodePixelHeight = int(adjusted_icon_size * screen_scaling)
        base_image.EndInit()
        self.filestream.Seek(0, IO.SeekOrigin.Begin)

        image_size = base_image.PixelWidth
        image_format = base_image.Format
        image_byte_per_pixel = int(base_image.Format.BitsPerPixel / 8)
        palette = base_image.Palette

        stride = int(image_size * image_byte_per_pixel)
        array_size = stride * image_size
        image_data = System.Array.CreateInstance(System.Byte, array_size)
        base_image.CopyPixels(image_data, stride, 0)

        scaled_size = int(adjusted_icon_size * screen_scaling)
        scaled_dpi = int(adjusted_dpi * screen_scaling)
        bitmap_source = \
            Imaging.BitmapSource.Create(scaled_size, scaled_size,
                                        scaled_dpi, scaled_dpi,
                                        image_format,
                                        palette,
                                        image_data,
                                        stride)
        return bitmap_source

    @property
    def small_bitmap(self):
        """Resamples image and creates bitmap for size :obj:`ICON_SMALL`.

        Returns:
            (Imaging.BitmapSource): object containing image data at given size
        """
        return self.create_bitmap(ICON_SMALL)

    @property
    def medium_bitmap(self):
        """Resamples image and creates bitmap for size :obj:`ICON_MEDIUM`.

        Returns:
            (Imaging.BitmapSource): object containing image data at given size
        """
        return self.create_bitmap(ICON_MEDIUM)

    @property
    def large_bitmap(self):
        """Resamples image and creates bitmap for size :obj:`ICON_LARGE`.

        Returns:
            (Imaging.BitmapSource): object containing image data at given size
        """
        return self.create_bitmap(ICON_LARGE)


# Superclass to all ui item classes --------------------------------------------
class GenericPyRevitUIContainer(object):
    """Common type for all pyRevit ui containers.

    Attributes:
        name (str): container name
        itemdata_mode (bool): if container is wrapping UI.*ItemData
    """
    def __init__(self):
        self.name = ''
        self._rvtapi_object = None
        self._sub_pyrvt_components = OrderedDict()
        self.itemdata_mode = False
        self._dirty = False
        self._visible = None
        self._enabled = None

    def __iter__(self):
        return iter(self._sub_pyrvt_components.values())

    def __repr__(self):
        return 'Name: {} RevitAPIObject: {}'.format(self.name,
                                                    self._rvtapi_object)

    def _get_component(self, cmp_name):
        try:
            return self._sub_pyrvt_components[cmp_name]
        except KeyError:
            raise PyRevitUIError('Can not retrieve item {} from {}'
                                 .format(cmp_name, self))

    def _add_component(self, new_component):
        self._sub_pyrvt_components[new_component.name] = new_component

    def _remove_component(self, expired_cmp_name):
        try:
            self._sub_pyrvt_components.pop(expired_cmp_name)
        except KeyError:
            raise PyRevitUIError('Can not remove item {} from {}'
                                 .format(expired_cmp_name, self))

    @property
    def visible(self):
        """Is container visible."""
        if hasattr(self._rvtapi_object, 'Visible'):
            return self._rvtapi_object.Visible
        elif hasattr(self._rvtapi_object, 'IsVisible'):
            return self._rvtapi_object.IsVisible
        else:
            return self._visible

    @visible.setter
    def visible(self, value):
        if hasattr(self._rvtapi_object, 'Visible'):
            self._rvtapi_object.Visible = value
        elif hasattr(self._rvtapi_object, 'IsVisible'):
            self._rvtapi_object.IsVisible = value
        else:
            self._visible = value

    @property
    def enabled(self):
        """Is container enabled."""
        if hasattr(self._rvtapi_object, 'Enabled'):
            return self._rvtapi_object.Enabled
        elif hasattr(self._rvtapi_object, 'IsEnabled'):
            return self._rvtapi_object.IsEnabled
        else:
            return self._enabled

    @enabled.setter
    def enabled(self, value):
        if hasattr(self._rvtapi_object, 'Enabled'):
            self._rvtapi_object.Enabled = value
        elif hasattr(self._rvtapi_object, 'IsEnabled'):
            self._rvtapi_object.IsEnabled = value
        else:
            self._enabled = value

    def process_deferred(self):
        try:
            if self._visible is not None:
                self.visible = self._visible
        except Exception as visible_err:
            raise PyRevitUIError('Error setting .visible {} | {} '
                                 .format(self, visible_err))

        try:
            if self._enabled is not None:
                self.enabled = self._enabled
        except Exception as enable_err:
            raise PyRevitUIError('Error setting .enabled {} | {} '
                                 .format(self, enable_err))

    def get_rvtapi_object(self):
        """Return underlying Revit API object for this container."""
        # FIXME: return type
        return self._rvtapi_object

    def set_rvtapi_object(self, rvtapi_obj):
        """Set underlying Revit API object for this container.

        Args:
            rvtapi_obj (obj): Revit API container object
        """
        # FIXME: rvtapi_obj type
        self._rvtapi_object = rvtapi_obj
        self.itemdata_mode = False
        self._dirty = True

    def get_adwindows_object(self):
        """Return underlying AdWindows API object for this container."""
        # FIXME: return type
        rvtapi_obj = self._rvtapi_object
        getRibbonItemMethod = \
            rvtapi_obj.GetType().GetMethod(
                'getRibbonItem',
                BindingFlags.NonPublic | BindingFlags.Instance
                )
        if getRibbonItemMethod:
            return getRibbonItemMethod.Invoke(rvtapi_obj, None)

    def get_flagged_children(self, state=True):
        """Get all children with their flag equal to given state.

        Flagging is a mechanism to mark certain containers. There are various
        reasons that container flagging might be used e.g. marking updated
        containers or the ones in need of an update or removal.

        Args:
            state (bool): flag state to filter children

        Returns:
            (list[*]): list of filtered child objects
        """
        # FIXME: return type
        flagged_cmps = []
        for component in self:
            flagged_cmps.extend(component.get_flagged_children(state))
            if component.is_dirty() == state:
                flagged_cmps.append(component)
        return flagged_cmps

    def keys(self):
        # FIXME: what does this do?
        list(self._sub_pyrvt_components.keys())

    def values(self):
        # FIXME: what does this do?
        list(self._sub_pyrvt_components.values())

    @staticmethod
    def is_native():
        """Is this container generated by pyRevit or is native."""
        return False

    def is_dirty(self):
        """Is dirty flag set."""
        if self._dirty:
            return self._dirty
        else:
            # check if there is any dirty child
            for component in self:
                if component.is_dirty():
                    return True
            return False

    def set_dirty_flag(self, state=True):
        """Set dirty flag to given state.

        See .get_flagged_children()

        Args:
            state (bool): state to set flag
        """
        self._dirty = state

    def contains(self, pyrvt_cmp_name):
        """Check if container contains a component with given name.

        Args:
            pyrvt_cmp_name (str): target component name
        """
        return pyrvt_cmp_name in self._sub_pyrvt_components.keys()

    def find_child(self, child_name):
        """Find a component with given name in children.

        Args:
            child_name (str): target component name

        Returns:
            (Any): component object if found, otherwise None
        """
        for sub_cmp in self._sub_pyrvt_components.values():
            if child_name == sub_cmp.name:
                return sub_cmp
            elif hasattr(sub_cmp, 'ui_title') \
                    and child_name == sub_cmp.ui_title:
                return sub_cmp

            component = sub_cmp.find_child(child_name)
            if component:
                return component

        return None

    def activate(self):
        """Activate this container in ui."""
        try:
            self.enabled = True
            self.visible = True
            self._dirty = True
        except Exception:
            raise PyRevitUIError('Can not activate: {}'.format(self))

    def deactivate(self):
        """Deactivate this container in ui."""
        try:
            self.enabled = False
            self.visible = False
            self._dirty = True
        except Exception:
            raise PyRevitUIError('Can not deactivate: {}'.format(self))

    def get_updated_items(self):
        # FIXME: reduntant, this is a use case and should be on uimaker side?
        return self.get_flagged_children()

    def get_unchanged_items(self):
        # FIXME: reduntant, this is a use case and should be on uimaker side?
        return self.get_flagged_children(state=False)

    def reorder_before(self, item_name, ritem_name):
        """Reorder and place item_name before ritem_name.

        Args:
            item_name (str): name of component to be moved
            ritem_name (str): name of component that should be on the right
        """
        apiobj = self.get_rvtapi_object()
        litem_idx = ritem_idx = None
        if hasattr(apiobj, 'Panels'):
            for item in apiobj.Panels:
                if item.Source.AutomationName == item_name:
                    litem_idx = apiobj.Panels.IndexOf(item)
                elif item.Source.AutomationName == ritem_name:
                    ritem_idx = apiobj.Panels.IndexOf(item)
            if litem_idx and ritem_idx:
                if litem_idx < ritem_idx:
                    apiobj.Panels.Move(litem_idx, ritem_idx - 1)
                elif litem_idx > ritem_idx:
                    apiobj.Panels.Move(litem_idx, ritem_idx)

    def reorder_beforeall(self, item_name):
        """Reorder and place item_name before all others.

        Args:
            item_name (str): name of component to be moved
        """
        # FIXME: verify docs description is correct
        apiobj = self.get_rvtapi_object()
        litem_idx = None
        if hasattr(apiobj, 'Panels'):
            for item in apiobj.Panels:
                if item.Source.AutomationName == item_name:
                    litem_idx = apiobj.Panels.IndexOf(item)
            if litem_idx:
                apiobj.Panels.Move(litem_idx, 0)

    def reorder_after(self, item_name, ritem_name):
        """Reorder and place item_name after ritem_name.

        Args:
            item_name (str): name of component to be moved
            ritem_name (str): name of component that should be on the left
        """
        apiobj = self.get_rvtapi_object()
        litem_idx = ritem_idx = None
        if hasattr(apiobj, 'Panels'):
            for item in apiobj.Panels:
                if item.Source.AutomationName == item_name:
                    litem_idx = apiobj.Panels.IndexOf(item)
                elif item.Source.AutomationName == ritem_name:
                    ritem_idx = apiobj.Panels.IndexOf(item)
            if litem_idx and ritem_idx:
                if litem_idx < ritem_idx:
                    apiobj.Panels.Move(litem_idx, ritem_idx)
                elif litem_idx > ritem_idx:
                    apiobj.Panels.Move(litem_idx, ritem_idx + 1)

    def reorder_afterall(self, item_name):
        """Reorder and place item_name after all others.

        Args:
            item_name (str): name of component to be moved
        """
        apiobj = self.get_rvtapi_object()
        litem_idx = None
        if hasattr(apiobj, 'Panels'):
            for item in apiobj.Panels:
                if item.Source.AutomationName == item_name:
                    litem_idx = apiobj.Panels.IndexOf(item)
            if litem_idx:
                max_idx = len(apiobj.Panels) - 1
                apiobj.Panels.Move(litem_idx, max_idx)


# Classes holding existing native ui elements
# (These elements are native and can not be modified) --------------------------
class GenericRevitNativeUIContainer(GenericPyRevitUIContainer):
    """Common base type for native Revit API UI containers."""
    def __init__(self):
        GenericPyRevitUIContainer.__init__(self)

    @staticmethod
    def is_native():
        """Is this container generated by pyRevit or is native."""
        return True

    def activate(self):
        """Activate this container in ui.

        Under current implementation, raises PyRevitUIError exception as
        native Revit API UI components should not be changed.
        """
        return self.deactivate()

    def deactivate(self):
        """Deactivate this container in ui.

        Under current implementation, raises PyRevitUIError exception as
        native Revit API UI components should not be changed.
        """
        raise PyRevitUIError('Can not de/activate native item: {}'
                             .format(self))


class RevitNativeRibbonButton(GenericRevitNativeUIContainer):
    """Revit API UI native ribbon button."""
    def __init__(self, adwnd_ribbon_button):
        GenericRevitNativeUIContainer.__init__(self)

        self.name = \
            safe_strtype(adwnd_ribbon_button.AutomationName)\
            .replace('\r\n', ' ')
        self._rvtapi_object = adwnd_ribbon_button


class RevitNativeRibbonGroupItem(GenericRevitNativeUIContainer):
    """Revit API UI native ribbon button."""
    def __init__(self, adwnd_ribbon_item):
        GenericRevitNativeUIContainer.__init__(self)

        self.name = adwnd_ribbon_item.Source.Title
        self._rvtapi_object = adwnd_ribbon_item

        # finding children on this button group
        for adwnd_ribbon_button in adwnd_ribbon_item.Items:
            self._add_component(RevitNativeRibbonButton(adwnd_ribbon_button))

    def button(self, name):
        """Get button item with given name.

        Args:
            name (str): name of button item to find

        Returns:
            (RevitNativeRibbonButton): button object if found
        """
        return super(RevitNativeRibbonGroupItem, self)._get_component(name)


class RevitNativeRibbonPanel(GenericRevitNativeUIContainer):
    """Revit API UI native ribbon button."""
    def __init__(self, adwnd_ribbon_panel):
        GenericRevitNativeUIContainer.__init__(self)

        self.name = adwnd_ribbon_panel.Source.Title
        self._rvtapi_object = adwnd_ribbon_panel

        all_adwnd_ribbon_items = []
        # getting a list of existing items under this panel
        # RibbonFoldPanel items are not visible. they automatically fold
        # buttons into stack on revit ui resize since RibbonFoldPanel are
        # not visible it does not make sense to create objects for them.
        # This pre-cleaner loop, finds the RibbonFoldPanel items and
        # adds the children to the main list
        for adwnd_ribbon_item in adwnd_ribbon_panel.Source.Items:
            if isinstance(adwnd_ribbon_item, AdWindows.RibbonFoldPanel):
                try:
                    for sub_rvtapi_item in adwnd_ribbon_item.Items:
                        all_adwnd_ribbon_items.append(sub_rvtapi_item)
                except Exception as append_err:
                    mlogger.debug('Can not get RibbonFoldPanel children: %s '
                                  '| %s', adwnd_ribbon_item, append_err)
            else:
                all_adwnd_ribbon_items.append(adwnd_ribbon_item)

        # processing the panel slideout for exising ribbon items
        for adwnd_slideout_item \
                in adwnd_ribbon_panel.Source.SlideOutPanelItemsView:
            all_adwnd_ribbon_items.append(adwnd_slideout_item)

        # processing the cleaned children list and
        # creating pyRevit native ribbon objects
        for adwnd_ribbon_item in all_adwnd_ribbon_items:
            try:
                if isinstance(adwnd_ribbon_item,
                              AdWindows.RibbonButton) \
                        or isinstance(adwnd_ribbon_item,
                                      AdWindows.RibbonToggleButton):
                    self._add_component(
                        RevitNativeRibbonButton(adwnd_ribbon_item))
                elif isinstance(adwnd_ribbon_item,
                                AdWindows.RibbonSplitButton):
                    self._add_component(
                        RevitNativeRibbonGroupItem(adwnd_ribbon_item))

            except Exception as append_err:
                mlogger.debug('Can not create native ribbon item: %s '
                              '| %s', adwnd_ribbon_item, append_err)

    def ribbon_item(self, item_name):
        """Get panel item with given name.

        Args:
            item_name (str): name of panel item to find

        Returns:
            (object):
                panel item if found, could be :obj:`RevitNativeRibbonButton`
                or :obj:`RevitNativeRibbonGroupItem`
        """
        return super(RevitNativeRibbonPanel, self)._get_component(item_name)


class RevitNativeRibbonTab(GenericRevitNativeUIContainer):
    """Revit API UI native ribbon tab."""
    def __init__(self, adwnd_ribbon_tab):
        GenericRevitNativeUIContainer.__init__(self)

        self.name = adwnd_ribbon_tab.Title
        self._rvtapi_object = adwnd_ribbon_tab

        # getting a list of existing panels under this tab
        try:
            for adwnd_ribbon_panel in adwnd_ribbon_tab.Panels:
                # only listing visible panels
                if adwnd_ribbon_panel.IsVisible:
                    self._add_component(
                        RevitNativeRibbonPanel(adwnd_ribbon_panel)
                    )
        except Exception as append_err:
            mlogger.debug('Can not get native panels for this native tab: %s '
                          '| %s', adwnd_ribbon_tab, append_err)

    def ribbon_panel(self, panel_name):
        """Get panel with given name.

        Args:
            panel_name (str): name of panel to find

        Returns:
            (RevitNativeRibbonPanel): panel if found
        """
        return super(RevitNativeRibbonTab, self)._get_component(panel_name)

    @staticmethod
    def is_pyrevit_tab():
        """Is this tab generated by pyRevit."""
        return False


# Classes holding non-native ui elements --------------------------------------
class _PyRevitSeparator(GenericPyRevitUIContainer):
    def __init__(self):
        GenericPyRevitUIContainer.__init__(self)
        self.name = coreutils.new_uuid()
        self.itemdata_mode = True


class _PyRevitRibbonButton(GenericPyRevitUIContainer):
    def __init__(self, ribbon_button):
        GenericPyRevitUIContainer.__init__(self)

        self.name = ribbon_button.Name
        self._rvtapi_object = ribbon_button

        # when container is in itemdata_mode, self._rvtapi_object is a
        # RibbonItemData and not an actual ui item a sunsequent call to
        # create_data_items will create ui for RibbonItemData objects
        self.itemdata_mode = isinstance(self._rvtapi_object,
                                        UI.RibbonItemData)

        self.ui_title = self.name
        if not self.itemdata_mode:
            self.ui_title = self._rvtapi_object.ItemText

        self.tooltip_image = self.tooltip_video = None

    def set_rvtapi_object(self, rvtapi_obj):
        GenericPyRevitUIContainer.set_rvtapi_object(self, rvtapi_obj)
        # update the ui title for the newly added rvtapi_obj
        self._rvtapi_object.ItemText = self.ui_title

    def set_icon(self, icon_file, icon_size=ICON_MEDIUM):
        try:
            button_icon = ButtonIcons(icon_file)
            rvtapi_obj = self.get_rvtapi_object()
            rvtapi_obj.Image = button_icon.small_bitmap
            if icon_size == ICON_LARGE:
                rvtapi_obj.LargeImage = button_icon.large_bitmap
            else:
                rvtapi_obj.LargeImage = button_icon.medium_bitmap
            self._dirty = True
        except Exception as icon_err:
            raise PyRevitUIError('Error in applying icon to button > {} : {}'
                                 .format(icon_file, icon_err))

    def set_tooltip(self, tooltip):
        try:
            if tooltip:
                self.get_rvtapi_object().ToolTip = tooltip
            else:
                adwindows_obj = self.get_adwindows_object()
                if adwindows_obj and adwindows_obj.ToolTip:
                    adwindows_obj.ToolTip.Content = None
            self._dirty = True
        except Exception as tooltip_err:
            raise PyRevitUIError('Item does not have tooltip property: {}'
                                 .format(tooltip_err))

    def set_tooltip_ext(self, tooltip_ext):
        try:
            if tooltip_ext:
                self.get_rvtapi_object().LongDescription = tooltip_ext
            else:
                adwindows_obj = self.get_adwindows_object()
                if adwindows_obj and adwindows_obj.ToolTip:
                    adwindows_obj.ToolTip.ExpandedContent = None
            self._dirty = True
        except Exception as tooltip_err:
            raise PyRevitUIError('Item does not have extended '
                                 'tooltip property: {}'.format(tooltip_err))

    def set_tooltip_image(self, tooltip_image):
        try:
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj and adwindows_obj.ToolTip:
                adwindows_obj.ToolTip.ExpandedImage = \
                    load_bitmapimage(tooltip_image)
            else:
                self.tooltip_image = tooltip_image
        except Exception as ttimage_err:
            raise PyRevitUIError('Error setting tooltip image {} | {} '
                                 .format(tooltip_image, ttimage_err))

    def set_tooltip_video(self, tooltip_video):
        try:
            adwindows_obj = self.get_adwindows_object()
            if isinstance(self.get_rvtapi_object().ToolTip, str):
                exToolTip = self.get_rvtapi_object().ToolTip
            else:
                exToolTip = None
            if adwindows_obj:
                adwindows_obj.ToolTip = AdWindows.RibbonToolTip()
                adwindows_obj.ToolTip.Title = self.ui_title
                adwindows_obj.ToolTip.Content = exToolTip
                _StackPanel = System.Windows.Controls.StackPanel()
                _video = System.Windows.Controls.MediaElement()
                _video.Source = Uri(tooltip_video)
                _StackPanel.Children.Add(_video)
                adwindows_obj.ToolTip.ExpandedContent = _StackPanel
                adwindows_obj.ResolveToolTip()
            else:
                self.tooltip_video = tooltip_video
        except Exception as ttvideo_err:
            raise PyRevitUIError('Error setting tooltip video {} | {} '
                                 .format(tooltip_video, ttvideo_err))

    def set_tooltip_media(self, tooltip_media):
        if tooltip_media.endswith(DEFAULT_TOOLTIP_IMAGE_FORMAT):
            self.set_tooltip_image(tooltip_media)
        elif tooltip_media.endswith(DEFAULT_TOOLTIP_VIDEO_FORMAT):
            self.set_tooltip_video(tooltip_media)

    def reset_highlights(self):
        if hasattr(AdInternal.Windows, 'HighlightMode'):
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj:
                adwindows_obj.Highlight = \
                    coreutils.get_enum_none(AdInternal.Windows.HighlightMode)

    def highlight_as_new(self):
        if hasattr(AdInternal.Windows, 'HighlightMode'):
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj:
                adwindows_obj.Highlight = \
                    AdInternal.Windows.HighlightMode.New

    def highlight_as_updated(self):
        if hasattr(AdInternal.Windows, 'HighlightMode'):
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj:
                adwindows_obj.Highlight = \
                    AdInternal.Windows.HighlightMode.Updated

    def process_deferred(self):
        GenericPyRevitUIContainer.process_deferred(self)

        try:
            if self.tooltip_image:
                self.set_tooltip_image(self.tooltip_image)
        except Exception as ttvideo_err:
            raise PyRevitUIError('Error setting deffered tooltip image {} | {} '
                                 .format(self.tooltip_video, ttvideo_err))

        try:
            if self.tooltip_video:
                self.set_tooltip_video(self.tooltip_video)
        except Exception as ttvideo_err:
            raise PyRevitUIError('Error setting deffered tooltip video {} | {} '
                                 .format(self.tooltip_video, ttvideo_err))

    def get_contexthelp(self):
        return self.get_rvtapi_object().GetContextualHelp()

    def set_contexthelp(self, ctxhelpurl):
        if ctxhelpurl:
            ch = UI.ContextualHelp(UI.ContextualHelpType.Url, ctxhelpurl)
            self.get_rvtapi_object().SetContextualHelp(ch)

    def set_title(self, ui_title):
        if self.itemdata_mode:
            self.ui_title = ui_title
            self._dirty = True
        else:
            self._rvtapi_object.ItemText = self.ui_title = ui_title
            self._dirty = True

    def get_title(self):
        if self.itemdata_mode:
            return self.ui_title
        else:
            return self._rvtapi_object.ItemText

    def get_control_id(self):
        adwindows_obj = self.get_adwindows_object()
        if adwindows_obj and hasattr(adwindows_obj, 'Id'):
            return getattr(adwindows_obj, 'Id', '')

    @property
    def assembly_name(self):
        return self._rvtapi_object.AssemblyName

    @property
    def class_name(self):
        return self._rvtapi_object.ClassName

    @property
    def availability_class_name(self):
        return self._rvtapi_object.AvailabilityClassName


class _PyRevitRibbonGroupItem(GenericPyRevitUIContainer):

    button = GenericPyRevitUIContainer._get_component

    def __init__(self, ribbon_item):
        GenericPyRevitUIContainer.__init__(self)

        self.name = ribbon_item.Name
        self._rvtapi_object = ribbon_item

        # when container is in itemdata_mode, self._rvtapi_object is a
        # RibbonItemData and not an actual ui item when container is in
        # itemdata_mode, only the necessary RibbonItemData objects will
        # be created for children a sunsequent call to create_data_items
        # will create ui for RibbonItemData objects
        self.itemdata_mode = isinstance(self._rvtapi_object,
                                        UI.RibbonItemData)

        # if button group shows the active button icon, then the child
        # buttons need to have large icons
        self._use_active_item_icon = self.is_splitbutton()

        # by default the last item used, stays on top as the default button
        self._sync_with_cur_item = True

        # getting a list of existing items under this item group.
        if not self.itemdata_mode:
            for revit_button in ribbon_item.GetItems():
                # feeding _sub_native_ribbon_items with an instance of
                # _PyRevitRibbonButton for existing buttons
                self._add_component(_PyRevitRibbonButton(revit_button))

    def is_splitbutton(self):
        return isinstance(self._rvtapi_object, UI.SplitButton) \
               or isinstance(self._rvtapi_object, UI.SplitButtonData)

    def set_rvtapi_object(self, rvtapi_obj):
        GenericPyRevitUIContainer.set_rvtapi_object(self, rvtapi_obj)
        if self.is_splitbutton():
            self.get_rvtapi_object().IsSynchronizedWithCurrentItem = \
                self._sync_with_cur_item

    def create_data_items(self):
        # iterate through data items and their associated revit
        # api data objects and create ui objects
        for pyrvt_ui_item in [x for x in self if x.itemdata_mode]:
            rvtapi_data_obj = pyrvt_ui_item.get_rvtapi_object()

            # create item in ui and get correspoding revit ui objects
            if isinstance(pyrvt_ui_item, _PyRevitRibbonButton):
                rvtapi_ribbon_item = \
                    self.get_rvtapi_object().AddPushButton(rvtapi_data_obj)
                rvtapi_ribbon_item.ItemText = pyrvt_ui_item.get_title()

                # replace data object with the newly create ribbon item
                pyrvt_ui_item.set_rvtapi_object(rvtapi_ribbon_item)

                # extended tooltips (images and videos) can only be applied when
                # the ui element is created
                pyrvt_ui_item.process_deferred()

            elif isinstance(pyrvt_ui_item, _PyRevitSeparator):
                self.get_rvtapi_object().AddSeparator()

        self.itemdata_mode = False

    def sync_with_current_item(self, state):
        try:
            if not self.itemdata_mode:
                self.get_rvtapi_object().IsSynchronizedWithCurrentItem = state
            self._sync_with_cur_item = state
            self._dirty = True
        except Exception as sync_item_err:
            raise PyRevitUIError('Item is not a split button. '
                                 '| {}'.format(sync_item_err))

    def set_icon(self, icon_file, icon_size=ICON_LARGE):
        try:
            button_icon = ButtonIcons(icon_file)
            rvtapi_obj = self.get_rvtapi_object()
            rvtapi_obj.Image = button_icon.small_bitmap
            if icon_size == ICON_LARGE:
                rvtapi_obj.LargeImage = button_icon.large_bitmap
            else:
                rvtapi_obj.LargeImage = button_icon.medium_bitmap
            self._dirty = True
        except Exception as icon_err:
            raise PyRevitUIError('Error in applying icon to button > {} : {}'
                                 .format(icon_file, icon_err))

    def get_contexthelp(self):
        return self.get_rvtapi_object().GetContextualHelp()

    def set_contexthelp(self, ctxhelpurl):
        if ctxhelpurl:
            ch = UI.ContextualHelp(UI.ContextualHelpType.Url, ctxhelpurl)
            self.get_rvtapi_object().SetContextualHelp(ch)

    def reset_highlights(self):
        if hasattr(AdInternal.Windows, 'HighlightMode'):
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj:
                adwindows_obj.HighlightDropDown = \
                    coreutils.get_enum_none(AdInternal.Windows.HighlightMode)

    def highlight_as_new(self):
        if hasattr(AdInternal.Windows, 'HighlightMode'):
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj:
                adwindows_obj.HighlightDropDown = \
                    AdInternal.Windows.HighlightMode.New

    def highlight_as_updated(self):
        if hasattr(AdInternal.Windows, 'HighlightMode'):
            adwindows_obj = self.get_adwindows_object()
            if adwindows_obj:
                adwindows_obj.HighlightDropDown = \
                    AdInternal.Windows.HighlightMode.Updated

    def create_push_button(self, button_name, asm_location, class_name,
                           icon_path='',
                           tooltip='', tooltip_ext='', tooltip_media='',
                           ctxhelpurl=None,
                           avail_class_name=None,
                           update_if_exists=False, ui_title=None):
        if self.contains(button_name):
            if update_if_exists:
                existing_item = self._get_component(button_name)
                try:
                    # Assembly and Class info of current active script
                    # button can not be updated.
                    if button_name != EXEC_PARAMS.command_name:
                        rvtapi_obj = existing_item.get_rvtapi_object()
                        rvtapi_obj.AssemblyName = asm_location
                        rvtapi_obj.ClassName = class_name
                        if avail_class_name:
                            existing_item.get_rvtapi_object() \
                                .AvailabilityClassName = avail_class_name
                except Exception as asm_update_err:
                    mlogger.debug('Error updating button asm info: %s '
                                  '| %s', button_name, asm_update_err)

                if not icon_path:
                    mlogger.debug('Icon not set for %s', button_name)
                else:
                    try:
                        # if button group shows the active button icon,
                        # then the child buttons need to have large icons
                        existing_item.set_icon(icon_path,
                                               icon_size=ICON_LARGE
                                               if self._use_active_item_icon
                                               else ICON_MEDIUM)
                    except PyRevitUIError as iconerr:
                        mlogger.error('Error adding icon for %s | %s',
                                      button_name, iconerr)

                existing_item.set_tooltip(tooltip)
                existing_item.set_tooltip_ext(tooltip_ext)
                if tooltip_media:
                    existing_item.set_tooltip_media(tooltip_media)

                # if ctx help on this group matches the existing,
                # update self ctx before changing the existing item ctx help
                self_ctxhelp = self.get_contexthelp()
                ctx_help = existing_item.get_contexthelp()
                if self_ctxhelp and ctx_help \
                        and self_ctxhelp.HelpType == ctx_help.HelpType \
                        and self_ctxhelp.HelpPath == ctx_help.HelpPath:
                    self.set_contexthelp(ctxhelpurl)
                # now change the existing item ctx help
                existing_item.set_contexthelp(ctxhelpurl)

                if ui_title:
                    existing_item.set_title(ui_title)

                existing_item.activate()
                return
            else:
                raise PyRevitUIError('Push button already exits and update '
                                     'is not allowed: {}'.format(button_name))

        mlogger.debug('Parent does not include this button. Creating: %s',
                      button_name)
        try:
            button_data = \
                UI.PushButtonData(button_name,
                                  button_name,
                                  asm_location,
                                  class_name)
            if avail_class_name:
                button_data.AvailabilityClassName = avail_class_name
            if not self.itemdata_mode:
                ribbon_button = \
                    self.get_rvtapi_object().AddPushButton(button_data)
                new_button = _PyRevitRibbonButton(ribbon_button)
            else:
                new_button = _PyRevitRibbonButton(button_data)

            if ui_title:
                new_button.set_title(ui_title)

            if not icon_path:
                mlogger.debug('Icon not set for %s', button_name)
            else:
                mlogger.debug('Creating icon for push button %s from file: %s',
                              button_name, icon_path)
                try:
                    # if button group shows the active button icon,
                    # then the child buttons need to have large icons
                    new_button.set_icon(
                        icon_path,
                        icon_size=ICON_LARGE
                        if self._use_active_item_icon else ICON_MEDIUM)
                except PyRevitUIError as iconerr:
                    mlogger.debug('Error adding icon for %s from %s '
                                  '| %s', button_name, icon_path, iconerr)

            new_button.set_tooltip(tooltip)
            new_button.set_tooltip_ext(tooltip_ext)
            if tooltip_media:
                new_button.set_tooltip_media(tooltip_media)

            new_button.set_contexthelp(ctxhelpurl)
            # if this is the first button being added
            if not self.keys():
                mlogger.debug('Setting ctx help on parent: %s', ctxhelpurl)
                self.set_contexthelp(ctxhelpurl)

            new_button.set_dirty_flag()
            self._add_component(new_button)

        except Exception as create_err:
            raise PyRevitUIError('Can not create button '
                                 '| {}'.format(create_err))

    def add_separator(self):
        if not self.itemdata_mode:
            self.get_rvtapi_object().AddSeparator()
        else:
            sep_cmp = _PyRevitSeparator()
            self._add_component(sep_cmp)
        self._dirty = True


class _PyRevitRibbonPanel(GenericPyRevitUIContainer):

    button = GenericPyRevitUIContainer._get_component
    ribbon_item = GenericPyRevitUIContainer._get_component

    def __init__(self, rvt_ribbon_panel, parent_tab):
        GenericPyRevitUIContainer.__init__(self)

        self.name = rvt_ribbon_panel.Name
        self._rvtapi_object = rvt_ribbon_panel

        self.parent_tab = parent_tab

        # when container is in itemdata_mode, only the necessary
        # RibbonItemData objects will be created for children a sunsequent
        # call to create_data_items will create ui for RibbonItemData objects
        # This is specifically helpful when creating stacks in panels.
        # open_stack and close_stack control this parameter
        self.itemdata_mode = False

        # getting a list of existing panels under this tab
        for revit_ribbon_item in self.get_rvtapi_object().GetItems():
            # feeding _sub_native_ribbon_items with an instance of
            # _PyRevitRibbonGroupItem for existing group items
            # _PyRevitRibbonPanel will find its existing ribbon items internally
            if isinstance(revit_ribbon_item, UI.PulldownButton):
                self._add_component(
                    _PyRevitRibbonGroupItem(revit_ribbon_item))
            elif isinstance(revit_ribbon_item, UI.PushButton):
                self._add_component(_PyRevitRibbonButton(revit_ribbon_item))
            else:
                raise PyRevitUIError('Can not determin ribbon item type: {}'
                                     .format(revit_ribbon_item))

    def get_adwindows_object(self):
        for panel in self.parent_tab.Panels:
            if panel.Source and panel.Source.Title == self.name:
                return panel

    def set_background(self, argb_color):
        panel_adwnd_obj = self.get_adwindows_object()
        color = argb_to_brush(argb_color)
        panel_adwnd_obj.CustomPanelBackground = color
        panel_adwnd_obj.CustomPanelTitleBarBackground = color
        panel_adwnd_obj.CustomSlideOutPanelBackground = color

    def reset_backgrounds(self):
        panel_adwnd_obj = self.get_adwindows_object()
        panel_adwnd_obj.CustomPanelBackground = None
        panel_adwnd_obj.CustomPanelTitleBarBackground = None
        panel_adwnd_obj.CustomSlideOutPanelBackground = None

    def set_panel_background(self, argb_color):
        panel_adwnd_obj = self.get_adwindows_object()
        panel_adwnd_obj.CustomPanelBackground = \
            argb_to_brush(argb_color)

    def set_title_background(self, argb_color):
        panel_adwnd_obj = self.get_adwindows_object()
        panel_adwnd_obj.CustomPanelTitleBarBackground = \
            argb_to_brush(argb_color)

    def set_slideout_background(self, argb_color):
        panel_adwnd_obj = self.get_adwindows_object()
        panel_adwnd_obj.CustomSlideOutPanelBackground = \
            argb_to_brush(argb_color)

    def reset_highlights(self):
        # no highlighting options for panels
        pass

    def highlight_as_new(self):
        # no highlighting options for panels
        pass

    def highlight_as_updated(self):
        # no highlighting options for panels
        pass

    def get_collapse(self):
        panel_adwnd_obj = self.get_adwindows_object()
        return panel_adwnd_obj.IsCollapsed

    def set_collapse(self, state):
        panel_adwnd_obj = self.get_adwindows_object()
        panel_adwnd_obj.IsCollapsed = state

    def open_stack(self):
        self.itemdata_mode = True

    def close_stack(self):
        self._create_data_items()

    def add_separator(self):
        self.get_rvtapi_object().AddSeparator()
        self._dirty = True

    def add_slideout(self):
        try:
            self.get_rvtapi_object().AddSlideOut()
            self._dirty = True
        except Exception as slideout_err:
            raise PyRevitUIError('Error adding slide out: {}'
                                 .format(slideout_err))

    def _create_data_items(self):
        # FIXME: if one item changes in stack and others dont change,
        # button will be created as pushbutton out of stack
        self.itemdata_mode = False

        # get a list of data item names and the
        # associated revit api data objects
        pyrvt_data_item_names = [x.name for x in self if x.itemdata_mode]
        rvtapi_data_objs = [x.get_rvtapi_object()
                            for x in self if x.itemdata_mode]

        # list of newly created revit_api ribbon items
        created_rvtapi_ribbon_items = []

        # create stack items in ui and get correspoding revit ui objects
        data_obj_count = len(rvtapi_data_objs)

        # if there are two or 3 items, create a proper stack
        if data_obj_count == 2 or data_obj_count == 3:
            created_rvtapi_ribbon_items = \
                self.get_rvtapi_object().AddStackedItems(*rvtapi_data_objs)
        # if there is only one item added,
        # add that to panel and forget about stacking
        elif data_obj_count == 1:
            rvtapi_pushbutton = \
                self.get_rvtapi_object().AddItem(*rvtapi_data_objs)
            created_rvtapi_ribbon_items.append(rvtapi_pushbutton)
        # if no items have been added, log the empty stack and return
        elif data_obj_count == 0:
            mlogger.debug('No new items has been added to stack. '
                          'Skipping stack creation.')
        # if none of the above, more than 3 items have been added.
        # Cleanup data item cache and raise an error.
        else:
            for pyrvt_data_item_name in pyrvt_data_item_names:
                self._remove_component(pyrvt_data_item_name)
            raise PyRevitUIError('Can not create stack of {}. '
                                 'Stack can only have 2 or 3 items.'
                                 .format(data_obj_count))

        # now that items are created and revit api objects are ready
        # iterate over the ribbon items and inject revit api objects
        # into the child pyrevit items
        for rvtapi_ribbon_item, pyrvt_data_item_name \
                in zip(created_rvtapi_ribbon_items, pyrvt_data_item_names):
            pyrvt_ui_item = self._get_component(pyrvt_data_item_name)
            # pyrvt_ui_item only had button data info.
            # Now that ui ribbon item has created, update pyrvt_ui_item
            # with corresponding revit api object.
            # .set_rvtapi_object() disables .itemdata_mode since
            # they're no longer data objects
            pyrvt_ui_item.set_rvtapi_object(rvtapi_ribbon_item)

            # extended tooltips (images and videos) can only be applied when
            # the ui element is created
            if isinstance(pyrvt_ui_item, _PyRevitRibbonButton):
                pyrvt_ui_item.process_deferred()

            # if pyrvt_ui_item is a group,
            # create children and update group item data
            if isinstance(pyrvt_ui_item, _PyRevitRibbonGroupItem):
                pyrvt_ui_item.create_data_items()

    def set_dlglauncher(self, dlg_button):
        panel_adwnd_obj = self.get_adwindows_object()
        button_adwnd_obj = dlg_button.get_adwindows_object()
        panel_adwnd_obj.Source.Items.Remove(button_adwnd_obj)
        panel_adwnd_obj.Source.DialogLauncher = button_adwnd_obj
        mlogger.debug('Added panel dialog button %s', dlg_button.name)

    def create_push_button(self, button_name, asm_location, class_name,
                           icon_path='',
                           tooltip='', tooltip_ext='', tooltip_media='',
                           ctxhelpurl=None,
                           avail_class_name=None,
                           update_if_exists=False, ui_title=None):
        if self.contains(button_name):
            if update_if_exists:
                existing_item = self._get_component(button_name)
                try:
                    # Assembly and Class info of current active
                    # script button can not be updated.
                    if button_name != EXEC_PARAMS.command_name:
                        rvtapi_obj = existing_item.get_rvtapi_object()
                        rvtapi_obj.AssemblyName = asm_location
                        rvtapi_obj.ClassName = class_name
                        if avail_class_name:
                            rvtapi_obj.AvailabilityClassName = avail_class_name
                except Exception as asm_update_err:
                    mlogger.debug('Error updating button asm info: %s '
                                  '| %s', button_name, asm_update_err)

                existing_item.set_tooltip(tooltip)
                existing_item.set_tooltip_ext(tooltip_ext)
                if tooltip_media:
                    existing_item.set_tooltip_media(tooltip_media)

                existing_item.set_contexthelp(ctxhelpurl)

                if ui_title:
                    existing_item.set_title(ui_title)

                if not icon_path:
                    mlogger.debug('Icon not set for %s', button_name)
                else:
                    try:
                        existing_item.set_icon(icon_path, icon_size=ICON_LARGE)
                    except PyRevitUIError as iconerr:
                        mlogger.error('Error adding icon for %s '
                                      '| %s', button_name, iconerr)
                existing_item.activate()
            else:
                raise PyRevitUIError('Push button already exits and update '
                                     'is not allowed: {}'.format(button_name))
        else:
            mlogger.debug('Parent does not include this button. Creating: %s',
                          button_name)
            try:
                button_data = \
                    UI.PushButtonData(button_name,
                                      button_name,
                                      asm_location,
                                      class_name)
                if avail_class_name:
                    button_data.AvailabilityClassName = avail_class_name
                if not self.itemdata_mode:
                    ribbon_button = \
                        self.get_rvtapi_object().AddItem(button_data)
                    new_button = _PyRevitRibbonButton(ribbon_button)
                else:
                    new_button = _PyRevitRibbonButton(button_data)

                if ui_title:
                    new_button.set_title(ui_title)

                if not icon_path:
                    mlogger.debug('Parent ui item is a panel and '
                                  'panels don\'t have icons.')
                else:
                    mlogger.debug('Creating icon for push button %s '
                                  'from file: %s', button_name, icon_path)
                    try:
                        new_button.set_icon(icon_path, icon_size=ICON_LARGE)
                    except PyRevitUIError as iconerr:
                        mlogger.error('Error adding icon for %s from %s '
                                      '| %s', button_name, icon_path, iconerr)

                new_button.set_tooltip(tooltip)
                new_button.set_tooltip_ext(tooltip_ext)
                if tooltip_media:
                    new_button.set_tooltip_media(tooltip_media)

                new_button.set_contexthelp(ctxhelpurl)

                new_button.set_dirty_flag()
                self._add_component(new_button)

            except Exception as create_err:
                raise PyRevitUIError('Can not create button | {}'
                                     .format(create_err))

    def _create_button_group(self, pulldowndata_type, item_name, icon_path,
                             update_if_exists=False):
        if self.contains(item_name):
            if update_if_exists:
                exiting_item = self._get_component(item_name)
                exiting_item.activate()
                if icon_path:
                    exiting_item.set_icon(icon_path)
            else:
                raise PyRevitUIError('Pull down button already exits and '
                                     'update is not allowed: {}'
                                     .format(item_name))
        else:
            mlogger.debug('Panel does not include this pull down button. '
                          'Creating: %s', item_name)
            try:
                # creating pull down button data and add to child list
                pdbutton_data = pulldowndata_type(item_name, item_name)
                if not self.itemdata_mode:
                    mlogger.debug('Creating pull down button: %s in %s',
                                  item_name, self)
                    new_push_button = \
                        self.get_rvtapi_object().AddItem(pdbutton_data)
                    pyrvt_pdbutton = _PyRevitRibbonGroupItem(new_push_button)
                    try:
                        pyrvt_pdbutton.set_icon(icon_path)
                    except PyRevitUIError as iconerr:
                        mlogger.debug('Error adding icon for %s from %s '
                                      '| %s', item_name, icon_path, iconerr)
                else:
                    mlogger.debug('Creating pull down button under stack: '
                                  '%s in %s', item_name, self)
                    pyrvt_pdbutton = _PyRevitRibbonGroupItem(pdbutton_data)
                    try:
                        pyrvt_pdbutton.set_icon(icon_path)
                    except PyRevitUIError as iconerr:
                        mlogger.debug('Error adding icon for %s from %s '
                                      '| %s', item_name, icon_path, iconerr)

                pyrvt_pdbutton.set_dirty_flag()
                self._add_component(pyrvt_pdbutton)

            except Exception as button_err:
                raise PyRevitUIError('Can not create pull down button: {}'
                                     .format(button_err))

    def create_pulldown_button(self, item_name, icon_path,
                               update_if_exists=False):
        self._create_button_group(UI.PulldownButtonData, item_name, icon_path,
                                  update_if_exists)

    def create_split_button(self, item_name, icon_path,
                            update_if_exists=False):
        if self.itemdata_mode and HOST_APP.is_older_than('2017'):
            raise PyRevitUIError('Revits earlier than 2017 do not support '
                                 'split buttons in a stack.')
        else:
            self._create_button_group(UI.SplitButtonData, item_name, icon_path,
                                      update_if_exists)
            self.ribbon_item(item_name).sync_with_current_item(True)

    def create_splitpush_button(self, item_name, icon_path,
                                update_if_exists=False):
        if self.itemdata_mode and HOST_APP.is_older_than('2017'):
            raise PyRevitUIError('Revits earlier than 2017 do not support '
                                 'split buttons in a stack.')
        else:
            self._create_button_group(UI.SplitButtonData, item_name, icon_path,
                                      update_if_exists)
            self.ribbon_item(item_name).sync_with_current_item(False)

    def create_panel_push_button(self, button_name, asm_location, class_name,
                                 tooltip='', tooltip_ext='', tooltip_media='',
                                 ctxhelpurl=None,
                                 avail_class_name=None,
                                 update_if_exists=False):
        self.create_push_button(button_name=button_name,
                                asm_location=asm_location,
                                class_name=class_name,
                                icon_path=None,
                                tooltip=tooltip,
                                tooltip_ext=tooltip_ext,
                                tooltip_media=tooltip_media,
                                ctxhelpurl=ctxhelpurl,
                                avail_class_name=avail_class_name,
                                update_if_exists=update_if_exists,
                                ui_title=None)
        self.set_dlglauncher(self.button(button_name))


class _PyRevitRibbonTab(GenericPyRevitUIContainer):
    ribbon_panel = GenericPyRevitUIContainer._get_component

    def __init__(self, revit_ribbon_tab, is_pyrvt_tab=False):
        GenericPyRevitUIContainer.__init__(self)

        self.name = revit_ribbon_tab.Title
        self._rvtapi_object = revit_ribbon_tab

        # is this tab created by pyrevit.revitui?
        if is_pyrvt_tab:
            self._rvtapi_object.Tag = PYREVIT_TAB_IDENTIFIER

        # getting a list of existing panels under this tab
        try:
            for revit_ui_panel in HOST_APP.uiapp.GetRibbonPanels(self.name):
                # feeding _sub_pyrvt_ribbon_panels with an instance of
                # _PyRevitRibbonPanel for existing panels _PyRevitRibbonPanel
                # will find its existing ribbon items internally
                new_pyrvt_panel = _PyRevitRibbonPanel(revit_ui_panel,
                                                      self._rvtapi_object)
                self._add_component(new_pyrvt_panel)
        except:
            # if .GetRibbonPanels fails, this tab is an existing native tab
            raise PyRevitUIError('Can not get panels for this tab: {}'
                                 .format(self._rvtapi_object))

    def get_adwindows_object(self):
        return self.get_rvtapi_object()

    def reset_highlights(self):
        # no highlighting options for tabs
        pass

    def highlight_as_new(self):
        # no highlighting options for tabs
        pass

    def highlight_as_updated(self):
        # no highlighting options for tabs
        pass

    @staticmethod
    def check_pyrevit_tab(revit_ui_tab):
        return hasattr(revit_ui_tab, 'Tag') \
               and revit_ui_tab.Tag == PYREVIT_TAB_IDENTIFIER

    def is_pyrevit_tab(self):
        return self.get_rvtapi_object().Tag == PYREVIT_TAB_IDENTIFIER

    def update_name(self, new_name):
        self.get_rvtapi_object().Title = new_name

    def create_ribbon_panel(self, panel_name, update_if_exists=False):
        """Create ribbon panel (RevitUI.RibbonPanel) from panel_name."""
        if self.contains(panel_name):
            if update_if_exists:
                exiting_pyrvt_panel = self._get_component(panel_name)
                exiting_pyrvt_panel.activate()
            else:
                raise PyRevitUIError('RibbonPanel already exits and update '
                                     'is not allowed: {}'.format(panel_name))
        else:
            try:
                # creating panel in tab
                ribbon_panel = \
                    HOST_APP.uiapp.CreateRibbonPanel(self.name, panel_name)
                # creating _PyRevitRibbonPanel object and
                # add new panel to list of current panels
                pyrvt_ribbon_panel = _PyRevitRibbonPanel(ribbon_panel,
                                                         self._rvtapi_object)
                pyrvt_ribbon_panel.set_dirty_flag()
                self._add_component(pyrvt_ribbon_panel)

            except Exception as panel_err:
                raise PyRevitUIError('Can not create panel: {}'
                                     .format(panel_err))


class _PyRevitUI(GenericPyRevitUIContainer):
    """Captures the existing ui state and elements at creation."""

    ribbon_tab = GenericPyRevitUIContainer._get_component

    def __init__(self, all_native=False):
        GenericPyRevitUIContainer.__init__(self)

        # Revit does not have any method to get a list of current tabs.
        # Getting a list of current tabs using adwindows.dll methods
        # Iterating over tabs,
        # because ComponentManager.Ribbon.Tabs.FindTab(tab.name)
        # does not return invisible tabs
        for revit_ui_tab in AdWindows.ComponentManager.Ribbon.Tabs:
            # feeding self._sub_pyrvt_ribbon_tabs with an instance of
            # _PyRevitRibbonTab or RevitNativeRibbonTab for each existing
            # tab. _PyRevitRibbonTab or RevitNativeRibbonTab will find
            # their existing panels only listing visible tabs
            # (there might be tabs with identical names
            # e.g. there are two Annotate tabs. They are activated as
            # neccessary per context but need to add inactive/invisible
            # pyrevit tabs (PYREVIT_TAB_IDENTIFIER) anyway.
            # if revit_ui_tab.IsVisible
            try:
                if not all_native \
                        and _PyRevitRibbonTab.check_pyrevit_tab(revit_ui_tab):
                    new_pyrvt_tab = _PyRevitRibbonTab(revit_ui_tab)
                else:
                    new_pyrvt_tab = RevitNativeRibbonTab(revit_ui_tab)
                self._add_component(new_pyrvt_tab)
                mlogger.debug('Tab added to the list of tabs: %s',
                              new_pyrvt_tab.name)
            except PyRevitUIError:
                # if _PyRevitRibbonTab(revit_ui_tab) fails,
                # Revit restricts access to its panels RevitNativeRibbonTab
                # uses a different method to access the panels
                # to interact with existing native ui
                new_pyrvt_tab = RevitNativeRibbonTab(revit_ui_tab)
                self._add_component(new_pyrvt_tab)
                mlogger.debug('Native tab added to the list of tabs: %s',
                              new_pyrvt_tab.name)

    def get_adwindows_ribbon_control(self):
        return AdWindows.ComponentManager.Ribbon

    @staticmethod
    def toggle_ribbon_updator(
            state,
            flow_direction=Windows.FlowDirection.LeftToRight):
        # cancel out the ribbon updator from previous runtime version
        current_ribbon_updator = \
            envvars.get_pyrevit_env_var(envvars.RIBBONUPDATOR_ENVVAR)
        if current_ribbon_updator:
            current_ribbon_updator.StopUpdatingRibbon()

        # reset env var
        envvars.set_pyrevit_env_var(envvars.RIBBONUPDATOR_ENVVAR, None)
        if state:
            # start or stop the ribbon updator
            panel_set = None
            try:
                main_wnd = ui.get_mainwindow()
                ribbon_root_type = ui.get_ribbon_roottype()
                panel_set = \
                    main_wnd.FindFirstChild[ribbon_root_type](main_wnd)
            except Exception as raex:
                mlogger.error('Error activating ribbon updator. | %s', raex)
                return

            if panel_set:
                types.RibbonEventUtils.StartUpdatingRibbon(
                    panelSet=panel_set,
                    flowDir=flow_direction,
                    tagTag=PYREVIT_TAB_IDENTIFIER
                )
                # set the new colorizer
                envvars.set_pyrevit_env_var(
                    envvars.RIBBONUPDATOR_ENVVAR,
                    types.RibbonEventUtils
                    )

    def set_RTL_flow(self):
        _PyRevitUI.toggle_ribbon_updator(
            state=True,
            flow_direction=Windows.FlowDirection.RightToLeft
            )

    def set_LTR_flow(self):
        # default is LTR, make sure any existing is stopped
        _PyRevitUI.toggle_ribbon_updator(state=False)

    def unset_RTL_flow(self):
        _PyRevitUI.toggle_ribbon_updator(state=False)

    def unset_LTR_flow(self):
        # default is LTR, make sure any existing is stopped
        _PyRevitUI.toggle_ribbon_updator(state=False)

    def get_pyrevit_tabs(self):
        return [tab for tab in self if tab.is_pyrevit_tab()]

    def create_ribbon_tab(self, tab_name, update_if_exists=False):
        if self.contains(tab_name):
            if update_if_exists:
                existing_pyrvt_tab = self._get_component(tab_name)
                existing_pyrvt_tab.activate()
            else:
                raise PyRevitUIError('RibbonTab already exits and update is '
                                     'not allowed: {}'.format(tab_name))
        else:
            try:
                # creating tab in Revit ui
                HOST_APP.uiapp.CreateRibbonTab(tab_name)
                # HOST_APP.uiapp.CreateRibbonTab() does
                # not return the created tab object.
                # so find the tab object in exiting ui
                revit_tab_ctrl = None
                for exiting_rvt_ribbon_tab in \
                        AdWindows.ComponentManager.Ribbon.Tabs:
                    if exiting_rvt_ribbon_tab.Title == tab_name:
                        revit_tab_ctrl = exiting_rvt_ribbon_tab

                # create _PyRevitRibbonTab object with
                # the recovered RibbonTab object
                # and add new _PyRevitRibbonTab to list of current tabs
                if revit_tab_ctrl:
                    pyrvt_ribbon_tab = _PyRevitRibbonTab(revit_tab_ctrl,
                                                         is_pyrvt_tab=True)
                    pyrvt_ribbon_tab.set_dirty_flag()
                    self._add_component(pyrvt_ribbon_tab)
                else:
                    raise PyRevitUIError('Tab created but can not '
                                         'be obtained from ui.')

            except Exception as tab_create_err:
                raise PyRevitUIError('Can not create tab: {}'
                                     .format(tab_create_err))


# Public function to return an instance of _PyRevitUI which is used
# to interact with current ui --------------------------------------------------
def get_current_ui(all_native=False):
    """Revit UI Wrapper class for interacting with current pyRevit UI.

    Returned class provides min required functionality for user interaction

    Examples:
        ```python
        current_ui = pyrevit.session.current_ui()
        this_script = pyrevit.session.get_this_command()
        current_ui.update_button_icon(this_script, new_icon)
        ```

    Returns:
        (_PyRevitUI): wrapper around active ribbon gui
    """
    return _PyRevitUI(all_native=all_native)


def get_uibutton(command_unique_name):
    """Find and return ribbon ui button with given unique id.

    Args:
        command_unique_name (str): unique id of pyRevit command

    Returns:
        (_PyRevitRibbonButton): ui button wrapper object
    """
    # FIXME: verify return type
    pyrvt_tabs = get_current_ui().get_pyrevit_tabs()
    for tab in pyrvt_tabs:
        button = tab.find_child(command_unique_name)
        if button:
            return button
    return None

"""Base module for handling extensions parsing."""
from pyrevit import HOST_APP, EXEC_PARAMS

# Extension types
# ------------------------------------------------------------------------------
LIB_EXTENSION_POSTFIX = '.lib'
UI_EXTENSION_POSTFIX = '.extension'


class UIExtensionType:
    """UI extension type."""
    ID = 'extension'
    POSTFIX = '.extension'


class LIBExtensionType:
    """Library extension type."""
    ID = 'lib'
    POSTFIX = '.lib'


class ExtensionTypes:
    """Extension types."""
    UI_EXTENSION = UIExtensionType
    LIB_EXTENSION = LIBExtensionType

    @classmethod
    def get_ext_types(cls):
        ext_types = set()
        for attr in dir(cls):
            if attr.endswith('_EXTENSION'):
                ext_types.add(getattr(cls, attr))
        return ext_types


# -----------------------------------------------------------------------------
# supported scripting languages
PYTHON_LANG = 'python'
CSHARP_LANG = 'csharp'
VB_LANG = 'visualbasic'
RUBY_LANG = 'ruby'
DYNAMO_LANG = 'dynamobim'
GRASSHOPPER_LANG = 'grasshopper'
# cpython hash-bang
CPYTHON_HASHBANG = '#! python3'

# supported script files
PYTHON_SCRIPT_FILE_FORMAT = '.py'
CSHARP_SCRIPT_FILE_FORMAT = '.cs'
VB_SCRIPT_FILE_FORMAT = '.vb'
RUBY_SCRIPT_FILE_FORMAT = '.rb'
DYNAMO_SCRIPT_FILE_FORMAT = '.dyn'
GRASSHOPPER_SCRIPT_FILE_FORMAT = '.gh'
GRASSHOPPERX_SCRIPT_FILE_FORMAT = '.ghx'
CONTENT_FILE_FORMAT = '.rfa'

# extension startup script
EXT_STARTUP_NAME = 'startup'
PYTHON_EXT_STARTUP_FILE = EXT_STARTUP_NAME + PYTHON_SCRIPT_FILE_FORMAT
CSHARP_EXT_STARTUP_FILE = EXT_STARTUP_NAME + CSHARP_SCRIPT_FILE_FORMAT
VB_EXT_STARTUP_FILE = EXT_STARTUP_NAME + VB_SCRIPT_FILE_FORMAT
RUBY_EXT_STARTUP_FILE = EXT_STARTUP_NAME + RUBY_SCRIPT_FILE_FORMAT

# -----------------------------------------------------------------------------
# supported metadata formats
YAML_FILE_FORMAT = '.yaml'
JSON_FILE_FORMAT = '.json'

# metadata filenames
EXT_MANIFEST_NAME = 'extension'
EXT_MANIFEST_FILE = EXT_MANIFEST_NAME + JSON_FILE_FORMAT

DEFAULT_BUNDLEMATA_NAME = 'bundle'
BUNDLEMATA_POSTFIX = DEFAULT_BUNDLEMATA_NAME + YAML_FILE_FORMAT

# metadata schema: Exensions

# metadata schema: Bundles
MDATA_UI_TITLE = 'title'
MDATA_TOOLTIP = 'tooltip'
MDATA_AUTHOR = 'author'
MDATA_AUTHORS = 'authors'
MDATA_LAYOUT = 'layout'
MDATA_COMMAND_HELP_URL = 'help_url'
MDATA_COMMAND_CONTEXT = 'context'
MDATA_COMMAND_CONTEXT_TYPE = "type"
MDATA_COMMAND_CONTEXT_NOT = "not_"
MDATA_COMMAND_CONTEXT_ANY = "any"
MDATA_COMMAND_CONTEXT_ALL = "all"
MDATA_COMMAND_CONTEXT_EXACT = "exact"
MDATA_COMMAND_CONTEXT_NOTANY = MDATA_COMMAND_CONTEXT_NOT + MDATA_COMMAND_CONTEXT_ANY
MDATA_COMMAND_CONTEXT_NOTALL = MDATA_COMMAND_CONTEXT_NOT + MDATA_COMMAND_CONTEXT_ALL
MDATA_COMMAND_CONTEXT_NOTEXACT = MDATA_COMMAND_CONTEXT_NOT + MDATA_COMMAND_CONTEXT_EXACT
MDATA_COMMAND_CONTEXT_ANY_SEP = "|"
MDATA_COMMAND_CONTEXT_ALL_SEP = "&"
MDATA_COMMAND_CONTEXT_EXACT_SEP = ";"
MDATA_COMMAND_CONTEXT_RULE = "({rule})"
MDATA_MIN_REVIT_VERSION = 'min_revit_version'
MDATA_MAX_REVIT_VERSION = 'max_revit_version'
MDATA_BETA_SCRIPT = 'is_beta'
MDATA_ENGINE = 'engine'
MDATA_ENGINE_CLEAN = 'clean'
MDATA_ENGINE_FULLFRAME = 'full_frame'
MDATA_ENGINE_PERSISTENT = 'persistent'
MDATA_ENGINE_MAINTHREAD = 'mainthread'
MDATA_LINK_BUTTON_MODULES = 'modules'
MDATA_LINK_BUTTON_ASSEMBLY = 'assembly'
MDATA_LINK_BUTTON_COMMAND_CLASS = 'command_class'
MDATA_LINK_BUTTON_AVAIL_COMMAND_CLASS = 'availability_class'
MDATA_URL_BUTTON_HYPERLINK = 'hyperlink'
MDATA_TEMPLATES_KEY = 'templates'
MDATA_BACKGROUND_KEY = 'background'
MDATA_BACKGROUND_PANEL_KEY = 'panel'
MDATA_BACKGROUND_TITLE_KEY = 'title'
MDATA_BACKGROUND_SLIDEOUT_KEY = 'slideout'
MDATA_HIGHLIGHT_KEY = 'highlight'
MDATA_HIGHLIGHT_TYPE_NEW = 'new'
MDATA_HIGHLIGHT_TYPE_UPDATED = 'updated'
MDATA_COLLAPSED_KEY = 'collapsed'
# metadata schema: DynamoBIM bundles
MDATA_ENGINE_DYNAMO_AUTOMATE = 'automate'
MDATA_ENGINE_DYNAMO_PATH = 'dynamo_path'
# MDATA_ENGINE_DYNAMO_PATH_EXEC = 'dynamo_path_exec'
MDATA_ENGINE_DYNAMO_PATH_CHECK_EXIST = 'dynamo_path_check_existing'
MDATA_ENGINE_DYNAMO_FORCE_MANUAL_RUN = 'dynamo_force_manual_run'
MDATA_ENGINE_DYNAMO_MODEL_NODES_INFO = 'dynamo_model_nodes_info'

# metadata schema: Bundles | legacy
UI_TITLE_PARAM = '__title__'
DOCSTRING_PARAM = '__doc__'
AUTHOR_PARAM = '__author__'
AUTHORS_PARAM = '__authors__'
COMMAND_HELP_URL_PARAM = '__helpurl__'
COMMAND_CONTEXT_PARAM = '__context__'
MIN_REVIT_VERSION_PARAM = '__min_revit_ver__'
MAX_REVIT_VERSION_PARAM = '__max_revit_ver__'
SHIFT_CLICK_PARAM = '__shiftclick__'
BETA_SCRIPT_PARAM = '__beta__'
HIGHLIGHT_SCRIPT_PARAM = '__highlight__'
CLEAN_ENGINE_SCRIPT_PARAM = '__cleanengine__'
FULLFRAME_ENGINE_PARAM = '__fullframeengine__'
PERSISTENT_ENGINE_PARAM = '__persistentengine__'

# -----------------------------------------------------------------------------
# supported bundles
TAB_POSTFIX = '.tab'
PANEL_POSTFIX = '.panel'
LINK_BUTTON_POSTFIX = '.linkbutton'
INVOKE_BUTTON_POSTFIX = '.invokebutton'
PUSH_BUTTON_POSTFIX = '.pushbutton'
SMART_BUTTON_POSTFIX = '.smartbutton'
PULLDOWN_BUTTON_POSTFIX = '.pulldown'
STACK_BUTTON_POSTFIX = '.stack'
SPLIT_BUTTON_POSTFIX = '.splitbutton'
SPLITPUSH_BUTTON_POSTFIX = '.splitpushbutton'
PANEL_PUSH_BUTTON_POSTFIX = '.panelbutton'
NOGUI_COMMAND_POSTFIX = '.nobutton'
CONTENT_BUTTON_POSTFIX = '.content'
URL_BUTTON_POSTFIX = '.urlbutton'

# known bundle sub-directories
COMP_LIBRARY_DIR_NAME = 'lib'
COMP_BIN_DIR_NAME = 'bin'
COMP_HOOKS_DIR_NAME = 'hooks'
COMP_CHECKS_DIR_NAME = 'checks'

# unique ids
UNIQUE_ID_SEPARATOR = '-'

# bundle layout elements
SEPARATOR_IDENTIFIER = '---'
SLIDEOUT_IDENTIFIER = '>>>'

# bundle icon
ICON_FILE_FORMAT = '.png'
ICON_DARK_SUFFIX = '.dark'
DEFAULT_ICON_FILE = 'icon' + ICON_FILE_FORMAT
DEFAULT_ON_ICON_FILE = 'on' + ICON_FILE_FORMAT
DEFAULT_OFF_ICON_FILE = 'off' + ICON_FILE_FORMAT

# bundle media for tooltips
DEFAULT_MEDIA_FILENAME = 'tooltip'

# bundle scripts
DEFAULT_SCRIPT_NAME = 'script'
DEFAULT_CONFIG_NAME = 'config'

# script files
PYTHON_SCRIPT_POSTFIX = DEFAULT_SCRIPT_NAME + PYTHON_SCRIPT_FILE_FORMAT
PYTHON_CONFIG_SCRIPT_POSTFIX = DEFAULT_CONFIG_NAME + PYTHON_SCRIPT_FILE_FORMAT

CSHARP_SCRIPT_POSTFIX = DEFAULT_SCRIPT_NAME + CSHARP_SCRIPT_FILE_FORMAT
CSHARP_CONFIG_SCRIPT_POSTFIX = DEFAULT_CONFIG_NAME + CSHARP_SCRIPT_FILE_FORMAT

VB_SCRIPT_POSTFIX = DEFAULT_SCRIPT_NAME + VB_SCRIPT_FILE_FORMAT
VB_CONFIG_SCRIPT_POSTFIX = DEFAULT_CONFIG_NAME + VB_SCRIPT_FILE_FORMAT

RUBY_SCRIPT_POSTFIX = DEFAULT_SCRIPT_NAME + RUBY_SCRIPT_FILE_FORMAT
RUBY_CONFIG_SCRIPT_POSTFIX = DEFAULT_CONFIG_NAME + RUBY_SCRIPT_FILE_FORMAT

DYNAMO_SCRIPT_POSTFIX = DEFAULT_SCRIPT_NAME + DYNAMO_SCRIPT_FILE_FORMAT
DYNAMO_CONFIG_SCRIPT_POSTFIX = DEFAULT_CONFIG_NAME + DYNAMO_SCRIPT_FILE_FORMAT

GRASSHOPPER_SCRIPT_POSTFIX = \
    DEFAULT_SCRIPT_NAME + GRASSHOPPER_SCRIPT_FILE_FORMAT
GRASSHOPPER_CONFIG_SCRIPT_POSTFIX = \
    DEFAULT_CONFIG_NAME + GRASSHOPPER_SCRIPT_FILE_FORMAT

GRASSHOPPERX_SCRIPT_POSTFIX = \
    DEFAULT_SCRIPT_NAME + GRASSHOPPERX_SCRIPT_FILE_FORMAT
GRASSHOPPERX_CONFIG_SCRIPT_POSTFIX = \
    DEFAULT_CONFIG_NAME + GRASSHOPPERX_SCRIPT_FILE_FORMAT

# bundle content
DEFAULT_CONTENT_NAME = 'content'
DEFAULT_ALT_CONTENT_NAME = 'other'

CONTENT_POSTFIX = DEFAULT_CONTENT_NAME + CONTENT_FILE_FORMAT
CONTENT_VERSION_POSTFIX = \
    DEFAULT_CONTENT_NAME + "_{version}" + CONTENT_FILE_FORMAT

ALT_CONTENT_POSTFIX = DEFAULT_ALT_CONTENT_NAME + CONTENT_FILE_FORMAT
ALT_CONTENT_VERSION_POSTFIX = \
    DEFAULT_ALT_CONTENT_NAME + "_{version}" + CONTENT_FILE_FORMAT

# bundle help
HELP_FILE_PATTERN = r'.*help\..+'

# -----------------------------------------------------------------------------
# Command bundle defaults
CTX_SELETION = 'selection'
CTX_ZERODOC = 'zero-doc'

"""Find, parse and cache extensions.

There are two types of extensions: UI Extensions (components.Extension) and
Library Extensions (components.LibraryExtension).

This module, finds the ui extensions installed and parses their directory for
tools or loads them from cache. It also finds the library extensions and adds
their directory address to the ui extensions so the python tools can use
the shared libraries.

To do its job correctly, this module needs to communicate with
pyrevit.userconfig to get a list of user extension folder and also
pyrevit.extensions.extpackages to check whether an extension is active or not.
"""

from pyrevit import EXEC_PARAMS
from pyrevit import MAIN_LIB_DIR, MISC_LIB_DIR
from pyrevit.coreutils.logger import get_logger
from pyrevit.userconfig import user_config


if not EXEC_PARAMS.doc_mode:
    try:
        if user_config.bin_cache:
            from pyrevit.extensions.cacher_bin import is_cache_valid,\
                get_cached_extension, update_cache
        else:
            from pyrevit.extensions.cacher_asc import is_cache_valid,\
                get_cached_extension, update_cache
    except AttributeError:
        user_config.bin_cache = True
        user_config.save_changes()
        from pyrevit.extensions.cacher_bin import is_cache_valid,\
            get_cached_extension, update_cache

#pylint: disable=C0413
from pyrevit.extensions.parser import parse_dir_for_ext_type,\
    get_parsed_extension, parse_comp_dir
from pyrevit.extensions.genericcomps import GenericUICommand
from pyrevit.extensions.components import Extension, LibraryExtension

import pyrevit.extensions.extpackages as extpkgs


#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


def _update_extension_search_paths(ui_ext, lib_ext_list, pyrvt_paths):
    for lib_ext in lib_ext_list:
        ui_ext.add_module_path(lib_ext.directory)

    for pyrvt_path in pyrvt_paths:
        ui_ext.add_module_path(pyrvt_path)


def _is_extension_enabled(ext_info):
    try:
        ext_pkg = extpkgs.get_ext_package_by_name(ext_info.name)
        if ext_pkg:
            return ext_pkg.is_enabled and ext_pkg.user_has_access
        else:
            mlogger.debug('Extension package is not defined: %s', ext_info.name)
    except Exception as ext_check_err:
        mlogger.error('Error checking state for extension: %s | %s',
                      ext_info.name, ext_check_err)

    # Lets be nice and load the package if it is not defined
    return True


def _remove_disabled_extensions(ext_list):
    cleaned_ext_list = []
    for extension in ext_list:
        if _is_extension_enabled(extension):
            cleaned_ext_list.append(extension)
        else:
            mlogger.debug('Skipping disabled extension: %s', extension.name)

    return cleaned_ext_list


def _parse_or_cache(ext_info):
    # parse the extension if ui_extension does not have a valid cache
    if not is_cache_valid(ext_info):
        mlogger.debug('Cache is not valid for: %s', ext_info)

        # Either cache is not available, not valid, or cache load has failed.
        # parse directory for components and return fully loaded ui_extension
        mlogger.debug('Parsing for ui_extension...')
        ui_extension = get_parsed_extension(ext_info)

        # update cache with newly parsed ui_extension
        mlogger.debug('UI Extension successfuly parsed: %s', ui_extension.name)
        mlogger.debug('Updating cache for ui_extension: %s', ui_extension.name)
        update_cache(ui_extension)

    # otherwise load the cache
    else:
        mlogger.debug('Cache is valid for: %s', ext_info)
        # if cache is valid, load the cached ui_extension
        # cacher module takes the ui_extension object and
        # injects cache data into it.
        ui_extension = get_cached_extension(ext_info)
        mlogger.debug('UI Extension successfuly loaded from cache: %s',
                     ui_extension.name)

    return ui_extension


def get_command_from_path(comp_path):
    """Returns a pyRevit command object from the given bundle directory.

    Args:
        comp_path (str): Full directory address of the command bundle

    Returns:
        (genericcomps.GenericUICommand): A subclass of pyRevit command object.
    """
    cmds = parse_comp_dir(comp_path, GenericUICommand)
    if cmds:
        return cmds[0]

    return None


def get_thirdparty_extension_data():
    """Returns all installed and active UI and Library extensions (not parsed).

    Returns:
        (list): list of components.Extension or components.LibraryExtension
    """
    # FIXME: reorganzie this code to use one single method to collect
    # extension data for both lib and ui
    ext_data_list = []

    for root_dir in user_config.get_thirdparty_ext_root_dirs():
        ext_data_list.extend(
            [ui_ext for ui_ext in parse_dir_for_ext_type(root_dir,
                                                         Extension)])
        ext_data_list.extend(
            [lib_ext for lib_ext in parse_dir_for_ext_type(root_dir,
                                                           LibraryExtension)])

    return _remove_disabled_extensions(ext_data_list)


def get_installed_lib_extensions(root_dir):
    """Returns all the installed and active Library extensions (not parsed).

    Args:
        root_dir (str): Extensions directory address

    Returns:
        (list[LibraryExtension]): list of components.LibraryExtension objects
    """
    lib_ext_list = \
        [lib_ext for lib_ext in parse_dir_for_ext_type(root_dir,
                                                       LibraryExtension)]
    return _remove_disabled_extensions(lib_ext_list)


def get_installed_ui_extensions():
    """Returns all UI extensions (fully parsed) under the given directory.

    This will also process the Library extensions and will add
    their path to the syspath of the UI extensions.

    Returns:
        (list[Extension]): list of components.Extension objects
    """
    ui_ext_list = []
    lib_ext_list = []

    # get a list of all directories that could include extensions
    ext_search_dirs = user_config.get_ext_root_dirs()
    mlogger.debug('Extension Directories: %s', ext_search_dirs)

    # collect all library extensions. Their dir paths need to be added
    # to sys.path for all commands
    for root_dir in ext_search_dirs:
        lib_ext_list.extend(get_installed_lib_extensions(root_dir))
        # Get a list of all installed extensions in this directory
        # _parser.parse_dir_for_ext_type() returns a list of extensions
        # in given directory

    for root_dir in ext_search_dirs:
        for ext_info in parse_dir_for_ext_type(root_dir, Extension):
            # test if cache is valid for this ui_extension
            # it might seem unusual to create a ui_extension and then
            # re-load it from cache but minimum information about the
            # ui_extension needs to be passed to the cache module for proper
            # hash calculation and ui_extension recovery. at this point
            # `ui_extension` does not include any sub-components
            # (e.g, tabs, panels, etc) ui_extension object is very small and
            # its creation doesn't add much overhead.

            if _is_extension_enabled(ext_info):
                ui_extension = _parse_or_cache(ext_info)
                ui_ext_list.append(ui_extension)
            else:
                mlogger.debug('Skipping disabled ui extension: %s',
                             ext_info.name)

    # update extension master syspaths with standard pyrevit lib paths and
    # lib address of other lib extensions (to support extensions that provide
    # library only to be used by other extensions)
    # all other lib paths internal to the extension and tool bundles have
    # already been set inside the extension bundles and will take precedence
    # over paths added by this method (they're the first paths added to the
    # search paths list, and these paths will follow)
    for ui_extension in ui_ext_list:
        _update_extension_search_paths(
            ui_extension,
            lib_ext_list,
            [MAIN_LIB_DIR, MISC_LIB_DIR]
            )

    return ui_ext_list

"""Base module to handle processing extensions as packages."""
import os
import os.path as op
import codecs
import json
from collections import defaultdict

from pyrevit import PyRevitException, HOST_APP
from pyrevit import labs
from pyrevit.compat import safe_strtype
from pyrevit.coreutils.logger import get_logger
from pyrevit.coreutils import git, fully_remove_dir
from pyrevit.userconfig import user_config

from pyrevit import extensions as exts


#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


class PyRevitPluginAlreadyInstalledException(PyRevitException):
    """Exception raised when extension is already installed."""
    def __init__(self, extpkg):
        super(PyRevitPluginAlreadyInstalledException, self).__init__()
        self.extpkg = extpkg
        PyRevitException(self)


class PyRevitPluginNoInstallLinkException(PyRevitException):
    """Exception raised when extension does not have an install link."""
    pass


class PyRevitPluginRemoveException(PyRevitException):
    """Exception raised when removing an extension."""
    pass


PLUGIN_EXT_DEF_MANIFEST_NAME = 'extensions'
PLUGIN_EXT_DEF_FILE = PLUGIN_EXT_DEF_MANIFEST_NAME + exts.JSON_FILE_FORMAT

EXTENSION_POSTFIXES = [x.POSTFIX for x in exts.ExtensionTypes.get_ext_types()]


class DependencyGraph:
    """Extension packages dependency graph."""
    def __init__(self, extpkg_list):
        self.dep_dict = defaultdict(list)
        self.extpkgs = extpkg_list
        for extpkg in extpkg_list:
            if extpkg.dependencies:
                for dep_pkg_name in extpkg.dependencies:
                    self.dep_dict[dep_pkg_name].append(extpkg)

    def has_installed_dependents(self, extpkg_name):
        if extpkg_name in self.dep_dict:
            for dep_pkg in self.dep_dict[extpkg_name]:
                if dep_pkg.is_installed:
                    return True
        else:
            return False


class ExtensionPackage:
    """Extension package class.

    This class contains the extension information and also manages installation,
    user configuration, and removal of the extension.
    See the ``__init__`` class documentation for the required and
    optional extension information.

    Attributes:
        type (extensions.ExtensionTypes): Extension type
        name (str): Extension name
        description (str): Extension description
        url (str): Url of online git repository
        website (str): Url of extension website
        image (str): Url of extension icon image (.png file)
        author (str): Name of extension author
        author_profile (str): Url of author profile
    """

    def __init__(self, info_dict, def_file_path=None):
        """Initialized the extension class based on provide information.

        Required info (Dictionary keys):
            type, name, description, url

        Optional info:
            website, image, author, author-url, authusers

        Args:
            info_dict (dict): A dictionary containing the required information
                              for initializing the extension.
            def_file_path (str): The file path of the extension definition file
        """
        self.type = exts.ExtensionTypes.UI_EXTENSION
        self.builtin = False
        self.default_enabled = True
        self.name = None
        self.description = None
        self.url = None
        self.def_file_path = set()
        self.authusers = set()
        self.authgroups = set()
        self.rocket_mode_compatible = False
        self.website = None
        self.image = None
        self.author = None
        self.author_profile = None
        self.dependencies = set()

        self.update_info(info_dict, def_file_path=def_file_path)

    def update_info(self, info_dict, def_file_path=None):
        ext_def_type = info_dict.get('type', None)
        for ext_type in exts.ExtensionTypes.get_ext_types():
            if ext_def_type == ext_type.ID:
                self.type = ext_type

        self.builtin = \
            safe_strtype(info_dict.get('builtin',
                                       self.builtin)).lower() == 'true'

        self.default_enabled = safe_strtype(
            info_dict.get('default_enabled', self.default_enabled)
            ).lower() == 'true'

        self.name = info_dict.get('name', self.name)
        self.description = info_dict.get('description', self.description)
        self.url = info_dict.get('url', self.url)

        if def_file_path:
            self.def_file_path.add(def_file_path)

        # update list of authorized users
        authusers = info_dict.get('authusers', [])
        if authusers:
            self.authusers.update(authusers)

        # update list of authorized user groups
        authgroups = info_dict.get('authgroups', [])
        if authgroups:
            self.authgroups.update(authgroups)

        # rocket mode compatibility
        self.rocket_mode_compatible = \
            safe_strtype(
                info_dict.get('rocket_mode_compatible',
                              self.rocket_mode_compatible)
                ).lower() == 'true'

        # extended attributes
        self.website = info_dict.get(
            'website',
            self.url.replace('.git', '') if self.url else self.website
            )
        self.image = info_dict.get('image', self.image)
        self.author = info_dict.get('author', self.author)

        self.author_profile = info_dict.get('author_profile',
                                            self.author_profile)
        # update list dependencies
        depends = info_dict.get('dependencies', [])
        if depends:
            self.dependencies.update(depends)

    def is_valid(self):
        return self.name is not None and self.url is not None

    def __repr__(self):
        return '<ExtensionPackage object. name:\'{}\' url:\'{}\' auth:{}>'\
            .format(self.name, self.url, self.authusers)

    @property
    def ext_dirname(self):
        """Installation directory name to use.

        Returns:
            (str): The name that should be used for the installation directory
                (based on the extension type).
        """
        return self.name + self.type.POSTFIX

    @property
    def is_installed(self):
        """Installation directory.

        Returns:
            (str): Installed directory path or empty string if not installed.
        """
        for ext_dir in user_config.get_ext_root_dirs():
            if op.exists(ext_dir):
                for sub_dir in os.listdir(ext_dir):
                    if op.isdir(op.join(ext_dir, sub_dir))\
                            and sub_dir == self.ext_dirname:
                        return op.join(ext_dir, sub_dir)
            else:
                mlogger.error('custom Extension path does not exist: %s',
                              ext_dir)

        return ''

    @property
    def installed_dir(self):
        """Installation directory.

        Returns:
            (str): Installed directory path or empty string if not installed.
        """
        return self.is_installed

    @property
    def is_removable(self):
        """Whether the extension is safe to remove.

        Checks whether it is safe to remove this extension by confirming if
        a git url is provided for this extension for later re-install.

        Returns:
            (bool): True if removable, False if not
        """
        return True if self.url else False

    @property
    def version(self):
        """Extension version.

        Returns:
            (str): Last commit hash of the extension git repo.
        """
        try:
            if self.is_installed:
                extpkg_repo = git.get_repo(self.installed_dir)
                return extpkg_repo.last_commit_hash
        except Exception:
            return None

    @property
    def config(self):
        """Returns a valid config manager for this extension.

        All config parameters will be saved in user config file.

        Returns:
            (pyrevit.coreutils.configparser.PyRevitConfigSectionParser):
                Config section handler
        """
        try:
            return user_config.get_section(self.ext_dirname)
        except Exception:
            cfg_section = user_config.add_section(self.ext_dirname)
            self.config.disabled = not self.default_enabled
            self.config.private_repo = self.builtin
            self.config.username = self.config.password = ''
            user_config.save_changes()
            return cfg_section

    @property
    def is_enabled(self):
        """Checks the default and user configured load state of the extension.

        Returns:
            (bool): True if package should be loaded
        """
        return not self.config.disabled

    @property
    def user_has_access(self):
        """Checks whether current user has access to this extension.

        Returns:
            (bool): True is current user has access
        """
        if self.authusers:
            return HOST_APP.username in self.authusers
        elif self.authgroups:
            for authgroup in self.authgroups:
                if labs.Common.Security.UserAuth.\
                        UserIsInSecurityGroup(authgroup):
                    return True
        else:
            return True

    def remove_pkg_config(self):
        """Removes the installed extension configuration."""
        user_config.remove_section(self.ext_dirname)
        user_config.save_changes()

    def disable_package(self):
        """Disables package in pyRevit configuration.

        It won't be loaded in the next session.
        """
        self.config.disabled = True
        user_config.save_changes()

    def toggle_package(self):
        """Disables/Enables package in pyRevit configuration.

        A disabled package won't be loaded in the next session.
        """
        self.config.disabled = not self.config.disabled
        user_config.save_changes()


def _update_extpkgs(ext_def_file, loaded_pkgs):
    with codecs.open(ext_def_file, 'r', 'utf-8') as extpkg_def_file:
        try:
            extpkg_dict = json.load(extpkg_def_file)
            defined_exts_pkgs = [extpkg_dict]
            if PLUGIN_EXT_DEF_MANIFEST_NAME in extpkg_dict.keys():
                defined_exts_pkgs = \
                    extpkg_dict[PLUGIN_EXT_DEF_MANIFEST_NAME]
        except Exception as def_file_err:
            print('Can not parse plugin ext definition file: {} '
                  '| {}'.format(ext_def_file, def_file_err))
            return

    for extpkg_def in defined_exts_pkgs:
        extpkg = ExtensionPackage(extpkg_def, ext_def_file)
        matched_pkg = None
        for loaded_pkg in loaded_pkgs:
            if loaded_pkg.name == extpkg.name:
                matched_pkg = loaded_pkg
                break
        if matched_pkg:
            matched_pkg.update_info(extpkg_def)
        elif extpkg.is_valid():
            loaded_pkgs.append(extpkg)


def _install_extpkg(extpkg, install_dir, install_dependencies=True):
    is_installed_path = extpkg.is_installed
    if is_installed_path:
        raise PyRevitPluginAlreadyInstalledException(extpkg)

    # if package is installable
    if extpkg.url:
        clone_path = op.join(install_dir, extpkg.ext_dirname)
        mlogger.info('Installing %s to %s', extpkg.name, clone_path)

        if extpkg.config.username and extpkg.config.password:
            git.git_clone(extpkg.url, clone_path,
                          username=extpkg.config.username,
                          password=extpkg.config.password)
        else:
            git.git_clone(extpkg.url, clone_path)
        mlogger.info('Extension successfully installed :thumbs_up:')
    else:
        raise PyRevitPluginNoInstallLinkException()

    if install_dependencies:
        if extpkg.dependencies:
            mlogger.info('Installing dependencies for %s', extpkg.name)
            for dep_pkg_name in extpkg.dependencies:
                dep_pkg = get_ext_package_by_name(dep_pkg_name)
                if dep_pkg:
                    _install_extpkg(dep_pkg,
                                     install_dir,
                                     install_dependencies=True)


def _remove_extpkg(extpkg, remove_dependencies=True):
    if extpkg.is_removable:
        dir_to_remove = extpkg.is_installed
        if dir_to_remove:
            fully_remove_dir(dir_to_remove)
            extpkg.remove_pkg_config()
            mlogger.info('Successfully removed extension from: %s',
                         dir_to_remove)
        else:
            raise PyRevitPluginRemoveException('Can not find installed path.')
    else:
        raise PyRevitPluginRemoveException('Extension does not have url '
                                           'and can not be installed later.')

    if remove_dependencies:
        dg = get_dependency_graph()
        mlogger.info('Removing dependencies for %s', extpkg.name)
        for dep_pkg_name in extpkg.dependencies:
            dep_pkg = get_ext_package_by_name(dep_pkg_name)
            if dep_pkg and not dg.has_installed_dependents(dep_pkg_name):
                _remove_extpkg(dep_pkg, remove_dependencies=True)


def _find_internal_extpkgs(ext_dir):
    internal_extpkg_def_files = []
    mlogger.debug('Looking for internal package defs under %s', ext_dir)
    for subfolder in os.listdir(ext_dir):
        if any([subfolder.endswith(x) for x in EXTENSION_POSTFIXES]):
            mlogger.debug('Found extension folder %s', subfolder)
            int_extpkg_deffile = \
                op.join(ext_dir, subfolder, exts.EXT_MANIFEST_FILE)
            mlogger.debug('Looking for %s', int_extpkg_deffile)
            if op.exists(int_extpkg_deffile):
                mlogger.debug('Found %s', int_extpkg_deffile)
                internal_extpkg_def_files.append(int_extpkg_deffile)
    return internal_extpkg_def_files


def get_ext_packages(authorized_only=True):
    """Returns the registered plugin extensions packages.

    Reads the list of registered plug-in extensions and returns a list of
    ExtensionPackage classes which contain information on the plug-in extension.

    Args:
        authorized_only (bool): Only return authorized extensions

    Returns:
        (list[ExtensionPackage]): list of registered plugin extensions
    """
    extpkgs = []
    for ext_dir in user_config.get_ext_root_dirs():
        # make a list of all availabe extension definition sources
        # default is under the extensions directory that ships with pyrevit
        extpkg_def_files = {op.join(ext_dir, PLUGIN_EXT_DEF_FILE)}
        # add other sources added by the user (using the cli)
        extpkg_def_files.update(user_config.get_ext_sources())
        for extpkg_def_file in extpkg_def_files:
            mlogger.debug('Looking for %s', extpkg_def_file)
            # check for external ext def file
            if op.exists(extpkg_def_file):
                mlogger.debug('Found %s', extpkg_def_file)
                _update_extpkgs(extpkg_def_file, extpkgs)
            # check internals now
            internal_extpkg_defs = _find_internal_extpkgs(ext_dir)
            for int_def_file in internal_extpkg_defs:
                _update_extpkgs(int_def_file, extpkgs)

    if authorized_only:
        return [x for x in extpkgs if x.user_has_access]

    return extpkgs


def get_ext_package_by_name(extpkg_name):
    for extpkg in get_ext_packages(authorized_only=False):
        if extpkg.name == extpkg_name:
            return extpkg
    return None


def get_dependency_graph():
    return DependencyGraph(get_ext_packages(authorized_only=False))


def install(extpkg, install_dir, install_dependencies=True):
    """Install the extension in the given parent directory.

    This method uses .installed_dir property of extension object 
    as installation directory name for this extension.
    This method also handles installation of extension dependencies.

    Args:
        extpkg (ExtensionPackage): Extension package to be installed
        install_dir (str): Parent directory to install extension in.
        install_dependencies (bool): Install the dependencies as well

    Raises:
        PyRevitException: on install error with error message
    """
    try:
        _install_extpkg(extpkg, install_dir, install_dependencies)
    except PyRevitPluginAlreadyInstalledException as already_installed_err:
        mlogger.warning('%s extension is already installed under %s',
                        already_installed_err.extpkg.name,
                        already_installed_err.extpkg.is_installed)
    except PyRevitPluginNoInstallLinkException:
        mlogger.error('Extension does not have an install link '
                      'and can not be installed.')


def remove(extpkg, remove_dependencies=True):
    """Removes the extension.

    Removes the extension from its installed directory
    and clears its configuration.

    Args:
        extpkg (ExtensionPackage): Extension package to be removed
        remove_dependencies (bool): Remove the dependencies as well

    Raises:
        PyRevitException: on remove error with error message
    """
    try:
        _remove_extpkg(extpkg, remove_dependencies)
    except PyRevitPluginRemoveException as remove_err:
        mlogger.error('Error removing extension: %s | %s',
                      extpkg.name, remove_err)

"""Generic extension components."""
import os
import os.path as op
import re
import codecs
import copy

from pyrevit import HOST_APP, PyRevitException
from pyrevit import coreutils
from pyrevit.coreutils import yaml
from pyrevit.coreutils import applocales
from pyrevit.coreutils import pyutils
from pyrevit.compat import PY3
from pyrevit.revit import ui
import pyrevit.extensions as exts


#pylint: disable=W0703,C0302,C0103
mlogger = coreutils.logger.get_logger(__name__)


EXT_DIR_KEY = 'directory'
SUB_CMP_KEY = 'components'
LAYOUT_ITEM_KEY = 'layout_items'
LAYOUT_DIR_KEY = 'directive'
TYPE_ID_KEY = 'type_id'
NAME_KEY = 'name'


class TypedComponent(object):
    """Component with a type id."""
    type_id = None


class CachableComponent(TypedComponent):
    """Cacheable Component."""
    def get_cache_data(self):
        cache_dict = self.__dict__.copy()
        if hasattr(self, TYPE_ID_KEY):
            cache_dict[TYPE_ID_KEY] = getattr(self, TYPE_ID_KEY)
        return cache_dict

    def load_cache_data(self, cache_dict):
        for k, v in cache_dict.items():
            self.__dict__[k] = v


class LayoutDirective(CachableComponent):
    """Layout directive."""
    def __init__(self, directive_type=None, target=None):
        self.directive_type = directive_type
        self.target = target


class LayoutItem(CachableComponent):
    """Layout item."""
    def __init__(self, name=None, directive=None):
        self.name = name
        self.directive = directive


class GenericComponent(CachableComponent):
    """Generic component object."""
    def __init__(self):
        self.name = None

    def __repr__(self):
        return '<GenericComponent object with name \'{}\'>'.format(self.name)

    @property
    def is_container(self):
        return hasattr(self, '__iter__')


class GenericUIComponent(GenericComponent):
    """Generic UI component."""
    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        GenericComponent.__init__(self)
        self.directory = cmp_path
        self.unique_name = self.parent_ctrl_id = None
        self.icon_file = None
        self._ui_title = None
        self._tooltip = self.author = self._help_url = None
        self.media_file = None
        self.min_revit_ver = self.max_revit_ver = None
        self.is_beta = False
        self.highlight_type = None
        self.collapsed = False
        self.version = None

        self.meta = {}
        self.meta_file = None

        self.modules = []
        self.module_paths = []

        self.binary_path = None
        self.library_path = None

        if self.directory:
            self._update_from_directory()

    @classmethod
    def matches(cls, component_path):
        return component_path.lower().endswith(cls.type_id)

    @classmethod
    def make_unique_name(cls, cmp_path):
        """Creates a unique name for the command.

        This is used to uniquely identify this command
        and also to create the class in pyRevit dll assembly.
        Current method create a unique name based on the command
        full directory address.

        Examples:
            for 'pyRevit.extension/pyRevit.tab/Edit.panel/Flip doors.pushbutton'
            unique name would be: 'pyrevit-pyrevit-edit-flipdoors'.
        """
        pieces = []
        inside_ext = False
        for dname in cmp_path.split(op.sep):
            if exts.ExtensionTypes.UI_EXTENSION.POSTFIX in dname:
                inside_ext = True

            name, ext = op.splitext(dname)
            if ext != '' and inside_ext:
                pieces.append(name)
            else:
                continue
        return coreutils.cleanup_string(
            exts.UNIQUE_ID_SEPARATOR.join(pieces),
            skip=[exts.UNIQUE_ID_SEPARATOR]
            ).lower()

    def __repr__(self):
        return '<type_id \'{}\' name \'{}\' @ \'{}\'>'\
            .format(self.type_id, self.name, self.directory)

    def _update_from_directory(self):
        self.name = op.splitext(op.basename(self.directory))[0]
        self._ui_title = self.name
        self.unique_name = GenericUIComponent.make_unique_name(self.directory)

        self.icon_file = ui.resolve_icon_file(self.directory, exts.DEFAULT_ICON_FILE)
        mlogger.debug('Icon file is: %s:%s', self.name, self.icon_file)

        self.media_file = \
            self.find_bundle_file([exts.DEFAULT_MEDIA_FILENAME], finder='name')
        mlogger.debug('Media file is: %s:%s', self.name, self.media_file)

        self._help_url = \
            self.find_bundle_file([exts.HELP_FILE_PATTERN], finder='regex')

        # each component can store custom libraries under
        # lib/ inside the component folder
        lib_path = op.join(self.directory, exts.COMP_LIBRARY_DIR_NAME)
        self.library_path = lib_path if op.exists(lib_path) else None

        # setting up search paths. These paths will be added to
        # all sub-components of this component.
        if self.library_path:
            self.module_paths.append(self.library_path)

        # each component can store custom binaries under
        # bin/ inside the component folder
        bin_path = op.join(self.directory, exts.COMP_BIN_DIR_NAME)
        self.binary_path = bin_path if op.exists(bin_path) else None

        # setting up search paths. These paths will be added to
        # all sub-components of this component.
        if self.binary_path:
            self.module_paths.append(self.binary_path)

        # find meta file
        self.meta_file = self.find_bundle_file([
            exts.BUNDLEMATA_POSTFIX
            ])
        if self.meta_file:
            # sets up self.meta
            try:
                self.meta = yaml.load_as_dict(self.meta_file)
                if self.meta:
                    self._read_bundle_metadata()
            except Exception as err:
                mlogger.error(
                    "Error reading meta file @ %s | %s", self.meta_file, err
                    )

    def _resolve_locale(self, source):
        if isinstance(source, str):
            return source
        elif isinstance(source, dict):
            return applocales.get_locale_string(source)

    def _resolve_liquid_tag(self, param_name, key, value):
        liquid_tag = '{{' + key + '}}'
        exst_val = getattr(self, param_name)
        if exst_val and (liquid_tag in exst_val):   #pylint: disable=E1135
            new_value = exst_val.replace(liquid_tag, value)
            setattr(self, param_name, new_value)

    def _read_bundle_metadata(self):
        self._ui_title = self.meta.get(exts.MDATA_UI_TITLE, self._ui_title)

        self._tooltip = self.meta.get(exts.MDATA_TOOLTIP, self._tooltip)

        # authors could be a list or single value
        self.author = self.meta.get(exts.MDATA_AUTHOR, self.author)
        self.author = self.meta.get(exts.MDATA_AUTHORS, self.author)
        if isinstance(self.author, list):
            self.author = '\n'.join(self.author)

        self._help_url = \
            self.meta.get(exts.MDATA_COMMAND_HELP_URL, self._help_url)

        self.min_revit_ver = \
            self.meta.get(exts.MDATA_MIN_REVIT_VERSION, self.min_revit_ver)
        self.max_revit_ver = \
            self.meta.get(exts.MDATA_MAX_REVIT_VERSION, self.max_revit_ver)

        self.is_beta = \
            self.meta.get(exts.MDATA_BETA_SCRIPT, 'false').lower() == 'true'

        highlight = \
            self.meta.get(exts.MDATA_HIGHLIGHT_KEY, None)
        if highlight and isinstance(highlight, str):
            self.highlight_type = highlight.lower()

        self.collapsed = \
            self.meta.get(exts.MDATA_COLLAPSED_KEY, 'false').lower() == 'true'

        self.modules = \
            self.meta.get(exts.MDATA_LINK_BUTTON_MODULES, self.modules)

    @property
    def control_id(self):
        if self.parent_ctrl_id:
            return self.parent_ctrl_id + '%{}'.format(self.name)
        else:
            return "CustomCtrl_%CustomCtrl_%{}".format(self.name)

    @property
    def ui_title(self):
        return self._resolve_locale(self._ui_title)

    @property
    def tooltip(self):
        return self._resolve_locale(self._tooltip)

    @property
    def help_url(self):
        return self._resolve_locale(self._help_url)

    @property
    def is_supported(self):
        if self.min_revit_ver:
            # If host is older than the minimum host version, raise exception
            if int(HOST_APP.version) < int(self.min_revit_ver):
                mlogger.debug('Requires min version: %s', self.min_revit_ver)
                return False
        if self.max_revit_ver:
            # If host is newer than the max host version, raise exception
            if int(HOST_APP.version) > int(self.max_revit_ver):
                mlogger.debug('Requires max version: %s', self.max_revit_ver)
                return False
        return True

    def get_full_bundle_name(self):
        return self.name + self.type_id

    def has_module_path(self, path):
        return path in self.module_paths

    def add_module_path(self, path):
        if path and not self.has_module_path(path):
            mlogger.debug('Appending syspath: %s to %s', path, self)
            self.module_paths.append(path)

    def remove_module_path(self, path):
        if path and self.has_module_path(path):
            mlogger.debug('Removing syspath: %s from %s', path, self)
            return self.module_paths.remove(path)

    def get_bundle_file(self, file_name):
        if self.directory and file_name:
            file_addr = op.join(self.directory, file_name)
            return file_addr if op.exists(file_addr) else None

    def find_bundle_file(self, patterns, finder='postfix'):
        if self.directory:
            for bundle_file in os.listdir(self.directory):
                if 'name' == finder:
                    for file_name in patterns:
                        if op.splitext(bundle_file)[0] == file_name:
                            return op.join(self.directory, bundle_file)
                elif 'postfix' == finder:
                    for file_postfix in patterns:
                        if bundle_file.endswith(file_postfix):
                            return op.join(self.directory, bundle_file)
                elif 'regex' == finder:
                    for regex_pattern in patterns:
                        if re.match(regex_pattern, bundle_file):
                            return op.join(self.directory, bundle_file)
        return None

    def find_bundle_module(self, module, by_host=False):
        # test of file_name is an actually path to a file
        if op.isfile(module):
            return module

        def build_assm_filename(module_filename):
            # build assembly by host version (assm_file_2020.ext)
            assm_name, assm_ext = op.splitext(module_filename)
            return assm_name + '_' + HOST_APP.version + assm_ext

        if by_host:
            module = build_assm_filename(module)

        # test if module is inside search paths
        for module_path in self.module_paths:
            possible_module_path = op.join(module_path, module)
            if op.isfile(possible_module_path):
                return possible_module_path

    def configure(self, config_dict):
        configurable_params = \
            ['_ui_title', '_tooltip', '_help_url', 'author']
        # get root key:value pairs
        for key, value in config_dict.items():
            for param_name in configurable_params:
                self._resolve_liquid_tag(param_name, key, value)
        # get key:value pairs grouped under special key, if exists
        templates = config_dict.get(exts.MDATA_TEMPLATES_KEY, {})
        for key, value in templates.items():
            for param_name in configurable_params:
                self._resolve_liquid_tag(param_name, key, value)


class GenericUIContainer(GenericUIComponent):
    """Superclass for all UI group items (tab, panel, button groups, stacks)."""
    allowed_sub_cmps = []

    def __init__(self, cmp_path=None):
        self.layout_items = []
        self.components = []
        # using classname otherwise exceptions in superclasses won't show
        GenericUIComponent.__init__(self, cmp_path=cmp_path)

    def _update_from_directory(self):
        # using classname otherwise exceptions in superclasses won't show
        GenericUIComponent._update_from_directory(self)
        # process layout
        # default is layout in metadata, the older layout file is deprecate
        # and is for fallback only
        if not self.parse_layout_metadata():
            mlogger.debug('Container does not have layout file defined: %s',
                self)


    def _apply_layout_directive(self, directive, component):
        # if matching directive found, process the directive
        if directive.directive_type == 'title':
            component._ui_title = directive.target

    def __iter__(self):
        # if item is not listed in layout, it will not be created
        if self.layout_items:
            mlogger.debug('Reordering components per layout file...')
            laidout_cmps = []
            for litem in self.layout_items:
                matching_cmp = \
                        next((x for x in self.components
                              if x.name == litem.name), None)
                if matching_cmp:
                    # apply directives before adding to list
                    if litem.directive:
                        self._apply_layout_directive(litem.directive,
                                                     matching_cmp)
                    laidout_cmps.append(matching_cmp)

            # insert separators and slideouts per layout definition
            mlogger.debug('Adding separators and slide outs per layout...')
            last_item_index = len(self.layout_items) - 1
            for idx, litem in enumerate(self.layout_items):
                if exts.SEPARATOR_IDENTIFIER in litem.name \
                        and idx < last_item_index:
                    separator = GenericUIComponent()
                    separator.type_id = exts.SEPARATOR_IDENTIFIER
                    laidout_cmps.insert(idx, separator)
                elif exts.SLIDEOUT_IDENTIFIER in litem.name \
                        and idx < last_item_index:
                    slideout = GenericUIComponent()
                    slideout.type_id = exts.SLIDEOUT_IDENTIFIER
                    laidout_cmps.insert(idx, slideout)

            mlogger.debug('Reordered sub_component list is: %s', laidout_cmps)
            return laidout_cmps
        else:
            return self.components

    def parse_layout_directive(self, layout_line):
        parts = re.findall(r'(.+)\[(.+):(.*)\]', layout_line)
        if parts:
            source_item, directive, target_value = parts[0]
            # cleanup values
            directive = directive.lower().strip()
            target_value = target_value.strip()
            # process any escape characters in target value
            # https://stackoverflow.com/a/4020824/2350244
            target_value = target_value.encode('utf-8')
            if PY3:
                target_value = target_value.decode('unicode_escape')
            else:
                target_value = target_value.decode('string_escape')
            # create directive obj
            return source_item, LayoutDirective(directive_type=directive,
                                                target=target_value)
        return layout_line, None

    def parse_layout_item(self, layout_line):
        if layout_line:
            layout_item_name, layout_item_drctv = \
                self.parse_layout_directive(layout_line)
            return LayoutItem(name=layout_item_name,
                              directive=layout_item_drctv)

    def parse_layout_items(self, layout_lines):
        for layout_line in layout_lines:
            layout_item = self.parse_layout_item(layout_line)
            if layout_item:
                self.layout_items.append(layout_item)
        mlogger.debug('Layout is: %s', self.layout_items)

    def parse_layout_metadata(self):
        layout = self.meta.get(exts.MDATA_LAYOUT, [])
        if layout:
            self.parse_layout_items(layout)
            return True

    def contains(self, item_name):
        return any([x.name == item_name for x in self.components])

    def add_module_path(self, path):
        if path and not self.has_module_path(path):
            mlogger.debug('Appending syspath: %s to %s', path, self)
            for component in self.components:
                component.add_module_path(path)
            self.module_paths.append(path)

    def remove_module_path(self, path):
        if path and self.has_module_path(path):
            mlogger.debug('Removing syspath: %s from %s', path, self)
            for component in self.components:
                component.remove_module_path(path)
            return self.module_paths.remove(path)

    def add_component(self, comp):
        # set search paths
        for path in self.module_paths:
            comp.add_module_path(path)
        # set its own control id on the child component
        if hasattr(comp, 'parent_ctrl_id'):
            comp.parent_ctrl_id = self.control_id
        # now add to list
        self.components.append(comp)

    def find_components_of_type(self, cmp_type):
        sub_comp_list = []
        for sub_comp in self.components:
            if isinstance(sub_comp, cmp_type):
                sub_comp_list.append(sub_comp)
            elif sub_comp.is_container:
                sub_comp_list.extend(sub_comp.find_components_of_type(cmp_type))

        return sub_comp_list

    def find_layout_items(self):
        layout_items = []
        layout_items.extend(self.layout_items)
        for sub_comp in self.components:
            if sub_comp.is_container:
                layout_items.extend(sub_comp.find_layout_items())
        return layout_items

    def configure(self, config_dict):
        # update self meta
        GenericUIComponent.configure(self, config_dict=config_dict)
        # create an updated dict to pass to children
        updated_dict = copy.deepcopy(config_dict)
        updated_dict = pyutils.merge(updated_dict, self.meta)
        # replace the meta values with the expanded values
        # so children can use the expanded
        updated_dict[exts.MDATA_UI_TITLE] = self.ui_title
        updated_dict[exts.MDATA_TOOLTIP] = self.tooltip
        updated_dict[exts.MDATA_COMMAND_HELP_URL] = self.help_url
        updated_dict[exts.AUTHOR_PARAM] = self.author
        if exts.AUTHORS_PARAM in updated_dict:
            updated_dict.pop(exts.AUTHORS_PARAM)
        for component in self:
            component.configure(updated_dict)

# superclass for all single command classes (link, push button, toggle button)
# GenericUICommand is not derived from GenericUIContainer since a command
# can not contain other elements
class GenericUICommand(GenericUIComponent):
    """Superclass for all single commands.

    The information provided by these classes will be used to create a
    push button under Revit UI. However, pyRevit expands the capabilities of
    push button beyond what is provided by Revit UI. (e.g. Toggle button
    changes it's icon based on its on/off status)
    See LinkButton and ToggleButton classes.
    """
    def __init__(self, cmp_path=None, needs_script=True):
        self.needs_script = needs_script
        self.script_file = self.config_script_file = None
        self.arguments = []
        self.context = None
        self.class_name = self.avail_class_name = None
        self.requires_clean_engine = False
        self.requires_fullframe_engine = False
        self.requires_persistent_engine = False
        self.requires_mainthread_engine = False
        # engine options specific to dynamo
        self.dynamo_path = None
        # self.dynamo_path_exec = False
        self.dynamo_path_check_existing = False
        self.dynamo_force_manual_run = False
        self.dynamo_model_nodes_info = None
        # using classname otherwise exceptions in superclasses won't show
        GenericUIComponent.__init__(self, cmp_path=cmp_path)

        mlogger.debug('Maximum host version: %s', self.max_revit_ver)
        mlogger.debug('Minimum host version: %s', self.min_revit_ver)
        mlogger.debug('command tooltip: %s', self._tooltip)
        mlogger.debug('Command author: %s', self.author)
        mlogger.debug('Command help url: %s', self._help_url)

        if self.is_beta:
            mlogger.debug('Command is in beta.')

    def _update_from_directory(self):
        # using classname otherwise exceptions in superclasses won't show
        GenericUIComponent._update_from_directory(self)

        # find script file
        self.script_file = \
            self.find_bundle_file([
                exts.PYTHON_SCRIPT_POSTFIX,
                exts.CSHARP_SCRIPT_POSTFIX,
                exts.VB_SCRIPT_POSTFIX,
                exts.RUBY_SCRIPT_POSTFIX,
                exts.DYNAMO_SCRIPT_POSTFIX,
                exts.GRASSHOPPER_SCRIPT_POSTFIX,
                exts.GRASSHOPPERX_SCRIPT_POSTFIX,
                ])

        if self.needs_script and not self.script_file:
            mlogger.error('Command %s: Does not have script file.', self)

        # if python
        if self.script_language == exts.PYTHON_LANG:
            # allow python tools to load side scripts
            self.add_module_path(self.directory)
            # read the metadata from python script if not metadata file
            if not self.meta and not self.is_cpython:
                # sets up self.meta from script global variables
                self._read_bundle_metadata_from_python_script()

        # find config scripts
        self.config_script_file = \
            self.find_bundle_file([
                exts.PYTHON_CONFIG_SCRIPT_POSTFIX,
                exts.CSHARP_CONFIG_SCRIPT_POSTFIX,
                exts.VB_CONFIG_SCRIPT_POSTFIX,
                exts.RUBY_CONFIG_SCRIPT_POSTFIX,
                exts.DYNAMO_CONFIG_SCRIPT_POSTFIX,
                exts.GRASSHOPPER_CONFIG_SCRIPT_POSTFIX,
                exts.GRASSHOPPERX_CONFIG_SCRIPT_POSTFIX,
                ])

        if not self.config_script_file:
            mlogger.debug(
                'Command %s: Does not have independent config script.',
                self)
            self.config_script_file = self.script_file

    def _read_bundle_metadata(self):
        # using classname otherwise exceptions in superclasses won't show
        GenericUIComponent._read_bundle_metadata(self)
        # determine engine configs
        if exts.MDATA_ENGINE in self.meta:
            self.requires_clean_engine = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_CLEAN, 'false').lower() == 'true'
            self.requires_fullframe_engine = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_FULLFRAME, 'false').lower() == 'true'
            self.requires_persistent_engine = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_PERSISTENT, 'false').lower() == 'true'

            # determine if engine is required to run on main thread
            # MDATA_ENGINE_MAINTHREAD is the generic option
            rme = self.meta[exts.MDATA_ENGINE].get(
                exts.MDATA_ENGINE_MAINTHREAD, 'false') == 'true'
            # MDATA_ENGINE_DYNAMO_AUTOMATE is specific naming for dynamo
            automate = self.meta[exts.MDATA_ENGINE].get(
                exts.MDATA_ENGINE_DYNAMO_AUTOMATE, 'false') == 'true'
            self.requires_mainthread_engine = rme or automate

            # process engine options specific to dynamo
            self.dynamo_path = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_DYNAMO_PATH, None)
            # self.dynamo_path_exec = \
            #     self.meta[exts.MDATA_ENGINE].get(
            #         exts.MDATA_ENGINE_DYNAMO_PATH_EXEC, 'true') == 'true'
            self.dynamo_path_check_existing = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_DYNAMO_PATH_CHECK_EXIST,
                    'false') == 'true'
            self.dynamo_force_manual_run = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_DYNAMO_FORCE_MANUAL_RUN,
                    'false') == 'true'
            self.dynamo_model_nodes_info = \
                self.meta[exts.MDATA_ENGINE].get(
                    exts.MDATA_ENGINE_DYNAMO_MODEL_NODES_INFO, None)

        # panel buttons should be active always
        if self.type_id == exts.PANEL_PUSH_BUTTON_POSTFIX:
            self.context = self._parse_context_directives(exts.CTX_ZERODOC)
        else:
            self.context = \
                self.meta.get(exts.MDATA_COMMAND_CONTEXT, None)
            if self.context:
                self.context = self._parse_context_directives(self.context)

    def _parse_context_list(self, context):
        context_rules = []

        str_items = [x for x in context if isinstance(x, str)]
        context_rules.append(
            exts.MDATA_COMMAND_CONTEXT_RULE.format(
                rule=exts.MDATA_COMMAND_CONTEXT_ALL_SEP.join(str_items)
                )
        )

        dict_items = [x for x in context if isinstance(x, dict)]
        for ditem in dict_items:
            context_rules.extend(self._parse_context_dict(ditem))

        return context_rules

    def _parse_context_dict(self, context):
        context_rules = []
        for ctx_key, ctx_value in context.items():
            if ctx_key == exts.MDATA_COMMAND_CONTEXT_TYPE:
                context_type = (
                    exts.MDATA_COMMAND_CONTEXT_ANY_SEP
                    if ctx_value == exts.MDATA_COMMAND_CONTEXT_ANY
                    else exts.MDATA_COMMAND_CONTEXT_ALL_SEP
                )
                continue

            if isinstance(ctx_value, str):
                ctx_value = [ctx_value]

            key = ctx_key.lower()
            condition = ""
            # all
            if key == exts.MDATA_COMMAND_CONTEXT_ALL \
                    or key == exts.MDATA_COMMAND_CONTEXT_NOTALL:
                condition = exts.MDATA_COMMAND_CONTEXT_ALL_SEP

            # any
            elif key == exts.MDATA_COMMAND_CONTEXT_ANY \
                    or key == exts.MDATA_COMMAND_CONTEXT_NOTANY:
                condition = exts.MDATA_COMMAND_CONTEXT_ANY_SEP

            # except
            elif key == exts.MDATA_COMMAND_CONTEXT_EXACT \
                    or key == exts.MDATA_COMMAND_CONTEXT_NOTEXACT:
                condition = exts.MDATA_COMMAND_CONTEXT_EXACT_SEP

            context = condition.join(
                [x for x in ctx_value if isinstance(x, str)]
                )
            formatted_rule = \
                exts.MDATA_COMMAND_CONTEXT_RULE.format(rule=context)
            if key.startswith(exts.MDATA_COMMAND_CONTEXT_NOT):
                formatted_rule = "!" + formatted_rule
            context_rules.append(formatted_rule)
        return context_rules

    def _parse_context_directives(self, context):
        context_rules = []

        if isinstance(context, str):
            context_rules.append(
                exts.MDATA_COMMAND_CONTEXT_RULE.format(rule=context)
            )
        elif isinstance(context, list):
            context_rules.extend(self._parse_context_list(context))

        elif isinstance(context, dict):
            if "rule" in context:
                return context["rule"]
            context_rules.extend(self._parse_context_dict(context))

        context_type = exts.MDATA_COMMAND_CONTEXT_ALL_SEP
        return context_type.join(context_rules)

    def _read_bundle_metadata_from_python_script(self):
        try:
            # reading script file content to extract parameters
            script_content = \
                coreutils.ScriptFileParser(self.script_file)

            self._ui_title = \
                script_content.extract_param(exts.UI_TITLE_PARAM) \
                    or self._ui_title

            script_docstring = script_content.get_docstring()
            custom_docstring = \
                script_content.extract_param(exts.DOCSTRING_PARAM)
            self._tooltip = \
                custom_docstring or script_docstring or self._tooltip

            script_author = script_content.extract_param(exts.AUTHOR_PARAM)
            script_author = script_content.extract_param(exts.AUTHORS_PARAM)
            if isinstance(script_author, list):
                script_author = '\n'.join(script_author)
            self.author = script_author or self.author

            # extracting min requried Revit and pyRevit versions
            self.max_revit_ver = \
                script_content.extract_param(exts.MAX_REVIT_VERSION_PARAM) \
                    or self.max_revit_ver
            self.min_revit_ver = \
                script_content.extract_param(exts.MIN_REVIT_VERSION_PARAM) \
                    or self.min_revit_ver
            self._help_url = \
                script_content.extract_param(exts.COMMAND_HELP_URL_PARAM) \
                    or self._help_url

            self.is_beta = \
                script_content.extract_param(exts.BETA_SCRIPT_PARAM) \
                    or self.is_beta

            self.highlight_type = \
                script_content.extract_param(exts.HIGHLIGHT_SCRIPT_PARAM) \
                    or self.highlight_type

            # only True when command is specifically asking for
            # a clean engine or a fullframe engine. False if not set.
            self.requires_clean_engine = \
                script_content.extract_param(exts.CLEAN_ENGINE_SCRIPT_PARAM) \
                    or False
            self.requires_fullframe_engine = \
                script_content.extract_param(exts.FULLFRAME_ENGINE_PARAM) \
                    or False
            self.requires_persistent_engine = \
                script_content.extract_param(exts.PERSISTENT_ENGINE_PARAM) \
                    or False

            # panel buttons should be active always
            if self.type_id == exts.PANEL_PUSH_BUTTON_POSTFIX:
                self.context = self._parse_context_directives(exts.CTX_ZERODOC)
            else:
                self.context = \
                    script_content.extract_param(exts.COMMAND_CONTEXT_PARAM)
                if self.context:
                    self.context = self._parse_context_directives(self.context)

        except Exception as parse_err:
            mlogger.log_parse_except(self.script_file, parse_err)

    @property
    def script_language(self):
        if self.script_file is not None:
            if self.script_file.endswith(exts.PYTHON_SCRIPT_FILE_FORMAT):
                return exts.PYTHON_LANG
            elif self.script_file.endswith(exts.CSHARP_SCRIPT_FILE_FORMAT):
                return exts.CSHARP_LANG
            elif self.script_file.endswith(exts.VB_SCRIPT_FILE_FORMAT):
                return exts.VB_LANG
            elif self.script_file.endswith(exts.RUBY_SCRIPT_FILE_FORMAT):
                return exts.RUBY_LANG
            elif self.script_file.endswith(exts.DYNAMO_SCRIPT_FILE_FORMAT):
                return exts.DYNAMO_LANG
            elif self.script_file.endswith(
                    exts.GRASSHOPPER_SCRIPT_FILE_FORMAT) \
                    or self.script_file.endswith(
                        exts.GRASSHOPPERX_SCRIPT_FILE_FORMAT
                    ):
                return exts.GRASSHOPPER_LANG
        else:
            return None

    @property
    def control_id(self):
        if self.parent_ctrl_id:
            return self.parent_ctrl_id + '%{}'.format(self.name)
        else:
            return '%{}'.format(self.name)

    @property
    def is_cpython(self):
        with open(self.script_file, 'r') as script_f:
            return exts.CPYTHON_HASHBANG in script_f.readline()

    def has_config_script(self):
        return self.config_script_file != self.script_file

"""Base module ofr parsing extensions."""
import os
import os.path as op

from pyrevit.coreutils import get_all_subclasses
from pyrevit.coreutils.logger import get_logger


#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


def _get_discovered_comps(comp_path, cmp_types_list):
    discovered_cmps = []
    mlogger.debug('Testing _get_component(s) on: %s ', comp_path)
    # comp_path might be a file or a dir,
    # but its name should not start with . or _:
    for cmp_type in cmp_types_list:
        mlogger.debug('Testing sub_directory %s for %s', comp_path, cmp_type)
        # if cmp_class can be created for this sub-dir, the add to list
        if cmp_type.matches(comp_path):
            component = cmp_type(cmp_path=comp_path)
            discovered_cmps.append(component)
            mlogger.debug('Successfuly created component: %s from: %s',
                          component, comp_path)

    return discovered_cmps


def _create_subcomponents(search_dir,
                          cmp_types_list,
                          create_from_search_dir=False):
    """Returns the objects in search_dir of the types in cmp_types_list.

    Arguments:
        search_dir (str): directory to parse
        cmp_types_list: This methods checks the subfolders in search_dir
            against the _get_component types provided in this list.
        create_from_search_dir (bool, optional): whether to create the _get_component objects, default to False

    Examples:
        ```python
        _create_subcomponents(search_dir,
                              [LinkButton, PushButton, or ToggleButton])
        ```
        this method creates LinkButton, PushButton, or ToggleButton for
        the parsed sub-directories under search_dir with matching .type_id
        identifiers in their names. (e.g. "folder.LINK_BUTTON_POSTFIX")

    Returns:
        list of created classes of types provided in cmp_types_list
    """
    sub_cmp_list = []

    if not create_from_search_dir:
        mlogger.debug('Searching directory: %s for components of type: %s',
                      search_dir, cmp_types_list)
        for file_or_dir in os.listdir(search_dir):
            full_path = op.join(search_dir, file_or_dir)
            if not file_or_dir.startswith(('.', '_')):
                sub_cmp_list.extend(_get_discovered_comps(full_path,
                                                          cmp_types_list))
            else:
                mlogger.debug('Skipping _get_component. '
                              'Name can not start with . or _: %s', full_path)
    else:
        sub_cmp_list.extend(_get_discovered_comps(search_dir,
                                                  cmp_types_list))

    return sub_cmp_list


def _get_subcomponents_classes(parent_classes):
    """Find available subcomponents for given parent types."""
    return [x for x in get_all_subclasses(parent_classes) if x.type_id]


def _parse_for_components(component):
    """Recursively parses the component directory for allowed components type.

    This method uses get_all_subclasses() to get a list of all subclasses
    of _get_component.allowed_sub_cmps type.
    This ensures that if any new type of component_class is added later,
    this method does not need to be updated as the new sub-class will be
    listed by .__subclasses__() method of the parent class and this method
    will check the directory for its .type_id.
    """
    for new_cmp in _create_subcomponents(
            component.directory,
            _get_subcomponents_classes(component.allowed_sub_cmps)):
        # add the successfulyl created _get_component to the
        # parent _get_component
        component.add_component(new_cmp)
        if new_cmp.is_container:
            # Recursive part: parse each sub-_get_component
            # for its allowed sub-sub-components.
            _parse_for_components(new_cmp)


def parse_comp_dir(comp_path, comp_class):
    return _create_subcomponents(
        comp_path,
        _get_subcomponents_classes([comp_class]),
        create_from_search_dir=True
        )


def get_parsed_extension(extension):
    """Creates and adds the extensions components to the package.

    Each package object is the root to a tree of components that exists
    under that package. (e.g. tabs, buttons, ...) sub components of package
    can be accessed by iterating the _get_component.
    See _basecomponents for types.
    """
    _parse_for_components(extension)
    return extension


def parse_dir_for_ext_type(root_dir, parent_cmp_type):
    """Return the objects of type parent_cmp_type of the extensions in root_dir.

    The package objects won't be parsed at this level.
    This is useful for collecting basic info on an extension type
    for cache cheching or updating extensions using their directory paths.

    Args:
        root_dir (str): directory to parse
        parent_cmp_type (type): type of objects to return
    """
    # making sure the provided directory exists.
    # This is mainly for the user defined package directories
    if not op.exists(root_dir):
        mlogger.debug('Extension search directory does not exist: %s', root_dir)
        return []

    # try creating extensions in given directory
    ext_data_list = []

    mlogger.debug('Parsing directory for extensions of type: %s',
                  parent_cmp_type)
    for ext_data in _create_subcomponents(root_dir, [parent_cmp_type]):
        mlogger.debug('Extension directory found: %s', ext_data)
        ext_data_list.append(ext_data)

    return ext_data_list

"""Base classes for pyRevit extension components."""
import os
import os.path as op
import json
import codecs

from pyrevit import PyRevitException, HOST_APP
from pyrevit.compat import safe_strtype
from pyrevit import framework
from pyrevit import coreutils
from pyrevit.coreutils.logger import get_logger
import pyrevit.extensions as exts
from pyrevit.extensions.genericcomps import GenericComponent
from pyrevit.extensions.genericcomps import GenericUIContainer
from pyrevit.extensions.genericcomps import GenericUICommand
from pyrevit import versionmgr


#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


EXT_HASH_VALUE_KEY = 'dir_hash_value'
EXT_HASH_VERSION_KEY = 'pyrvt_version'


# Derived classes here correspond to similar elements in Revit ui.
# Under Revit UI:
# Packages contain Tabs, Tabs contain, Panels, Panels contain Stacks,
# Commands, or Command groups
# ------------------------------------------------------------------------------
class NoButton(GenericUICommand):
    """This is not a button."""
    type_id = exts.NOGUI_COMMAND_POSTFIX


class NoScriptButton(GenericUICommand):
    """Base for buttons that doesn't run a script."""
    def __init__(self, cmp_path=None, needs_commandclass=False):
        # using classname otherwise exceptions in superclasses won't show
        GenericUICommand.__init__(self, cmp_path=cmp_path, needs_script=False)
        self.assembly = self.command_class = self.avail_command_class = None
        # read metadata from metadata file
        if self.meta:
            # get the target assembly from metadata
            self.assembly = \
                self.meta.get(exts.MDATA_LINK_BUTTON_ASSEMBLY, None)

            # get the target command class from metadata
            self.command_class = \
                self.meta.get(exts.MDATA_LINK_BUTTON_COMMAND_CLASS, None)

            # get the target command class from metadata
            self.avail_command_class = \
                self.meta.get(exts.MDATA_LINK_BUTTON_AVAIL_COMMAND_CLASS, None)

            # for invoke buttons there is no script source so
            # assign the metadata file to the script
            self.script_file = self.config_script_file = self.meta_file
        else:
            mlogger.debug("%s does not specify target assembly::class.", self)

        if self.directory and not self.assembly:
            mlogger.error("%s does not specify target assembly.", self)

        if self.directory and needs_commandclass and not self.command_class:
            mlogger.error("%s does not specify target command class.", self)

        mlogger.debug('%s assembly.class: %s.%s',
                      self, self.assembly, self.command_class)

    def get_target_assembly(self, required=False):
        assm_file = self.assembly.lower()
        if not assm_file.endswith(framework.ASSEMBLY_FILE_TYPE):
            assm_file += '.' + framework.ASSEMBLY_FILE_TYPE

        # try finding assembly for this specific host version
        target_asm_by_host = self.find_bundle_module(assm_file, by_host=True)
        if target_asm_by_host:
            return target_asm_by_host
        # try find assembly by its name
        target_asm = self.find_bundle_module(assm_file)
        if target_asm:
            return target_asm

        if required:
            mlogger.error("%s can not find target assembly.", self)

        return ''


class LinkButton(NoScriptButton):
    """Link button."""
    type_id = exts.LINK_BUTTON_POSTFIX

    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        NoScriptButton.__init__(
            self,
            cmp_path=cmp_path,
            needs_commandclass=True
            )

        if self.context:
            mlogger.warn(
                "Linkbutton bundles do not support \"context:\". "
                "Use \"availability_class:\" instead and specify name of "
                "availability class in target assembly | %s", self
                )
            self.context = None


class InvokeButton(NoScriptButton):
    """Invoke button."""
    type_id = exts.INVOKE_BUTTON_POSTFIX

    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        NoScriptButton.__init__(self, cmp_path=cmp_path)


class PushButton(GenericUICommand):
    """Push button."""
    type_id = exts.PUSH_BUTTON_POSTFIX


class PanelPushButton(GenericUICommand):
    """Panel push button."""
    type_id = exts.PANEL_PUSH_BUTTON_POSTFIX


class SmartButton(GenericUICommand):
    """Smart button."""
    type_id = exts.SMART_BUTTON_POSTFIX


class ContentButton(GenericUICommand):
    """Content Button."""
    type_id = exts.CONTENT_BUTTON_POSTFIX

    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        GenericUICommand.__init__(
            self,
            cmp_path=cmp_path,
            needs_script=False
            )
        # find content file
        self.script_file = \
            self.find_bundle_file([
                exts.CONTENT_VERSION_POSTFIX.format(
                    version=HOST_APP.version
                    ),
                ])
        if not self.script_file:
            self.script_file = \
                self.find_bundle_file([
                    exts.CONTENT_POSTFIX,
                    ])
        # requires at least one bundles
        if self.directory and not self.script_file:
            mlogger.error('Command %s: Does not have content file.', self)
            self.script_file = ''

        # find alternative content file
        self.config_script_file = \
            self.find_bundle_file([
                exts.ALT_CONTENT_VERSION_POSTFIX.format(
                    version=HOST_APP.version
                    ),
                ])
        if not self.config_script_file:
            self.config_script_file = \
                self.find_bundle_file([
                    exts.ALT_CONTENT_POSTFIX,
                    ])
        if not self.config_script_file:
            self.config_script_file = self.script_file


class URLButton(GenericUICommand):
    """URL button."""
    type_id = exts.URL_BUTTON_POSTFIX

    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        GenericUICommand.__init__(self, cmp_path=cmp_path, needs_script=False)
        self.target_url = None
        # read metadata from metadata file
        if self.meta:
            # get the target url from metadata
            self.target_url = \
                self.meta.get(exts.MDATA_URL_BUTTON_HYPERLINK, None)
            # for url buttons there is no script source so
            # assign the metadata file to the script
            self.script_file = self.config_script_file = self.meta_file
        else:
            mlogger.debug("%s does not specify target assembly::class.", self)

        if self.directory and not self.target_url:
            mlogger.error("%s does not specify target url.", self)

        mlogger.debug('%s target url: %s', self, self.target_url)

    def get_target_url(self):
        return self.target_url or ""


class GenericUICommandGroup(GenericUIContainer):
    """Generic UI command group.

    Command groups only include commands.
    These classes can include GenericUICommand as sub components.
    """
    allowed_sub_cmps = [GenericUICommand, NoScriptButton]

    @property
    def control_id(self):
        # stacks don't have control id
        if self.parent_ctrl_id:
            deepend_parent_id = self.parent_ctrl_id.replace(
                '_%CustomCtrl',
                '_%CustomCtrl_%CustomCtrl'
            )
            return deepend_parent_id + '%{}'.format(self.name)
        else:
            return '%{}%'.format(self.name)

    def has_commands(self):
        for component in self:
            if isinstance(component, GenericUICommand):
                return True


class PullDownButtonGroup(GenericUICommandGroup):
    """Pulldown button group."""
    type_id = exts.PULLDOWN_BUTTON_POSTFIX


class SplitPushButtonGroup(GenericUICommandGroup):
    """Split push button group."""
    type_id = exts.SPLITPUSH_BUTTON_POSTFIX


class SplitButtonGroup(GenericUICommandGroup):
    """Split button group."""
    type_id = exts.SPLIT_BUTTON_POSTFIX


class GenericStack(GenericUIContainer):
    """Generic UI stack.

    Stacks include GenericUICommand, or GenericUICommandGroup.
    """
    type_id = exts.STACK_BUTTON_POSTFIX

    allowed_sub_cmps = \
        [GenericUICommandGroup, GenericUICommand, NoScriptButton]

    @property
    def control_id(self):
        # stacks don't have control id
        return self.parent_ctrl_id if self.parent_ctrl_id else ''

    def has_commands(self):
        for component in self:
            if not component.is_container:
                if isinstance(component, GenericUICommand):
                    return True
            else:
                if component.has_commands():
                    return True


class StackButtonGroup(GenericStack):
    """Stack buttons group."""
    type_id = exts.STACK_BUTTON_POSTFIX


class Panel(GenericUIContainer):
    """Panel container.

    Panels include GenericStack, GenericUICommand, or GenericUICommandGroup
    """
    type_id = exts.PANEL_POSTFIX
    allowed_sub_cmps = \
        [GenericStack, GenericUICommandGroup, GenericUICommand, NoScriptButton]

    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        GenericUIContainer.__init__(self, cmp_path=cmp_path)
        self.panel_background = \
            self.title_background = \
                self.slideout_background = None
        # read metadata from metadata file
        if self.meta:
            # check for background color configs
            self.panel_background = \
                self.meta.get(exts.MDATA_BACKGROUND_KEY, None)
            if self.panel_background:
                if isinstance(self.panel_background, dict):
                    self.title_background = self.panel_background.get(
                        exts.MDATA_BACKGROUND_TITLE_KEY, None)
                    self.slideout_background = self.panel_background.get(
                        exts.MDATA_BACKGROUND_SLIDEOUT_KEY, None)
                    self.panel_background = self.panel_background.get(
                        exts.MDATA_BACKGROUND_PANEL_KEY, None)
                elif not isinstance(self.panel_background, str):
                    mlogger.error(
                        "%s bad background definition in metadata.", self)

    def has_commands(self):
        for component in self:
            if not component.is_container:
                if isinstance(component, GenericUICommand):
                    return True
            else:
                if component.has_commands():
                    return True

    def contains(self, item_name):
        # Panels contain stacks. But stacks itself does not have any ui and its
        # subitems are displayed within the ui of the parent panel.
        # This is different from pulldowns and other button groups.
        # Button groups, contain and display their sub components in their
        # own drop down menu. So when checking if panel has a button,
        # panel should check all the items visible to the user and respond.
        item_exists = GenericUIContainer.contains(self, item_name)
        if item_exists:
            return True
        else:
            # if child is a stack item, check its children too
            for component in self:
                if isinstance(component, GenericStack) \
                        and component.contains(item_name):
                    return True


class Tab(GenericUIContainer):
    """Tab container for Panels."""
    type_id = exts.TAB_POSTFIX
    allowed_sub_cmps = [Panel]

    def has_commands(self):
        for panel in self:
            if panel.has_commands():
                return True
        return False


class Extension(GenericUIContainer):
    """UI Tools extension."""
    type_id = exts.ExtensionTypes.UI_EXTENSION.POSTFIX
    allowed_sub_cmps = [Tab]

    def __init__(self, cmp_path=None):
        self.pyrvt_version = None
        self.dir_hash_value = None
        # using classname otherwise exceptions in superclasses won't show
        GenericUIContainer.__init__(self, cmp_path=cmp_path)

    def _calculate_extension_dir_hash(self):
        """Creates a unique hash # to represent state of directory."""
        # search does not include png files:
        #   if png files are added the parent folder mtime gets affected
        #   cache only saves the png address and not the contents so they'll
        #   get loaded everytime
        #       see http://stackoverflow.com/a/5141710/2350244
        pat = '(\\' + exts.TAB_POSTFIX + ')|(\\' + exts.PANEL_POSTFIX + ')'
        pat += '|(\\' + exts.PULLDOWN_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.SPLIT_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.SPLITPUSH_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.STACK_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.PUSH_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.SMART_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.LINK_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.PANEL_PUSH_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.PANEL_PUSH_BUTTON_POSTFIX + ')'
        pat += '|(\\' + exts.CONTENT_BUTTON_POSTFIX + ')'
        # tnteresting directories
        pat += '|(\\' + exts.COMP_LIBRARY_DIR_NAME + ')'
        pat += '|(\\' + exts.COMP_HOOKS_DIR_NAME + ')'
        # search for scripts, setting files (future support), and layout files
        patfile = '(\\' + exts.PYTHON_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.CSHARP_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.VB_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.RUBY_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.DYNAMO_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.GRASSHOPPER_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.GRASSHOPPERX_SCRIPT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.CONTENT_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.YAML_FILE_FORMAT + ')'
        patfile += '|(\\' + exts.JSON_FILE_FORMAT + ')'
        from pyrevit.revit import ui
        return coreutils.calculate_dir_hash(self.directory, pat, patfile) + str(ui.get_current_theme())

    def _update_from_directory(self):   #pylint: disable=W0221
        # using classname otherwise exceptions in superclasses won't show
        GenericUIContainer._update_from_directory(self)
        self.pyrvt_version = versionmgr.get_pyrevit_version().get_formatted()

        # extensions can store event hooks under
        # hooks/ inside the component folder
        hooks_path = op.join(self.directory, exts.COMP_HOOKS_DIR_NAME)
        self.hooks_path = hooks_path if op.exists(hooks_path) else None

        # extensions can store preflight checks under
        # checks/ inside the component folder
        checks_path = op.join(self.directory, exts.COMP_CHECKS_DIR_NAME)
        self.checks_path = checks_path if op.exists(checks_path) else None

        self.dir_hash_value = self._calculate_extension_dir_hash()

    @property
    def control_id(self):
        return None

    @property
    def startup_script(self):
        return self.find_bundle_file([
            exts.PYTHON_EXT_STARTUP_FILE,
            exts.CSHARP_EXT_STARTUP_FILE,
            exts.VB_EXT_STARTUP_FILE,
            exts.RUBY_EXT_STARTUP_FILE,
        ])

    def get_hash(self):
        return coreutils.get_str_hash(safe_strtype(self.get_cache_data()))

    def get_all_commands(self):
        return self.find_components_of_type(GenericUICommand)

    def get_manifest_file(self):
        return self.get_bundle_file(exts.EXT_MANIFEST_FILE)

    def get_manifest(self):
        manifest_file = self.get_manifest_file()
        if manifest_file:
            with codecs.open(manifest_file, 'r', 'utf-8') as mfile:
                try:
                    manifest_cfg = json.load(mfile)
                    return manifest_cfg
                except Exception as manfload_err:
                    print('Can not parse ext manifest file: {} '
                          '| {}'.format(manifest_file, manfload_err))
                    return

    def configure(self):
        cfg_dict = self.get_manifest()
        if cfg_dict:
            for component in self:
                component.configure(cfg_dict)

    def get_extension_modules(self):
        modules = []
        if self.binary_path and op.exists(self.binary_path):
            for item in os.listdir(self.binary_path):
                item_path = op.join(self.binary_path, item)
                item_name = item.lower()
                if op.isfile(item_path) \
                        and item_name.endswith(framework.ASSEMBLY_FILE_TYPE):
                    modules.append(item_path)
        return modules

    def get_command_modules(self):
        referenced_modules = set()
        for cmd in self.get_all_commands():
            for module in cmd.modules:
                cmd_module = cmd.find_bundle_module(module)
                if cmd_module:
                    referenced_modules.add(cmd_module)
        return referenced_modules

    def get_hooks(self):
        hook_scripts = os.listdir(self.hooks_path) if self.hooks_path else []
        return [op.join(self.hooks_path, x) for x in hook_scripts]

    def get_checks(self):
        check_scripts = os.listdir(self.checks_path) if self.checks_path else []
        return [op.join(self.checks_path, x) for x in check_scripts]


class LibraryExtension(GenericComponent):
    """Library extension."""
    type_id = exts.ExtensionTypes.LIB_EXTENSION.POSTFIX

    def __init__(self, cmp_path=None):
        # using classname otherwise exceptions in superclasses won't show
        GenericComponent.__init__(self)
        self.directory = cmp_path

        if self.directory:
            self.name = op.splitext(op.basename(self.directory))[0]

    def __repr__(self):
        return '<type_id \'{}\' name \'{}\' @ \'{}\'>'\
            .format(self.type_id, self.name, self.directory)

    @classmethod
    def matches(cls, component_path):
        return component_path.lower().endswith(cls.type_id)

"""Base module to handle extension binary caching."""
import pickle

from pyrevit import PyRevitException
from pyrevit.coreutils import appdata
from pyrevit.coreutils.logger import get_logger

#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


loaded_extensions = []


def _get_cache_file(cached_ext):
    return appdata.get_data_file(file_id='cache_{}'.format(cached_ext.name),
                                 file_ext='pickle')


def update_cache(parsed_ext):
    try:
        mlogger.debug('Writing cache for: %s', parsed_ext)
        cache_file = _get_cache_file(parsed_ext)
        mlogger.debug('Cache file is: %s', cache_file)
        with open(cache_file, 'wb') as bin_cache_file:
            pickle.dump(parsed_ext, bin_cache_file, pickle.HIGHEST_PROTOCOL)
    except Exception as err:
        raise PyRevitException('Error writing cache for: {} | {}'
                               .format(parsed_ext, err))


def get_cached_extension(installed_ext):
    for loaded_ext in loaded_extensions:
        if loaded_ext.name == installed_ext.name:
            return loaded_ext

    try:
        mlogger.debug('Reading cache for: %s', installed_ext)
        cache_file = _get_cache_file(installed_ext)
        mlogger.debug('Cache file is: %s', cache_file)
        with open(cache_file, 'rb') as bin_cache_file:
            unpickled_pkg = pickle.load(bin_cache_file)
    except Exception as err:
        raise PyRevitException('Error reading cache for: {} | {}'
                               .format(installed_ext, err))

    return unpickled_pkg


def is_cache_valid(extension):
    try:
        cached_ext = get_cached_extension(extension)
        mlogger.debug('Extension cache directory is: %s for: %s',
                      extension.directory, extension)
        cache_dir_valid = \
            cached_ext.directory == extension.directory

        mlogger.debug('Extension cache version is: %s for: %s',
                      extension.pyrvt_version, extension)
        cache_version_valid = \
            cached_ext.pyrvt_version == extension.pyrvt_version

        mlogger.debug('Extension hash value is: %s for: %s',
                      extension.dir_hash_value, extension)
        cache_hash_valid = \
            cached_ext.dir_hash_value == extension.dir_hash_value

        cache_valid = \
            cache_dir_valid and cache_version_valid and cache_hash_valid

        # add loaded package to list so it can be recovered later
        if cache_valid:
            loaded_extensions.append(cached_ext)

        # cache is valid if both version and hash value match
        return cache_valid

    except PyRevitException as err:
        mlogger.debug('Error reading cache file or file is not available: %s',
                      err)
        return False

    except Exception as err:
        mlogger.debug('Error determining cache validity: %s | %s',
                      extension, err)
        return False

"""Base module to handle extension ASCII caching."""
import json
import codecs

from pyrevit import PyRevitException
from pyrevit.coreutils import appdata
from pyrevit.coreutils import get_all_subclasses
from pyrevit.coreutils.logger import get_logger
from pyrevit.extensions import components as comps
from pyrevit.extensions import genericcomps as gencomps

#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


def _get_cache_file(cached_ext):
    return appdata.get_data_file(file_id='cache_{}'.format(cached_ext.name),
                                 file_ext='json')


def _make_cache_from_cmp(obj):
    return json.dumps(obj,
                      default=lambda o: o.get_cache_data(),
                      sort_keys=True,
                      indent=4,
                      ensure_ascii=False)


def _make_layoutitems_from_cache(parent_cmp, cached_layoutitems):
    if hasattr(parent_cmp, gencomps.LAYOUT_ITEM_KEY):
        litems = []
        for litem_cache in cached_layoutitems:
            # grab the layout directive cache and create component
            ldir = None
            ldir_cache = litem_cache.get(gencomps.LAYOUT_DIR_KEY, {})
            if ldir_cache:
                ldir = gencomps.LayoutDirective()
                ldir.load_cache_data(ldir_cache)

            litem = gencomps.LayoutItem()
            litem.load_cache_data(litem_cache)
            # set the layout directive
            setattr(litem, gencomps.LAYOUT_DIR_KEY, ldir)
            litems.append(litem)
        setattr(parent_cmp, gencomps.LAYOUT_ITEM_KEY, litems)


def _make_sub_cmp_from_cache(parent_cmp, cached_sub_cmps):
    mlogger.debug('Processing cache for: %s', parent_cmp)
    # get allowed classes under this component
    allowed_sub_cmps = get_all_subclasses(parent_cmp.allowed_sub_cmps)
    mlogger.debug('Allowed sub components are: %s', allowed_sub_cmps)
    # iterate through list of cached sub components
    for cached_cmp in cached_sub_cmps:  # type: dict
        for sub_class in allowed_sub_cmps:
            if sub_class.type_id == cached_cmp[gencomps.TYPE_ID_KEY]:
                mlogger.debug('Creating sub component from cache: %s, %s',
                              cached_cmp[gencomps.NAME_KEY], sub_class)

                # cached_cmp might contain gencomps.SUB_CMP_KEY. This needs to be
                # removed since this function will make all the children
                # recursively. So if this component has gencomps.SUB_CMP_KEY means
                # it has sub components:
                sub_cmp_cache = None
                if gencomps.SUB_CMP_KEY in cached_cmp.keys():
                    # drop subcomponents dict from cached_cmp since we
                    # don't want the loaded_cmp to include this
                    sub_cmp_cache = cached_cmp.pop(gencomps.SUB_CMP_KEY)

                # grab the layout items
                # layoutitems_cache is list[dict]
                layoutitems_cache = None
                if gencomps.LAYOUT_ITEM_KEY in cached_cmp.keys():
                    # drop layoutitems dict from cached_cmp since we
                    # don't want the loaded_cmp to include this
                    layoutitems_cache = cached_cmp.pop(gencomps.LAYOUT_ITEM_KEY)

                # create sub component from cleaned cached_cmp
                loaded_cmp = sub_class()
                loaded_cmp.load_cache_data(cached_cmp)

                # now process sub components for loaded_cmp if any
                if sub_cmp_cache:
                    _make_sub_cmp_from_cache(loaded_cmp, sub_cmp_cache)

                # apply layout items
                if layoutitems_cache:
                    _make_layoutitems_from_cache(loaded_cmp, layoutitems_cache)

                parent_cmp.add_component(loaded_cmp)


def _read_cache_for(cached_ext):
    try:
        mlogger.debug('Reading cache for: %s', cached_ext)
        cache_file = _get_cache_file(cached_ext)
        mlogger.debug('Cache file is: %s', cache_file)
        with codecs.open(_get_cache_file(cached_ext), 'r', 'utf-8') \
                as cache_file:
            cached_tab_dict = json.load(cache_file)
        return cached_tab_dict
    except Exception as err:
        raise PyRevitException('Error reading cache for: {} | {}'
                               .format(cached_ext, err))


def _write_cache_for(parsed_ext):
    try:
        mlogger.debug('Writing cache for: %s', parsed_ext)
        cache_file = _get_cache_file(parsed_ext)
        mlogger.debug('Cache file is: %s', cache_file)
        with codecs.open(cache_file, 'w', 'utf-8') as cache_file:
            cache_file.write(_make_cache_from_cmp(parsed_ext))
    except Exception as err:
        mlogger.debug('Error writing cache...')
        raise PyRevitException('Error writing cache for: {} | {}'
                               .format(parsed_ext, err))


def update_cache(parsed_ext):
    mlogger.debug('Updating cache for tab: %s ...', parsed_ext.name)
    _write_cache_for(parsed_ext)
    mlogger.debug('Cache updated for tab: %s', parsed_ext.name)


def get_cached_extension(installed_ext):
    cached_ext_dict = _read_cache_for(installed_ext)
    # try:
    mlogger.debug('Constructing components from cache for: %s',
                    installed_ext)
    # get cached sub component dictionary and call recursive maker function
    _make_sub_cmp_from_cache(installed_ext,
                                cached_ext_dict.pop(gencomps.SUB_CMP_KEY))
    mlogger.debug('Load successful...')
    # except Exception as err:
    #     mlogger.debug('Error reading cache...')
    #     raise PyRevitException('Error creating ext from cache for: {} | {}'
    #                            .format(installed_ext.name, err))

    return installed_ext


def is_cache_valid(extension):
    try:
        cached_ext_dict = _read_cache_for(extension)  # type: dict
        mlogger.debug('Extension cache directory is: %s for: %s',
                      extension.directory, extension)
        cache_dir_valid = cached_ext_dict[gencomps.EXT_DIR_KEY] == extension.directory

        mlogger.debug('Extension cache version is: %s for: %s',
                      extension.pyrvt_version, extension)
        cache_version_valid = \
            cached_ext_dict[comps.EXT_HASH_VERSION_KEY] == extension.pyrvt_version

        mlogger.debug('Extension hash value is: %s for:%s',
                      extension.dir_hash_value, extension)
        cache_hash_valid = \
            cached_ext_dict[comps.EXT_HASH_VALUE_KEY] == extension.dir_hash_value

        cache_valid = \
            cache_dir_valid and cache_version_valid and cache_hash_valid

        # cache is valid if both version and hash value match
        return cache_valid

    except PyRevitException as err:
        mlogger.debug(err)
        return False

    except Exception as err:
        mlogger.debug('Error determining cache validity: %s | %s',
                      extension, err)

"""Reusable WPF forms for pyRevit.

Examples:
    ```python
    from pyrevit.forms import WPFWindow
    ```
"""
#pylint: disable=consider-using-f-string,wrong-import-position

import re
import sys
import os
import os.path as op
import string
from collections import OrderedDict, namedtuple
import threading
from functools import wraps
import datetime
import webbrowser

from pyrevit import HOST_APP, EXEC_PARAMS, DOCS, BIN_DIR
from pyrevit import PyRevitCPythonNotSupported, PyRevitException
from pyrevit.compat import PY3, IRONPY340
from pyrevit.compat import safe_strtype

if PY3 and not IRONPY340:
    raise PyRevitCPythonNotSupported('pyrevit.forms')

from pyrevit import coreutils
from pyrevit.coreutils.logger import get_logger
from pyrevit.coreutils import colors
from pyrevit import framework
from pyrevit.framework import System
from pyrevit.framework import Threading
from pyrevit.framework import Interop
from pyrevit.framework import Input
from pyrevit.framework import wpf, Forms, Controls, Media
from pyrevit.framework import CPDialogs
from pyrevit.framework import ComponentModel
from pyrevit.framework import ObservableCollection
from pyrevit.framework import Uri, UriKind, ResourceDictionary
from pyrevit.api import AdWindows
from pyrevit import revit, UI, DB
from pyrevit.forms import utils
from pyrevit.forms import toaster
from pyrevit import versionmgr
from pyrevit.userconfig import user_config

import pyevent #pylint: disable=import-error

import Autodesk.Windows.ComponentManager #pylint: disable=import-error
import Autodesk.Internal.InfoCenter #pylint: disable=import-error

#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


DEFAULT_CMDSWITCHWND_WIDTH = 600
DEFAULT_SEARCHWND_WIDTH = 600
DEFAULT_SEARCHWND_HEIGHT = 100
DEFAULT_INPUTWINDOW_WIDTH = 500
DEFAULT_INPUTWINDOW_HEIGHT = 600
DEFAULT_RECOGNIZE_ACCESS_KEY = False

WPF_HIDDEN = framework.Windows.Visibility.Hidden
WPF_COLLAPSED = framework.Windows.Visibility.Collapsed
WPF_VISIBLE = framework.Windows.Visibility.Visible


XAML_FILES_DIR = op.dirname(__file__)


ParamDef = namedtuple('ParamDef', ['name', 'istype', 'definition', 'isreadonly'])
"""Parameter definition tuple.

Attributes:
    name (str): parameter name
    istype (bool): true if type parameter, otherwise false
    definition (Autodesk.Revit.DB.Definition): parameter definition object
    isreadonly (bool): true if the parameter value can't be edited
"""


# https://gui-at.blogspot.com/2009/11/inotifypropertychanged-in-ironpython.html
class reactive(property):
    """Decorator for WPF bound properties."""
    def __init__(self, getter):
        def newgetter(ui_control):
            try:
                return getter(ui_control)
            except AttributeError:
                return None
        super(reactive, self).__init__(newgetter)

    def setter(self, setter):
        """Property setter."""
        def newsetter(ui_control, newvalue):
            oldvalue = self.fget(ui_control)
            if oldvalue != newvalue:
                setter(ui_control, newvalue)
                ui_control.OnPropertyChanged(setter.__name__)
        return property(
            fget=self.fget,
            fset=newsetter,
            fdel=self.fdel,
            doc=self.__doc__)


class Reactive(ComponentModel.INotifyPropertyChanged):
    """WPF property updator base mixin."""
    PropertyChanged, _propertyChangedCaller = pyevent.make_event()

    def add_PropertyChanged(self, value):
        """Called when a property is added to the object."""
        self.PropertyChanged += value

    def remove_PropertyChanged(self, value):
        """Called when a property is removed from the object."""
        self.PropertyChanged -= value

    def OnPropertyChanged(self, prop_name):
        """Called when a property is changed.

        Args:
            prop_name (str): property name
        """
        if self._propertyChangedCaller:
            args = ComponentModel.PropertyChangedEventArgs(prop_name)
            self._propertyChangedCaller(self, args)


class WindowToggler(object):
    """Context manager to toggle window visibility."""
    def __init__(self, window):
        self._window = window

    def __enter__(self):
        self._window.hide()

    def __exit__(self, exception, exception_value, traceback):
        self._window.show_dialog()


class WPFWindow(framework.Windows.Window):
    r"""WPF Window base class for all pyRevit forms.

    Args:
        xaml_source (str): xaml source filepath or xaml content
        literal_string (bool): xaml_source contains xaml content, not filepath
        handle_esc (bool): handle Escape button and close the window
        set_owner (bool): set the owner of window to host app window

    Examples:
        ```python
        from pyrevit import forms
        layout = '<Window ' \
                 'xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" ' \
                 'xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" ' \
                 'ShowInTaskbar="False" ResizeMode="NoResize" ' \
                 'WindowStartupLocation="CenterScreen" ' \
                 'HorizontalContentAlignment="Center">' \
                 '</Window>'
        w = forms.WPFWindow(layout, literal_string=True)
        w.show()
        ```
    """

    def __init__(self, xaml_source, literal_string=False, handle_esc=True, set_owner=True):
        """Initialize WPF window and resources."""
        # load xaml
        self.load_xaml(
            xaml_source,
            literal_string=literal_string,
            handle_esc=handle_esc,
            set_owner=set_owner
            )

    def load_xaml(self, xaml_source, literal_string=False, handle_esc=True, set_owner=True):
        """Load the window XAML file.

        Args:
            xaml_source (str): The XAML content or file path to load.
            literal_string (bool, optional): True if `xaml_source` is content,
                False if it is a path. Defaults to False.
            handle_esc (bool, optional): Whether the ESC key should be handled.
                Defaults to True.
            set_owner (bool, optional): Whether to se the window owner.
                Defaults to True.
        """
        # create new id for this window
        self.window_id = coreutils.new_uuid()

        if not literal_string:
            wpf.LoadComponent(self, self._determine_xaml(xaml_source))
        else:
            wpf.LoadComponent(self, framework.StringReader(xaml_source))

        # set properties
        self.thread_id = framework.get_current_thread_id()
        if set_owner:
            self.setup_owner()
        self.setup_icon()
        WPFWindow.setup_resources(self)
        if handle_esc:
            self.setup_default_handlers()


    def _determine_xaml(self, xaml_source):
        xaml_file = xaml_source
        if not op.exists(xaml_file):
            xaml_file = os.path.join(EXEC_PARAMS.command_path, xaml_source)

        english_xaml_file = xaml_file.replace('.xaml', '.en_us.xaml')
        localized_xaml_file = xaml_file.replace(
            '.xaml',
            '.{}.xaml'.format(user_config.user_locale)
        )

        english_xaml_resfile = \
            xaml_file.replace('.xaml', '.ResourceDictionary.en_us.xaml')
        localized_xaml_resfile = xaml_file.replace(
            '.xaml',
            '.ResourceDictionary.{}.xaml'.format(user_config.user_locale)
        )

        # if localized version of xaml file is provided, use that
        if os.path.isfile(localized_xaml_file):
            return localized_xaml_file

        if os.path.isfile(english_xaml_file):
            return english_xaml_file

        # otherwise look for .ResourceDictionary files and load those,
        # returning the original xaml_file
        if os.path.isfile(localized_xaml_resfile):
            self.merge_resource_dict(localized_xaml_resfile)

        elif os.path.isfile(english_xaml_resfile):
            self.merge_resource_dict(english_xaml_resfile)

        return xaml_file

    def merge_resource_dict(self, xaml_source):
        """Merge a ResourceDictionary xaml file with this window.

        Args:
            xaml_source (str): xaml file with the resource dictionary
        """
        lang_dictionary = ResourceDictionary()
        lang_dictionary.Source = Uri(xaml_source, UriKind.Absolute)
        self.Resources.MergedDictionaries.Add(lang_dictionary)

    def get_locale_string(self, string_name):
        """Get localized string.

        Args:
            string_name (str): string name

        Returns:
            (str): localized string
        """
        return self.FindResource(string_name)

    def setup_owner(self):
        """Set the window owner."""
        wih = Interop.WindowInteropHelper(self)
        wih.Owner = AdWindows.ComponentManager.ApplicationWindow

    @staticmethod
    def setup_resources(wpf_ctrl):
        """Sets the WPF resources."""
        #2c3e50
        wpf_ctrl.Resources['pyRevitDarkColor'] = \
            Media.Color.FromArgb(0xFF, 0x2c, 0x3e, 0x50)

        #23303d
        wpf_ctrl.Resources['pyRevitDarkerDarkColor'] = \
            Media.Color.FromArgb(0xFF, 0x23, 0x30, 0x3d)

        #ffffff
        wpf_ctrl.Resources['pyRevitButtonColor'] = \
            Media.Color.FromArgb(0xFF, 0xff, 0xff, 0xff)

        #f39c12
        wpf_ctrl.Resources['pyRevitAccentColor'] = \
            Media.Color.FromArgb(0xFF, 0xf3, 0x9c, 0x12)

        wpf_ctrl.Resources['pyRevitDarkBrush'] = \
            Media.SolidColorBrush(wpf_ctrl.Resources['pyRevitDarkColor'])
        wpf_ctrl.Resources['pyRevitAccentBrush'] = \
            Media.SolidColorBrush(wpf_ctrl.Resources['pyRevitAccentColor'])

        wpf_ctrl.Resources['pyRevitDarkerDarkBrush'] = \
            Media.SolidColorBrush(wpf_ctrl.Resources['pyRevitDarkerDarkColor'])

        wpf_ctrl.Resources['pyRevitButtonForgroundBrush'] = \
            Media.SolidColorBrush(wpf_ctrl.Resources['pyRevitButtonColor'])

        wpf_ctrl.Resources['pyRevitRecognizesAccessKey'] = \
            DEFAULT_RECOGNIZE_ACCESS_KEY

    def setup_default_handlers(self):
        """Set the default handlers."""
        self.PreviewKeyDown += self.handle_input_key    #pylint: disable=E1101

    def handle_input_key(self, sender, args):    #pylint: disable=W0613
        """Handle keyboard input and close the window on Escape."""
        if args.Key == Input.Key.Escape:
            self.Close()

    def set_icon(self, icon_path):
        """Set window icon to given icon path."""
        self.Icon = utils.bitmap_from_file(icon_path)

    def setup_icon(self):
        """Setup default window icon."""
        self.set_icon(op.join(BIN_DIR, 'pyrevit_settings.png'))

    def hide(self):
        """Hide window."""
        self.Hide()

    def show(self, modal=False):
        """Show window."""
        if modal:
            return self.ShowDialog()
        # else open non-modal
        self.Show()

    def show_dialog(self):
        """Show modal window."""
        return self.ShowDialog()

    @staticmethod
    def set_image_source_file(wpf_element, image_file):
        """Set source file for image element.

        Args:
            wpf_element (System.Windows.Controls.Image): xaml image element
            image_file (str): image file path
        """
        if not op.exists(image_file):
            wpf_element.Source = \
                utils.bitmap_from_file(
                    os.path.join(EXEC_PARAMS.command_path,
                                 image_file)
                    )
        else:
            wpf_element.Source = utils.bitmap_from_file(image_file)

    def set_image_source(self, wpf_element, image_file):
        """Set source file for image element.

        Args:
            wpf_element (System.Windows.Controls.Image): xaml image element
            image_file (str): image file path
        """
        WPFWindow.set_image_source_file(wpf_element, image_file)

    def dispatch(self, func, *args, **kwargs):
        """Runs the function in a new thread.

        Args:
            func (Callable): function to run
            *args (Any): positional arguments to pass to func
            **kwargs (Any): keyword arguments to pass to func
        """
        if framework.get_current_thread_id() == self.thread_id:
            t = threading.Thread(
                target=func,
                args=args,
                kwargs=kwargs
                )
            t.start()
        else:
            # ask ui thread to call the func with args and kwargs
            self.Dispatcher.Invoke(
                System.Action(
                    lambda: func(*args, **kwargs)
                    ),
                Threading.DispatcherPriority.Background
                )

    def conceal(self):
        """Conceal window."""
        return WindowToggler(self)

    @property
    def pyrevit_version(self):
        """Active pyRevit formatted version e.g. '4.9-beta'."""
        return 'pyRevit {}'.format(
            versionmgr.get_pyrevit_version().get_formatted()
            )

    @staticmethod
    def hide_element(*wpf_elements):
        """Collapse elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be collaped
        """
        for wpfel in wpf_elements:
            wpfel.Visibility = WPF_COLLAPSED

    @staticmethod
    def show_element(*wpf_elements):
        """Show collapsed elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be set to visible.
        """
        for wpfel in wpf_elements:
            wpfel.Visibility = WPF_VISIBLE

    @staticmethod
    def toggle_element(*wpf_elements):
        """Toggle visibility of elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be toggled.
        """
        for wpfel in wpf_elements:
            if wpfel.Visibility == WPF_VISIBLE:
                WPFWindow.hide_element(wpfel)
            elif wpfel.Visibility == WPF_COLLAPSED:
                WPFWindow.show_element(wpfel)

    @staticmethod
    def disable_element(*wpf_elements):
        """Enable elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be enabled
        """
        for wpfel in wpf_elements:
            wpfel.IsEnabled = False

    @staticmethod
    def enable_element(*wpf_elements):
        """Enable elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be enabled
        """
        for wpfel in wpf_elements:
            wpfel.IsEnabled = True

    def handle_url_click(self, sender, args): #pylint: disable=unused-argument
        """Callback for handling click on package website url."""
        return webbrowser.open_new_tab(sender.NavigateUri.AbsoluteUri)


class WPFPanel(framework.Windows.Controls.Page):
    r"""WPF panel base class for all pyRevit dockable panels.

    panel_id (str) must be set on the type to dockable panel uuid
    panel_source (str): xaml source filepath

    Examples:
        ```python
        from pyrevit import forms
        class MyPanel(forms.WPFPanel):
            panel_id = "181e05a4-28f6-4311-8a9f-d2aa528c8755"
            panel_source = "MyPanel.xaml"

        forms.register_dockable_panel(MyPanel)
        # then from the button that needs to open the panel
        forms.open_dockable_panel("181e05a4-28f6-4311-8a9f-d2aa528c8755")
        ```
    """

    panel_id = None
    panel_source = None

    def __init__(self):
        """Initialize WPF panel and resources."""
        if not self.panel_id:
            raise PyRevitException("\"panel_id\" property is not set")
        if not self.panel_source:
            raise PyRevitException("\"panel_source\" property is not set")

        if not op.exists(self.panel_source):
            wpf.LoadComponent(self,
                              os.path.join(EXEC_PARAMS.command_path,
                              self.panel_source))
        else:
            wpf.LoadComponent(self, self.panel_source)

        # set properties
        self.thread_id = framework.get_current_thread_id()
        WPFWindow.setup_resources(self)

    def set_image_source(self, wpf_element, image_file):
        """Set source file for image element.

        Args:
            wpf_element (System.Windows.Controls.Image): xaml image element
            image_file (str): image file path
        """
        WPFWindow.set_image_source_file(wpf_element, image_file)

    @staticmethod
    def hide_element(*wpf_elements):
        """Collapse elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be collaped
        """
        WPFPanel.hide_element(*wpf_elements)

    @staticmethod
    def show_element(*wpf_elements):
        """Show collapsed elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be set to visible.
        """
        WPFPanel.show_element(*wpf_elements)

    @staticmethod
    def toggle_element(*wpf_elements):
        """Toggle visibility of elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be toggled.
        """
        WPFPanel.toggle_element(*wpf_elements)

    @staticmethod
    def disable_element(*wpf_elements):
        """Enable elements.

        Args:
            *wpf_elements (list[UIElement]): WPF framework elements to be enabled
        """
        WPFPanel.disable_element(*wpf_elements)

    @staticmethod
    def enable_element(*wpf_elements):
        """Enable elements.

        Args:
            *wpf_elements (list): WPF framework elements to be enabled
        """
        WPFPanel.enable_element(*wpf_elements)

    def handle_url_click(self, sender, args): #pylint: disable=unused-argument
        """Callback for handling click on package website url."""
        return webbrowser.open_new_tab(sender.NavigateUri.AbsoluteUri)


class _WPFPanelProvider(UI.IDockablePaneProvider):
    """Internal Panel provider for panels."""

    def __init__(self, panel_type, default_visible=True):
        self._panel_type = panel_type
        self._default_visible = default_visible
        self.panel = self._panel_type()

    def SetupDockablePane(self, data):
        """Setup forms.WPFPanel set on this instance."""
        # TODO: need to implement panel data
        # https://apidocs.co/apps/revit/2021.1/98157ec2-ab26-6ab7-2933-d1b4160ba2b8.htm
        data.FrameworkElement = self.panel
        data.VisibleByDefault = self._default_visible


def is_registered_dockable_panel(panel_type):
    """Check if dockable panel is already registered.

    Args:
        panel_type (forms.WPFPanel): dockable panel type
    """
    panel_uuid = coreutils.Guid.Parse(panel_type.panel_id)
    dockable_panel_id = UI.DockablePaneId(panel_uuid)
    return UI.DockablePane.PaneExists(dockable_panel_id)


def register_dockable_panel(panel_type, default_visible=True):
    """Register dockable panel.

    Args:
        panel_type (forms.WPFPanel): dockable panel type
        default_visible (bool, optional):
            whether panel should be visible by default
    """
    if not issubclass(panel_type, WPFPanel):
        raise PyRevitException(
            "Dockable pane must be a subclass of forms.WPFPanel"
            )

    panel_uuid = coreutils.Guid.Parse(panel_type.panel_id)
    dockable_panel_id = UI.DockablePaneId(panel_uuid)
    panel_provider = _WPFPanelProvider(panel_type, default_visible)
    HOST_APP.uiapp.RegisterDockablePane(
        dockable_panel_id,
        panel_type.panel_title,
        panel_provider
    )

    return panel_provider.panel


def open_dockable_panel(panel_type_or_id):
    """Open previously registered dockable panel.

    Args:
        panel_type_or_id (forms.WPFPanel, str): panel type or id
    """
    toggle_dockable_panel(panel_type_or_id, True)


def close_dockable_panel(panel_type_or_id):
    """Close previously registered dockable panel.

    Args:
        panel_type_or_id (forms.WPFPanel, str): panel type or id
    """
    toggle_dockable_panel(panel_type_or_id, False)


def toggle_dockable_panel(panel_type_or_id, state):
    """Toggle previously registered dockable panel.

    Args:
        panel_type_or_id (forms.WPFPanel | str): panel type or id
        state (bool): True to show the panel, False to hide it.
    """
    dpanel_id = None
    if isinstance(panel_type_or_id, str):
        panel_id = coreutils.Guid.Parse(panel_type_or_id)
        dpanel_id = UI.DockablePaneId(panel_id)
    elif issubclass(panel_type_or_id, WPFPanel):
        panel_id = coreutils.Guid.Parse(panel_type_or_id.panel_id)
        dpanel_id = UI.DockablePaneId(panel_id)
    else:
        raise PyRevitException("Given type is not a forms.WPFPanel")

    if dpanel_id:
        if UI.DockablePane.PaneIsRegistered(dpanel_id):
            dockable_panel = HOST_APP.uiapp.GetDockablePane(dpanel_id)
            if state:
                dockable_panel.Show()
            else:
                dockable_panel.Hide()
        else:
            raise PyRevitException(
                "Panel with id \"%s\" is not registered" % panel_type_or_id
                )


class TemplateUserInputWindow(WPFWindow):
    """Base class for pyRevit user input standard forms.

    Args:
        context (any): window context element(s)
        title (str): window title
        width (int): window width
        height (int): window height
        **kwargs (Any): other arguments to be passed to :func:`_setup`
    """

    xaml_source = 'BaseWindow.xaml'

    def __init__(self, context, title, width, height, **kwargs):
        """Initialize user input window."""
        WPFWindow.__init__(self,
                           op.join(XAML_FILES_DIR, self.xaml_source),
                           handle_esc=True)
        self.Title = title or 'pyRevit'
        self.Width = width
        self.Height = height

        self._context = context
        self.response = None

        # parent window?
        owner = kwargs.get('owner', None)
        if owner:
            # set wpf windows directly
            self.Owner = owner
            self.WindowStartupLocation = \
                framework.Windows.WindowStartupLocation.CenterOwner

        self._setup(**kwargs)

    def _setup(self, **kwargs):
        """Private method to be overriden by subclasses for window setup."""
        pass

    @classmethod
    def show(cls, context,  #pylint: disable=W0221
             title='User Input',
             width=DEFAULT_INPUTWINDOW_WIDTH,
             height=DEFAULT_INPUTWINDOW_HEIGHT, **kwargs):
        """Show user input window.

        Args:
            context (any): window context element(s)
            title (str): window title
            width (int): window width
            height (int): window height
            **kwargs (any): other arguments to be passed to window
        """
        dlg = cls(context, title, width, height, **kwargs)
        dlg.ShowDialog()
        return dlg.response


class TemplateListItem(Reactive):
    """Base class for checkbox option wrapping another object."""

    def __init__(self, orig_item,
                 checked=False, checkable=True, name_attr=None):
        """Initialize the checkbox option and wrap given obj.

        Args:
            orig_item (any): Object to wrap (must have name property
                             or be convertable to string with str()
            checked (bool): Initial state. Defaults to False
            checkable (bool): Use checkbox for items
            name_attr (str): Get this attribute of wrapped object as name
        """
        super(TemplateListItem, self).__init__()
        self.item = orig_item
        self.state = checked
        self._nameattr = name_attr
        self._checkable = checkable

    def __nonzero__(self):
        return self.state

    def __str__(self):
        return self.name or str(self.item)

    def __contains__(self, value):
        return value in self.name

    def __getattr__(self, param_name):
        return getattr(self.item, param_name)

    @property
    def name(self):
        """Name property."""
        # get custom attr, or name or just str repr
        if self._nameattr:
            return safe_strtype(getattr(self.item, self._nameattr))
        elif hasattr(self.item, 'name'):
            return getattr(self.item, 'name', '')
        else:
            return safe_strtype(self.item)

    def unwrap(self):
        """Unwrap and return wrapped object."""
        return self.item

    @reactive
    def checked(self):
        """Id checked."""
        return self.state

    @checked.setter
    def checked(self, value):
        self.state = value

    @property
    def checkable(self):
        """List Item CheckBox Visibility."""
        return WPF_VISIBLE if self._checkable \
            else WPF_COLLAPSED

    @checkable.setter
    def checkable(self, value):
        self._checkable = value


class SelectFromList(TemplateUserInputWindow):
    """Standard form to select from a list of items.

    Any object can be passed in a list to the ``context`` argument. This class
    wraps the objects passed to context, in `TemplateListItem`.
    This class provides the necessary mechanism to make this form work both
    for selecting items from a list, and from a list of checkboxes. See the
    list of arguments below for additional options and features.

    Args:
        context (list[str] or dict[list[str]]):
            list of items to be selected from
            OR
            dict of list of items to be selected from.
            use dict when input items need to be grouped
            e.g. List of sheets grouped by sheet set.
        title (str, optional): window title. see super class for defaults.
        width (int, optional): window width. see super class for defaults.
        height (int, optional): window height. see super class for defaults.

    Keyword Args:
        button_name (str, optional):
            name of select button. defaults to 'Select'
        name_attr (str, optional):
            object attribute that should be read as item name.
        multiselect (bool, optional):
            allow multi-selection (uses check boxes). defaults to False
        info_panel (bool, optional):
            show information panel and fill with .description property of item
        return_all (bool, optional):
            return all items. This is handly when some input items have states
            and the script needs to check the state changes on all items.
            This options works in multiselect mode only. defaults to False
        filterfunc (function):
            filter function to be applied to context items.
        resetfunc (function):
            reset function to be called when user clicks on Reset button
        group_selector_title (str):
            title for list group selector. defaults to 'List Group'
        default_group (str): name of defautl group to be selected
        sort_groups (str, optional): 
            Determines the sorting type applied to the list groups. This attribute can take one of the following values:
                'sorted': This will sort the groups in standard alphabetical order
                'natural': This will sort the groups in a manner that is more intuitive for human perception, especially when there are numbers involved.
                'unsorted': The groups will maintain the original order in which they were provided, without any reordering.
                Defaults to 'sorted'.

    Examples:
        ```python
        from pyrevit import forms
        items = ['item1', 'item2', 'item3']
        forms.SelectFromList.show(items, button_name='Select Item')
        ['item1']
        ```
        ```python
        from pyrevit import forms
        ops = [viewsheet1, viewsheet2, viewsheet3]
        res = forms.SelectFromList.show(ops,
                                        multiselect=False,
                                        name_attr='Name',
                                        button_name='Select Sheet')
        ```
        
        ```python
        from pyrevit import forms
        ops = {'Sheet Set A': [viewsheet1, viewsheet2, viewsheet3],
               'Sheet Set B': [viewsheet4, viewsheet5, viewsheet6]}
        res = forms.SelectFromList.show(ops,
                                        multiselect=True,
                                        name_attr='Name',
                                        group_selector_title='Sheet Sets',
                                        button_name='Select Sheets',
                                        sort_groups='sorted')
        ```
        
        This module also provides a wrapper base class :obj:`TemplateListItem`
        for when the checkbox option is wrapping another element,
        e.g. a Revit ViewSheet. Derive from this base class and define the
        name property to customize how the checkbox is named on the dialog.

        ```python
        from pyrevit import forms
        class MyOption(forms.TemplateListItem):
           @property
           def name(self):
               return '{} - {}{}'.format(self.item.SheetNumber,
                                         self.item.SheetNumber)
        ops = [MyOption('op1'), MyOption('op2', True), MyOption('op3')]
        res = forms.SelectFromList.show(ops,
                                        multiselect=True,
                                        button_name='Select Item')
        [bool(x) for x in res]  # or [x.state for x in res]
        [True, False, True]
        ```

    """

    in_check = False
    in_uncheck = False
    xaml_source = 'SelectFromList.xaml'

    @property
    def use_regex(self):
        """Is using regex?"""
        return self.regexToggle_b.IsChecked

    def _setup(self, **kwargs):
        # custom button name?
        button_name = kwargs.get('button_name', 'Select')
        if button_name:
            self.select_b.Content = button_name

        # attribute to use as name?
        self._nameattr = kwargs.get('name_attr', None)

        # multiselect?
        if kwargs.get('multiselect', False):
            self.multiselect = True
            self.list_lb.SelectionMode = Controls.SelectionMode.Extended
            self.show_element(self.checkboxbuttons_g)
        else:
            self.multiselect = False
            self.list_lb.SelectionMode = Controls.SelectionMode.Single
            self.hide_element(self.checkboxbuttons_g)

        # info panel?
        self.info_panel = kwargs.get('info_panel', False)

        # return checked items only?
        self.return_all = kwargs.get('return_all', False)

        # filter function?
        self.filter_func = kwargs.get('filterfunc', None)

        # reset function?
        self.reset_func = kwargs.get('resetfunc', None)
        if self.reset_func:
            self.show_element(self.reset_b)

        # context group title?
        self.ctx_groups_title = \
            kwargs.get('group_selector_title', 'List Group')
        self.ctx_groups_title_tb.Text = self.ctx_groups_title

        self.ctx_groups_active = kwargs.get('default_group', None)

        # group sorting?
        self.sort_groups = kwargs.get('sort_groups', 'sorted')
        if self.sort_groups not in ['sorted', 'unsorted', 'natural']:
            raise PyRevitException("Invalid value for 'sort_groups'. Allowed values are: 'sorted', 'unsorted', 'natural'.")

        # check for custom templates
        items_panel_template = kwargs.get('items_panel_template', None)
        if items_panel_template:
            self.Resources["ItemsPanelTemplate"] = items_panel_template

        item_container_template = kwargs.get('item_container_template', None)
        if item_container_template:
            self.Resources["ItemContainerTemplate"] = item_container_template

        item_template = kwargs.get('item_template', None)
        if item_template:
            self.Resources["ItemTemplate"] = \
                item_template

        # nicely wrap and prepare context for presentation, then present
        self._prepare_context()

        # setup search and filter fields
        self.hide_element(self.clrsearch_b)

        # active event listeners
        self.search_tb.TextChanged += self.search_txt_changed
        self.ctx_groups_selector_cb.SelectionChanged += self.selection_changed

        self.clear_search(None, None)

    def _prepare_context_items(self, ctx_items):
        new_ctx = []
        # filter context if necessary
        if self.filter_func:
            ctx_items = filter(self.filter_func, ctx_items)

        for item in ctx_items:
            if isinstance(item, TemplateListItem):
                item.checkable = self.multiselect
                new_ctx.append(item)
            else:
                new_ctx.append(
                    TemplateListItem(item,
                                     checkable=self.multiselect,
                                     name_attr=self._nameattr)
                    )

        return new_ctx

    @staticmethod
    def _natural_sort_key(key):
        return [int(c) if c.isdigit() else c.lower() for c in re.split(r'(\d+)', key)]

    def _prepare_context(self):
        if isinstance(self._context, dict) and self._context.keys():
            # Sort the groups if necessary
            if self.sort_groups == "sorted":
                sorted_groups = sorted(self._context.keys())
            elif self.sort_groups == "natural":
                sorted_groups = sorted(self._context.keys(), key=self._natural_sort_key)
            else:
                sorted_groups = self._context.keys()  # No sorting
            
            self._update_ctx_groups(sorted_groups)
            
            new_ctx = OrderedDict()
            for ctx_grp in sorted_groups:
                items = self._prepare_context_items(self._context[ctx_grp])
                new_ctx[ctx_grp] = items  # Do not sort the items within the groups

            self._context = new_ctx
        else:
            self._context = self._prepare_context_items(self._context)

    def _update_ctx_groups(self, ctx_group_names):
        self.show_element(self.ctx_groups_dock)
        self.ctx_groups_selector_cb.ItemsSource = ctx_group_names
        if self.ctx_groups_active in ctx_group_names:
            self.ctx_groups_selector_cb.SelectedIndex = \
                ctx_group_names.index(self.ctx_groups_active)
        else:
            self.ctx_groups_selector_cb.SelectedIndex = 0

    def _get_active_ctx_group(self):
        return self.ctx_groups_selector_cb.SelectedItem

    def _get_active_ctx(self):
        if isinstance(self._context, dict):
            return self._context[self._get_active_ctx_group()]
        else:
            return self._context

    def _list_options(self, option_filter=None):
        if option_filter:
            self.checkall_b.Content = 'Check'
            self.uncheckall_b.Content = 'Uncheck'
            self.toggleall_b.Content = 'Toggle'
            # get a match score for every item and sort high to low
            fuzzy_matches = sorted(
                [(x,
                  coreutils.fuzzy_search_ratio(
                      target_string=x.name,
                      sfilter=option_filter,
                      regex=self.use_regex))
                 for x in self._get_active_ctx()],
                key=lambda x: x[1],
                reverse=True
                )
            # filter out any match with score less than 80
            self.list_lb.ItemsSource = \
                ObservableCollection[TemplateListItem](
                    [x[0] for x in fuzzy_matches if x[1] >= 80]
                    )
        else:
            self.checkall_b.Content = 'Check All'
            self.uncheckall_b.Content = 'Uncheck All'
            self.toggleall_b.Content = 'Toggle All'
            self.list_lb.ItemsSource = \
                ObservableCollection[TemplateListItem](self._get_active_ctx())

    @staticmethod
    def _unwrap_options(options):
        unwrapped = []
        for optn in options:
            if isinstance(optn, TemplateListItem):
                unwrapped.append(optn.unwrap())
            else:
                unwrapped.append(optn)
        return unwrapped

    def _get_options(self):
        if self.multiselect:
            if self.return_all:
                return [x for x in self._get_active_ctx()]
            else:
                return self._unwrap_options(
                    [x for x in self._get_active_ctx()
                     if x.state or x in self.list_lb.SelectedItems]
                    )
        else:
            return self._unwrap_options([self.list_lb.SelectedItem])[0]

    def _set_states(self, state=True, flip=False, selected=False):
        if selected:
            current_list = self.list_lb.SelectedItems
        else:
            current_list = self.list_lb.ItemsSource
        for checkbox in current_list:
            # using .checked to push ui update
            if flip:
                checkbox.checked = not checkbox.checked
            else:
                checkbox.checked = state

    def _toggle_info_panel(self, state=True):
        if state:
            # enable the info panel
            self.splitterCol.Width = System.Windows.GridLength(8)
            self.infoCol.Width = System.Windows.GridLength(self.Width/2)
            self.show_element(self.infoSplitter)
            self.show_element(self.infoPanel)
        else:
            self.splitterCol.Width = self.infoCol.Width = \
                System.Windows.GridLength.Auto
            self.hide_element(self.infoSplitter)
            self.hide_element(self.infoPanel)

    def toggle_all(self, sender, args):    #pylint: disable=W0613
        """Handle toggle all button to toggle state of all check boxes."""
        self._set_states(flip=True)

    def check_all(self, sender, args):    #pylint: disable=W0613
        """Handle check all button to mark all check boxes as checked."""
        self._set_states(state=True)

    def uncheck_all(self, sender, args):    #pylint: disable=W0613
        """Handle uncheck all button to mark all check boxes as un-checked."""
        self._set_states(state=False)

    def check_selected(self, sender, args):    #pylint: disable=W0613
        """Mark selected checkboxes as checked."""
        if not self.in_check:
            try:
                self.in_check = True
                self._set_states(state=True, selected=True)
            finally:
                self.in_check = False

    def uncheck_selected(self, sender, args):    #pylint: disable=W0613
        """Mark selected checkboxes as unchecked."""
        if not self.in_uncheck:
            try:
                self.in_uncheck = True
                self._set_states(state=False, selected=True)
            finally:
                self.in_uncheck = False

    def button_reset(self, sender, args):#pylint: disable=W0613
        """Handle reset button click."""
        if self.reset_func:
            all_items = self.list_lb.ItemsSource
            self.reset_func(all_items)

    def button_select(self, sender, args):    #pylint: disable=W0613
        """Handle select button click."""
        self.response = self._get_options()
        self.Close()

    def search_txt_changed(self, sender, args):    #pylint: disable=W0613
        """Handle text change in search box."""
        if self.info_panel:
            self._toggle_info_panel(state=False)

        if self.search_tb.Text == '':
            self.hide_element(self.clrsearch_b)
        else:
            self.show_element(self.clrsearch_b)

        self._list_options(option_filter=self.search_tb.Text)

    def selection_changed(self, sender, args):
        """Handle selection change."""
        if self.info_panel:
            self._toggle_info_panel(state=False)

        self._list_options(option_filter=self.search_tb.Text)

    def selected_item_changed(self, sender, args):
        """Handle selected item change."""
        if self.info_panel and self.list_lb.SelectedItem is not None:
            self._toggle_info_panel(state=True)
            self.infoData.Text = \
                getattr(self.list_lb.SelectedItem, 'description', '')

    def toggle_regex(self, sender, args):
        """Activate regex in search."""
        self.regexToggle_b.Content = \
            self.Resources['regexIcon'] if self.use_regex \
                else self.Resources['filterIcon']
        self.search_txt_changed(sender, args)
        self.search_tb.Focus()

    def clear_search(self, sender, args):    #pylint: disable=W0613
        """Clear search box."""
        self.search_tb.Text = ' '
        self.search_tb.Clear()
        self.search_tb.Focus()


class CommandSwitchWindow(TemplateUserInputWindow):
    """Standard form to select from a list of command options.

    Keyword Args:
        context (list[str]): list of command options to choose from
        switches (list[str]): list of on/off switches
        message (str): window title message
        config (dict): dictionary of config dicts for options or switches
        recognize_access_key (bool): recognize '_' as mark of access key

    Returns:
        (str | tuple[str, dict]): name of selected option.
            if ``switches`` option is used, returns a tuple
            of selection option name and dict of switches

    Examples:
        This is an example with series of command options:

        ```python
        from pyrevit import forms
        ops = ['option1', 'option2', 'option3', 'option4']
        forms.CommandSwitchWindow.show(ops, message='Select Option')
        'option2'
        ```

        A more advanced example of combining command options, on/off switches,
        and option or switch configuration options:

        ```python
        from pyrevit import forms
        ops = ['option1', 'option2', 'option3', 'option4']
        switches = ['switch1', 'switch2']
        cfgs = {'option1': { 'background': '0xFF55FF'}}
        rops, rswitches = forms.CommandSwitchWindow.show(
            ops,
            switches=switches
            message='Select Option',
            config=cfgs,
            recognize_access_key=False
            )
        rops
        'option2'
        rswitches
        {'switch1': False, 'switch2': True}
        ```
    """

    xaml_source = 'CommandSwitchWindow.xaml'

    def _setup(self, **kwargs):
        self.selected_switch = ''
        self.Width = DEFAULT_CMDSWITCHWND_WIDTH
        self.Title = 'Command Options'

        message = kwargs.get('message', None)
        self._switches = kwargs.get('switches', [])
        if not isinstance(self._switches, dict):
            self._switches = dict.fromkeys(self._switches)

        configs = kwargs.get('config', None)

        self.message_label.Content = \
            message if message else 'Pick a command option:'

        self.Resources['pyRevitRecognizesAccessKey'] = \
            kwargs.get('recognize_access_key', DEFAULT_RECOGNIZE_ACCESS_KEY)

        # creates the switches first
        for switch, state in self._switches.items():
            my_togglebutton = framework.Controls.Primitives.ToggleButton()
            my_togglebutton.Content = switch
            my_togglebutton.IsChecked = state if state else False
            if configs and switch in configs:
                self._set_config(my_togglebutton, configs[switch])
            self.button_list.Children.Add(my_togglebutton)

        for option in self._context:
            my_button = framework.Controls.Button()
            my_button.Content = option
            my_button.Click += self.process_option
            if configs and option in configs:
                self._set_config(my_button, configs[option])
            self.button_list.Children.Add(my_button)

        self._setup_response()
        self.search_tb.Focus()
        self._filter_options()

    @staticmethod
    def _set_config(item, config_dict):
        bg = config_dict.get('background', None)
        if bg:
            bg = bg.replace('0x', '#')
            item.Background = Media.BrushConverter().ConvertFrom(bg)

    def _setup_response(self, response=None):
        if self._switches:
            switches = [x for x in self.button_list.Children
                        if hasattr(x, 'IsChecked')]
            self.response = response, {x.Content: x.IsChecked
                                       for x in switches}
        else:
            self.response = response

    def _filter_options(self, option_filter=None):
        if option_filter:
            self.search_tb.Tag = ''
            option_filter = option_filter.lower()
            for button in self.button_list.Children:
                if option_filter not in button.Content.lower():
                    button.Visibility = WPF_COLLAPSED
                else:
                    button.Visibility = WPF_VISIBLE
        else:
            self.search_tb.Tag = \
                'Type to Filter / Tab to Select / Enter or Click to Run'
            for button in self.button_list.Children:
                button.Visibility = WPF_VISIBLE

    def _get_active_button(self):
        buttons = []
        for button in self.button_list.Children:
            if button.Visibility == WPF_VISIBLE:
                buttons.append(button)
        if len(buttons) == 1:
            return buttons[0]
        else:
            for x in buttons:
                if x.IsFocused:
                    return x

    def handle_click(self, sender, args):    #pylint: disable=W0613
        """Handle mouse click."""
        self.Close()

    def handle_input_key(self, sender, args):
        """Handle keyboard inputs."""
        if args.Key == Input.Key.Escape:
            if self.search_tb.Text:
                self.search_tb.Text = ''
            else:
                self.Close()
        elif args.Key == Input.Key.Enter:
            active_button = self._get_active_button()
            if active_button:
                self.process_option(active_button, None)
                args.Handled = True
        elif args.Key != Input.Key.Tab \
                and args.Key != Input.Key.Space\
                and args.Key != Input.Key.LeftShift\
                and args.Key != Input.Key.RightShift:
            self.search_tb.Focus()

    def search_txt_changed(self, sender, args):    #pylint: disable=W0613
        """Handle text change in search box."""
        self._filter_options(option_filter=self.search_tb.Text)

    def process_option(self, sender, args):    #pylint: disable=W0613
        """Handle click on command option button."""
        self.Close()
        if sender:
            self._setup_response(response=sender.Content)


class GetValueWindow(TemplateUserInputWindow):
    """Standard form to get simple values from user.

    Examples:
        ```python
        from pyrevit import forms
        items = ['item1', 'item2', 'item3']
        forms.SelectFromList.show(items, button_name='Select Item')
        ['item1']
        ```
    """

    xaml_source = 'GetValueWindow.xaml'

    def _setup(self, **kwargs):
        self.Width = 400
        # determine value type
        self.value_type = kwargs.get('value_type', 'string')
        value_prompt = kwargs.get('prompt', None)
        value_default = kwargs.get('default', None)
        self.reserved_values = kwargs.get('reserved_values', [])

        # customize window based on type
        if self.value_type == 'string':
            self.show_element(self.stringPanel_dp)
            self.stringValue_tb.Text = value_default if value_default else ''
            self.stringValue_tb.Focus()
            self.stringValue_tb.SelectAll()
            self.stringPrompt.Text = \
                value_prompt if value_prompt else 'Enter string:'
            if self.reserved_values:
                self.string_value_changed(None, None)
        elif self.value_type == 'dropdown':
            self.show_element(self.dropdownPanel_db)
            self.dropdownPrompt.Text = \
                value_prompt if value_prompt else 'Pick one value:'
            self.dropdown_cb.ItemsSource = self._context
            if value_default:
                self.dropdown_cb.SelectedItem = value_default
        elif self.value_type == 'date':
            self.show_element(self.datePanel_dp)
            self.datePrompt.Text = \
                value_prompt if value_prompt else 'Pick date:'
        elif self.value_type == 'slider':
            self.show_element(self.sliderPanel_sp)
            self.sliderPrompt.Text = value_prompt
            self.numberPicker.Minimum = kwargs.get('min', 0)
            self.numberPicker.Maximum = kwargs.get('max', 100)
            self.numberPicker.TickFrequency = kwargs.get('interval', 1)
            self.numberPicker.Value = \
                value_default if isinstance(value_default, (float, int)) \
                    else self.numberPicker.Minimum

    def string_value_changed(self, sender, args): #pylint: disable=unused-argument
        """Handle string vlaue update event."""
        filtered_rvalues = \
            sorted([x for x in self.reserved_values
                    if self.stringValue_tb.Text == str(x)])
        similar_rvalues = \
            sorted([x for x in self.reserved_values
                    if self.stringValue_tb.Text in str(x)],
                   reverse=True)
        filtered_rvalues.extend(similar_rvalues)
        if filtered_rvalues:
            self.reservedValuesList.ItemsSource = filtered_rvalues
            self.show_element(self.reservedValuesListPanel)
            self.okayButton.IsEnabled = \
                self.stringValue_tb.Text not in filtered_rvalues
        else:
            self.reservedValuesList.ItemsSource = []
            self.hide_element(self.reservedValuesListPanel)
            self.okayButton.IsEnabled = True

    def select(self, sender, args):    #pylint: disable=W0613
        """Process input data and set the response."""
        self.Close()
        if self.value_type == 'string':
            self.response = self.stringValue_tb.Text
        elif self.value_type == 'dropdown':
            self.response = self.dropdown_cb.SelectedItem
        elif self.value_type == 'date':
            if self.datePicker.SelectedDate:
                datestr = self.datePicker.SelectedDate.ToString("MM/dd/yyyy")
                self.response = datetime.datetime.strptime(datestr, r'%m/%d/%Y')
            else:
                self.response = None
        elif self.value_type == 'slider':
            self.response = self.numberPicker.Value


class TemplatePromptBar(WPFWindow):
    """Template context-manager class for creating prompt bars.

    Prompt bars are show at the top of the active Revit window and are
    designed for better prompt visibility.

    Args:
        height (int): window height
        **kwargs (Any): other arguments to be passed to :func:`_setup`
    """

    xaml_source = 'TemplatePromptBar.xaml'

    def __init__(self, height=32, **kwargs):
        """Initialize user prompt window."""
        WPFWindow.__init__(self,
                           op.join(XAML_FILES_DIR, self.xaml_source))

        self.user_height = height
        self.update_window()
        self._setup(**kwargs)

    def update_window(self):
        """Update the prompt bar to match Revit window."""
        screen_area = HOST_APP.proc_screen_workarea
        scale_factor = 1.0 / HOST_APP.proc_screen_scalefactor
        top = left = width = height = 0

        window_rect = revit.ui.get_window_rectangle()

        # set width and height
        width = window_rect.Right - window_rect.Left
        height = self.user_height

        top = window_rect.Top
        # in maximized window, the top might be off the active screen
        # due to windows thicker window frames
        # lets cut the height and re-adjust the top
        top_diff = abs(screen_area.Top - top)
        if 10 > top_diff > 0 and top_diff < height:
            height -= top_diff
            top = screen_area.Top

        left = window_rect.Left
        # in maximized window, Left also might be off the active screen
        # due to windows thicker window frames
        # let's fix the width to accomodate the extra pixels as well
        left_diff = abs(screen_area.Left - left)
        if 10 > left_diff > 0 and left_diff < width:
            # deduct two times the left negative offset since this extra
            # offset happens on both left and right side
            width -= left_diff * 2
            left = screen_area.Left

        self.Top = top * scale_factor
        self.Left = left * scale_factor
        self.Width = width * scale_factor
        self.Height = height

    def _setup(self, **kwargs):
        """Private method to be overriden by subclasses for prompt setup."""
        pass

    def _prepare(self):
        pass

    def _cleanup(self):
        pass

    def __enter__(self):
        self._prepare()
        self.Show()
        return self

    def __exit__(self, exception, exception_value, traceback):
        self._cleanup()
        self.Close()


class WarningBar(TemplatePromptBar):
    """Show warning bar at the top of Revit window.

    Keyword Args:
        title (string): warning bar text

    Examples:
        ```python
        with WarningBar(title='my warning'):
           # do stuff
        ```
    """

    xaml_source = 'WarningBar.xaml'

    def _setup(self, **kwargs):
        self.message_tb.Text = kwargs.get('title', '')


class ProgressBar(TemplatePromptBar):
    """Show progress bar at the top of Revit window.

    Keyword Args:
        title (string): progress bar text, defaults to 0/100 progress format 
        indeterminate (bool): create indeterminate progress bar
        cancellable (bool): add cancel button to progress bar
        step (int): update progress intervals

    Examples:
        ```python
        from pyrevit import forms
        count = 1
        with forms.ProgressBar(title='my command progress message') as pb:
           # do stuff
           pb.update_progress(count, 100)
           count += 1
        ```

        Progress bar title could also be customized to show the current and
        total progress values. In example below, the progress bar message
        will be in format "0 of 100"

        ```python
        with forms.ProgressBar(title='{value} of {max_value}') as pb:
        ```

        By default progress bar updates the progress every time the
        .update_progress method is called. For operations with a large number
        of max steps, the gui update process time will have a significate
        effect on the overall execution time of the command. In these cases,
        set the value of step argument to something larger than 1. In example
        below, the progress bar updates once per every 10 units of progress.

        ```python
        with forms.ProgressBar(title='message', steps=10):
        ```

        Progress bar could also be set to indeterminate for operations of
        unknown length. In this case, the progress bar will show an infinitely
        running ribbon:

        ```python
        with forms.ProgressBar(title='message', indeterminate=True):
        ```

        if cancellable is set on the object, a cancel button will show on the
        progress bar and .cancelled attribute will be set on the ProgressBar
        instance if users clicks on cancel button:

        ```python
        with forms.ProgressBar(title='message',
                               cancellable=True) as pb:
           # do stuff
           if pb.cancelled:
               # wrap up and cancel operation
        ```
    """

    xaml_source = 'ProgressBar.xaml'

    def _setup(self, **kwargs):
        self.max_value = 1
        self.new_value = 0
        self.step = kwargs.get('step', 0)

        self.cancelled = False
        has_cancel = kwargs.get('cancellable', False)
        if has_cancel:
            self.show_element(self.cancel_b)

        self.pbar.IsIndeterminate = kwargs.get('indeterminate', False)
        self._title = kwargs.get('title', '{value}/{max_value}')
        self._hostwnd = None
        self._host_task_pbar = None

    def _prepare(self):
        self._hostwnd = revit.ui.get_mainwindow()
        if self._hostwnd:
            self._host_task_pbar = System.Windows.Shell.TaskbarItemInfo()
            self._hostwnd.TaskbarItemInfo = self._host_task_pbar

    def _cleanup(self):
        if self._hostwnd:
            self._hostwnd.TaskbarItemInfo = None

    def _update_task_pbar(self):
        if self._host_task_pbar is not None:
            if self.indeterminate:
                self._host_task_pbar.ProgressState = \
                    System.Windows.Shell.TaskbarItemProgressState.Indeterminate
            else:
                self._host_task_pbar.ProgressState = \
                    System.Windows.Shell.TaskbarItemProgressState.Normal
                self._host_task_pbar.ProgressValue = \
                    (self.new_value / float(self.max_value))

    def _update_pbar(self):
        self.update_window()
        self.pbar.Maximum = self.max_value
        self.pbar.Value = self.new_value

        # updating title
        title_text = \
            string.Formatter().vformat(self._title,
                                       (),
                                       coreutils.SafeDict(
                                           {'value': self.new_value,
                                            'max_value': self.max_value}
                                           ))

        self.pbar_text.Text = title_text

    def _donothing(self):
        pass

    def _dispatch_updater(self):
        # ask WPF dispatcher for gui update
        self.pbar.Dispatcher.Invoke(System.Action(self._update_pbar),
                                    Threading.DispatcherPriority.Background)
        # ask WPF dispatcher for gui update
        self.pbar.Dispatcher.Invoke(System.Action(self._update_task_pbar),
                                    Threading.DispatcherPriority.Background)
        # give it a little free time to update ui
        self.pbar.Dispatcher.Invoke(System.Action(self._donothing),
                                    Threading.DispatcherPriority.Background)

    @property
    def title(self):
        """Progress bar title."""
        return self._title

    @title.setter
    def title(self, value):
        if isinstance(value, str):
            self._title = value    #pylint: disable=W0201

    @property
    def indeterminate(self):
        """Progress bar indeterminate state."""
        return self.pbar.IsIndeterminate

    @indeterminate.setter
    def indeterminate(self, value):
        self.pbar.IsIndeterminate = value

    def clicked_cancel(self, sender, args):    #pylint: disable=W0613
        """Handler for cancel button clicked event."""
        self.cancel_b.Content = 'Cancelling...'
        self.cancelled = True    #pylint: disable=W0201

    def reset(self):
        """Reset progress value to 0."""
        self.update_progress(0, 1)

    def update_progress(self, new_value, max_value=1):
        """Update progress bar state with given min, max values.

        Args:
            new_value (float): current progress value
            max_value (float): total progress value
        """
        self.max_value = max_value    #pylint: disable=W0201
        self.new_value = new_value    #pylint: disable=W0201
        if self.new_value == 0:
            self._dispatch_updater()
        elif self.step > 0:
            if self.new_value % self.step == 0:
                self._dispatch_updater()
        else:
            self._dispatch_updater()


class SearchPrompt(WPFWindow):
    """Standard prompt for pyRevit search.

    Args:
        search_db (list): list of possible search targets
        width (int): width of search prompt window
        height (int): height of search prompt window

    Keyword Args:
        search_tip (str): text to show in grayscale when search box is empty
        switches (str): list of switches

    Returns:
        (tuple[str, dict] | str): matched string if switches are not provided,
            matched strings, and dict of switches otherwise.

    Examples:
        ```python
        from pyrevit import forms
        # assume search input of '/switch1 target1'
        matched_str, args, switches = forms.SearchPrompt.show(
            search_db=['target1', 'target2', 'target3', 'target4'],
            switches=['/switch1', '/switch2'],
            search_tip='pyRevit Search'
            )
        matched_str
        'target1'
        args
        ['--help', '--branch', 'branchname']
        switches
        {'/switch1': True, '/switch2': False}
        ```
    """
    def __init__(self, search_db, width, height, **kwargs):
        """Initialize search prompt window."""
        WPFWindow.__init__(self,
                           op.join(XAML_FILES_DIR, 'SearchPrompt.xaml'))
        self.Width = width
        self.MinWidth = self.Width
        self.Height = height

        self.search_tip = kwargs.get('search_tip', '')

        if isinstance(search_db, list):
            self._search_db = None
            self._search_db_keys = search_db
        elif isinstance(search_db, dict):
            self._search_db = search_db
            self._search_db_keys = sorted(self._search_db.keys())
        else:
            raise PyRevitException("Unknown search database type")

        self._search_res = None
        self._switches = kwargs.get('switches', [])
        self._setup_response()

        self.search_tb.Focus()
        self.hide_element(self.tab_icon)
        self.hide_element(self.return_icon)
        self.search_tb.Text = ''
        self.set_search_results()

    def _setup_response(self, response=None):
        switch_dict = dict.fromkeys(self._switches)
        for switch in self.search_term_switches:
            switch_dict[switch] = True
        arguments = self.search_term_args
        # remove first arg which is command name
        if len(arguments) >= 1:
            arguments = arguments[1:]

        self.response = response, arguments, switch_dict

    @property
    def search_input(self):
        """Current search input."""
        return self.search_tb.Text

    @search_input.setter
    def search_input(self, value):
        self.search_tb.Text = value
        self.search_tb.CaretIndex = len(value)

    @property
    def search_input_parts(self):
        """Current cleaned up search term."""
        return self.search_input.strip().split()

    @property
    def search_term(self):
        """Current cleaned up search term."""
        return self.search_input.lower().strip()

    @property
    def search_term_switches(self):
        """Find matching switches in search term."""
        switches = set()
        for stpart in self.search_input_parts:
            if stpart.lower() in self._switches:
                switches.add(stpart)
        return switches

    @property
    def search_term_args(self):
        """Find arguments in search term."""
        args = []
        switches = self.search_term_switches
        for spart in self.search_input_parts:
            if spart.lower() not in switches:
                args.append(spart)
        return args

    @property
    def search_term_main(self):
        """Current cleaned up search term without the listed switches."""
        if len(self.search_term_args) >= 1:
            return self.search_term_args[0]
        else:
            return ''

    @property
    def search_matches(self):
        """List of matches for the given search term."""
        # remove duplicates while keeping order
        # results = list(set(self._search_results))
        return OrderedDict.fromkeys(self._search_results).keys()

    def update_results_display(self, fill_match=False):
        """Update search prompt results based on current input text."""
        self.directmatch_tb.Text = ''
        self.wordsmatch_tb.Text = ''

        results = self.search_matches
        res_cout = len(results)

        mlogger.debug('unique results count: %s', res_cout)
        mlogger.debug('unique results: %s', results)

        if res_cout > 1:
            self.show_element(self.tab_icon)
            self.hide_element(self.return_icon)
        elif res_cout == 1:
            self.hide_element(self.tab_icon)
            self.show_element(self.return_icon)
        else:
            self.hide_element(self.tab_icon)
            self.hide_element(self.return_icon)

        if self._result_index >= res_cout:
            self._result_index = 0   #pylint: disable=W0201

        if self._result_index < 0:
            self._result_index = res_cout - 1   #pylint: disable=W0201

        if not self.search_input:
            self.directmatch_tb.Text = self.search_tip
            return

        if results:
            input_term = self.search_term
            cur_res = results[self._result_index]
            mlogger.debug('current result: %s', cur_res)
            if fill_match:
                self.search_input = cur_res
            else:
                if cur_res.lower().startswith(input_term):
                    self.directmatch_tb.Text = \
                        self.search_input + cur_res[len(input_term):]
                    mlogger.debug('directmatch_tb.Text: %s',
                                  self.directmatch_tb.Text)
                else:
                    self.wordsmatch_tb.Text = '- {}'.format(cur_res)
                    mlogger.debug('wordsmatch_tb.Text: %s',
                                  self.wordsmatch_tb.Text)
            tooltip = self._search_db.get(cur_res, None)
            if tooltip:
                self.tooltip_tb.Text = tooltip
                self.show_element(self.tooltip_tb)
            else:
                self.hide_element(self.tooltip_tb)
            self._search_res = cur_res
            return True
        return False

    def set_search_results(self, *args):
        """Set search results for returning."""
        self._result_index = 0
        self._search_results = []

        mlogger.debug('search input: %s', self.search_input)
        mlogger.debug('search term: %s', self.search_term)
        mlogger.debug('search term (main): %s', self.search_term_main)
        mlogger.debug('search term (parts): %s', self.search_input_parts)
        mlogger.debug('search term (args): %s', self.search_term_args)
        mlogger.debug('search term (switches): %s', self.search_term_switches)

        for resultset in args:
            mlogger.debug('result set: %s}', resultset)
            self._search_results.extend(sorted(resultset))

        mlogger.debug('results: %s', self._search_results)

    def find_direct_match(self, input_text):
        """Find direct text matches in search term."""
        results = []
        if input_text:
            for cmd_name in self._search_db_keys:
                if cmd_name.lower().startswith(input_text):
                    results.append(cmd_name)

        return results

    def find_word_match(self, input_text):
        """Find direct word matches in search term."""
        results = []
        if input_text:
            cur_words = input_text.split(' ')
            for cmd_name in self._search_db_keys:
                if all([x in cmd_name.lower() for x in cur_words]):
                    results.append(cmd_name)

        return results

    def search_txt_changed(self, sender, args):    #pylint: disable=W0613
        """Handle text changed event."""
        input_term = self.search_term_main
        dmresults = self.find_direct_match(input_term)
        wordresults = self.find_word_match(input_term)
        self.set_search_results(dmresults, wordresults)
        self.update_results_display()

    def handle_kb_key(self, sender, args):    #pylint: disable=W0613
        """Handle keyboard input event."""
        shiftdown = Input.Keyboard.IsKeyDown(Input.Key.LeftShift) \
            or Input.Keyboard.IsKeyDown(Input.Key.RightShift)
        # Escape: set response to none and close
        if args.Key == Input.Key.Escape:
            self._setup_response()
            self.Close()
        # Enter: close, returns matched response automatically
        elif args.Key == Input.Key.Enter:
            if self.search_tb.Text != '':
                self._setup_response(response=self._search_res)
                args.Handled = True
                self.Close()
        # Shift+Tab, Tab: Cycle through matches
        elif args.Key == Input.Key.Tab and shiftdown:
            self._result_index -= 1
            self.update_results_display()
        elif args.Key == Input.Key.Tab:
            self._result_index += 1
            self.update_results_display()
        # Up, Down: Cycle through matches
        elif args.Key == Input.Key.Up:
            self._result_index -= 1
            self.update_results_display()
        elif args.Key == Input.Key.Down:
            self._result_index += 1
            self.update_results_display()
        # Right, End: Autocomplete with displayed match
        elif args.Key in [Input.Key.Right,
                          Input.Key.End]:
            self.update_results_display(fill_match=True)

    @classmethod
    def show(cls, search_db,    #pylint: disable=W0221
             width=DEFAULT_SEARCHWND_WIDTH,
             height=DEFAULT_SEARCHWND_HEIGHT, **kwargs):
        """Show search prompt."""
        dlg = cls(search_db, width, height, **kwargs)
        dlg.ShowDialog()
        return dlg.response


class RevisionOption(TemplateListItem):
    """Revision wrapper for :func:`select_revisions`."""
    def __init__(self, revision_element):
        super(RevisionOption, self).__init__(revision_element)

    @property
    def name(self):
        """Revision name (description)."""
        revnum = self.item.SequenceNumber
        rev_settings = \
            DB.RevisionSettings.GetRevisionSettings(self.item.Document)

        if rev_settings.RevisionNumbering == DB.RevisionNumbering.PerProject:
            revnum = self.item.RevisionNumber
        return '{}-{}-{}'.format(revnum,
                                 self.item.Description,
                                 self.item.RevisionDate)


class SheetOption(TemplateListItem):
    """Sheet wrapper for :func:`select_sheets`."""
    def __init__(self, sheet_element):
        super(SheetOption, self).__init__(sheet_element)

    @property
    def name(self):
        """Sheet name."""
        return '{} - {}{}' \
            .format(self.item.SheetNumber,
                    self.item.Name,
                    ' (placeholder)' if self.item.IsPlaceholder else '')

    @property
    def number(self):
        """Sheet number."""
        return self.item.SheetNumber


class ViewOption(TemplateListItem):
    """View wrapper for :func:`select_views`."""
    def __init__(self, view_element):
        super(ViewOption, self).__init__(view_element)

    @property
    def name(self):
        """View name."""
        return '{} ({})'.format(revit.query.get_name(self.item),
                                self.item.ViewType)


class LevelOption(TemplateListItem):
    """Level wrapper for :func:`select_levels`."""
    def __init__(self, level_element):
        super(LevelOption, self).__init__(level_element)

    @property
    def name(self):
        """Level name."""
        return revit.query.get_name(self.item)


class FamilyParamOption(TemplateListItem):
    """Level wrapper for :func:`select_family_parameters`."""
    def __init__(self, fparam, builtin=False, labeled=False):
        super(FamilyParamOption, self).__init__(fparam)
        self.isbuiltin = builtin
        self.islabeled = labeled

    @property
    def name(self):
        """Family Parameter name."""
        return self.item.Definition.Name

    @property
    def istype(self):
        """Is type parameter."""
        return not self.item.IsInstance


def select_revisions(title='Select Revision',
                     button_name='Select',
                     width=DEFAULT_INPUTWINDOW_WIDTH,
                     multiple=True,
                     filterfunc=None,
                     doc=None):
    """Standard form for selecting revisions.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for revisions; defaults to active document

    Returns:
        (list[DB.Revision]): list of selected revisions

    Examples:
        ```python
        from pyrevit import forms
        forms.select_revisions()
        [<Autodesk.Revit.DB.Revision object>,
         <Autodesk.Revit.DB.Revision object>]
        ```
    """
    doc = doc or DOCS.doc
    revisions = sorted(revit.query.get_revisions(doc=doc),
                       key=lambda x: x.SequenceNumber)

    if filterfunc:
        revisions = filter(filterfunc, revisions)

    # ask user for revisions
    selected_revs = SelectFromList.show(
        [RevisionOption(x) for x in revisions],
        title=title,
        button_name=button_name,
        width=width,
        multiselect=multiple,
        checked_only=True
        )

    return selected_revs


def select_sheets(title='Select Sheets',
                  button_name='Select',
                  width=DEFAULT_INPUTWINDOW_WIDTH,
                  multiple=True,
                  filterfunc=None,
                  doc=None,
                  include_placeholder=True,
                  use_selection=False):
    """Standard form for selecting sheets.

    Sheets are grouped into sheet sets and sheet set can be selected from
    a drop down box at the top of window.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for sheets; defaults to active document
        include_placeholder (bool, optional): include a placeholder.
            Defaults to True
        use_selection (bool, optional):
            ask if user wants to use currently selected sheets.

    Returns:
        (list[DB.ViewSheet]): list of selected sheets

    Examples:
        ```python
        from pyrevit import forms
        forms.select_sheets()
        [<Autodesk.Revit.DB.ViewSheet object>,
         <Autodesk.Revit.DB.ViewSheet object>]
        ```
    """
    doc = doc or DOCS.doc

    # check for previously selected sheets
    if use_selection:
        current_selected_sheets = revit.get_selection() \
                                       .include(DB.ViewSheet) \
                                       .elements
        if filterfunc:
            current_selected_sheets = \
                filter(filterfunc, current_selected_sheets)

        if not include_placeholder:
            current_selected_sheets = \
                [x for x in current_selected_sheets if not x.IsPlaceholder]

        if current_selected_sheets \
                and ask_to_use_selected("sheets",
                                        count=len(current_selected_sheets),
                                        multiple=multiple):
            return current_selected_sheets \
                if multiple else current_selected_sheets[0]

    # otherwise get all sheets and prompt for selection
    all_ops = {}
    all_sheets = DB.FilteredElementCollector(doc) \
                   .OfClass(DB.ViewSheet) \
                   .WhereElementIsNotElementType() \
                   .ToElements()

    if filterfunc:
        all_sheets = filter(filterfunc, all_sheets)

    if not include_placeholder:
        all_sheets = [x for x in all_sheets if not x.IsPlaceholder]

    all_sheets_ops = sorted([SheetOption(x) for x in all_sheets],
                            key=lambda x: x.number)
    all_ops['All Sheets'] = all_sheets_ops

    sheetsets = revit.query.get_sheet_sets(doc)
    for sheetset in sheetsets:
        sheetset_sheets = \
            [x for x in sheetset.Views if isinstance(x, DB.ViewSheet)]
        if filterfunc:
            sheetset_sheets = filter(filterfunc, sheetset_sheets)
        sheetset_ops = sorted([SheetOption(x) for x in sheetset_sheets],
                              key=lambda x: x.number)
        all_ops[sheetset.Name] = sheetset_ops

    # ask user for multiple sheets
    selected_sheets = SelectFromList.show(
        all_ops,
        title=title,
        group_selector_title='Sheet Sets:',
        button_name=button_name,
        width=width,
        multiselect=multiple,
        checked_only=True,
        default_group='All Sheets'
        )

    return selected_sheets


def select_views(title='Select Views',
                 button_name='Select',
                 width=DEFAULT_INPUTWINDOW_WIDTH,
                 multiple=True,
                 filterfunc=None,
                 doc=None,
                 use_selection=False):
    """Standard form for selecting views.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for views; defaults to active document
        use_selection (bool, optional):
            ask if user wants to use currently selected views.

    Returns:
        (list[DB.View]): list of selected views

    Examples:
        ```python
        from pyrevit import forms
        forms.select_views()
        [<Autodesk.Revit.DB.View object>,
         <Autodesk.Revit.DB.View object>]
        ```
    """
    doc = doc or DOCS.doc

    # check for previously selected sheets
    if use_selection:
        current_selected_views = revit.get_selection() \
                                      .include(DB.View) \
                                      .elements
        if filterfunc:
            current_selected_views = \
                filter(filterfunc, current_selected_views)

        if current_selected_views \
                and ask_to_use_selected("views",
                                        count=len(current_selected_views),
                                        multiple=multiple):
            return current_selected_views \
                if multiple else current_selected_views[0]

    # otherwise get all sheets and prompt for selection
    all_graphviews = revit.query.get_all_views(doc=doc)

    if filterfunc:
        all_graphviews = filter(filterfunc, all_graphviews)

    selected_views = SelectFromList.show(
        sorted([ViewOption(x) for x in all_graphviews],
               key=lambda x: x.name),
        title=title,
        button_name=button_name,
        width=width,
        multiselect=multiple,
        checked_only=True
        )

    return selected_views


def select_levels(title='Select Levels',
                  button_name='Select',
                  width=DEFAULT_INPUTWINDOW_WIDTH,
                  multiple=True,
                  filterfunc=None,
                  doc=None,
                  use_selection=False):
    """Standard form for selecting levels.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for levels; defaults to active document
        use_selection (bool, optional):
            ask if user wants to use currently selected levels.

    Returns:
        (list[DB.Level]): list of selected levels

    Examples:
        ```python
        from pyrevit import forms
        forms.select_levels()
        [<Autodesk.Revit.DB.Level object>,
         <Autodesk.Revit.DB.Level object>]
        ```
    """
    doc = doc or DOCS.doc

    # check for previously selected sheets
    if use_selection:
        current_selected_levels = revit.get_selection() \
                                       .include(DB.Level) \
                                       .elements

        if filterfunc:
            current_selected_levels = \
                filter(filterfunc, current_selected_levels)

        if current_selected_levels \
                and ask_to_use_selected("levels",
                                        count=len(current_selected_levels),
                                        multiple=multiple):
            return current_selected_levels \
                if multiple else current_selected_levels[0]

    all_levels = \
        revit.query.get_elements_by_categories(
            [DB.BuiltInCategory.OST_Levels],
            doc=doc
            )

    if filterfunc:
        all_levels = filter(filterfunc, all_levels)

    selected_levels = SelectFromList.show(
        sorted([LevelOption(x) for x in all_levels],
               key=lambda x: x.Elevation),
        title=title,
        button_name=button_name,
        width=width,
        multiselect=multiple,
        checked_only=True,
        )
    return selected_levels


def select_viewtemplates(title='Select View Templates',
                         button_name='Select',
                         width=DEFAULT_INPUTWINDOW_WIDTH,
                         multiple=True,
                         filterfunc=None,
                         doc=None):
    """Standard form for selecting view templates.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for views; defaults to active document

    Returns:
        (list[DB.View]): list of selected view templates

    Examples:
        ```python
        from pyrevit import forms
        forms.select_viewtemplates()
        [<Autodesk.Revit.DB.View object>,
         <Autodesk.Revit.DB.View object>]
        ```
    """
    doc = doc or DOCS.doc
    all_viewtemplates = revit.query.get_all_view_templates(doc=doc)

    if filterfunc:
        all_viewtemplates = filter(filterfunc, all_viewtemplates)

    selected_viewtemplates = SelectFromList.show(
        sorted([ViewOption(x) for x in all_viewtemplates],
               key=lambda x: x.name),
        title=title,
        button_name=button_name,
        width=width,
        multiselect=multiple,
        checked_only=True
        )

    return selected_viewtemplates


def select_schedules(title='Select Schedules',
                     button_name='Select',
                     width=DEFAULT_INPUTWINDOW_WIDTH,
                     multiple=True,
                     filterfunc=None,
                     doc=None):
    """Standard form for selecting schedules.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for views; defaults to active document

    Returns:
        (list[DB.ViewSchedule]): list of selected schedules

    Examples:
        ```python
        from pyrevit import forms
        forms.select_schedules()
        [<Autodesk.Revit.DB.ViewSchedule object>,
         <Autodesk.Revit.DB.ViewSchedule object>]
        ```
    """
    doc = doc or DOCS.doc
    all_schedules = revit.query.get_all_schedules(doc=doc)

    if filterfunc:
        all_schedules = filter(filterfunc, all_schedules)

    selected_schedules = \
        SelectFromList.show(
            sorted([ViewOption(x) for x in all_schedules],
                   key=lambda x: x.name),
            title=title,
            button_name=button_name,
            width=width,
            multiselect=multiple,
            checked_only=True
        )

    return selected_schedules


def select_open_docs(title='Select Open Documents',
                     button_name='OK',
                     width=DEFAULT_INPUTWINDOW_WIDTH,    #pylint: disable=W0613
                     multiple=True,
                     check_more_than_one=True,
                     filterfunc=None):
    """Standard form for selecting open documents.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        check_more_than_one (bool, optional): 
        filterfunc (function):
            filter function to be applied to context items.

    Returns:
        (list[DB.Document]): list of selected documents

    Examples:
        ```python
        from pyrevit import forms
        forms.select_open_docs()
        [<Autodesk.Revit.DB.Document object>,
         <Autodesk.Revit.DB.Document object>]
        ```
    """
    # find open documents other than the active doc
    open_docs = [d for d in revit.docs if not d.IsLinked]    #pylint: disable=E1101
    if check_more_than_one:
        open_docs.remove(revit.doc)    #pylint: disable=E1101

    if not open_docs:
        alert('Only one active document is found. '
              'At least two documents must be open. '
              'Operation cancelled.')
        return

    return SelectFromList.show(
        open_docs,
        name_attr='Title',
        multiselect=multiple,
        title=title,
        button_name=button_name,
        filterfunc=filterfunc
        )


def select_titleblocks(title='Select Titleblock',
                       button_name='Select',
                       no_tb_option='No Title Block',
                       width=DEFAULT_INPUTWINDOW_WIDTH,
                       multiple=False,
                       filterfunc=None,
                       doc=None):
    """Standard form for selecting a titleblock.

    Args:
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        no_tb_option (str, optional): name of option for no title block
        width (int, optional): width of list window
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to False
        filterfunc (function):
            filter function to be applied to context items.
        doc (DB.Document, optional):
            source document for titleblocks; defaults to active document

    Returns:
        (DB.ElementId): selected titleblock id.

    Examples:
        ```python
        from pyrevit import forms
        forms.select_titleblocks()
        <Autodesk.Revit.DB.ElementId object>
        ```
    """
    doc = doc or DOCS.doc
    titleblocks = DB.FilteredElementCollector(doc)\
                    .OfCategory(DB.BuiltInCategory.OST_TitleBlocks)\
                    .WhereElementIsElementType()\
                    .ToElements()

    tblock_dict = {'{}: {}'.format(tb.FamilyName,
                                   revit.query.get_name(tb)): tb.Id
                   for tb in titleblocks}
    tblock_dict[no_tb_option] = DB.ElementId.InvalidElementId
    selected_titleblocks = SelectFromList.show(sorted(tblock_dict.keys()),
                                               title=title,
                                               button_name=button_name,
                                               width=width,
                                               multiselect=multiple,
                                               filterfunc=filterfunc)
    if selected_titleblocks:
        if multiple:
            return [tblock_dict[x] for x in selected_titleblocks]
        else:
            return tblock_dict[selected_titleblocks]


def select_swatch(title='Select Color Swatch', button_name='Select'):
    """Standard form for selecting a color swatch.

    Args:
        title (str, optional): swatch list window title
        button_name (str, optional): swatch list window button caption

    Returns:
        (pyrevit.coreutils.colors.RGB): rgb color

    Examples:
        ```python
        from pyrevit import forms
        forms.select_swatch(title="Select Text Color")
        <RGB #CD8800>
        ```
    """
    itemplate = utils.load_ctrl_template(
        os.path.join(XAML_FILES_DIR, "SwatchContainerStyle.xaml")
        )
    swatch = SelectFromList.show(
        colors.COLORS.values(),
        title=title,
        button_name=button_name,
        width=300,
        multiselect=False,
        item_template=itemplate
        )

    return swatch


def select_image(images, title='Select Image', button_name='Select'):
    """Standard form for selecting an image.

    Args:
        images (list[str] | list[framework.Imaging.BitmapImage]):
            list of image file paths or bitmaps
        title (str, optional): swatch list window title
        button_name (str, optional): swatch list window button caption

    Returns:
        (str): path of the selected image

    Examples:
        ```python
        from pyrevit import forms
        forms.select_image(['C:/path/to/image1.png',
                            'C:/path/to/image2.png'],
                            title="Select Variation")
        'C:/path/to/image1.png'
        ```
    """
    ptemplate = utils.load_itemspanel_template(
        os.path.join(XAML_FILES_DIR, "ImageListPanelStyle.xaml")
        )

    itemplate = utils.load_ctrl_template(
        os.path.join(XAML_FILES_DIR, "ImageListContainerStyle.xaml")
        )

    bitmap_images = {}
    for imageobj in images:
        if isinstance(imageobj, str):
            img = utils.bitmap_from_file(imageobj)
            if img:
                bitmap_images[img] = imageobj
        elif isinstance(imageobj, framework.Imaging.BitmapImage):
            bitmap_images[imageobj] = imageobj

    selected_image = SelectFromList.show(
        sorted(bitmap_images.keys(), key=lambda x: x.UriSource.AbsolutePath),
        title=title,
        button_name=button_name,
        width=500,
        multiselect=False,
        item_template=itemplate,
        items_panel_template=ptemplate
        )

    return bitmap_images.get(selected_image, None)


def select_parameters(src_element,
                      title='Select Parameters',
                      button_name='Select',
                      multiple=True,
                      filterfunc=None,
                      include_instance=True,
                      include_type=True,
                      exclude_readonly=True):
    """Standard form for selecting parameters from given element.

    Args:
        src_element (DB.Element): source element
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        include_instance (bool, optional): list instance parameters
        include_type (bool, optional): list type parameters
        exclude_readonly (bool, optional): only shows parameters that are editable

    Returns:
        (list[ParamDef]): list of paramdef objects

    Examples:
        ```python
        forms.select_parameter(
            src_element,
            title='Select Parameters',
            multiple=True,
            include_instance=True,
            include_type=True
        )
        [<ParamDef >, <ParamDef >]
        ```
    """
    param_defs = []
    non_storage_type = coreutils.get_enum_none(DB.StorageType)
    if include_instance:
        # collect instance parameters
        param_defs.extend(
            [ParamDef(name=x.Definition.Name,
                      istype=False,
                      definition=x.Definition,
                      isreadonly=x.IsReadOnly)
             for x in src_element.Parameters
             if x.StorageType != non_storage_type]
        )

    if include_type:
        # collect type parameters
        src_type = revit.query.get_type(src_element)
        param_defs.extend(
            [ParamDef(name=x.Definition.Name,
                      istype=True,
                      definition=x.Definition,
                      isreadonly=x.IsReadOnly)
             for x in src_type.Parameters
             if x.StorageType != non_storage_type]
        )

    if exclude_readonly:
        param_defs = filter(lambda x: not x.isreadonly, param_defs)

    if filterfunc:
        param_defs = filter(filterfunc, param_defs)

    param_defs.sort(key=lambda x: x.name)

    itemplate = utils.load_ctrl_template(
        os.path.join(XAML_FILES_DIR, "ParameterItemStyle.xaml")
        )
    selected_params = SelectFromList.show(
        param_defs,
        title=title,
        button_name=button_name,
        width=450,
        multiselect=multiple,
        item_template=itemplate
        )

    return selected_params


def select_family_parameters(family_doc,
                             title='Select Parameters',
                             button_name='Select',
                             multiple=True,
                             filterfunc=None,
                             include_instance=True,
                             include_type=True,
                             include_builtin=True,
                             include_labeled=True):
    """Standard form for selecting parameters from given family document.

    Args:
        family_doc (DB.Document): source family document
        title (str, optional): list window title
        button_name (str, optional): list window button caption
        multiple (bool, optional):
            allow multi-selection (uses check boxes). defaults to True
        filterfunc (function):
            filter function to be applied to context items.
        include_instance (bool, optional): list instance parameters
        include_type (bool, optional): list type parameters
        include_builtin (bool, optional): list builtin parameters
        include_labeled (bool, optional): list parameters used as labels

    Returns:
        (list[DB.FamilyParameter]): list of family parameter objects

    Examples:
        ```python
        forms.select_family_parameters(
            family_doc,
            title='Select Parameters',
            multiple=True,
            include_instance=True,
            include_type=True
        )
        [<DB.FamilyParameter >, <DB.FamilyParameter >]
        ```
    """
    family_doc = family_doc or DOCS.doc
    family_params = revit.query.get_family_parameters(family_doc)
    # get all params used in labeles
    label_param_ids = \
        [x.Id for x in revit.query.get_family_label_parameters(family_doc)]

    if filterfunc:
        family_params = filter(filterfunc, family_params)

    param_defs = []
    for family_param in family_params:
        if not include_instance and family_param.IsInstance:
            continue
        if not include_type and not family_param.IsInstance:
            continue
        if not include_builtin and family_param.Id.IntegerValue < 0:
            continue
        if not include_labeled and family_param.Id in label_param_ids:
            continue

        param_defs.append(
            FamilyParamOption(family_param,
                              builtin=family_param.Id.IntegerValue < 0,
                              labeled=family_param.Id in label_param_ids)
            )

    param_defs.sort(key=lambda x: x.name)

    itemplate = utils.load_ctrl_template(
        os.path.join(XAML_FILES_DIR, "FamilyParameterItemStyle.xaml")
        )
    selected_params = SelectFromList.show(
        {
            'All Parameters': param_defs,
            'Type Parameters': [x for x in param_defs if x.istype],
            'Built-in Parameters': [x for x in param_defs if x.isbuiltin],
            'Used as Label': [x for x in param_defs if x.islabeled],
        },
        title=title,
        button_name=button_name,
        group_selector_title='Parameter Filters:',
        width=450,
        multiselect=multiple,
        item_template=itemplate
        )

    return selected_params


def alert(msg, title=None, sub_msg=None, expanded=None, footer='',
          ok=True, cancel=False, yes=False, no=False, retry=False,
          warn_icon=True, options=None, exitscript=False):
    r"""Show a task dialog with given message.

    Args:
        msg (str): message to be displayed
        title (str, optional): task dialog title
        sub_msg (str, optional): sub message, use html to create clickable links
        expanded (str, optional): expanded area message
        footer (str, optional): footer text
        ok (bool, optional): show OK button, defaults to True
        cancel (bool, optional): show Cancel button, defaults to False
        yes (bool, optional): show Yes button, defaults to False
        no (bool, optional): show NO button, defaults to False
        retry (bool, optional): show Retry button, defaults to False
        warn_icon (bool, optional): show warning icon
        options (list[str], optional): list of command link titles in order
        exitscript (bool, optional): exit if cancel or no, defaults to False

    Returns:
        (bool): True if okay, yes, or retry, otherwise False

    Examples:
        ```python
        from pyrevit import forms
        forms.alert('Are you sure?',
                    sub_msg='<a href=\"https://discourse.pyrevitlabs.io/ \">Click here if you are not sure and want to go to the pyRevit Forum</a>',
                    ok=False, yes=True, no=True, exitscript=True)
        ```
    """
    # BUILD DIALOG
    cmd_name = EXEC_PARAMS.command_name
    if not title:
        title = cmd_name if cmd_name else 'pyRevit'
    tdlg = UI.TaskDialog(title)

    # process input types
    just_ok = ok and not any([cancel, yes, no, retry])

    options = options or []
    # add command links if any
    if options:
        clinks = coreutils.get_enum_values(UI.TaskDialogCommandLinkId)
        max_clinks = len(clinks)
        for idx, cmd in enumerate(options):
            if idx < max_clinks:
                tdlg.AddCommandLink(clinks[idx], cmd)
    # otherwise add buttons
    else:
        buttons = coreutils.get_enum_none(UI.TaskDialogCommonButtons)
        if yes:
            buttons |= UI.TaskDialogCommonButtons.Yes
        elif ok:
            buttons |= UI.TaskDialogCommonButtons.Ok

        if cancel:
            buttons |= UI.TaskDialogCommonButtons.Cancel
        if no:
            buttons |= UI.TaskDialogCommonButtons.No
        if retry:
            buttons |= UI.TaskDialogCommonButtons.Retry
        tdlg.CommonButtons = buttons

    # set texts
    tdlg.MainInstruction = msg
    tdlg.MainContent = sub_msg
    tdlg.ExpandedContent = expanded
    if footer:
        footer = footer.strip() + '\n'
    tdlg.FooterText = footer + 'pyRevit {}'.format(
        versionmgr.get_pyrevit_version().get_formatted()
        )
    tdlg.TitleAutoPrefix = False

    # set icon
    tdlg.MainIcon = \
        UI.TaskDialogIcon.TaskDialogIconWarning \
        if warn_icon else UI.TaskDialogIcon.TaskDialogIconNone

    # tdlg.VerificationText = 'verif'

    # SHOW DIALOG
    res = tdlg.Show()

    # PROCESS REPONSES
    # positive response
    mlogger.debug('alert result: %s', res)
    if res == UI.TaskDialogResult.Ok \
            or res == UI.TaskDialogResult.Yes \
            or res == UI.TaskDialogResult.Retry:
        if just_ok and exitscript:
            sys.exit()
        return True
    # negative response
    elif res == coreutils.get_enum_none(UI.TaskDialogResult) \
            or res == UI.TaskDialogResult.Cancel \
            or res == UI.TaskDialogResult.No:
        if exitscript:
            sys.exit()
        else:
            return False

    # command link response
    elif 'CommandLink' in str(res):
        tdresults = sorted(
            [x for x in coreutils.get_enum_values(UI.TaskDialogResult)
             if 'CommandLink' in str(x)]
            )
        residx = tdresults.index(res)
        return options[residx]
    elif exitscript:
        sys.exit()
    else:
        return False


def alert_ifnot(condition, msg, *args, **kwargs):
    """Show a task dialog with given message if condition is NOT met.

    Args:
        condition (bool): condition to test
        msg (str): message to be displayed
        *args (Any): additional arguments
        **kwargs (Any): additional keyword arguments

    Keyword Args:
        title (str, optional): task dialog title
        ok (bool, optional): show OK button, defaults to True
        cancel (bool, optional): show Cancel button, defaults to False
        yes (bool, optional): show Yes button, defaults to False
        no (bool, optional): show NO button, defaults to False
        retry (bool, optional): show Retry button, defaults to False
        exitscript (bool, optional): exit if cancel or no, defaults to False

    Returns:
        (bool): True if okay, yes, or retry, otherwise False

    Examples:
        ```python
        from pyrevit import forms
        forms.alert_ifnot(value > 12,
                          'Are you sure?',
                           ok=False, yes=True, no=True, exitscript=True)
        ```
    """
    if not condition:
        return alert(msg, *args, **kwargs)


def pick_folder(title=None, owner=None):
    """Show standard windows pick folder dialog.

    Args:
        title (str, optional): title for the window
        owner (object, optional): owner of the dialog

    Returns:
        (str): folder path
    """
    if CPDialogs:
        fb_dlg = CPDialogs.CommonOpenFileDialog()
        fb_dlg.IsFolderPicker = True
        if title:
            fb_dlg.Title = title

        res = CPDialogs.CommonFileDialogResult.Cancel
        if owner:
            res = fb_dlg.ShowDialog(owner)
        else:
            res = fb_dlg.ShowDialog()

        if res == CPDialogs.CommonFileDialogResult.Ok:
            return fb_dlg.FileName
    else:
        fb_dlg = Forms.FolderBrowserDialog()
        if title:
            fb_dlg.Description = title
        if fb_dlg.ShowDialog() == Forms.DialogResult.OK:
            return fb_dlg.SelectedPath


def result_item_result_clicked(sender, e, debug=False):
    """Callback for a result item click event."""
    if debug:
        print("Result clicked")  # using print_md here will break the script
    pass


def show_balloon(header, text, tooltip='', group='', is_favourite=False, is_new=False, timestamp=None, click_result=result_item_result_clicked):
    r"""Show ballon in the info center section.

    Args:
        header (str): Category section (Bold)
        text (str): Title section (Regular)
        tooltip (str): Tooltip
        group (str): Group
        is_favourite (bool): Add a blue star before header
        is_new (bool): Flag to new
        timestamp (str): Set timestamp
        click_result (def): Executed after a click event

    Examples:
        ```python
        from pyrevit import forms
        date = '2019-01-01 00:00:00'
        date = datetime.datetime.strptime(date, '%Y-%m-%d %H:%M:%S')
        forms.show_balloon("my header", "Lorem ipsum", tooltip='tooltip',   group='group', is_favourite=True, is_new=True, timestamp = date, click_result = forms.result_item_result_clicked)
        ```
    """
    result_item = Autodesk.Internal.InfoCenter.ResultItem()
    result_item.Category = header
    result_item.Title = text
    result_item.TooltipText = tooltip
    result_item.Group = group
    result_item.IsFavorite = is_favourite
    result_item.IsNew = is_new
    if timestamp:
        result_item.Timestamp = timestamp
    result_item.ResultClicked += click_result
    balloon = Autodesk.Windows.ComponentManager.InfoCenterPaletteManager.ShowBalloon(
        result_item)
    return balloon


def pick_file(file_ext='*', files_filter='', init_dir='',
              restore_dir=True, multi_file=False, unc_paths=False, title=None):
    r"""Pick file dialog to select a destination file.

    Args:
        file_ext (str): file extension
        files_filter (str): file filter
        init_dir (str): initial directory
        restore_dir (bool): restore last directory
        multi_file (bool): allow select multiple files
        unc_paths (bool): return unc paths
        title (str): text to show in the title bar

    Returns:
        (str | list[str]): file path or list of file paths if multi_file=True

    Examples:
        ```python
        from pyrevit import forms
        forms.pick_file(file_ext='csv')
        r'C:\output\somefile.csv'
        ```

        ```python
        forms.pick_file(file_ext='csv', multi_file=True)
        [r'C:\output\somefile1.csv', r'C:\output\somefile2.csv']
        ```

        ```python
        forms.pick_file(files_filter='All Files (*.*)|*.*|'
                                         'Excel Workbook (*.xlsx)|*.xlsx|'
                                         'Excel 97-2003 Workbook|*.xls',
                            multi_file=True)
        [r'C:\output\somefile1.xlsx', r'C:\output\somefile2.xls']
        ```
    """
    of_dlg = Forms.OpenFileDialog()
    if files_filter:
        of_dlg.Filter = files_filter
    else:
        of_dlg.Filter = '|*.{}'.format(file_ext)
    of_dlg.RestoreDirectory = restore_dir
    of_dlg.Multiselect = multi_file
    if init_dir:
        of_dlg.InitialDirectory = init_dir
    if title:
        of_dlg.Title = title
    if of_dlg.ShowDialog() == Forms.DialogResult.OK:
        if multi_file:
            if unc_paths:
                return [coreutils.dletter_to_unc(x)
                        for x in of_dlg.FileNames]
            return of_dlg.FileNames
        else:
            if unc_paths:
                return coreutils.dletter_to_unc(of_dlg.FileName)
            return of_dlg.FileName


def save_file(file_ext='', files_filter='', init_dir='', default_name='',
              restore_dir=True, unc_paths=False, title=None):
    r"""Save file dialog to select a destination file for data.

    Args:
        file_ext (str): file extension
        files_filter (str): file filter
        init_dir (str): initial directory
        default_name (str): default file name
        restore_dir (bool): restore last directory
        unc_paths (bool): return unc paths
        title (str): text to show in the title bar

    Returns:
        (str): file path

    Examples:
        ```python
        from pyrevit import forms
        forms.save_file(file_ext='csv')
        r'C:\output\somefile.csv'
        ```
    """
    sf_dlg = Forms.SaveFileDialog()
    if files_filter:
        sf_dlg.Filter = files_filter
    else:
        sf_dlg.Filter = '|*.{}'.format(file_ext)
    sf_dlg.RestoreDirectory = restore_dir
    if init_dir:
        sf_dlg.InitialDirectory = init_dir
    if title:
        sf_dlg.Title = title

    # setting default filename
    sf_dlg.FileName = default_name

    if sf_dlg.ShowDialog() == Forms.DialogResult.OK:
        if unc_paths:
            return coreutils.dletter_to_unc(sf_dlg.FileName)
        return sf_dlg.FileName


def pick_excel_file(save=False, title=None):
    """File pick/save dialog for an excel file.

    Args:
        save (bool): show file save dialog, instead of file pick dialog
        title (str): text to show in the title bar

    Returns:
        (str): file path
    """
    if save:
        return save_file(file_ext='xlsx')
    return pick_file(files_filter='Excel Workbook (*.xlsx)|*.xlsx|'
                                  'Excel 97-2003 Workbook|*.xls',
                     title=title)


def save_excel_file(title=None):
    """File save dialog for an excel file.

    Args:
        title (str): text to show in the title bar

    Returns:
        (str): file path
    """
    return pick_excel_file(save=True, title=title)


def check_workshared(doc=None, message='Model is not workshared.'):
    """Verify if model is workshared and notify user if not.

    Args:
        doc (DB.Document): target document, current of not provided
        message (str): prompt message if returning False

    Returns:
        (bool): True if doc is workshared
    """
    doc = doc or DOCS.doc
    if not doc.IsWorkshared:
        alert(message, warn_icon=True)
        return False
    return True


def check_selection(exitscript=False,
                    message='At least one element must be selected.'):
    """Verify if selection is not empty notify user if it is.

    Args:
        exitscript (bool): exit script if returning False
        message (str): prompt message if returning False

    Returns:
        (bool): True if selection has at least one item
    """
    if revit.get_selection().is_empty:
        alert(message, exitscript=exitscript)
        return False
    return True


def check_familydoc(doc=None, family_cat=None, exitscript=False):
    """Verify document is a Family and notify user if not.

    Args:
        doc (DB.Document): target document, current of not provided
        family_cat (str): family category name
        exitscript (bool): exit script if returning False

    Returns:
        (bool): True if doc is a Family and of provided category

    Examples:
        ```python
        from pyrevit import forms
        forms.check_familydoc(doc=revit.doc, family_cat='Data Devices')
        True
        ```
    """
    doc = doc or DOCS.doc
    family_cat = revit.query.get_category(family_cat)
    if doc.IsFamilyDocument and family_cat:
        if doc.OwnerFamily.FamilyCategory.Id == family_cat.Id:
            return True
    elif doc.IsFamilyDocument and not family_cat:
        return True

    family_type_msg = ' of type {}'\
                      .format(family_cat.Name) if family_cat else''
    alert('Active document must be a Family document{}.'
          .format(family_type_msg), exitscript=exitscript)
    return False


def check_modeldoc(doc=None, exitscript=False):
    """Verify document is a not a Model and notify user if not.

    Args:
        doc (DB.Document): target document, current of not provided
        exitscript (bool): exit script if returning False

    Returns:
        (bool): True if doc is a Model

    Examples:
        ```python
        from pyrevit import forms
        forms.check_modeldoc(doc=revit.doc)
        True
        ```
    """
    doc = doc or DOCS.doc
    if not doc.IsFamilyDocument:
        return True

    alert('Active document must be a Revit model (not a Family).',
          exitscript=exitscript)
    return False


def check_modelview(view, exitscript=False):
    """Verify target view is a model view.

    Args:
        view (DB.View): target view
        exitscript (bool): exit script if returning False

    Returns:
        (bool): True if view is model view

    Examples:
        ```python
        from pyrevit import forms
        forms.check_modelview(view=revit.active_view)
        True
        ```
    """
    if not isinstance(view, (DB.View3D, DB.ViewPlan, DB.ViewSection)):
        alert("Active view must be a model view.", exitscript=exitscript)
        return False
    return True


def check_viewtype(view, view_type, exitscript=False):
    """Verify target view is of given type.

    Args:
        view (DB.View): target view
        view_type (DB.ViewType): type of view
        exitscript (bool): exit script if returning False

    Returns:
        (bool): True if view is of given type

    Examples:
        ```python
        from pyrevit import forms
        forms.check_viewtype(revit.active_view, DB.ViewType.DrawingSheet)
        True
        ```
    """
    if view.ViewType != view_type:
        alert(
            "Active view must be a {}.".format(
                ' '.join(coreutils.split_words(str(view_type)))),
            exitscript=exitscript
            )
        return False
    return True


def check_graphicalview(view, exitscript=False):
    """Verify target view is a graphical view.

    Args:
        view (DB.View): target view
        exitscript (bool): exit script if returning False

    Returns:
        (bool): True if view is a graphical view

    Examples:
        ```python
        from pyrevit import forms
        forms.check_graphicalview(revit.active_view)
        True
        ```
    """
    if not view.Category:
        alert(
            "Active view must be a grahical view.",
            exitscript=exitscript
            )
        return False
    return True


def toast(message, title='pyRevit', appid='pyRevit',
          icon=None, click=None, actions=None):
    """Show a Windows 10 notification.

    Args:
        message (str): notification message
        title (str): notification title
        appid (str): app name (will show under message)
        icon (str): file path to icon .ico file (defaults to pyRevit icon)
        click (str): click action commands string
        actions (dict): dictionary of button names and action strings

    Examples:
        ```python
        script.toast("Hello World!",
                     title="My Script",
                     appid="MyAPP",
                     click="https://eirannejad.github.io/pyRevit/",
                     actions={
                         "Open Google":"https://google.com",
                         "Open Toast64":"https://github.com/go-toast/toast"
                         })
        ```
    """
    toaster.send_toast(
        message,
        title=title,
        appid=appid,
        icon=icon,
        click=click,
        actions=actions)


def ask_for_string(default=None, prompt=None, title=None, **kwargs):
    """Ask user to select a string value.

    This is a shortcut function that configures :obj:`GetValueWindow` for
    string data types. kwargs can be used to pass on other arguments.

    Args:
        default (str): default unique string. must not be in reserved_values
        prompt (str): prompt message
        title (str): title message
        kwargs (type): other arguments to be passed to :obj:`GetValueWindow`

    Returns:
        (str): selected string value

    Examples:
        ```python
        forms.ask_for_string(
            default='some-tag',
            prompt='Enter new tag name:',
            title='Tag Manager')
        'new-tag'
        ```
    """
    return GetValueWindow.show(
        None,
        value_type='string',
        default=default,
        prompt=prompt,
        title=title,
        **kwargs
        )


def ask_for_unique_string(reserved_values,
                          default=None, prompt=None, title=None, **kwargs):
    """Ask user to select a unique string value.

    This is a shortcut function that configures :obj:`GetValueWindow` for
    unique string data types. kwargs can be used to pass on other arguments.

    Args:
        reserved_values (list[str]): list of reserved (forbidden) values
        default (str): default unique string. must not be in reserved_values
        prompt (str): prompt message
        title (str): title message
        kwargs (type): other arguments to be passed to :obj:`GetValueWindow`

    Returns:
        (str): selected unique string

    Examples:
        ```python
        forms.ask_for_unique_string(
            prompt='Enter a Unique Name',
            title=self.Title,
            reserved_values=['Ehsan', 'Gui', 'Guido'],
            owner=self)
        'unique string'
        ```

        In example above, owner argument is provided to be passed to underlying
        :obj:`GetValueWindow`.

    """
    return GetValueWindow.show(
        None,
        value_type='string',
        default=default,
        prompt=prompt,
        title=title,
        reserved_values=reserved_values,
        **kwargs
        )


def ask_for_one_item(items, default=None, prompt=None, title=None, **kwargs):
    """Ask user to select an item from a list of items.

    This is a shortcut function that configures :obj:`GetValueWindow` for
    'single-select' data types. kwargs can be used to pass on other arguments.

    Args:
        items (list[str]): list of items to choose from
        default (str): default selected item
        prompt (str): prompt message
        title (str): title message
        kwargs (type): other arguments to be passed to :obj:`GetValueWindow`

    Returns:
        (str): selected item

    Examples:
        ```python
        forms.ask_for_one_item(
            ['test item 1', 'test item 2', 'test item 3'],
            default='test item 2',
            prompt='test prompt',
            title='test title'
        )
        'test item 1'
        ```
    """
    return GetValueWindow.show(
        items,
        value_type='dropdown',
        default=default,
        prompt=prompt,
        title=title,
        **kwargs
        )


def ask_for_date(default=None, prompt=None, title=None, **kwargs):
    """Ask user to select a date value.

    This is a shortcut function that configures :obj:`GetValueWindow` for
    date data types. kwargs can be used to pass on other arguments.

    Args:
        default (datetime.datetime): default selected date value
        prompt (str): prompt message
        title (str): title message
        kwargs (type): other arguments to be passed to :obj:`GetValueWindow`

    Returns:
        (datetime.datetime): selected date

    Examples:
        ```python
        forms.ask_for_date(default="", title="Enter deadline:")
        datetime.datetime(2019, 5, 17, 0, 0)
        ```
    """
    # FIXME: window does not set default value
    return GetValueWindow.show(
        None,
        value_type='date',
        default=default,
        prompt=prompt,
        title=title,
        **kwargs
        )


def ask_for_number_slider(default=None, min=0, max=100, interval=1, prompt=None, title=None, **kwargs):
    """Ask user to select a number value.

    This is a shortcut function that configures :obj:`GetValueWindow` for
    numbers. kwargs can be used to pass on other arguments.

    Args:
        default (str): default unique string. must not be in reserved_values
        min (int): minimum value on slider
        max (int): maximum value on slider
        interval (int): number interval between values
        prompt (str): prompt message
        title (str): title message
        kwargs (type): other arguments to be passed to :obj:`GetValueWindow`

    Returns:
        (str): selected string value

    Examples:
        ```python
        forms.ask_for_number_slider(
            default=50,
            min = 0,
            max = 100,
            interval = 5,
            prompt='Select a number:',
            title='test title')
        '50'
        ```
    
    In this example, the slider will allow values such as '40, 45, 50, 55, 60' etc
    """
    return GetValueWindow.show(
        None,
        value_type='slider',
        default=default,
        prompt=prompt,
        title=title,
        max=max,
        min=min,
        interval=interval,
        **kwargs
        )


def ask_to_use_selected(type_name, count=None, multiple=True):
    """Ask user if wants to use currently selected elements.

    Args:
        type_name (str): Element type of expected selected elements
        count (int): Number of selected items
        multiple (bool): Whether multiple selected items are allowed
    """
    report = type_name.lower()
    # multiple = True
    message = \
        "You currently have %s selected. Do you want to proceed with "\
        "currently selected item(s)?"
    # check is selecting multiple is allowd
    if not multiple:
        # multiple = False
        message = \
            "You currently have %s selected and only one is required. "\
            "Do you want to use the first selected item?"

    # check if count is provided
    if count is not None:
        report = '{} {}'.format(count, report)
    return alert(message % report, yes=True, no=True)


def ask_for_color(default=None):
    """Show system color picker and ask for color.

    Args:
        default (str): default color in HEX ARGB e.g. #ff808080

    Returns:
        (str): selected color in HEX ARGB e.g. #ff808080, or None if cancelled

    Examples:
        ```python
        forms.ask_for_color()
        '#ff808080'
        ```
    """
    # colorDlg.Color
    color_picker = Forms.ColorDialog()
    if default:
        default = default.replace('#', '')
        color_picker.Color = System.Drawing.Color.FromArgb(
            int(default[:2], 16),
            int(default[2:4], 16),
            int(default[4:6], 16),
            int(default[6:8], 16)
        )
    color_picker.FullOpen = True
    if color_picker.ShowDialog() == Forms.DialogResult.OK:
        c = color_picker.Color
        c_hex = ''.join('{:02X}'.format(int(x)) for x in [c.A, c.R, c.G, c.B])
        return '#' + c_hex


def inform_wip():
    """Show work-in-progress prompt to user and exit script.

    Examples:
        ```python
        forms.inform_wip()
        ```
    """
    alert("Work in progress.", exitscript=True)

"""Base module for pushing toast messages on Win 10.

This module is a wrapper for a cli utility that provides toast message
functionality. See `https://github.com/go-toast/toast`
"""

import os.path as op
import subprocess

from pyrevit import BIN_DIR
from pyrevit.coreutils.logger import get_logger


#pylint: disable=W0703,C0302
mlogger = get_logger(__name__)  #pylint: disable=C0103


def get_toaster():
    """Return full file path of the toast binary utility."""
    return op.join(BIN_DIR, 'pyrevit-toast.exe')


def send_toast(message,
               title=None, appid=None, icon=None, click=None, actions=None):
    """Send toast notificaton.

    Args:
        message (str): notification message
        title (str): notification title
        appid (str): application unique id (see `--app-id` cli option)
        icon (str): notification icon (see `--icon` cli option)
        click (str): click action (see `--activation-arg` cli option)
        actions (dict[str:str]):
            list of actions (see `--action` and `--action-arg` cli options)
    """
    # set defaults
    if not title:
        title = 'pyRevit'
    if not appid:
        appid = title
    if not icon:
        icon = op.join(BIN_DIR, 'pyRevit.ico')
    if not actions:
        actions = {}

    # build the toast
    toast_args = r'"{}"'.format(get_toaster())
    toast_args += r' --app-id "{}"'.format(appid)
    toast_args += r' --title "{}"'.format(title)
    toast_args += r' --message "{}"'.format(message)
    toast_args += r' --icon "{}"'.format(icon)
    toast_args += r' --audio "default"'
    # toast_args += r' --duration "long"'
    if click:
        toast_args += r' --activation-arg "{}"'.format(click)
    for action, args in actions.items():
        toast_args += r' --action "{}" --action-arg "{}"'.format(action, args)

    # send the toast now
    mlogger.debug('toasting: %s', toast_args)
    subprocess.Popen(toast_args, shell=True)

"""Utility functions to support forms module."""

from pyrevit import framework
from pyrevit.framework import wpf, Controls, Imaging


def bitmap_from_file(bitmap_file):
    """Create BitmapImage from a bitmap file.

    Args:
        bitmap_file (str): path to bitmap file

    Returns:
        (BitmapImage): bitmap image object
    """
    bitmap = Imaging.BitmapImage()
    bitmap.BeginInit()
    bitmap.UriSource = framework.Uri(bitmap_file)
    bitmap.CacheOption = Imaging.BitmapCacheOption.OnLoad
    bitmap.CreateOptions = Imaging.BitmapCreateOptions.IgnoreImageCache
    bitmap.EndInit()
    bitmap.Freeze()
    return bitmap


def load_component(xaml_file, comp_type):
    """Load WPF component from xaml file.

    Args:
        xaml_file (str): xaml file path
        comp_type (System.Windows.Controls): WPF control type

    Returns:
        (System.Windows.Controls): loaded WPF control
    """
    return wpf.LoadComponent(comp_type, xaml_file)


def load_ctrl_template(xaml_file):
    """Load System.Windows.Controls.ControlTemplate from xaml file.

    Args:
        xaml_file (str): xaml file path

    Returns:
        (System.Windows.Controls.ControlTemplate): loaded control template
    """
    return load_component(xaml_file, Controls.ControlTemplate())


def load_itemspanel_template(xaml_file):
    """Load System.Windows.Controls.ItemsPanelTemplate from xaml file.

    Args:
        xaml_file (str): xaml file path

    Returns:
        (System.Windows.Controls.ControlTemplate): loaded items-panel template
    """
    return load_component(xaml_file, Controls.ItemsPanelTemplate())

"""Loader base module."""
from pyrevit import EXEC_PARAMS


LOADER_ADDON_NAMESPACE = 'PyRevitLoader'


HASH_CUTOFF_LENGTH = 16

"""Assembly maker module."""
import os.path as op
from collections import namedtuple

from pyrevit import PYREVIT_ADDON_NAME, EXEC_PARAMS
from pyrevit import framework
from pyrevit.framework import AppDomain, Version
from pyrevit.framework import AssemblyName, AssemblyBuilderAccess
from pyrevit import coreutils
from pyrevit.coreutils import assmutils
from pyrevit.coreutils import appdata
from pyrevit.coreutils import logger
from pyrevit.versionmgr import get_pyrevit_version

from pyrevit.loader import HASH_CUTOFF_LENGTH
from pyrevit.runtime import BASE_TYPES_DIR_HASH
from pyrevit.runtime import typemaker
from pyrevit.userconfig import user_config

from System.Reflection.Emit import AssemblyBuilder

# Generic named tuple for passing assembly information to other modules
ExtensionAssemblyInfo = namedtuple('ExtensionAssemblyInfo',
                                   ['name', 'location', 'reloading'])


#pylint: disable=W0703,C0302,C0103
mlogger = logger.get_logger(__name__)


def _make_extension_hash(extension):
    # creates a hash based on hash of baseclasses module that
    # the extension is based upon and also the user configuration version
    return coreutils.get_str_hash(
        BASE_TYPES_DIR_HASH
        + EXEC_PARAMS.engine_ver
        + extension.get_hash())[:HASH_CUTOFF_LENGTH]


def _make_ext_asm_fileid(extension):
    return '{}_{}'.format(_make_extension_hash(extension), extension.name)


def _is_pyrevit_ext_asm(asm_name, extension):
    # if this is a pyRevit package assembly
    return asm_name.startswith(PYREVIT_ADDON_NAME) \
           and asm_name.endswith(extension.name)


def _is_pyrevit_ext_already_loaded(ext_asm_name):
    mlogger.debug('Asking Revit for previously loaded package assemblies: %s',
                  ext_asm_name)
    return len(assmutils.find_loaded_asm(ext_asm_name))


def _is_any_ext_asm_loaded(extension):
    for loaded_asm in AppDomain.CurrentDomain.GetAssemblies():
        mlogger.debug('Checking for loaded extension asm: %s ? %s : %s',
                      extension.name, loaded_asm.GetName().Name, loaded_asm)
        if _is_pyrevit_ext_asm(loaded_asm.GetName().Name, extension):
            return True
    return False


def _update_component_cmd_types(extension):
    for cmd_component in extension.get_all_commands():
        typemaker.make_bundle_types(
            extension,
            cmd_component,
            module_builder=None
            )


def _create_asm_file(extension, ext_asm_file_name, ext_asm_file_path):
    # check to see if any older assemblies have been loaded for this package
    ext_asm_full_file_name = \
        coreutils.make_canonical_name(ext_asm_file_name,
                                      framework.ASSEMBLY_FILE_TYPE)

    # this means that we currently have this package loaded and
    # we're reloading a new version
    is_reloading_pkg = _is_any_ext_asm_loaded(extension)

    # create assembly
    mlogger.debug('Building assembly for package: %s', extension)
    pyrvt_ver_int_tuple = get_pyrevit_version().as_int_tuple()
    win_asm_name = AssemblyName(Name=ext_asm_file_name,
                                Version=Version(pyrvt_ver_int_tuple[0],
                                                pyrvt_ver_int_tuple[1],
                                                pyrvt_ver_int_tuple[2]))
    mlogger.debug('Generated assembly name for this package: %s',
                  ext_asm_file_name)
    mlogger.debug('Generated windows assembly name for this package: %s',
                  win_asm_name)
    mlogger.debug('Generated assembly file name for this package: %s',
                  ext_asm_full_file_name)

    if int(__revit__.Application.VersionNumber) >= 2025:
        asm_builder = AssemblyBuilder.DefineDynamicAssembly(
            win_asm_name,
            AssemblyBuilderAccess.Run)

        # get module builder
        module_builder = asm_builder.DefineDynamicModule(ext_asm_file_name)
    else:
        # get assembly builder
        asm_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
            win_asm_name,
            AssemblyBuilderAccess.RunAndSave,
            op.dirname(ext_asm_file_path))

        # get module builder
        module_builder = asm_builder.DefineDynamicModule(ext_asm_file_name,
                                                         ext_asm_full_file_name)

    # create command classes
    for cmd_component in extension.get_all_commands():
        # create command executor class for this command
        mlogger.debug('Creating types for command: %s', cmd_component)
        typemaker.make_bundle_types(extension, cmd_component, module_builder)

    if int(__revit__.Application.VersionNumber) >= 2025:
        from Lokad.ILPack import AssemblyGenerator
        generator = AssemblyGenerator()
        generator.GenerateAssembly(asm_builder, ext_asm_file_path)
    else:
        # save final assembly
        asm_builder.Save(ext_asm_full_file_name)

    assmutils.load_asm_file(ext_asm_file_path)

    mlogger.debug('Executer assembly saved.')
    return ExtensionAssemblyInfo(ext_asm_file_name,
                                 ext_asm_file_path,
                                 is_reloading_pkg)


def _produce_asm_file(extension):
    # unique assembly filename for this package
    ext_asm_fileid = _make_ext_asm_fileid(extension)
    ext_asm_file_path = \
        appdata.get_data_file(file_id=ext_asm_fileid,
                              file_ext=framework.ASSEMBLY_FILE_TYPE)
    # make unique assembly name for this package
    ext_asm_file_name = coreutils.get_file_name(ext_asm_file_path)

    if _is_pyrevit_ext_already_loaded(ext_asm_file_name):
        mlogger.debug('Extension assembly is already loaded: %s',
                      ext_asm_file_name)
        _update_component_cmd_types(extension)
        return ExtensionAssemblyInfo(ext_asm_file_name, ext_asm_file_path, True)
    elif appdata.is_data_file_available(
            file_id=ext_asm_fileid,
            file_ext=framework.ASSEMBLY_FILE_TYPE):
        mlogger.debug('Extension assembly file already exists: %s',
                      ext_asm_file_path)
        try:
            loaded_assm = assmutils.load_asm_file(ext_asm_file_path)
            for asm_name in loaded_assm.GetReferencedAssemblies():
                mlogger.debug('Checking referenced assembly: %s', asm_name)
                ref_asm_file_path = \
                    appdata.is_file_available(
                        file_name=asm_name.Name,
                        file_ext=framework.ASSEMBLY_FILE_TYPE
                        )
                if ref_asm_file_path:
                    mlogger.debug('Loading referenced assembly: %s',
                                  ref_asm_file_path)
                    try:
                        assmutils.load_asm_file(ref_asm_file_path)
                    except Exception as load_err:
                        mlogger.error('Error loading referenced assembly: %s '
                                      '| %s', ref_asm_file_path, load_err)

            _update_component_cmd_types(extension)
            return ExtensionAssemblyInfo(ext_asm_file_name,
                                         ext_asm_file_path,
                                         False)
        except Exception as ext_asm_load_err:
            mlogger.error('Error loading extension assembly: %s | %s',
                          ext_asm_file_path, ext_asm_load_err)
    else:
        return _create_asm_file(extension,
                                ext_asm_file_name,
                                ext_asm_file_path)


def create_assembly(extension):
    """Create an extension assembly.

    Args:
        extension (pyrevit.extensions.components.Extension): pyRevit extension.

    Returns:
        (ExtensionAssemblyInfo): assembly info
    """
    mlogger.debug('Creating assembly for extension: %s', extension.name)
    # create assembly file and return assembly path to be used in UI creation
    # try:
    ext_asm_info = _produce_asm_file(extension)
    mlogger.debug('Assembly created: %s', ext_asm_info)
    return ext_asm_info
    # except Exception as asm_err:
    #     mlogger.critical('Can not create assembly for: {}' \
    #                     '| {}'.format(extension, asm_err))


def cleanup_assembly_files():
    if coreutils.get_revit_instance_count() == 1:
        for asm_file_path in appdata.list_data_files(file_ext='dll'):
            if not assmutils.find_loaded_asm(asm_file_path, by_location=True):
                appdata.garbage_data_file(asm_file_path)
                asm_log_file = asm_file_path.replace('.dll', '.log')
                if op.exists(asm_log_file):
                    appdata.garbage_data_file(asm_log_file)

"""Hooks management."""
import os.path as op
import re
from collections import namedtuple

from pyrevit import HOST_APP
from pyrevit import framework
from pyrevit import coreutils
from pyrevit.coreutils.logger import get_logger
from pyrevit.coreutils import envvars
from pyrevit.runtime.types import EventHooks
import pyrevit.extensions as exts

from pyrevit.loader import sessioninfo


SUPPORTED_LANGUAGES = [
    exts.PYTHON_SCRIPT_FILE_FORMAT,
    exts.CSHARP_SCRIPT_FILE_FORMAT,
    exts.VB_SCRIPT_FILE_FORMAT,
    ]

#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


ExtensionEventHook = namedtuple('ExtensionEventHook', [
    'id',
    'name',
    'target',
    'script',
    'syspaths',
    'extension_name',
    ])


def get_hooks_handler():
    """Get the hook handler environment variable.

    Returns:
        (EventHooks): hook handler
    """
    return envvars.get_pyrevit_env_var(envvars.HOOKSHANDLER_ENVVAR)


def set_hooks_handler(handler):
    """Set the hook handler environment variable.

    Args:
        handler (EventHooks): hook handler
    """
    envvars.set_pyrevit_env_var(envvars.HOOKSHANDLER_ENVVAR, handler)


def is_valid_hook_script(hook_script):
    """Check if the given hook script is valid.

    Args:
        hook_script (str): hook script path

    Returns:
        (bool): True if the script is valid, False otherwise
    """
    return op.splitext(op.basename(hook_script))[1] in SUPPORTED_LANGUAGES


def _get_hook_parts(extension, hook_script):
    # finds the two parts of the hook script name
    # e.g command-before-exec[ID_INPLACE_COMPONENT].py
    # ('command-before-exec', 'ID_INPLACE_COMPONENT')
    parts = re.findall(
        r'([a-z -]+)\[?([A-Z _]+)?\]?\..+',
        op.basename(hook_script)
        )
    if parts:
        return parts[0]
    else:
        return '', ''


def _create_hook_id(extension, hook_script):
    hook_script_id = op.basename(hook_script)
    pieces = [extension.unique_name, hook_script_id]
    return coreutils.cleanup_string(
        exts.UNIQUE_ID_SEPARATOR.join(pieces),
        skip=[exts.UNIQUE_ID_SEPARATOR]
        ).lower()


def get_extension_hooks(extension):
    """Get the hooks of the given extension.

    Args:
        extension (pyrevit.extensions.components.Extension): pyRevit extension

    Returns:
        (list[ExtensionEventHook]): list of hooks
    """
    event_hooks = []
    for hook_script in extension.get_hooks():
        if is_valid_hook_script(hook_script):
            name, target = _get_hook_parts(extension, hook_script)
            if name:
                event_hooks.append(
                    ExtensionEventHook(
                        id=_create_hook_id(extension, hook_script),
                        name=name,
                        target=target,
                        script=hook_script,
                        syspaths=extension.module_paths,
                        extension_name=extension.name,
                    )
                )
    return event_hooks


def get_event_hooks():
    """Get all the event hooks."""
    hooks_handler = get_hooks_handler()
    return hooks_handler.GetAllEventHooks()


def register_hooks(extension):
    """Register the hooks for the given extension.

    Args:
        extension (pyrevit.extensions.components.Extension): pyRevit extension
    """
    hooks_handler = get_hooks_handler()
    for ext_hook in get_extension_hooks(extension):
        try:
            hooks_handler.RegisterHook(
                uniqueId=ext_hook.id,
                eventName=ext_hook.name,
                eventTarget=ext_hook.target,
                scriptPath=ext_hook.script,
                searchPaths=framework.Array[str](ext_hook.syspaths),
                extensionName=ext_hook.extension_name,
            )
        except Exception as hookEx:
            mlogger.error("Failed registering hook script %s | %s",
                          ext_hook.script, hookEx)


def unregister_hooks(extension):
    """Unregister all hooks for the given extension.

    Args:
        extension (pyrevit.extensions.components.Extension): pyRevit extension
    """
    hooks_handler = get_hooks_handler()
    for ext_hook in get_extension_hooks(extension):
        hooks_handler.UnRegisterHook(uniqueId=ext_hook.id)


def unregister_all_hooks():
    """Unregister all hooks."""
    hooks_handler = get_hooks_handler()
    hooks_handler.UnRegisterAllHooks(uiApp=HOST_APP.uiapp)


def activate():
    """Activate all event hooks."""
    hooks_handler = get_hooks_handler()
    hooks_handler.ActivateEventHooks(uiApp=HOST_APP.uiapp)


def deactivate():
    """Deactivate all event hooks."""
    hooks_handler = get_hooks_handler()
    hooks_handler.DeactivateEventHooks(uiApp=HOST_APP.uiapp)


def setup_hooks(session_id=None):
    """Setup the hooks for the given session.
    
    If no session is specified, use the current one.

    Args:
        session_id (str, optional): Session. Defaults to None.
    """
    # make sure session id is availabe
    if not session_id:
        session_id = sessioninfo.get_session_uuid()

    hooks_handler = get_hooks_handler()
    if hooks_handler:
        # deactivate old
        hooks_handler.DeactivateEventHooks(uiApp=HOST_APP.uiapp)
    # setup new
    hooks_handler = EventHooks(session_id)
    set_hooks_handler(hooks_handler)
    unregister_all_hooks()

"""Manage information about pyRevit sessions."""
import sys
from collections import namedtuple

from pyrevit import HOST_APP, HOME_DIR

from pyrevit import versionmgr
from pyrevit.compat import safe_strtype
from pyrevit.versionmgr import about
from pyrevit import coreutils
from pyrevit.coreutils.logger import get_logger
from pyrevit.coreutils import envvars
from pyrevit.userconfig import user_config
from pyrevit import runtime
from pyrevit.loader.systemdiag import system_diag


#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


RuntimeInfo = namedtuple('RuntimeInfo', ['pyrevit_version',
                                         'engine_version',
                                         'host_version'])
"""Session runtime information tuple.

Args:
    pyrevit_version (str): formatted pyRevit version
    engine_version (int): active IronPython engine version
    host_version (str): Current Revit version
"""


def setup_runtime_vars():
    """Setup runtime environment variables with session information."""
    # set pyrevit version
    pyrvt_ver = versionmgr.get_pyrevit_version().get_formatted()
    envvars.set_pyrevit_env_var(envvars.VERSION_ENVVAR, pyrvt_ver)

    # set app version env var
    if HOST_APP.is_newer_than(2017):
        envvars.set_pyrevit_env_var(envvars.APPVERSION_ENVVAR,
                                    HOST_APP.subversion)
    else:
        envvars.set_pyrevit_env_var(envvars.APPVERSION_ENVVAR,
                                    HOST_APP.version)

    # set ironpython engine version env var
    attachment = user_config.get_current_attachment()
    if attachment and attachment.Clone:
        envvars.set_pyrevit_env_var(envvars.CLONENAME_ENVVAR,
                                    attachment.Clone.Name)
        envvars.set_pyrevit_env_var(envvars.IPYVERSION_ENVVAR,
                                    str(attachment.Engine.Version))
    else:
        mlogger.debug('Can not determine attachment.')
        envvars.set_pyrevit_env_var(envvars.CLONENAME_ENVVAR, "Unknown")
        envvars.set_pyrevit_env_var(envvars.IPYVERSION_ENVVAR, "0")

    # set cpython engine version env var
    cpyengine = user_config.get_active_cpython_engine()
    if cpyengine:
        envvars.set_pyrevit_env_var(envvars.CPYVERSION_ENVVAR,
                                    str(cpyengine.Version))
    else:
        envvars.set_pyrevit_env_var(envvars.CPYVERSION_ENVVAR, "0")

    # set a list of important assemblies
    # this is required for dotnet script execution
    set_loaded_pyrevit_referenced_modules(
        runtime.get_references()
        )


def get_runtime_info():
    """Return runtime information tuple.

    Returns:
        (RuntimeInfo): runtime info tuple

    Examples:
        ```python
        sessioninfo.get_runtime_info()
        ```
    """
    # FIXME: add example output
    return RuntimeInfo(
        pyrevit_version=envvars.get_pyrevit_env_var(envvars.VERSION_ENVVAR),
        engine_version=envvars.get_pyrevit_env_var(envvars.IPYVERSION_ENVVAR),
        host_version=envvars.get_pyrevit_env_var(envvars.APPVERSION_ENVVAR)
        )


def set_session_uuid(uuid_str):
    """Set session uuid on environment variable.

    Args:
        uuid_str (str): session uuid string
    """
    envvars.set_pyrevit_env_var(envvars.SESSIONUUID_ENVVAR, uuid_str)


def get_session_uuid():
    """Read session uuid from environment variable.

    Returns:
        (str): session uuid string
    """
    return envvars.get_pyrevit_env_var(envvars.SESSIONUUID_ENVVAR)


def new_session_uuid():
    """Create a new uuid for a pyRevit session.

    Returns:
        (str): session uuid string
    """
    uuid_str = safe_strtype(coreutils.new_uuid())
    set_session_uuid(uuid_str)
    return uuid_str


def get_loaded_pyrevit_assemblies():
    """Return list of loaded pyRevit assemblies from environment variable.

    Returns:
        (list[str]): list of loaded assemblies
    """
    # FIXME: verify and document return type
    loaded_assms_str = envvars.get_pyrevit_env_var(envvars.LOADEDASSMS_ENVVAR)
    if loaded_assms_str:
        return loaded_assms_str.split(coreutils.DEFAULT_SEPARATOR)
    else:
        return []


def set_loaded_pyrevit_assemblies(loaded_assm_name_list):
    """Set the environment variable with list of loaded assemblies.

    Args:
        loaded_assm_name_list (list[str]): list of assembly names
    """
    envvars.set_pyrevit_env_var(
        envvars.LOADEDASSMS_ENVVAR,
        coreutils.DEFAULT_SEPARATOR.join(loaded_assm_name_list)
        )


def get_loaded_pyrevit_referenced_modules():
    loaded_assms_str = envvars.get_pyrevit_env_var(envvars.REFEDASSMS_ENVVAR)
    if loaded_assms_str:
        return set(loaded_assms_str.split(coreutils.DEFAULT_SEPARATOR))
    else:
        return set()


def set_loaded_pyrevit_referenced_modules(loaded_assm_name_list):
    envvars.set_pyrevit_env_var(
        envvars.REFEDASSMS_ENVVAR,
        coreutils.DEFAULT_SEPARATOR.join(loaded_assm_name_list)
        )


def update_loaded_pyrevit_referenced_modules(loaded_assm_name_list):
    loaded_modules = get_loaded_pyrevit_referenced_modules()
    loaded_modules.update(loaded_assm_name_list)
    set_loaded_pyrevit_referenced_modules(loaded_modules)


def report_env():
    """Report python version, home directory, config file, etc."""
    # run diagnostics
    system_diag()

    # get python version that includes last commit hash
    mlogger.info('pyRevit version: %s - </> with :growing_heart: in %s',
                 envvars.get_pyrevit_env_var(envvars.VERSION_ENVVAR),
                 about.get_pyrevit_about().madein)

    if user_config.rocket_mode:
        mlogger.info('pyRevit Rocket Mode enabled. :rocket:')

    mlogger.info('Host is %s pid: %s', HOST_APP.pretty_name, HOST_APP.proc_id)
    # ipy 2.7.10 has a new line in its sys.version :rolling-eyes-emoji:
    mlogger.info('Running on: %s', sys.version.replace('\n', ' '))
    mlogger.info('User is: %s', HOST_APP.username)
    mlogger.info('Home Directory is: %s', HOME_DIR)
    mlogger.info('Session uuid is: %s', get_session_uuid())
    mlogger.info('Runtime assembly is: %s', runtime.RUNTIME_ASSM_NAME)
    mlogger.info('Config file is (%s): %s',
                 user_config.config_type, user_config.config_file)

"""The loader module manages the workflow of loading a new pyRevit session.

Its main purpose is to orchestrate the process of finding pyRevit extensions,
creating dll assemblies for them, and creating a user interface
in the host application.

Everything starts from `sessionmgr.load_session()` function...

The only public function is `load_session()` that loads a new session.
Everything else is private.
"""
import sys
from collections import namedtuple

from pyrevit import EXEC_PARAMS, HOST_APP
from pyrevit import MAIN_LIB_DIR, MISC_LIB_DIR
from pyrevit import framework
from pyrevit.coreutils import Timer
from pyrevit.coreutils import assmutils
from pyrevit.coreutils import envvars
from pyrevit.coreutils import appdata
from pyrevit.coreutils import logger
from pyrevit.coreutils import applocales
from pyrevit.loader import sessioninfo
from pyrevit.loader import asmmaker
from pyrevit.loader import uimaker
from pyrevit.loader import hooks
from pyrevit.userconfig import user_config
from pyrevit.extensions import extensionmgr
from pyrevit.versionmgr import updater
from pyrevit.versionmgr import upgrade
from pyrevit import telemetry
from pyrevit import routes
# import the runtime first to get all the c-sharp code to compile
from pyrevit import runtime
from pyrevit.runtime import types as runtime_types
# now load the rest of module that could depend on the compiled runtime
from pyrevit import output

from pyrevit import DB, UI, revit


#pylint: disable=W0703,C0302,C0103,no-member
mlogger = logger.get_logger(__name__)


AssembledExtension = namedtuple('AssembledExtension', ['ext', 'assm'])


def _clear_running_engines():
    # clear the cached engines
    try:
        my_output = output.get_output()
        if my_output:
            my_output.close_others(all_open_outputs=True)

        runtime_types.ScriptEngineManager.ClearEngines(
            excludeEngine=EXEC_PARAMS.engine_id
            )
    except AttributeError:
        return False


def _setup_output():
    # create output window and assign handle
    out_window = runtime.types.ScriptConsole()
    runtime_info = sessioninfo.get_runtime_info()
    out_window.AppVersion = '{}:{}:{}'.format(
        runtime_info.pyrevit_version,
        int(runtime_info.engine_version),
        runtime_info.host_version
        )

    # create output stream and set stdout to it
    # we're not opening the output window here.
    # The output stream will open the window if anything is being printed.
    outstr = runtime.types.ScriptIO(out_window)
    sys.stdout = outstr
    # sys.stderr = outstr
    stdout_hndlr = logger.get_stdout_hndlr()
    stdout_hndlr.stream = outstr

    return out_window


def _cleanup_output():
    sys.stdout = None
    stdout_hndlr = logger.get_stdout_hndlr()
    stdout_hndlr.stream = None


# -----------------------------------------------------------------------------
# Functions related to creating/loading a new pyRevit session
# -----------------------------------------------------------------------------
def _check_autoupdate_inprogress():
    return envvars.get_pyrevit_env_var(envvars.AUTOUPDATING_ENVVAR)


def _set_autoupdate_inprogress(state):
    envvars.set_pyrevit_env_var(envvars.AUTOUPDATING_ENVVAR, state)


def _perform_onsessionloadstart_ops():
    # clear the cached engines
    if not _clear_running_engines():
        mlogger.debug('No Engine Manager exists...')

    # check for updates
    if user_config.auto_update \
            and not _check_autoupdate_inprogress():
        mlogger.info('Auto-update is active. Attempting update...')
        _set_autoupdate_inprogress(True)
        updater.update_pyrevit()
        _set_autoupdate_inprogress(False)

    # once pre-load is complete, report environment conditions
    uuid_str = sessioninfo.new_session_uuid()
    sessioninfo.report_env()

    # reset the list of assemblies loaded under pyRevit session
    sessioninfo.set_loaded_pyrevit_assemblies([])

    # init executor
    runtime_types.ScriptExecutor.Initialize()

    # init routes
    routes.init()

    # asking telemetry module to setup the telemetry system
    # (active or not active)
    telemetry.setup_telemetry(uuid_str)

    # apply Upgrades
    upgrade.upgrade_existing_pyrevit()

    # setup hooks
    hooks.setup_hooks()


def _perform_onsessionloadcomplete_ops():
    # cleanup old assembly files.
    asmmaker.cleanup_assembly_files()

    # clean up temp app files between sessions.
    appdata.cleanup_appdata_folder()

    # activate hooks now
    hooks.activate()

    # activate internal handlers
    # toggle doc colorizer
    revit.tabs.init_doc_colorizer(user_config)

    # activate runtime routes server
    if user_config.routes_server:
        routes.active_routes_api()
        active_server = routes.activate_server()
        if active_server:
            mlogger.info(str(active_server))
        else:
            mlogger.error('Routes servers failed activation')


def _new_session():
    """Create an assembly and UI for each installed UI extensions."""
    assembled_exts = []
    # get all installed ui extensions
    for ui_ext in extensionmgr.get_installed_ui_extensions():
        # configure extension components for metadata
        # e.g. liquid templates like {{author}}
        ui_ext.configure()

        # collect all module references from extensions
        ui_ext_modules = []
        # FIXME: currently dlls inside bin/ are not pre-loaded since
        # this will lock them by Revit. Maybe all dlls should be loaded
        # from memory (read binary and load assembly)?
        # ui_ext_modules.extend(ui_ext.get_extension_modules())
        ui_ext_modules.extend(ui_ext.get_command_modules())
        # make sure they are all loaded
        assmutils.load_asm_files(ui_ext_modules)
        # and update env information
        sessioninfo.update_loaded_pyrevit_referenced_modules(ui_ext_modules)

        # create a dll assembly and get assembly info
        ext_asm_info = asmmaker.create_assembly(ui_ext)
        if not ext_asm_info:
            mlogger.critical('Failed to create assembly for: %s', ui_ext)
            continue
        else:
            mlogger.info('Extension assembly created: %s', ui_ext.name)

        assembled_exts.append(
            AssembledExtension(ext=ui_ext, assm=ext_asm_info)
        )

    # add names of the created assemblies to the session info
    sessioninfo.set_loaded_pyrevit_assemblies(
        [x.assm.name for x in assembled_exts]
    )

    # run startup scripts for this ui extension, if any
    for assm_ext in assembled_exts:
        if assm_ext.ext.startup_script:
            # build syspaths for the startup script
            sys_paths = [assm_ext.ext.directory]
            if assm_ext.ext.library_path:
                sys_paths.insert(0, assm_ext.ext.library_path)

            mlogger.info('Running startup tasks for %s', assm_ext.ext.name)
            mlogger.debug('Executing startup script for extension: %s',
                          assm_ext.ext.name)

            # now run
            execute_extension_startup_script(
                assm_ext.ext.startup_script,
                assm_ext.ext.name,
                sys_paths=sys_paths
                )

    # register extension hooks
    for assm_ext in assembled_exts:
        hooks.register_hooks(assm_ext.ext)

    # update/create ui (needs the assembly to link button actions
    # to commands saved in the dll)
    for assm_ext in assembled_exts:
        uimaker.update_pyrevit_ui(
            assm_ext.ext,
            assm_ext.assm,
            user_config.load_beta
        )
        mlogger.info('UI created for extension: %s', assm_ext.ext.name)

    # re-sort the ui elements
    for assm_ext in assembled_exts:
        uimaker.sort_pyrevit_ui(assm_ext.ext)

    # cleanup existing UI. This is primarily for cleanups after reloading
    uimaker.cleanup_pyrevit_ui()

    # reflow the ui if requested, depending on the language direction
    if user_config.respect_language_direction:
        current_applocale = applocales.get_current_applocale()
        uimaker.reflow_pyrevit_ui(direction=current_applocale.lang_dir)
    else:
        uimaker.reflow_pyrevit_ui()


def load_session():
    """Handles loading/reloading of the pyRevit addin and extensions.

    To create a proper ui, pyRevit extensions needs to be properly parsed and
    a dll assembly needs to be created. This function handles these tasks
    through interactions with .extensions, .loader.asmmaker, and .loader.uimaker.

    Examples:
        ```python
        from pyrevit.loader.sessionmgr import load_session
        load_session()     # start loading a new pyRevit session
        ```

    Returns:
        (str): sesion uuid
    """
    # setup runtime environment variables
    sessioninfo.setup_runtime_vars()

    # the loader dll addon, does not create an output window
    # if an output window is not provided, create one
    if EXEC_PARAMS.first_load:
        output_window = _setup_output()
    else:
        from pyrevit import script
        output_window = script.get_output()

    # initialize timer to measure load time
    timer = Timer()

    # perform pre-load tasks
    _perform_onsessionloadstart_ops()

    # create a new session
    _new_session()

    # perform post-load tasks
    _perform_onsessionloadcomplete_ops()

    # log load time and thumbs-up :)
    endtime = timer.get_time()
    success_emoji = ':OK_hand:' if endtime < 3.00 else ':thumbs_up:'
    mlogger.info('Load time: %s seconds %s', endtime, success_emoji)

    # if everything went well, self destruct
    try:
        timeout = user_config.startuplog_timeout
        if timeout > 0 and not logger.loggers_have_errors():
            if EXEC_PARAMS.first_load:
                # output_window is of type ScriptConsole
                output_window.SelfDestructTimer(timeout)
            else:
                # output_window is of type PyRevitOutputWindow
                output_window.self_destruct(timeout)
    except Exception as imp_err:
        mlogger.error('Error setting up self_destruct on output window | %s',
                      imp_err)

    _cleanup_output()
    return sessioninfo.get_session_uuid()


def _perform_onsessionreload_ops():
    pass


def _perform_onsessionreloadcomplete_ops():
    pass


def reload_pyrevit():
    _perform_onsessionreload_ops()
    mlogger.info('Reloading....')
    session_Id = load_session()
    _perform_onsessionreloadcomplete_ops()
    return session_Id

# -----------------------------------------------------------------------------
# Functions related to finding/executing
# pyrevit command or script in current session
# -----------------------------------------------------------------------------
class PyRevitExternalCommandType(object):
    """PyRevit external command type."""
    def __init__(self, extcmd_type, extcmd_availtype):
        self._extcmd_type = extcmd_type
        self._extcmd = extcmd_type()
        if extcmd_availtype:
            self._extcmd_availtype = extcmd_availtype
            self._extcmd_avail = extcmd_availtype()
        else:
            self._extcmd_availtype = None
            self._extcmd_avail = None

    @property
    def extcmd_type(self):
        return self._extcmd_type

    @property
    def typename(self):
        return self._extcmd_type.FullName

    @property
    def extcmd_availtype(self):
        return self._extcmd_availtype

    @property
    def avail_typename(self):
        return self._extcmd_availtype.FullName

    @property
    def script(self):
        return getattr(self._extcmd.ScriptData, 'ScriptPath', None)

    @property
    def config_script(self):
        return getattr(self._extcmd.ScriptData, 'ConfigScriptPath', None)

    @property
    def search_paths(self):
        value = getattr(self._extcmd.ScriptRuntimeConfigs, 'SearchPaths', [])
        return list(value)

    @property
    def arguments(self):
        value = getattr(self._extcmd.ScriptRuntimeConfigs, 'Arguments', [])
        return list(value)

    @property
    def engine_cfgs(self):
        return getattr(self._extcmd.ScriptRuntimeConfigs, 'EngineConfigs', '')

    @property
    def helpsource(self):
        return getattr(self._extcmd.ScriptData, 'HelpSource', None)

    @property
    def tooltip(self):
        return getattr(self._extcmd.ScriptData, 'Tooltip', None)

    @property
    def name(self):
        return getattr(self._extcmd.ScriptData, 'CommandName', None)

    @property
    def bundle(self):
        return getattr(self._extcmd.ScriptData, 'CommandBundle', None)

    @property
    def extension(self):
        return getattr(self._extcmd.ScriptData, 'CommandExtension', None)

    @property
    def unique_id(self):
        return getattr(self._extcmd.ScriptData, 'CommandUniqueId', None)

    def is_available(self, category_set, zerodoc=False):
        if self._extcmd_availtype:
            return self._extcmd_avail.IsCommandAvailable(HOST_APP.uiapp,
                                                         category_set)
        elif not zerodoc:
            return True

        return False


pyrevit_extcmdtype_cache = []


def find_all_commands(category_set=None, cache=True):
    global pyrevit_extcmdtype_cache    #pylint: disable=W0603
    if cache and pyrevit_extcmdtype_cache:    #pylint: disable=E0601
        pyrevit_extcmds = pyrevit_extcmdtype_cache
    else:
        pyrevit_extcmds = []
        for loaded_assm_name in sessioninfo.get_loaded_pyrevit_assemblies():
            loaded_assm = assmutils.find_loaded_asm(loaded_assm_name)
            if loaded_assm:
                all_exported_types = loaded_assm[0].GetTypes()

                for pyrvt_type in all_exported_types:
                    tname = pyrvt_type.FullName
                    availtname = pyrvt_type.Name \
                                 + runtime.CMD_AVAIL_NAME_POSTFIX
                    pyrvt_availtype = None

                    if not tname.endswith(runtime.CMD_AVAIL_NAME_POSTFIX)\
                            and runtime.RUNTIME_NAMESPACE not in tname:
                        for exported_type in all_exported_types:
                            if exported_type.Name == availtname:
                                pyrvt_availtype = exported_type

                        pyrevit_extcmds.append(
                            PyRevitExternalCommandType(pyrvt_type,
                                                       pyrvt_availtype)
                            )
        if cache:
            pyrevit_extcmdtype_cache = pyrevit_extcmds

    # now check commands in current context if requested
    if category_set:
        return [x for x in pyrevit_extcmds
                if x.is_available(category_set=category_set,
                                  zerodoc=HOST_APP.uidoc is None)]
    else:
        return pyrevit_extcmds


def find_all_available_commands(use_current_context=True, cache=True):
    if use_current_context:
        cset = revit.get_selection_category_set()
    else:
        cset = None

    return find_all_commands(category_set=cset, cache=cache)


def find_pyrevitcmd(pyrevitcmd_unique_id):
    """Find a pyRevit command.

    Searches the pyRevit-generated assemblies under current session for
    the command with the matching unique name (class name) and returns the
    command type. Notice that this returned value is a 'type' and should be
    instantiated before use.

    Examples:
        ```python
        cmd = find_pyrevitcmd('pyRevitCorepyRevitpyRevittoolsReload')
        command_instance = cmd()
        command_instance.Execute() # Provide commandData, message, elements
        ```

    Args:
        pyrevitcmd_unique_id (str): Unique name for the command

    Returns:
        (type):Type for the command with matching unique name
    """
    # go through assmebles loaded under current pyRevit session
    # and try to find the command
    mlogger.debug('Searching for pyrevit command: %s', pyrevitcmd_unique_id)
    for loaded_assm_name in sessioninfo.get_loaded_pyrevit_assemblies():
        mlogger.debug('Expecting assm: %s', loaded_assm_name)
        loaded_assm = assmutils.find_loaded_asm(loaded_assm_name)
        if loaded_assm:
            mlogger.debug('Found assm: %s', loaded_assm_name)
            for pyrvt_type in loaded_assm[0].GetTypes():
                mlogger.debug('Found Type: %s', pyrvt_type)
                if pyrvt_type.FullName == pyrevitcmd_unique_id:
                    mlogger.debug('Found pyRevit command in %s',
                                  loaded_assm_name)
                    return pyrvt_type
            mlogger.debug('Could not find pyRevit command.')
        else:
            mlogger.debug('Can not find assm: %s', loaded_assm_name)

    return None


def create_tmp_commanddata():
    tmp_cmd_data = \
        framework.FormatterServices.GetUninitializedObject(
            UI.ExternalCommandData
            )
    tmp_cmd_data.Application = HOST_APP.uiapp
    # tmp_cmd_data.IsReadOnly = False
    # tmp_cmd_data.View = None
    # tmp_cmd_data.JournalData = None
    return tmp_cmd_data


def execute_command_cls(extcmd_type, arguments=None,
                        config_mode=False, exec_from_ui=False):

    command_instance = extcmd_type()
    # pass the arguments to the instance
    if arguments:
        command_instance.ScriptRuntimeConfigs.Arguments = \
            framework.List[str](arguments)
    # this is a manual execution from python code and not by user
    command_instance.ExecConfigs.MimicExecFromUI = exec_from_ui
    # force using the config script
    command_instance.ExecConfigs.UseConfigScript = config_mode

    # Execute(
    # ExternalCommandData commandData,
    # string message,
    # ElementSet elements
    # )
    re = command_instance.Execute(create_tmp_commanddata(),
                                  '',
                                  DB.ElementSet())
    command_instance = None
    return re


def execute_command(pyrevitcmd_unique_id):
    """Executes a pyRevit command.

    Args:
        pyrevitcmd_unique_id (str): Unique/Class Name of the pyRevit command
    """
    cmd_class = find_pyrevitcmd(pyrevitcmd_unique_id)

    if not cmd_class:
        mlogger.error('Can not find command with unique name: %s',
                      pyrevitcmd_unique_id)
        return None
    else:
        execute_command_cls(cmd_class)


def execute_extension_startup_script(script_path, ext_name, sys_paths=None):
    """Executes a script using pyRevit script executor.

    Args:
        script_path (str): Address of the script file
        ext_name (str): Name of the extension
        sys_paths (list): additional search paths
    """
    core_syspaths = [MAIN_LIB_DIR, MISC_LIB_DIR]
    if sys_paths:
        sys_paths.extend(core_syspaths)
    else:
        sys_paths = core_syspaths

    script_data = runtime.types.ScriptData()
    script_data.ScriptPath = script_path
    script_data.ConfigScriptPath = None
    script_data.CommandUniqueId = ''
    script_data.CommandName = 'Starting {}'.format(ext_name)
    script_data.CommandBundle = ''
    script_data.CommandExtension = ext_name
    script_data.HelpSource = ''

    script_runtime_cfg = runtime.types.ScriptRuntimeConfigs()
    script_runtime_cfg.CommandData = create_tmp_commanddata()
    script_runtime_cfg.SelectedElements = None
    script_runtime_cfg.SearchPaths = framework.List[str](sys_paths or [])
    script_runtime_cfg.Arguments = framework.List[str]([])
    script_runtime_cfg.EngineConfigs = \
        runtime.create_ipyengine_configs(
            clean=True,
            full_frame=True,
            persistent=True,
        )
    script_runtime_cfg.RefreshEngine = False
    script_runtime_cfg.ConfigMode = False
    script_runtime_cfg.DebugMode = False
    script_runtime_cfg.ExecutedFromUI = False

    runtime.types.ScriptExecutor.ExecuteScript(
        script_data,
        script_runtime_cfg
    )

"""Session diagnostics."""
from pyrevit import HOST_APP

from pyrevit.userconfig import user_config
from pyrevit.framework import DriveInfo, Path
from pyrevit.coreutils.logger import get_logger


#pylint: disable=W0703,C0302,C0103
mlogger = get_logger(__name__)


def check_min_host_version():
    # get required version and build from user config
    req_build = user_config.required_host_build
    if req_build:
        if HOST_APP.build != req_build:
            mlogger.warning('You are not using the required host build: %s',
                            req_build)


def check_host_drive_freespace():
    # get min free space from user config
    min_freespace = user_config.min_host_drivefreespace
    if min_freespace:
        # find host drive and check free space
        host_drive = Path.GetPathRoot(HOST_APP.proc_path)
        for drive in DriveInfo.GetDrives():
            if drive.Name == host_drive:
                free_hd_space = float(drive.TotalFreeSpace) / (1024 ** 3)

                if free_hd_space < min_freespace:
                    mlogger.warning('Remaining space on local drive '
                                    'is less than %sGB...', min_freespace)


def system_diag():
    """Verifies system status is appropriate for a pyRevit session."""
    # checking available drive space
    check_host_drive_freespace()

    # check if user is running the required host version and build
    check_min_host_version()

"""UI maker."""
import sys
import imp

from pyrevit import HOST_APP, EXEC_PARAMS, PyRevitException
from pyrevit.coreutils import assmutils
from pyrevit.coreutils.logger import get_logger
from pyrevit.coreutils import applocales

if not EXEC_PARAMS.doc_mode:
    from pyrevit.coreutils import ribbon

#pylint: disable=W0703,C0302,C0103,C0413
import pyrevit.extensions as exts
from pyrevit.extensions import components
from pyrevit.userconfig import user_config


mlogger = get_logger(__name__)


CONFIG_SCRIPT_TITLE_POSTFIX = u'\u25CF'


class UIMakerParams:
    """UI maker parameters.

    Args:
        par_ui (_PyRevitUI): Parent UI item
        par_cmp (GenericUIComponent): Parent UI component
        cmp_item (GenericUIComponent): UI component item
        asm_info (AssemblyInfo): Assembly info
        create_beta (bool, optional): Create beta button. Defaults to False
    """
    def __init__(self, par_ui, par_cmp, cmp_item, asm_info, create_beta=False):
        self.parent_ui = par_ui
        self.parent_cmp = par_cmp
        self.component = cmp_item
        self.asm_info = asm_info
        self.create_beta_cmds = create_beta


def _make_button_tooltip(button):
    tooltip = button.tooltip + '\n\n' if button.tooltip else ''
    tooltip += 'Bundle Name:\n{} ({})'\
        .format(button.name, button.type_id.replace('.', ''))
    if button.author:
        tooltip += '\n\nAuthor(s):\n{}'.format(button.author)
    return tooltip


def _make_button_tooltip_ext(button, asm_name):

    tooltip_ext = ''

    if button.min_revit_ver and not button.max_revit_ver:
        tooltip_ext += 'Compatible with {} {} and above\n\n'\
            .format(HOST_APP.proc_name,
                    button.min_revit_ver)

    if button.max_revit_ver and not button.min_revit_ver:
        tooltip_ext += 'Compatible with {} {} and earlier\n\n'\
            .format(HOST_APP.proc_name,
                    button.max_revit_ver)

    if button.min_revit_ver and button.max_revit_ver:
        if int(button.min_revit_ver) != int(button.max_revit_ver):
            tooltip_ext += 'Compatible with {} {} to {}\n\n'\
                .format(HOST_APP.proc_name,
                        button.min_revit_ver, button.max_revit_ver)
        else:
            tooltip_ext += 'Compatible with {} {} only\n\n'\
                .format(HOST_APP.proc_name,
                        button.min_revit_ver)

    if isinstance(button, (components.LinkButton, components.InvokeButton)):
        tooltip_ext += 'Class Name:\n{}\n\nAssembly Name:\n{}\n\n'.format(
            button.command_class or 'Runs first matching DB.IExternalCommand',
            button.assembly)
    else:
        tooltip_ext += 'Class Name:\n{}\n\nAssembly Name:\n{}\n\n'\
            .format(button.unique_name, asm_name)

    if button.control_id:
        tooltip_ext += 'Control Id:\n{}'\
            .format(button.control_id)

    return tooltip_ext


def _make_tooltip_ext_if_requested(button, asm_name):
    if user_config.tooltip_debug_info:
        return _make_button_tooltip_ext(button, asm_name)


def _make_ui_title(button):
    if button.has_config_script():
        return button.ui_title + ' {}'.format(CONFIG_SCRIPT_TITLE_POSTFIX)
    else:
        return button.ui_title


def _make_full_class_name(asm_name, class_name):
    if asm_name and class_name:
        return '{}.{}'.format(asm_name, class_name)
    return None


def _set_highlights(button, ui_item):
    ui_item.reset_highlights()
    if button.highlight_type == exts.MDATA_HIGHLIGHT_TYPE_UPDATED:
        ui_item.highlight_as_updated()
    elif button.highlight_type == exts.MDATA_HIGHLIGHT_TYPE_NEW:
        ui_item.highlight_as_new()


def _get_effective_classname(button):
    """Verifies if button has class_name set.

    This means that typemaker has created a executor type for this command.
    If class_name is not set, this function returns button.unique_name.
    This allows for the UI button to be created and linked to the previously 
    created assembly.
    If the type does not exist in the assembly, the UI button will not work,
    however this allows updating the command with the correct executor type,
    once command script has been fixed and pyrevit is reloaded.

    Args:
        button (pyrevit.extensions.genericcomps.GenericUICommand): button

    Returns:
        (str): class_name (or unique_name if class_name is None)
    """
    return button.class_name if button.class_name else button.unique_name


def _produce_ui_separator(ui_maker_params):
    """Create a separator.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_item = ui_maker_params.parent_ui
    ext_asm_info = ui_maker_params.asm_info

    if not ext_asm_info.reloading:
        mlogger.debug('Adding separator to: %s', parent_ui_item)
        try:
            if hasattr(parent_ui_item, 'add_separator'):    # re issue #361
                parent_ui_item.add_separator()
        except PyRevitException as err:
            mlogger.error('UI error: %s', err.msg)

    return None


def _produce_ui_slideout(ui_maker_params):
    """Create a slide out.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_item = ui_maker_params.parent_ui
    ext_asm_info = ui_maker_params.asm_info

    if not ext_asm_info.reloading:
        mlogger.debug('Adding slide out to: %s', parent_ui_item)
        try:
            parent_ui_item.add_slideout()
        except PyRevitException as err:
            mlogger.error('UI error: %s', err.msg)

    return None


def _produce_ui_smartbutton(ui_maker_params):
    """Create a smart button.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_item = ui_maker_params.parent_ui
    parent = ui_maker_params.parent_cmp
    smartbutton = ui_maker_params.component
    ext_asm_info = ui_maker_params.asm_info

    if not smartbutton.is_supported:
        return None

    if smartbutton.is_beta and not ui_maker_params.create_beta_cmds:
        return None

    mlogger.debug('Producing smart button: %s', smartbutton)
    try:
        parent_ui_item.create_push_button(
            button_name=smartbutton.name,
            asm_location=ext_asm_info.location,
            class_name=_get_effective_classname(smartbutton),
            icon_path=smartbutton.icon_file or parent.icon_file,
            tooltip=_make_button_tooltip(smartbutton),
            tooltip_ext=_make_tooltip_ext_if_requested(smartbutton,
                                                       ext_asm_info.name),
            tooltip_media=smartbutton.media_file,
            ctxhelpurl=smartbutton.help_url,
            avail_class_name=smartbutton.avail_class_name,
            update_if_exists=True,
            ui_title=_make_ui_title(smartbutton))
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None

    smartbutton_ui = parent_ui_item.button(smartbutton.name)

    mlogger.debug('Importing smart button as module: %s', smartbutton)
    try:
        # replacing EXEC_PARAMS.command_name value with button name so the
        # init script can log under its own name
        prev_commandname = \
            __builtins__['__commandname__'] \
            if '__commandname__' in __builtins__ else None
        prev_commandpath = \
            __builtins__['__commandpath__'] \
            if '__commandpath__' in __builtins__ else None
        prev_shiftclick = \
            __builtins__['__shiftclick__'] \
            if '__shiftclick__' in __builtins__ else False
        prev_debugmode = \
            __builtins__['__forceddebugmode__'] \
            if '__forceddebugmode__' in __builtins__ else False

        __builtins__['__commandname__'] = smartbutton.name
        __builtins__['__commandpath__'] = smartbutton.script_file
        __builtins__['__shiftclick__'] = False
        __builtins__['__forceddebugmode__'] = False
    except Exception as err:
        mlogger.error('Smart button setup error: %s | %s', smartbutton, err)
        return smartbutton_ui

    try:
        # setup sys.paths for the smart command
        current_paths = list(sys.path)
        for search_path in smartbutton.module_paths:
            if search_path not in current_paths:
                sys.path.append(search_path)

        # importing smart button script as a module
        importedscript = imp.load_source(smartbutton.unique_name,
                                         smartbutton.script_file)
        # resetting EXEC_PARAMS.command_name to original
        __builtins__['__commandname__'] = prev_commandname
        __builtins__['__commandpath__'] = prev_commandpath
        __builtins__['__shiftclick__'] = prev_shiftclick
        __builtins__['__forceddebugmode__'] = prev_debugmode
        mlogger.debug('Import successful: %s', importedscript)
        mlogger.debug('Running self initializer: %s', smartbutton)

        # reset sys.paths back to normal
        sys.path = current_paths

        res = False
        try:
            # running the smart button initializer function
            res = importedscript.__selfinit__(smartbutton,
                                              smartbutton_ui, HOST_APP.uiapp)
        except Exception as button_err:
            mlogger.error('Error initializing smart button: %s | %s',
                          smartbutton, button_err)

        # if the __selfinit__ function returns False
        # remove the button
        if res is False:
            mlogger.debug('SelfInit returned False on Smartbutton: %s',
                          smartbutton_ui)
            smartbutton_ui.deactivate()

        mlogger.debug('SelfInit successful on Smartbutton: %s', smartbutton_ui)
    except Exception as err:
        mlogger.error('Smart button script import error: %s | %s',
                      smartbutton, err)
        return smartbutton_ui

    _set_highlights(smartbutton, smartbutton_ui)

    return smartbutton_ui


def _produce_ui_linkbutton(ui_maker_params):
    """Create a link button.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_item = ui_maker_params.parent_ui
    parent = ui_maker_params.parent_cmp
    linkbutton = ui_maker_params.component
    ext_asm_info = ui_maker_params.asm_info

    if not linkbutton.is_supported:
        return None

    if linkbutton.is_beta and not ui_maker_params.create_beta_cmds:
        return None

    mlogger.debug('Producing button: %s', linkbutton)
    try:
        linked_asm = None
        # attemp to find the assembly file
        linked_asm_file = linkbutton.get_target_assembly()
        # if not found, search the loaded assemblies
        # this is usually a slower process
        if linked_asm_file:
            linked_asm = assmutils.load_asm_file(linked_asm_file)
        else:
            linked_asm_list = assmutils.find_loaded_asm(linkbutton.assembly)
            # cancel button creation if not found
            if not linked_asm_list:
                mlogger.error("Can not find target assembly for %s", linkbutton)
                return None
            linked_asm = linked_asm_list[0]

        linked_asm_name = linked_asm.GetName().Name
        parent_ui_item.create_push_button(
            button_name=linkbutton.name,
            asm_location=linked_asm.Location,
            class_name=_make_full_class_name(
                linked_asm_name,
                linkbutton.command_class
                ),
            icon_path=linkbutton.icon_file or parent.icon_file,
            tooltip=_make_button_tooltip(linkbutton),
            tooltip_ext=_make_tooltip_ext_if_requested(linkbutton,
                                                       ext_asm_info.name),
            tooltip_media=linkbutton.media_file,
            ctxhelpurl=linkbutton.help_url,
            avail_class_name=_make_full_class_name(
                linked_asm_name,
                linkbutton.avail_command_class
                ),
            update_if_exists=True,
            ui_title=_make_ui_title(linkbutton))
        linkbutton_ui = parent_ui_item.button(linkbutton.name)

        _set_highlights(linkbutton, linkbutton_ui)

        return linkbutton_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_pushbutton(ui_maker_params):
    """Create a push button.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_item = ui_maker_params.parent_ui
    parent = ui_maker_params.parent_cmp
    pushbutton = ui_maker_params.component
    ext_asm_info = ui_maker_params.asm_info

    if not pushbutton.is_supported:
        return None

    if pushbutton.is_beta and not ui_maker_params.create_beta_cmds:
        return None

    mlogger.debug('Producing button: %s', pushbutton)
    try:
        parent_ui_item.create_push_button(
            button_name=pushbutton.name,
            asm_location=ext_asm_info.location,
            class_name=_get_effective_classname(pushbutton),
            icon_path=pushbutton.icon_file or parent.icon_file,
            tooltip=_make_button_tooltip(pushbutton),
            tooltip_ext=_make_tooltip_ext_if_requested(pushbutton,
                                                       ext_asm_info.name),
            tooltip_media=pushbutton.media_file,
            ctxhelpurl=pushbutton.help_url,
            avail_class_name=pushbutton.avail_class_name,
            update_if_exists=True,
            ui_title=_make_ui_title(pushbutton))
        pushbutton_ui = parent_ui_item.button(pushbutton.name)

        _set_highlights(pushbutton, pushbutton_ui)

        return pushbutton_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_pulldown(ui_maker_params):
    """Create a pulldown button.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ribbon_panel = ui_maker_params.parent_ui
    pulldown = ui_maker_params.component

    mlogger.debug('Producing pulldown button: %s', pulldown)
    try:
        parent_ribbon_panel.create_pulldown_button(pulldown.ui_title,
                                                   pulldown.icon_file,
                                                   update_if_exists=True)
        pulldown_ui = parent_ribbon_panel.ribbon_item(pulldown.ui_title)

        _set_highlights(pulldown, pulldown_ui)

        return pulldown_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_split(ui_maker_params):
    """Produce a split button.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ribbon_panel = ui_maker_params.parent_ui
    split = ui_maker_params.component

    mlogger.debug('Producing split button: %s}', split)
    try:
        parent_ribbon_panel.create_split_button(split.ui_title,
                                                split.icon_file,
                                                update_if_exists=True)
        split_ui = parent_ribbon_panel.ribbon_item(split.ui_title)

        _set_highlights(split, split_ui)

        return split_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_splitpush(ui_maker_params):
    """Create split push button.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ribbon_panel = ui_maker_params.parent_ui
    splitpush = ui_maker_params.component

    mlogger.debug('Producing splitpush button: %s', splitpush)
    try:
        parent_ribbon_panel.create_splitpush_button(splitpush.ui_title,
                                                    splitpush.icon_file,
                                                    update_if_exists=True)
        splitpush_ui = parent_ribbon_panel.ribbon_item(splitpush.ui_title)

        _set_highlights(splitpush, splitpush_ui)

        return splitpush_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_stacks(ui_maker_params):
    """Create a stack of ui items.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_panel = ui_maker_params.parent_ui
    stack_parent = ui_maker_params.parent_cmp
    stack_cmp = ui_maker_params.component
    ext_asm_info = ui_maker_params.asm_info

    # if sub_cmp is a stack, ask parent_ui_item to open a stack
    # (parent_ui_item.open_stack).
    # All subsequent items will be placed under this stack. Close the stack
    # (parent_ui_item.close_stack) to finish adding items to the stack.
    try:
        parent_ui_panel.open_stack()
        mlogger.debug('Opened stack: %s', stack_cmp.name)

        if HOST_APP.is_older_than('2017'):
            _component_creation_dict[exts.SPLIT_BUTTON_POSTFIX] = \
                _produce_ui_pulldown
            _component_creation_dict[exts.SPLITPUSH_BUTTON_POSTFIX] = \
                _produce_ui_pulldown

        # capturing and logging any errors on stack item
        # (e.g when parent_ui_panel's stack is full and can not add any
        # more items it will raise an error)
        _recursively_produce_ui_items(
            UIMakerParams(parent_ui_panel,
                          stack_parent,
                          stack_cmp,
                          ext_asm_info,
                          ui_maker_params.create_beta_cmds))

        if HOST_APP.is_older_than('2017'):
            _component_creation_dict[exts.SPLIT_BUTTON_POSTFIX] = \
                _produce_ui_split
            _component_creation_dict[exts.SPLITPUSH_BUTTON_POSTFIX] = \
                _produce_ui_splitpush

        try:
            parent_ui_panel.close_stack()
            mlogger.debug('Closed stack: %s', stack_cmp.name)
            return stack_cmp
        except PyRevitException as err:
            mlogger.error('Error creating stack | %s', err)

    except Exception as err:
        mlogger.error('Can not create stack under this parent: %s | %s',
                      parent_ui_panel, err)


def _produce_ui_panelpushbutton(ui_maker_params):
    """Create a push button with the given parameters.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui_item = ui_maker_params.parent_ui
    # parent = ui_maker_params.parent_cmp
    panelpushbutton = ui_maker_params.component
    ext_asm_info = ui_maker_params.asm_info

    if panelpushbutton.is_beta and not ui_maker_params.create_beta_cmds:
        return None

    mlogger.debug('Producing panel button: %s', panelpushbutton)
    try:
        parent_ui_item.create_panel_push_button(
            button_name=panelpushbutton.name,
            asm_location=ext_asm_info.location,
            class_name=_get_effective_classname(panelpushbutton),
            tooltip=_make_button_tooltip(panelpushbutton),
            tooltip_ext=_make_tooltip_ext_if_requested(panelpushbutton,
                                                       ext_asm_info.name),
            tooltip_media=panelpushbutton.media_file,
            ctxhelpurl=panelpushbutton.help_url,
            avail_class_name=panelpushbutton.avail_class_name,
            update_if_exists=True)

        panelpushbutton_ui = parent_ui_item.button(panelpushbutton.name)

        _set_highlights(panelpushbutton, panelpushbutton_ui)

        return panelpushbutton_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_panels(ui_maker_params):
    """Create a panel with the given parameters.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.

    Returns:
        (RevitNativeRibbonPanel): The created panel
    """
    parent_ui_tab = ui_maker_params.parent_ui
    panel = ui_maker_params.component

    if panel.is_beta and not ui_maker_params.create_beta_cmds:
        return None

    mlogger.debug('Producing ribbon panel: %s', panel)
    try:
        parent_ui_tab.create_ribbon_panel(panel.name, update_if_exists=True)
        panel_ui = parent_ui_tab.ribbon_panel(panel.name)

        # set backgrounds
        panel_ui.reset_backgrounds()
        if panel.panel_background:
            panel_ui.set_background(panel.panel_background)
        # override the title background if exists
        if panel.title_background:
            panel_ui.set_title_background(panel.title_background)
        # override the slideout background if exists
        if panel.slideout_background:
            panel_ui.set_slideout_background(panel.slideout_background)

        _set_highlights(panel, panel_ui)

        panel_ui.set_collapse(panel.collapsed)

        return panel_ui
    except PyRevitException as err:
        mlogger.error('UI error: %s', err.msg)
        return None


def _produce_ui_tab(ui_maker_params):
    """Create a tab with the given parameters.

    Args:
        ui_maker_params (UIMakerParams): Standard parameters for making ui item.
    """
    parent_ui = ui_maker_params.parent_ui
    tab = ui_maker_params.component

    mlogger.debug('Verifying tab: %s', tab)
    if tab.has_commands():
        mlogger.debug('Tabs has command: %s', tab)
        mlogger.debug('Producing ribbon tab: %s', tab)
        try:
            parent_ui.create_ribbon_tab(tab.name, update_if_exists=True)
            tab_ui = parent_ui.ribbon_tab(tab.name)

            _set_highlights(tab, tab_ui)

            return tab_ui
        except PyRevitException as err:
            mlogger.error('UI error: %s', err.msg)
            return None
    else:
        mlogger.debug('Tab does not have any commands. Skipping: %s', tab.name)
        return None


_component_creation_dict = {
    exts.TAB_POSTFIX: _produce_ui_tab,
    exts.PANEL_POSTFIX: _produce_ui_panels,
    exts.STACK_BUTTON_POSTFIX: _produce_ui_stacks,
    exts.PULLDOWN_BUTTON_POSTFIX: _produce_ui_pulldown,
    exts.SPLIT_BUTTON_POSTFIX: _produce_ui_split,
    exts.SPLITPUSH_BUTTON_POSTFIX: _produce_ui_splitpush,
    exts.PUSH_BUTTON_POSTFIX: _produce_ui_pushbutton,
    exts.SMART_BUTTON_POSTFIX: _produce_ui_smartbutton,
    exts.CONTENT_BUTTON_POSTFIX: _produce_ui_pushbutton,
    exts.URL_BUTTON_POSTFIX: _produce_ui_pushbutton,
    exts.LINK_BUTTON_POSTFIX: _produce_ui_linkbutton,
    exts.INVOKE_BUTTON_POSTFIX: _produce_ui_pushbutton,
    exts.SEPARATOR_IDENTIFIER: _produce_ui_separator,
    exts.SLIDEOUT_IDENTIFIER: _produce_ui_slideout,
    exts.PANEL_PUSH_BUTTON_POSTFIX: _produce_ui_panelpushbutton,
    }


def _recursively_produce_ui_items(ui_maker_params):
    cmp_count = 0
    for sub_cmp in ui_maker_params.component:
        ui_item = None
        try:
            mlogger.debug('Calling create func %s for: %s',
                          _component_creation_dict[sub_cmp.type_id],
                          sub_cmp)
            ui_item = _component_creation_dict[sub_cmp.type_id](
                UIMakerParams(ui_maker_params.parent_ui,
                              ui_maker_params.component,
                              sub_cmp,
                              ui_maker_params.asm_info,
                              ui_maker_params.create_beta_cmds))
            if ui_item:
                cmp_count += 1
        except KeyError:
            mlogger.debug('Can not find create function for: %s', sub_cmp)
        except Exception as create_err:
            mlogger.critical(
                'Error creating item: %s | %s', sub_cmp, create_err
            )

        mlogger.debug('UI item created by create func is: %s', ui_item)

        if ui_item \
                and not isinstance(ui_item, components.GenericStack) \
                and sub_cmp.is_container:
            subcmp_count = _recursively_produce_ui_items(
                UIMakerParams(ui_item,
                              ui_maker_params.component,
                              sub_cmp,
                              ui_maker_params.asm_info,
                              ui_maker_params.create_beta_cmds))

            # if component does not have any sub components hide it
            if subcmp_count == 0:
                ui_item.deactivate()

    return cmp_count


if not EXEC_PARAMS.doc_mode:
    current_ui = ribbon.get_current_ui()


def update_pyrevit_ui(ui_ext, ext_asm_info, create_beta=False):
    """Updates/Creates pyRevit ui for the extension and assembly dll address.

    Args:
        ui_ext (GenericUIContainer): UI container.
        ext_asm_info (AssemblyInfo): Assembly info.
        create_beta (bool, optional): Create beta ui. Defaults to False.
    """
    mlogger.debug('Creating/Updating ui for extension: %s', ui_ext)
    cmp_count = _recursively_produce_ui_items(
        UIMakerParams(current_ui, None, ui_ext, ext_asm_info, create_beta))
    mlogger.debug('%s components were created for: %s', cmp_count, ui_ext)


def sort_pyrevit_ui(ui_ext):
    """Sorts pyRevit UI.

    Args:
        ui_ext (GenericUIContainer): UI container.
    """
    # only works on panels so far
    # re-ordering of ui components deeper than panels have not been implemented
    for tab in current_ui.get_pyrevit_tabs():
        for litem in ui_ext.find_layout_items():
            if litem.directive:
                if litem.directive.directive_type == 'before':
                    tab.reorder_before(litem.name, litem.directive.target)
                elif litem.directive.directive_type == 'after':
                    tab.reorder_after(litem.name, litem.directive.target)
                elif litem.directive.directive_type == 'afterall':
                    tab.reorder_afterall(litem.name)
                elif litem.directive.directive_type == 'beforeall':
                    tab.reorder_beforeall(litem.name)


def cleanup_pyrevit_ui():
    """Cleanup the pyrevit UI.

    Hide all items that were not touched after a reload
    meaning they have been removed in extension folder structure
    and thus are not updated.
    """
    untouched_items = current_ui.get_unchanged_items()
    for item in untouched_items:
        if not item.is_native():
            try:
                mlogger.debug('Deactivating: %s', item)
                item.deactivate()
            except Exception as deact_err:
                mlogger.debug(deact_err)



def reflow_pyrevit_ui(direction=applocales.DEFAULT_LANG_DIR):
    """Set the flow direction of the tabs."""
    if direction == "LTR":
        current_ui.set_LTR_flow()
    elif direction == "RTL":
        current_ui.set_RTL_flow()
