#! python3
#r: numpy, scikit-image

import rhinoscriptsyntax as rs
import Rhino

import math
import numpy as np
from skimage.transform import radon, rescale
from skimage.measure import moments
from scipy.integrate import cumulative_trapezoid
import skimage as ski

filepath = rs.OpenFileName("Select Image File")

def pad_and_transfom_image(image, slices = 256):
    # In order to compute an accurate Radon transform, we need to zero pad the image
    # so that the circumcircle of the original image is entirely contained.
    n,m = image.shape
    diagonal_radius = 0.5 * np.sqrt(n*n + m*m)
    x_padding = int(0.5 + diagonal_radius - n/2)
    y_padding = int(0.5 + diagonal_radius - m/2)
    padded = np.pad(image,[(x_padding,x_padding),(y_padding,y_padding)])
    # Now we can compute the Radon transform and turn that into an array of CDFs
    theta = np.linspace(0., 180., slices, endpoint=False)
    cdf = radon(padded, theta=theta)
    for i in range(slices):
        cdf[1:,i] = cumulative_trapezoid(cdf[:,i])
        cdf[0,i] = 0
        cdf[:,i] /= cdf[-1,i]
    
    return cdf, math.pi * theta / 180.0

def invert_cdfs(cdf, n_points):
    n,m = cdf.shape
    # Compute the initial guess by sampling the product distribution of the
    # x and y distributions
    px_coordinates = np.arange(n)
    initial = np.random.rand(n_points,2)
    initial[:,0] = np.interp(initial[:,0], cdf[:,0], px_coordinates)
    initial[:,1] = np.interp(initial[:,1], cdf[:,m//2], px_coordinates)
    # Then compute the ideal set of directional locations
    
    y = (np.arange(n_points) + 0.5)/n_points
    dists = np.empty((n_points, m))
    for i in range(m):
        dists[:,i] = np.interp(y, cdf[:,i], px_coordinates)
    
    return initial, dists

def slice_iterations(pts, slice_dists, angles, n_iterations = 500, n_slices = 32):
    pts = pts.copy()
    delta = np.zeros_like(pts)
    m = len(angles)
    idxs = np.arange(len(angles))
    
    for _ in range(n_iterations):
        delta[:] = 0
        
        for i in np.random.choice(idxs, n_slices):
            theta = angles[i]
            u = np.array([np.cos(theta), np.sin(theta)])
            proj = pts @ u
            idx = np.argsort(proj)
            delta[idx] += np.outer(slice_dists[:,i] - proj[idx], u)
        delta /= n_slices
        pts += delta
        
    return pts

image = 1 - ski.io.imread(filepath,"L")
cdf, theta = pad_and_transfom_image(image)
initial, dists = invert_cdfs(cdf, 5000)
pts = slice_iterations(initial, dists, theta, n_iterations = 1000)
center = pts.mean(axis = 0)

cloud = rs.AddPointCloud(list(xy - center for xy in pts))