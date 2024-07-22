#r: scipy, numpy, jax[cpu], jaxopt

import numpy as np
import scipy.interpolate as interp
from scipy.linalg import solve
from scipy.sparse.linalg import spsolve

# Enable float 64 computation
import jax
jax.config.update("jax_enable_x64", True)

from jax import grad
from jax import vmap
from jax import jit
import jax.numpy as jnp
import jaxopt

import Rhino
import Rhino.Geometry as rg
from Rhino.Input.Custom import GetObject
from Rhino.Input.Custom import OptionInteger
from Rhino.Input.Custom import OptionToggle 

import rhinoscriptsyntax as rs


def knots_number_superfluous(num_control, degree):
    """Calculate the number of knots in a nurbs curve."""
    return num_control + degree + 1


def knots_number(num_control, degree):
    """Calculate the number of knots in a nurbs curve following the Rhino convention."""
    return num_control + degree - 1


def knots_uniform(num_control, degree):
    """Create a uniform clamped knot vector."""
    m = knots_number(num_control, degree)

    knots = np.arange(m)
    
    return knots / np.max(knots)


def knots_uniform_clamped(num_control, degree):
    """Create a uniform clamped knot vector."""
    m = knots_number(num_control, degree)

    knots = np.arange(m - degree - 1)    
    knot_max = np.max(knots)

    knots_domain = knots / knot_max
    knots_padding = np.zeros(degree - 1)

    knots = np.concatenate((
        knots_padding,
        knots_domain,
        1 + knots_padding))

    return knots    


def knots_uniform_clamped_superfluous(num_control, degree):
    """Cook up a knot vector with 2 superfluos knots and clamped ends."""
    m = knots_number_superfluous(num_control, degree)

    knots = np.arange(m) - float(degree)

    knots[0:degree] = 0
    knots[-degree:] = knots[-degree - 1]

    return knots / np.max(knots)


def knots_uniform_periodic(num_control, degree):
    """Create a uniform periodic knot vector."""
    m = knots_number(num_control, degree)

    knots = np.arange(m - 2 * (degree - 1))
    knot_max = np.max(knots)

    knots_domain = knots / knot_max
    knots_padding = np.arange(1, degree) / knot_max

    knots = np.concatenate((
        knots_padding[::-1] * -1.0,
        knots_domain,
        1 + knots_padding))
    
    assert knots.size == m, f"{knots.size=} is not {m}"

    return knots


def knots_uniform_periodic_superfluous(num_control, degree):
    """Create a uniform periodic knot vector."""
    m = knots_number(num_control, degree)

    knots = np.arange(m - 2 * (degree - 1))
    knot_max = np.max(knots)

    knots_domain = knots / knot_max
    knots_padding = np.arange(1, degree + 1) / knot_max

    knots = np.concatenate((
        knots_padding[::-1] * -1.0,
        knots_domain,
        1 + knots_padding))
    
    assert knots.size == m + 2, f"{knots.size=} is not {m + 2}"
    
    return knots


def knots_from_rhino_curve(rh_curve):
    """Extract the knot vector of a rhino curve and return it as a numpy array."""
    return list(rh_curve.Knots)


def knots_from_rhino_curve_superfluous(rh_curve):
    """Extract the knot vector (with superfluous knots) of a rhino curve and return it as a numpy array."""
    knots = knots_from_rhino_curve(rh_curve)

    return knots[:1] + knots + knots[-1:]


def bspline_matrix_uniform(params, degree, num_control, as_dense_array=True):
    """Generate the evaluation matrix of a B-Spline given a knot vector and a degree.
    The number of number of control points, knots and curve degree must be compatible.
    """
    knots = knots_uniform_clamped_superfluous(num_control, degree)

    return bspline_matrix(params, degree, num_control, knots, as_dense_array)


def bspline_matrix_from_rhino_curve(params, rh_curve, as_dense_array=True):
    """Generate the evaluation matrix of a B-Spline given a knot vector and a degree.
    The number of number of control points, knots and curve degree must be compatible.
    """
    knots = knots_from_rhino_curve_superfluous(rh_curve)    
    degree = rh_curve.Degree
    num_control = len(rh_curve.Points)

    bmatrix = bspline_matrix(params, degree, num_control, knots, as_dense_array)

    if rh_curve.IsRational:
        weights = np.array([point.Weight for point in rh_curve.Points])
        return bspline_matrix_rational(params, degree, num_control, knots, weights, as_dense_array)

    return bmatrix


def bspline_matrix(params, degree, num_control, knots, as_dense_array=True):
    """Generate the evaluation matrix of a B-Spline given a knot vector and a degree.
    The number of number of control points, knots and curve degree must be compatible.
    """
    B = interp.BSpline.design_matrix(params, knots, degree)
    
    if as_dense_array:
        return B.toarray()

    return B


def bspline_matrix_rational(params, degree, num_control, knots, weights, as_dense_array=True):
    """Generate the evaluation matrix of a B-Spline given a knot vector and a degree.
    The number of number of control points, knots and curve degree must be compatible.
    """
    B = bspline_matrix(params, degree, num_control, knots, as_dense_array)
    R_sum = B @ weights
    return (B * weights) / np.reshape(R_sum, (-1, 1))    


def rhino_curve_control_points(rh_curve):
    """Extract the control points of a curve."""
    control_polygon = rh_curve.Points.ControlPolygon()
    control_points = [rh_curve.Points.GetPoint(i)[1] for i in range(control_polygon.Count)]

    return control_points


def rhino_curve_control_points_array(rh_curve):
    """Extract the control points of a curve and return a numpy array containing them."""
    control_points = rhino_curve_control_points(rh_curve)

    return rhino_points_to_array(control_points)


def rhino_curve_points_at(rh_curve, params):
    """Evaluate points on a Rhino curve at parameters t."""
    return [rh_curve.PointAt(t) for t in params]


def rhino_curve_curvatures_at(rh_curve, params):
    """Evaluate curvature vectors on a Rhino curve at parameters t."""
    return [rh_curve.CurvatureAt(t) for t in params]


def rhino_curve_points_at_numpy(rh_curve, params, B, C, as_dense_array=True):
    """Evaluate points on a Rhino curve at parameters t using a numpy backend."""
    B = bspline_matrix_from_rhino_curve(params, rh_curve, as_dense_array)
    C = rhino_curve_control_points_array(rh_curve)

    return curve_points_at_numpy(B, C)


def curve_points_at_numpy(B, C):
    """Evaluate points on a curve defined by a bspline matrix and a matrix of control points, using a numpy backend."""
    return B @ C


def rhino_points_to_array(rhino_points):
    """Convert Rhino points to a numpy array."""
    return jnp.array([[pt.X, pt.Y, pt.Z] for pt in rhino_points])


def points_array_to_rhino(points):
    """Convert a numpy array of points to a list of Rhino points"""
    def point_to_rhino_point(point):
        point = point.tolist()
        return rg.Point3d(*point)

    return [point_to_rhino_point(pt) for pt in points]


def create_parameters_uniform(n_params):
    """Create a uniformy-spaced parameters in the interval (0, 1)"""
    return np.linspace(0.0, 1.0, n_params)


def create_parameters_uniform_arclength(rh_curve, n_params):
    """Create a uniformy-spaced arc-length parameters."""
    return np.array(rh_curve.DivideByCount(n_params - 1, True))


def calculate_weights_curvature(rh_curve, params):
    """Calculate a normalized vector (0-1) of curvature-based weights."""
    curvatures_on_target = rhino_curve_curvatures_at(rh_curve, params)
    w_target = np.linalg.norm(rhino_points_to_array(curvatures_on_target), axis=-1)
    w_target = w_target / np.max(w_target)

    return w_target


def create_parameters_closest_point(rh_curve, points):
    """Create a vector of curve parameters on a curve based on a list of points."""
    params = []
    for point in points:
        _, t = rh_curve.ClosestPoint(point)
        params.append(t)
    return np.array(params)


def create_parameters_closest_point_local(rh_curve, points, params_test):
    """Create a vector of curve parameters on a curve based on a list of points."""
    params = []
    assert len(points) == len(params_test), f"{len(points)=} vs. {len(params)=}"
    for point, param in zip(points, params_test):
        _, t = rh_curve.LocalClosestPoint(point, param)
        params.append(t)
    return np.array(params)


def calculate_curve_endpoints_array(rh_curve):
    """Calculate an array with the endpoints of a Rhino curve."""
    control_points = rhino_curve_control_points(rh_curve)
    C_target = rhino_points_to_array(control_points)

    return C_target[(0, -1), :]
        

def calculate_curve_endtangents_array(rh_curve):
    """Calculate an array with the endpoints of a Rhino curve."""
    tangents = []
    for t, direction in zip(rh_curve.Domain, (1.0, -1.0)):
        tangent = rh_curve.TangentAt(t) * direction
        tangents.append([tangent.X, tangent.Y, tangent.Z])
    
    return jnp.array(tangents)
        

def calculate_curve_endtangents_scales_array(rh_curve):
    """Calculate an array with the tangent scales of a Rhino curve."""
    control_points = rhino_curve_control_points(rh_curve)
    C = rhino_points_to_array(control_points)
    V = C[1:,:] - C[:-1,:]
    
    return jnp.linalg.norm(V[(0, -1), :], axis=-1)


def calculate_control_polygon_endtangents_scales_array(C):
    """Calculate an array with the tangent scales of control points array."""    
    V = C[1:,:] - C[:-1,:]
    
    return jnp.linalg.norm(V[(0, -1), :], axis=-1)


def calculate_bspline_matrix_free(bmatrix, indices):
    """Select the free columns of a bspline matrix."""
    return bmatrix[:, indices]


def calculate_bspline_matrix_fixed(bmatrix, indices):
    """Select the fixed columns of a bspline matrix."""
    return bmatrix[:, indices]


def build_rhino_curve(control_points, degree):
    """Create a Rhino curve from control points and a degree.
    The domain of the curve is (0, 1).
    """
    curve = rs.AddCurve(control_points, degree=degree)
    curve = rs.coercecurve(curve)
    curve.Domain = rg.Interval(0.0, 1.0)

    return curve


def fit_curve_leastsquares(
    B,
    indices_free,
    indices_fixed,
    P_target,
    C_fixed,
    is_periodic=False):
    """
    Fit a curve to another curve in a least-squares sense.
    """
    if is_periodic:
        return fit_curve_least_squares_free_endpoints(B, P_target)

    return fit_curve_least_squares_fixed_endpoints(B, indices_free, indices_fixed, P_target, C_fixed)


def fit_curve_least_squares_fixed_endpoints(
    B,
    indices_free,
    indices_fixed,
    P_target,
    C_fixed):
    """
    Fit a curve to another curve in a least-squares sense while fixing endpoints.
    """
    # select bspline functions free and fixed submatrices
    B_free = calculate_bspline_matrix_free(B, indices_free)
    B_fixed = calculate_bspline_matrix_free(B, indices_fixed)

    # Calculate "free" target points
    P_target_free = P_target - B_fixed @ C_fixed

    # assemble left and right hand sides of the linear system
    lhs = B_free.T @ B_free
    rhs = B_free.T @ P_target_free
    
    # Solve least squares linear system
    return jnp.linalg.solve(lhs, rhs)


def fit_curve_least_squares_free_endpoints(B, P_target):
    """
    Fit a curve to another curve in a least-squares sense, without fixing endpoints.
    """
    # assemble left and right hand sides of the linear system
    lhs = B.T @ B
    rhs = B.T @ P_target
    
    # Solve least squares linear system
    return jnp.linalg.solve(lhs, rhs)


def variance_normalized(x):
    """Calculate the normalized variance on a collection of datapoints"""
    # return jnp.atleast_1d(jnp.var(lengths) / jnp.mean(lengths))
    return jnp.var(x) / jnp.mean(x)


def loss_fn(c_free, B, C_fixed, P_target, equalizing_strength, smoothing_strength, is_periodic=False):
    """Evaluate a loss function defined on the free control points of a curve."""

    # assemble control points
    C_free = jnp.reshape(c_free, (-1, 3))
    if is_periodic:
        C = C_free
    else: 
        C = jnp.concatenate((C_fixed[None, 0, :], C_free, C_fixed[None, -1, :]))

    # evaluate points on rebuilt curve
    P_rebuilt = curve_points_at_numpy(B, C)

    # calculate length squared between pairs of control points
    c_lengths = vmap(distance_sqrd)(C[:-1, :], C[1:, :])
    if is_periodic:
        length_last = distance_sqrd(C[0, :], C[-1, :])
        c_lengths = jnp.concatenate((c_lengths, jnp.atleast_1d(length_last)))

    # calculate the chamfer distance between two sets of points
    distance = chamfer_distance(P_rebuilt, P_target)
    equalizer = variance_normalized(c_lengths) * equalizing_strength
    smoothing = laplacian_smoothing(C, is_periodic) * smoothing_strength
    
    return distance + equalizer + smoothing
    


def loss_fn_with_tangents(c_free, B, C_fixed, P_target, T_target, equalizing_strength, smoothing_strength):
    """Evaluate a loss function defined on the free control points of a curve."""
    # compute second and penultimate control points position
    tangent_scales = c_free[-2:, None]        
    C_tangent = C_fixed + T_target * tangent_scales

    # assemble control points
    c_free = c_free[:-2]    
    C_free = jnp.reshape(c_free, (-1, 3))
    
    C = jnp.concatenate((C_fixed[None, 0, :], C_tangent[None, 0, :], C_free, C_tangent[None, -1, :], C_fixed[None, -1, :]))

    # evaluate points on rebuilt curve
    P_rebuilt = curve_points_at_numpy(B, C)

    # calculate length squared between pairs of control points
    c_lengths = vmap(distance_sqrd)(C[1:, :], C[:-1, :])

    # calculate the chamfer distance between two sets of points
    distance = chamfer_distance(P_rebuilt, P_target)
    equalizer = variance_normalized(c_lengths) * equalizing_strength
    smoothing = laplacian_smoothing(C) * smoothing_strength

    return distance + equalizer + smoothing
    

def distance_sqrd(p, q):
    """Compute the squared distance between two points."""
    return jnp.sum(jnp.square(p - q), axis=-1)


def chamfer_distance(P, Q):
    """Calculate the chamfer distance between two point sets."""
    assert P.shape == Q.shape

    # vectorize function
    vmap_distance_sqrt = vmap(distance_sqrd, in_axes=(0, None))

    # distances from P to Q
    distances_pq = vmap_distance_sqrt(P, Q)
    min_distance_pq = jnp.min(distances_pq, axis=-1)
    assert distances_pq.shape == (P.shape[0], Q.shape[0])
    
    # distances from Q to P
    distances_qp = vmap_distance_sqrt(Q, P)
    min_distance_qp = jnp.min(distances_qp, axis=-1)
    
    return jnp.mean(min_distance_pq) + jnp.mean(min_distance_qp)


def laplacian_smoothing(P, is_periodic=False):
    """Compute the Laplacian smoothing energy of a chain of points P.
    The points are assumed to be ordered as to form a sequence.
    The energy is computed for all points except the first and the last.
    """
    def laplacian_smoothing_indices(a, b, c):
        p_before= P[a, :] 
        p = P[b, :]
        p_after = P[c, :] 
        laplacian = 0.5 * (p_before + p_after) - p

        return jnp.sum(jnp.square(laplacian))

    def laplacian_smoothing_point(index):
        return laplacian_smoothing_indices(index - 1, index, index + 1)

    indices = jnp.arange(1, P.shape[0] - 1)
    energy = vmap(laplacian_smoothing_point)(indices)

    if is_periodic:
        energy = jnp.concatenate((
            energy, 
            jnp.atleast_1d(laplacian_smoothing_indices(-1, 0, 1)),
            jnp.atleast_1d(laplacian_smoothing_indices(-2, -1, 0))
            ))

    return jnp.mean(energy)


def fit_curve(
        target_curve,
        n_control, 
        degree,
        preserve_tangents,
        n_params, 
        n_iters, 
        optimizer_name, 
        tol,
        equalizer_strength,
        smoothing_strength,
        ):
    """
    The one and only rebuild curve command.
    """
    # Safety first
    assert degree >= 2, "The minimum supported degree is 2"
    assert n_control >= 3, "Please specify more at least 3 control points for rebuild"
    assert n_control > degree, "Number of control points must be larger than degree!"
    
    if preserve_tangents:
        assert n_control >= 4, "Number of control points must be >= 4 to preserve tangents!"

    # Convert polycurve to nurbs curve
    if not target_curve.HasNurbsForm():
        raise ValueError("Target curve has no valid curve form!")

    if isinstance(target_curve, rg.PolyCurve):
        print("The target curve is a polycurve. Converting it to a NURBS curve...")
        target_curve = target_curve.ToNurbsCurve()
    elif isinstance(target_curve, rg.PolylineCurve):
        print("The target curve is a polyline. Converting it to a NURBS curve...")
        target_curve = target_curve.ToNurbsCurve()
    elif isinstance(target_curve, rg.ArcCurve):
        print("The target curve is an arc curve. Converting it to a NURBS curve...")
        target_curve = target_curve.ToNurbsCurve()

    # Disable preserve tangents if curve is closed
    if target_curve.IsClosed and preserve_tangents:
        print("The target curve is closed. Sorry, butI cannot preserve tangents!")
        preserve_tangents = False

    # Reparametrize target curve to keep things nice and simple
    target_curve.Domain = rg.Interval(0.0, 1.0)
    
    # Get target curve control points
    C_target = rhino_curve_control_points_array(target_curve)

    # Create params vector on target curve
    params_target = create_parameters_uniform_arclength(target_curve, n_params)
    if target_curve.IsClosed:
        params_target = np.concatenate((params_target, params_target[:1]))
    
    # Sample points on target curve    
    points_on_target = rhino_curve_points_at(target_curve, params_target)
    P_target = rhino_points_to_array(points_on_target)

    # Create params vector on rebuilt curve
    params = create_parameters_uniform(n_params)

    # Calculate fixed points on curve
    C_fixed = calculate_curve_endpoints_array(target_curve)
    assert np.allclose(C_target[(0, -1), :], C_fixed)

    # Defined free and fixed indices
    indices_fixed = (0, n_control - 1)
    indices_free = tuple(range(1, n_control - 1))

    # Create the fitting B spline matrix with a uniform knot vector
    if target_curve.IsClosed:
        print("Creating Bspline matrix from periodic knots")
        _n_control = n_control + degree
        print(f"Bumping up number of control points by {degree}, from {n_control} to {_n_control}")
        knots = knots_uniform_periodic_superfluous(_n_control, degree)
        B = interp.BSpline.design_matrix(params, knots, degree).toarray()        
        B1 = B[:,:degree]
        B2 = B[:,degree:-degree]
        B3 = B[:,-degree:]    

        B = np.hstack((B1 + B3, B2))

    else:
        # Create bspline evaluation matrix
        print("Creating Bspline matrix from clamped knots")
        knots = knots_uniform_clamped_superfluous(n_control, degree)
        B = interp.BSpline.design_matrix(params, knots, degree).toarray()

    B = jnp.asarray(B)

    # Calculate initial guess for control points
    print("Starting from least squares!")
    if target_curve.IsClosed:
        print("Closed and unclamped least squares!")
        C_free = fit_curve_least_squares_free_endpoints(
            B,
            P_target                
            )

        # combine free and fixed control points
        C = jnp.vstack((C_free, C_free[:degree, :]))

    else:
        print("Open and clamped least squares!")
        C_free = fit_curve_least_squares_fixed_endpoints(
            B,
            indices_free,
            indices_fixed,
            P_target,
            C_fixed             
            )

        # combine free and fixed control points
        C = jnp.concatenate((C_fixed[None, 0, :], C_free, C_fixed[None, -1, :]))

    print(f"{C.shape=}")

    if preserve_tangents:
        # calculate end tangent unit vectors
        T_target = calculate_curve_endtangents_array(target_curve)

        # calculate start tangent scale 
        tangent_scales = calculate_control_polygon_endtangents_scales_array(C)
        print(f"{tangent_scales=}")
            
        # overwrite free indices to fix the first two
        indices_free = tuple(range(2, n_control - 2))
        
        C_free = C_free[1:-1]

    c_free = C_free.ravel()

    if preserve_tangents:
        c_free = jnp.concatenate((c_free, tangent_scales))

    c_free = c_free.ravel()
    
    if n_iters > 0:
        print("Optimizing...")

        # warmstarting
        if preserve_tangents:
            jit_loss_fn = jit(loss_fn_with_tangents)
            _loss_args = (B, C_fixed, P_target, T_target, equalizer_strength, smoothing_strength)
        else:
            jit_loss_fn = jit(loss_fn, static_argnums=(6,))            
            _loss_args = (B, C_fixed, P_target, equalizer_strength, smoothing_strength, target_curve.IsClosed)
            
        loss = jit_loss_fn(c_free, *_loss_args)
        print(f"Start loss: {loss:.6f}")

        optimizer_fn = jaxopt.ScipyMinimize
        if preserve_tangents:
            optimizer_fn = jaxopt.ScipyBoundedMinimize
        
        optimizer = optimizer_fn(
            fun=jit_loss_fn,
            method=optimizer_name,
            jit=False,
            has_aux=False,
            tol=tol,
            maxiter=n_iters,
            callback=None,
            )

        if preserve_tangents:
            # define bounds
            bounds_upper = jnp.ones(c_free.size) * jnp.inf
            num_controlpoints_free = c_free.size - 2            
            bounds_lower = -1.0 * jnp.ones(num_controlpoints_free,) * jnp.inf
            bounds_lower = jnp.concatenate((bounds_lower, 0.1 * tangent_scales))
            bounds = (bounds_lower, bounds_upper)

            # run optimizer           
            c_free_star, info = optimizer.run(
                c_free, 
                bounds,
                *_loss_args)
        else:
            c_free_star, info = optimizer.run(c_free, *_loss_args)

        print(f"Last loss: {info.fun_val:.6f}")
        print(f"{info.success=}")
        print(f"{info.iter_num=}")

        if preserve_tangents:
            # Compute second and penultimate control points position
            tangent_scales = c_free_star[-2:, None]        
            C_tangent = C_fixed + T_target * tangent_scales

            # Assemble control points
            c_free_star = c_free_star[:-2]    
            C_free_star = jnp.reshape(c_free_star, (-1, 3))
        
            C = jnp.concatenate((C_fixed[None, 0, :], C_tangent[None, 0, :], C_free_star, C_tangent[None, -1, :], C_fixed[None, -1, :]))

        else:
            C_free_star = jnp.reshape(c_free_star, (-1, 3))

            # Combine free and fixed control points
            if not target_curve.IsClosed:
                C = jnp.concatenate((C_fixed[None, 0, :], C_free_star, C_fixed[None, -1, :]))
            else:
                C = jnp.vstack((C_free_star, C_free_star[:degree, :]))

    print("Post-processing data to output Rhino geometry")
    control_points_star = points_array_to_rhino(C)
    rh_knots = knots[1:-1]

    return rs.AddNurbsCurve(control_points_star, rh_knots, degree)


def rebuild_curve_command():
    """The one and only RebuildCrv command"""
    point_count = 4
    degree = 3
    preserve_tangents = False

    go = GetObject()
    go.SetCommandPrompt("Select curve to rebuild. Press Enter when done")
    go.GeometryFilter = go.GeometryFilter.Curve
    go.AcceptNothing(True)
    go.EnablePreSelect(False, False)
    
    option_point_count = OptionInteger(point_count, 3, 1000)
    option_degree = OptionInteger(degree, 2, 9)
    option_preserve_tangents = OptionToggle(preserve_tangents, "No", "Yes")

    max_loops = 20
    current_loop = 0

    while True:
        current_loop += 1

        go.AddOptionInteger("PointCount", option_point_count, "Number of control points")
        go.AddOptionInteger("Degree", option_degree, "Curve degree")
        go.AddOptionToggle("PreserveTangents", option_preserve_tangents)
        result = go.Get()

        if result == Rhino.Input.GetResult.Object:
            target_curve = go.Objects()[0].Curve()

        elif result == Rhino.Input.GetResult.Option:
            if option_point_count.CurrentValue != point_count:
                point_count = option_point_count.CurrentValue
            if option_degree.CurrentValue != degree:
                degree = option_degree.CurrentValue
            if option_preserve_tangents.CurrentValue != preserve_tangents:
                preserve_tangents = option_preserve_tangents.CurrentValue

        elif result == Rhino.Input.GetResult.Nothing:
            rebuilt_curve = fit_curve(
                target_curve,
                point_count, 
                degree,
                preserve_tangents=preserve_tangents,
                n_params=500, 
                n_iters=100, 
                optimizer_name="L-BFGS-B", 
                tol=1e-9,
                equalizer_strength=0.0,
                smoothing_strength=0.0
                )
            Rhino.RhinoApp.Write(f"Rebuilt using degree {degree} curve with {point_count} control points!\n")
            break
        
        elif result == Rhino.Input.GetResult.Cancel:
            Rhino.RhinoApp.Write("Cancelled\n")
            break

        Rhino.RhinoApp.Write(f"{result}\n")

        if current_loop > max_loops:
            Rhino.App.Write(f"Max loops exceeded!")
            break
    

if __name__ == "__main__":
    rebuild_curve_command()
