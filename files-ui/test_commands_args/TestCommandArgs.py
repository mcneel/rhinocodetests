#! python3

# special vars should be available when project is open
# and this code belongs to the project
# and NOT available otherwise
print(f"{__rhino_command__}\n")
print(f"{__rhino_doc__}\n")
print(f"{__rhino_runmode__}\n")
print(f"{__is_interactive__}\n")
