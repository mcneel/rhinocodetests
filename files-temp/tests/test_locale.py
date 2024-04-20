#! python 3
from Rhino.Runtime.Code import RhinoCode as R
import locale

defaultLocale = locale.getlocale()
thisLocale = locale.getlocale()
if defaultLocale[0] == "en-US" \
    or thisLocale[0] == "en-US":
    locale.setlocale("en_US")
    R.Logger.Warn("updated locale from en-US to en_US")
R.Logger.Info(f"Python locale is {thisLocale}")