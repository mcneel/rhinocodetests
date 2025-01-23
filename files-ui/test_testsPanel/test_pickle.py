#! python3
import pickle
import pickle_functions as pf


s = pickle.dumps(pf.Good_T)

result = s is not None