# server.py
from http.server import BaseHTTPRequestHandler, HTTPServer

PORT = 8002

stopped = False


class MyHttpRequestHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        global stopped
        print(self.path)
        if self.path.endswith("/end"):
            stopped = True
            self.wfile.write(bytes("", "utf-8"))
            self.send_response(200)
        else:
            report = "Hello!"
            self.wfile.write(bytes(report, "utf-8"))
            self.send_response(200)


Handler = MyHttpRequestHandler

httpd = HTTPServer(("", PORT), Handler)
print("Http Server Serving at port", PORT)
while not stopped:
    httpd.handle_request()
    print("one more")
print("Http Server Stopped")