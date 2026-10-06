"""Serve a WebGL build on the LAN for device testing.

    python Tools/serve_webgl.py Builds/WebGL [--port 8000]

Then open http://<this-machine-LAN-IP>:<port>/ on a phone on the same Wi-Fi.
"""
import argparse
import functools
import http.server
import socket


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # Always serve the latest build; Unity's own data cache revalidates separately.
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def lan_ip():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
        try:
            s.connect(("192.0.2.1", 80))  # No packet is sent; just picks the outbound interface.
            return s.getsockname()[0]
        except OSError:
            return "127.0.0.1"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()
    handler = functools.partial(Handler, directory=args.directory)
    server = http.server.ThreadingHTTPServer(("0.0.0.0", args.port), handler)
    print(f"Serving {args.directory} at http://{lan_ip()}:{args.port}/", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
