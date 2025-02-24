#! python3
# requirements: termcolor

from termcolor import colored, cprint

text = colored('Hello, World!', 'red', attrs=['reverse', 'blink'])
print(text)
cprint('Hello, World!', 'yellow', 'on_red')


def print_red_on_cyan(x):
    cprint(x, 'red', 'on_cyan')


print_red_on_cyan('Hello, World!')
print_red_on_cyan('Hello, Universe!')

print("😍")
