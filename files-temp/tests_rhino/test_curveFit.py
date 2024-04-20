#! python3
# venv: curvefit
# requirements: jaxlib, jaxopt, jax
import numpy as np
import math
import cmath
import scipy.integrate as integrate
from scipy.special import gamma
import scipy.interpolate as interp
import jax.numpy as jnp
from jax import grad, jit, vmap
from jaxopt import GaussNewton,LevenbergMarquardt, GradientDescent
from jaxopt import ProjectedGradient
from jaxopt.projection import projection_non_negative
from jax import config
import rhinoscriptsyntax as rs

config.update("jax_enable_x64", True)


def polyline_fourier_coeffs(obj, fourier_points):
    points = rs.PolylineVertices(obj)
    out = []

    for alpha, beta in fourier_points:
        acc = 0j
        prev = points[0]
        length_acc = 0
        for i in range(1,len(points)):
            p = points[i]
            dx = p.X - prev.X 
            dy = p.Y - prev.Y
            span = 1j * (alpha * dx + beta * dy)
            acc += dy * cmath.exp(1j * (alpha * prev.X + beta * prev.Y)) * (cmath.exp(span) - 1) / span
            prev = p
        out.append(acc)
    return np.array(out)

def initial_guess(obj, n):
    d = rs.CurveDomain(obj)
    x = np.zeros(n)
    y = np.zeros(n)
    for i, t in enumerate(np.linspace(0,1,n)):
        p = rs.EvaluateCurve(obj, d[0] + t * (d[1] - d[0]))
        x[i] = p.X
        y[i] = p.Y
    return x, y, np.ones(n) 

def uniform_knot(ncontrol, degree):
    m = ncontrol + degree + 1
    knots = np.arange(m, dtype = float) - degree
    knots[0:degree] = 0
    knots[-degree:] = knots[-degree - 1]
    knots /= np.max(knots)
    return knots

def add_flat_curve(x,y,w,degree = 3):
    knots = uniform_knot(len(x), degree)[1:-1]
    pts = []
    for i,px in enumerate(x):
        pts.append((px,y[i],0))
    rs.AddNurbsCurve(pts, knots, degree, list(w))

def uniform_bspline_eval_matrices(eval_points, degree, ncontrol):
    # Cook up a knot vector
    m = ncontrol + degree + 1
    knots = np.arange(m, dtype = float) - degree
    knots[0:degree] = 0
    knots[-degree:] = knots[-degree - 1]
    knots /= np.max(knots)
    # Use it to compute weights for the derivative matrix
    weights = np.zeros(ncontrol - 1)
    for i in range(ncontrol - 1):
        weights[i] = degree / (knots[i+degree+1] - knots[i+1])
    # Then compute the derivative design matrix - a lower-order B spline
    # matrix, coupled with a differential opperator and some scaling
    diff = np.zeros((ncontrol - 1, ncontrol))
    for i in range(ncontrol - 1):
        w = weights[i]
        diff[i,i] = -1 * w
        diff[i,i+1] = w
    # And compute the design matrix for the k-1 degree bspline
    dm = interp.BSpline.design_matrix(eval_points, knots[1:-1], degree-1).toarray()

    return interp.BSpline.design_matrix(eval_points, knots, degree).toarray(), dm @ diff 

def fixed_quad_coeffs(qdegree, bdegree, ncontrol, points):
    """ Takes the number of quadrature points to use, the B-spline degree, and a list of points
    to evaluate ft at.
    
    Returns an evaluator that takes control point coordinates and returns fourier coefficients. """
    
    
    t,cheb_w = np.polynomial.legendre.leggauss(qdegree)
    cheb_w = 0.5 * cheb_w
    t = 0.5 * (t + 1)
    
    B, dB = uniform_bspline_eval_matrices(t, bdegree, ncontrol)
    
    def f(px,py,pw):
        def g(omega):
            w = B @ pw
            dw = dB @ pw

            x = B @ (pw * px)
            y = B @ (pw * py)
            dy = dB @ (pw * py)
            
            dydt = (dy * w - y * dw) / (w**2)
            
            return  cheb_w @ (dydt * jnp.exp(1j * (omega[0] * x + omega[1] * y) / w))
            
        new = vmap(g,0,0)(points)
        return new 
    
    return f

def optimized_control_points(value_to_control, initial_value, eval_points, reference, epsilon = 1e-9):
    x_0,_,_ = value_to_control(initial_value)
    bezier_eval = fixed_quad_coeffs(30, 3, len(x_0), eval_points)
    
    def objective(z):
        x,y,w = value_to_control(z)
        residual = bezier_eval(x,y, w) - reference
        regularization = epsilon * jnp.sum(jnp.diff(x)**2 + jnp.diff(y)**2)
        return jnp.real(jnp.sum(residual * jnp.conj(residual))) + regularization
    
    solver = GradientDescent(fun=objective, maxiter = 100000, tol = 1e-9)
    return solver.run(initial_value).params


degree= 3
fourier_points = np.array([(1.0,0),(0,1.0),(1,1),(2.0,0),(0,2.0),(1.0,2),(2,1),(2,2),(3,1),(1,3),(3,2),(3,2),(3,3)])

obj = rs.GetCurveObject()[0]

reference_coeffs = polyline_fourier_coeffs(obj, fourier_points)

def fixed_endpoint_condition(x0,y0,w0):

    n = len(x0)
    x_matrix = np.zeros((n,3 * (n - 2)))
    y_matrix = np.zeros((n,3 * (n - 2)))
    w_matrix = np.zeros((n,3 * (n - 2)))

    x_fixed = x0.copy()
    y_fixed = y0.copy()
    w_fixed = w0.copy()
    x_fixed[1:-1] = 0
    y_fixed[1:-1] = 0
    w_fixed[1:-1] = 0

    z0 = np.hstack([x0[1:-1],y0[1:-1],w0[1:-1]])
    for i in range(n - 2):
        x_matrix[i+1,i] = 1
        y_matrix[i+1, i + n - 2] = 1
        w_matrix[i+1, i + 2 * (n - 2)] = 1


    print(y_fixed + y_matrix @ z0)

    def f(z):
        return x_fixed + x_matrix @ z, y_fixed + y_matrix @ z, w_fixed + w_matrix @ z,

    return z0, f

z0, f = fixed_endpoint_condition(*initial_guess(obj, 6))

sol = optimized_control_points(f, z0, fourier_points, reference_coeffs)

x,y,w = f(sol)
add_flat_curve(x,y,np.array(w))