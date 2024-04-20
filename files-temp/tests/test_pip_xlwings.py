# requirements: pywin32, xlwings

import xlwings as xw

def write_string_to_excel():
    app = xw.apps.active
    
    if app:
        wb = app.active_book
        ws = wb.sheets.active

        ws.range('A1').value = 'test'
    else:
        print("No active Excel instance found.")

if __name__ == "__main__":
    write_string_to_excel()
