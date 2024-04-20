# r: lbt-dragonfly

from ladybug.location import Location
from ladybug.sunpath import Sunpath

# Create location. You can also extract location data from an epw file.
syd = Location('Sydney', latitude=-33.87, longitude=151.22, time_zone=10)

# Initiate sun path and print altitude and azimuth
sp = Sunpath.from_location(syd)
sun = sp.calculate_sun(month=11, day=15, hour=11.0)
print('altitude: {}, azimuth: {}'.format(sun.altitude, sun.azimuth))