#! python 3

def recursive_function(n, sum):
    if n < 1:
        return sum
    else:
        return recursive_function(n-1, sum+n)

c = 998
print(recursive_function(c, 0))