# r: pulp
# https://discourse.mcneel.com/t/python-pulp-cant-set-external-solver/179055/2
from pulp import *

# Some problem from the pulp documentation page
prob = LpProblem("TheWhiskasProblem", LpMinimize)

x1 = LpVariable("ChickenPercent", 0, None, LpInteger)
x2 = LpVariable("BeefPercent", 0)

prob += 0.013 * x1 + 0.008 * x2, "Total Cost of Ingredients per can"

prob += x1 + x2 == 100, "PercentagesSum"
prob += 0.100 * x1 + 0.200 * x2 >= 8.0, "ProteinRequirement"
prob += 0.080 * x1 + 0.100 * x2 >= 6.0, "FatRequirement"
prob += 0.001 * x1 + 0.005 * x2 <= 2.0, "FibreRequirement"
prob += 0.002 * x1 + 0.005 * x2 <= 0.4, "SaltRequirement"

# Setting the solver to HiGHS now throws an error
path_highs = "/opt/homebrew/Cellar/highs/1.7.0/bin/highs"
solver_HiGHS = HiGHS_CMD(path=path_highs, timeLimit=30)
solver_CBC = PULP_CBC_CMD(timeLimit=30)

c = prob.solve(solver_HiGHS)
print(c)
#c = prob.solve(solver_CBC)
