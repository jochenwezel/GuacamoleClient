"""Loopback-only page and synthetic event recorder for the integrated Windows pilot."""

import argparse
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--port", type=int, default=49173)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    fixture = (Path(__file__).parent / "NativeBrowserPilotFixture.html").read_text(encoding="utf-8-sig")
    recorder = """
    <script>
    for (const type of ['keydown', 'keyup', 'input', 'focus', 'blur']) {
      document.getElementById('input').addEventListener(type, () => {
        fetch('/probe-state', {method:'POST', headers:{'Content-Type':'application/json'},
          body:JSON.stringify(probeState())});
      });
    }
    </script>
    """
    html = fixture.replace("</html>", recorder + "</html>").encode("utf-8")

    class Handler(BaseHTTPRequestHandler):
        def do_GET(self):
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(html)

        def do_POST(self):
            length = int(self.headers.get("Content-Length", "0"))
            if self.path != "/probe-state" or not 0 < length <= 1_000_000:
                self.send_error(400)
                return
            state = json.loads(self.rfile.read(length))
            (args.output / "input.json").write_text(json.dumps(state, indent=2), encoding="utf-8")
            self.send_response(204)
            self.end_headers()

        def log_message(self, *_):
            pass

    ThreadingHTTPServer(("127.0.0.1", args.port), Handler).serve_forever()


if __name__ == "__main__":
    main()
