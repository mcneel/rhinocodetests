#! python3
# requirements: cv-3

import cv3

img = cv3.imread('/Users/ein/Downloads/TestCV3/rhino-small.jpg')
gray = cv3.rgb2gray(img)
cv3.imwrite('/Users/ein/Downloads/TestCV3/outputs/gray.jpg', gray, mkdir=True)
