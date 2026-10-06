"""Serve a WebGL build on the LAN and collect benchmark results posted by the player.

    python Tools/serve_webgl.py Builds/PerfSpike [--port 8000]

Open http://<this-machine-LAN-IP>:<port>/?bench on any device; results are appended to
Builds/bench-results.jsonl (one JSON object per run) and echoed to the terminal.
"""
import argparse
import datetime
import functools
import http.server
import json
import os
import socket

RESULTS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Builds", "bench-results.jsonl")


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # Always serve the latest build; Unity's own data cache revalidates separately.
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def do_POST(self):
        if self.path.rstrip("/") != "/bench":
            self.send_error(404)
            return
        body = self.rfile.read(int(self.headers.get("Content-Length", 0)))
        try:
            result = json.loads(body)
        except ValueError:
            self.send_error(400, "Body must be JSON")
            return
        result["receivedAt"] = datetime.datetime.now().isoformat(timespec="seconds")
        result["client"] = self.client_address[0]
        result["userAgent"] = self.headers.get("User-Agent", "")
        os.makedirs(os.path.dirname(RESULTS), exist_ok=True)
        with open(RESULTS, "a", encoding="utf-8") as f:
            f.write(json.dumps(result) + "\n")
        print("BENCH", json.dumps(result), flush=True)
        self.send_response(204)
        self.end_headers()


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
    print(f"Serving {args.directory} at http://{lan_ip()}:{args.port}/  (benchmark: add ?bench)", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
