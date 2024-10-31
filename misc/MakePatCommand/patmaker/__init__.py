"""Create patterns based on AutoCAD .pat standard."""
# pylint: disable=import-error,invalid-name
import os.path as op
import re
import datetime
from math import sqrt, pi, sin, cos, acos, degrees
from typing import List, Tuple

import Rhino
import Rhino.Geometry as G

from System.Collections.Generic import List


PI = pi
HALF_PI = PI / 2.0
ZERO_TOL = 5e-06

COORD_RESOLUTION = 16

# 0.5 < MODEL < 848.5 inches, source: http://hatchkit.com.au/faq.php#Tip7
MAX_MODEL_DOMAIN = 100.0

# 0.002 < DRAFTING < 84.85 inches
MAX_DETAIL_DOMAIN = MAX_MODEL_DOMAIN / 10.0

MAX_DOMAIN_MULT = 8

RATIO_RESOLUTION = 2
ANGLE_CORR_RATIO = 0.01


def round_vector(length):
    length = length if abs(length) > ZERO_TOL else 0.0
    return round(length, COORD_RESOLUTION)


def flatten_zeros(length):
    length_str = ("{:." + str(COORD_RESOLUTION) + "f}").format(length)
    return re.sub(r"\.0+$", ".0", length_str)


def current_time():
    return datetime.datetime.now().strftime("%H:%M:%S")


def current_date():
    return datetime.datetime.now().strftime("%Y-%m-%d")


class _PatternPoint:
    def __init__(self, u_point, v_point):
        self.u = round_vector(u_point)
        self.v = round_vector(v_point)

    def __repr__(self):
        return "<_PatternPoint U:{0:.20f} V:{1:.20f}>".format(self.u, self.v)

    def __eq__(self, other):
        return self.u == other.u and self.v == other.v

    def __hash__(self):
        # todo: come up with method for sorting lines
        return hash(self.u + self.v)

    def __add__(self, other):
        return _PatternPoint(self.u + other.u, self.v + other.v)

    def __sub__(self, other):
        return _PatternPoint(self.u - other.u, self.v - other.v)

    def distance_to(self, point):
        return sqrt((point.u - self.u) ** 2 + (point.v - self.v) ** 2)

    def rotate(self, angle, origin=None):
        # default origin to 0,0 if not set
        origin = origin or _PatternPoint(0, 0)
        tu = self.u - origin.u
        tv = self.v - origin.v
        self.u = origin.u + (tu * cos(angle) - tv * sin(angle))
        self.v = origin.v + (tu * sin(angle) + tv * cos(angle))
        return True


class _PatternLine:
    def __init__(self, start_p, end_p):
        """

        Args:
            start_p (_PatternPoint):
            end_p (_PatternPoint):
        """
        self.start_point = start_p if start_p.v <= end_p.v else end_p
        self.end_point = end_p if start_p.v <= end_p.v else start_p
        self.u_vector = G.Vector2f(1, 0)

    def __repr__(self):
        return "<_PatternLine Start:{} End:{} Length:{} Angle:{}>".format(
            self.start_point, self.end_point, self.length, self.angle
        )

    @property
    def direction(self):
        return _PatternPoint(
            self.end_point.u - self.start_point.u, self.end_point.v - self.start_point.v
        )

    @property
    def angle(self):
        # always return angle to u direction
        d = G.Vector3f(self.direction.u, self.direction.v, 0)
        d.Unitize()
        d = G.Vector2f(d.X, d.Y)
        dotproduct = G.Vector2f.Multiply(d, self.u_vector)
        return acos(dotproduct)

    @property
    def center_point(self):
        return _PatternPoint(
            (self.end_point.u + self.start_point.u) / 2.0,
            (self.end_point.v + self.start_point.v) / 2.0,
        )

    @property
    def length(self):
        return abs(sqrt(self.direction.u ** 2 + self.direction.v ** 2))

    def point_on_line(self, point, tolerance=ZERO_TOL):
        a = self.start_point
        b = self.end_point
        c = point
        if (
            0.0
            <= abs((a.u - c.u) * (b.v - c.v) - (a.v - c.v) * (b.u - c.u))
            <= tolerance
        ):
            return True
        else:
            return False

    def intersect(self, pat_line):
        xdiff = _PatternPoint(
            self.start_point.u - self.end_point.u,
            pat_line.start_point.u - pat_line.end_point.u,
        )
        ydiff = _PatternPoint(
            self.start_point.v - self.end_point.v,
            pat_line.start_point.v - pat_line.end_point.v,
        )

        def det(a, b):
            return a.u * b.v - a.v * b.u

        div = det(xdiff, ydiff)
        if div == 0:
            raise Exception("Lines do not intersect.")

        d = _PatternPoint(
            det(self.start_point, self.end_point),
            det(pat_line.start_point, pat_line.end_point),
        )
        int_point_x = det(d, xdiff) / div
        int_point_y = det(d, ydiff) / div

        return _PatternPoint(int_point_x, int_point_y)

    def rotate(self, angle, origin=None):
        self.start_point.rotate(angle, origin=origin)
        self.end_point.rotate(angle, origin=origin)


class _PatternSafeGrid:
    def __init__(self, domain, diag_angle, u_tiles, v_tiles, flipped=False):
        self._domain = domain
        self._flipped = flipped
        self._diag_angle = diag_angle
        # find out the axis line to calculate angle and length
        self._axis_line = _PatternLine(
            _PatternPoint(0, 0),
            _PatternPoint(self._domain.u * u_tiles, self._domain.v * v_tiles),
        )
        # now determine the parameters necessary to
        # calculate span, offset, and shift
        self._determine_abstract_params(u_tiles, v_tiles)

    def __eq__(self, other):
        return 0 <= self.grid_angle - other.grid_angle <= ZERO_TOL

    def __hash__(self):
        return hash(self.grid_angle)

    def _determine_abstract_params(self, u_tiles, v_tiles):
        if self._axis_line.angle <= self._diag_angle:
            if not self._flipped:
                self._offset_direction = -1.0
            else:
                self._offset_direction = 1.0

            self._angle = self._axis_line.angle
            self._u_tiles = u_tiles
            self._v_tiles = v_tiles
            self._domain_u = self._domain.u
            self._domain_v = self._domain.v

        else:
            if not self._flipped:
                self._offset_direction = 1.0
            else:
                self._offset_direction = -1.0

            self._angle = (
                HALF_PI - self._axis_line.angle
                if not self._flipped
                else self._axis_line.angle - HALF_PI
            )
            self._u_tiles = v_tiles
            self._v_tiles = u_tiles
            self._domain_u = self._domain.v
            self._domain_v = self._domain.u

    def is_valid(self):
        return self.shift

    def __repr__(self):
        return (
            "<_PatternSafeGrid GridAngle:{} Angle:{} "
            "U_Tiles:{} V_Tiles:{} "
            "Domain_U:{} Domain_V:{} Offset_Dir:{} "
            "Span:{} Offset:{} Shift:{}>".format(
                self.grid_angle,
                self._angle,
                self._u_tiles,
                self._v_tiles,
                self._domain_u,
                self._domain_v,
                self._offset_direction,
                self.span,
                self.offset,
                self.shift,
            )
        )

    @property
    def grid_angle(self):
        return (
            self._axis_line.angle if not self._flipped else PI - self._axis_line.angle
        )

    @property
    def span(self):
        return self._axis_line.length

    @property
    def offset(self):
        if self._angle == 0.0:
            total_offset = self._domain_v * self._offset_direction
        elif self._v_tiles == 0.0:
            total_offset = abs(self._domain_u * sin(self._angle)) * self._offset_direction
        else:
            total_offset = (
                abs(self._domain_u * sin(self._angle) / self._v_tiles)
                * self._offset_direction
            )
        return total_offset

    @property
    def shift(self):
        if self._angle == 0.0:
            return 0

        def find_nxt_grid_point(offset_line):
            u_mult = 0
            while u_mult < self._u_tiles:
                for v_mult in range(0, self._v_tiles):
                    grid_point = _PatternPoint(
                        self._domain_u * u_mult, self._domain_v * v_mult
                    )
                    if offset_line.point_on_line(grid_point):
                        return grid_point
                u_mult += 1
            if u_mult >= self._u_tiles:
                return None

        if self._u_tiles == self._v_tiles == 1:
            return abs(self._domain_u * cos(self._angle))
        else:
            # calculate the abstract offset axis
            offset_u = abs(self.offset * sin(self._angle))
            offset_v = -abs(self.offset * cos(self._angle))
            offset_vector = _PatternPoint(offset_u, offset_v)
            # find the offset line
            abstract_axis_start_point = _PatternPoint(0, 0)
            abstract_axis_end_point = _PatternPoint(
                self._domain_u * self._u_tiles, self._domain_v * self._v_tiles
            )
            offset_vector_start = abstract_axis_start_point + offset_vector
            offset_vector_end = abstract_axis_end_point + offset_vector
            offset_axis = _PatternLine(offset_vector_start, offset_vector_end)

            # try to find the next occurance on the abstract offset axis
            nxt_grid_point = find_nxt_grid_point(offset_axis)

            if nxt_grid_point:
                total_shift = offset_axis.start_point.distance_to(nxt_grid_point)
                return total_shift
            else:
                return 0


class _PatternDomain:
    def __init__(self, start_u, start_v, end_u, end_v, expandable):
        self._origin = _PatternPoint(min(start_u, end_u), min(start_v, end_v))
        self._corner = _PatternPoint(max(start_u, end_u), max(start_v, end_v))
        self._bounds = self._corner - self._origin
        self._normalized_domain = _PatternPoint(
            1.0, 1.0 * (self._bounds.v / self._bounds.u)
        )
        if self._zero_domain():
            raise Exception("Can not process zero domain.")

        self.u_vec = _PatternLine(_PatternPoint(0, 0), _PatternPoint(self._bounds.u, 0))
        self.v_vec = _PatternLine(_PatternPoint(0, 0), _PatternPoint(0, self._bounds.v))

        self._max_domain = MAX_MODEL_DOMAIN

        self._expandable = expandable
        self._target_domain = self._max_domain

        self.diagonal = _PatternLine(
            _PatternPoint(0.0, 0.0), _PatternPoint(self._bounds.u, self._bounds.v)
        )

        self._calculate_safe_angles()

    def __repr__(self):
        return "<_PatternDomain U:{} V:{} SafeAngles:{}>".format(
            self._bounds.u, self._bounds.v, len(self.safe_angles)
        )

    def _zero_domain(self):
        return self._bounds.u == 0 or self._bounds.v == 0

    def _calculate_safe_angles(self):
        # setup tile counters
        u_mult = v_mult = 1
        self.safe_angles = []
        processed_ratios = {1.0}

        # add standard angles to the list
        self.safe_angles.append(
            _PatternSafeGrid(self._bounds, self.diagonal.angle, u_mult, 0)
        )

        self.safe_angles.append(
            _PatternSafeGrid(self._bounds, self.diagonal.angle, u_mult, 0, flipped=True)
        )

        self.safe_angles.append(
            _PatternSafeGrid(self._bounds, self.diagonal.angle, u_mult, v_mult)
        )

        self.safe_angles.append(
            _PatternSafeGrid(
                self._bounds, self.diagonal.angle, u_mult, v_mult, flipped=True
            )
        )

        self.safe_angles.append(
            _PatternSafeGrid(self._bounds, self.diagonal.angle, 0, v_mult)
        )

        # traverse the tile space and add safe grids to the list
        while self._bounds.u * u_mult <= self._target_domain / 2.0:
            v_mult = 1
            while self._bounds.v * v_mult <= self._target_domain / 2.0:
                ratio = round(v_mult / float(u_mult), RATIO_RESOLUTION)
                if ratio not in processed_ratios:
                    # for every tile, also add the mirrored tile
                    angle1 = _PatternSafeGrid(
                        self._bounds, self.diagonal.angle, u_mult, v_mult
                    )

                    angle2 = _PatternSafeGrid(
                        self._bounds, self.diagonal.angle, u_mult, v_mult, flipped=True
                    )

                    if angle1.is_valid() and angle2.is_valid():
                        self.safe_angles.append(angle1)
                        self.safe_angles.append(angle2)
                        processed_ratios.add(ratio)
                v_mult += 1
            u_mult += 1

    def expand(self):
        # expand target domain for more safe angles
        if self._target_domain > self._max_domain * MAX_DOMAIN_MULT:
            return False
        else:
            self._target_domain += self._max_domain / 2
            self._calculate_safe_angles()
            return True

    def get_domain_coords(self, pat_line):
        return _PatternLine(
            pat_line.start_point - self._origin, pat_line.end_point - self._origin
        )

    def get_grid_params(self, axis_angle):
        return min(self.safe_angles, key=lambda x: abs(x.grid_angle - axis_angle))

    def get_required_correction(self, axis_angle):
        return abs(axis_angle - self.get_grid_params(axis_angle).grid_angle)

    def get_best_angle(self, axis_angle):
        if self._expandable:
            while self.get_required_correction(axis_angle) >= ANGLE_CORR_RATIO:
                if not self.expand():
                    break
        return self.get_grid_params(axis_angle)


class _PatternGrid:
    def __init__(self, pat_domain, init_line):
        self._domain = pat_domain
        self._grid = self._domain.get_best_angle(init_line.angle)
        self.angle = self._grid.grid_angle
        self.span = self._grid.span
        self.offset = self._grid.offset
        self.shift = self._grid.shift

        self.segment_lines = []
        init_line.rotate(self.angle - init_line.angle, origin=init_line.center_point)
        self.segment_lines.append(init_line)

    def __repr__(self):
        return "<_PatternGrid Angle:{} Span:{} Offset:{} Shift:{}>".format(
            self.angle, self.span, self.offset, self.shift
        )

    def adopt_line(self, pat_line):
        # todo: optimise grid creation. check overlap and combine
        # overlapping lines into one grid
        return False

    @property
    def origin(self):
        # collect all line segment points
        point_list = []
        for seg_line in self.segment_lines:
            point_list.extend([seg_line.start_point, seg_line.end_point])

        # origin is the point that is closest to zero
        if self.angle <= HALF_PI:
            return min(point_list, key=lambda x: x.distance_to(_PatternPoint(0, 0)))
        else:
            return min(
                point_list,
                key=lambda x: x.distance_to(
                    _PatternPoint(self._domain.u_vec.length, 0)
                ),
            )

    @property
    def segments(self):
        pen_down = self.segment_lines[0].length
        return [pen_down, pen_down - self.span]


class _FillPattern:
    def __init__(
        self,
        pat_domain,
        pat_name,
        scale=1.0,
        rotation=0,
        flip_u=False,
        flip_v=False,
    ):
        self._domain = pat_domain
        self._pattern_grids = []
        self._input_fillgrids = []

        self._name = pat_name
        self._scale = scale
        self._rotation = rotation
        self._flip_u = flip_u
        self._flip_v = flip_v

    def __repr__(self):
        return "<_Pattern Name:{} Scale:{}>".format(self._name, self._scale)

    def append_line(self, pat_line):
        # get line in current domain
        domain_line = self._domain.get_domain_coords(pat_line)
        # check if line overlaps any of existing grids
        for pat_grid in self._pattern_grids:
            if pat_grid.adopt_line(domain_line):
                return True
        # if line does not overlap any of existing grids, create new grid
        new_grid = _PatternGrid(self._domain, domain_line)
        self._pattern_grids.append(new_grid)

    @property
    def name(self):
        return self._name

    def _make_fill_grid(self, pattern_grid):
        fg_scale = self._scale

        fg_rotation = self._rotation
        if (self._flip_u and not self._flip_v) or (self._flip_v and not self._flip_u):
            fg_rotation = -fg_rotation

        fill_grid = Rhino.DocObjects.HatchLine()

        # determine and set angle
        if self._flip_u and self._flip_v:
            fill_grid.Angle = PI + pattern_grid.angle
        elif self._flip_u:
            fill_grid.Angle = PI - pattern_grid.angle
        elif self._flip_v:
            fill_grid.Angle = -pattern_grid.angle
        else:
            fill_grid.Angle = pattern_grid.angle
        fill_grid.Angle += fg_rotation

        # determine and set origin
        # apply flips
        origin_u = -pattern_grid.origin.u if self._flip_u else pattern_grid.origin.u
        origin_v = -pattern_grid.origin.v if self._flip_v else pattern_grid.origin.v
        # apply rotation if any
        fg_origin = _PatternPoint(origin_u, origin_v)
        if fg_rotation:
            fg_origin.rotate(fg_rotation)
        fill_grid.BasePoint = G.Point2d(fg_origin.u * fg_scale, fg_origin.v * fg_scale)

        # determine and set offset
        shift = pattern_grid.shift * fg_scale
        offset = 0
        if self._flip_u and self._flip_v:
            offset = pattern_grid.offset * fg_scale
        elif self._flip_u or self._flip_v:
            offset = -pattern_grid.offset * fg_scale
        else:
            offset = pattern_grid.offset * fg_scale

        # determine and set shift
        fill_grid.Offset = G.Vector2d(shift, offset)

        # build and set segments list
        if pattern_grid.segments:
            scaled_segments = [seg * fg_scale for seg in pattern_grid.segments]
            for segment in scaled_segments:
                fill_grid.AppendDash(segment)

        return fill_grid

    def create_pattern(self):
        fill_grids = [self._make_fill_grid(x) for x in self._pattern_grids]

        fill_pat = Rhino.DocObjects.HatchPattern()
        fill_pat.Name = self._name
        fill_pat.FillType = Rhino.DocObjects.HatchPatternFillType.Lines

        # Apply the FillGrids
        for fill_grid in fill_grids:
            fill_pat.AddHatchLine(fill_grid)

        return fill_pat


def get_segments(curve: G.Curve, max_length: float = .5) -> List[G.Line]:
    lines = []
    polyline: G.Polyline = curve.ToPolyline(0.1, 0.1, .1, max_length).ToPolyline()
    for line in polyline.GetSegments():
        lines.append(line)
    return lines


def make_pattern(
    pat_name: str,
    pat_lines: List[G.Line],
    domain: Tuple[G.Point2f, G.Point2f],
    scale=1.0,
    rotation=0,
    flip_u=False,
    flip_v=False,
    allow_expansion=False
):
    pat_domain = _PatternDomain(
        domain[0].X,
        domain[0].Y,
        domain[1].X,
        domain[1].Y,
        allow_expansion,
    )

    fill_pattern = _FillPattern(
        pat_domain, pat_name, scale, rotation, flip_u, flip_v
    )

    for line in pat_lines:
        startp = _PatternPoint(line.From.X, line.From.Y)
        endp = _PatternPoint(line.To.X, line.To.Y)
        pat_line = _PatternLine(startp, endp)
        try:
            fill_pattern.append_line(pat_line)
        except Exception as pat_line_err:
            pass

    return fill_pattern.create_pattern()
