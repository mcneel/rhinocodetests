# https://towardsdatascience.com/creating-pdf-files-with-python-ad3ccadfae0f
# r: fpdf

from pathlib import Path
import os.path as op
from fpdf import FPDF


pdf_w = 210
pdf_h = 297


DEST = op.join(Path.home(), "Downloads", "test.pdf")


class PDF(FPDF):
    def lines(self):
        self.set_fill_color(32.0, 47.0, 250.0)
        self.rect(5.0, 5.0, 200.0, 287.0, "DF")
        self.set_fill_color(255, 255, 255)
        self.rect(8.0, 8.0, 194.0, 282.0, "FD")


pdf = PDF(unit="mm")

pdf.add_page()
pdf.lines()

pdf.set_author("Ehsan")

pdf.output(DEST, "F")
