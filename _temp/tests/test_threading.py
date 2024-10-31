import os
import os.path as op
import time
import threading as t

log_file = op.join(op.dirname(__file__), 'log.log')

def main():
    while True:
        time.sleep(2)
        with open(log_file, 'a') as f:
            f.write(f"{t.get_ident()}\n");


for i in range(2):
    # background
    # t.Thread(target=main, daemon=True).start()
    t.Thread(target=main).start()

# time.sleep(6)
# input()
