#! python3

result = False
_, context = __this__.TryGetContext()
if context.CompileGuards.Contains("RHINO_8_OR_GREATER"):
    result = True
