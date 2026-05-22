#!/usr/bin/env python3
"""test_rtt_client.py — smoke test for NOBlackBox RTT

Usage:
  # Test no-password mode (default config)
  python3 test_rtt_client.py

  # Test password-protected mode (server must have password set)
  python3 test_rtt_client.py --password mysecret

Reads until required header lines appear or timeout — no fixed sleep.
"""
import socket, sys, time, argparse

HOST = "127.0.0.1"
PORT = 42674

def crc64_ecma(password):
    """CRC-64-ECMA over UTF-16LE, MSB-first, Tacview-style (init=all-1s, final-xor=all-1s)."""
    if not password:
        return "0"
    poly = 0x42F0E1EBA9EA3693
    data = password.encode("utf-16-le")
    crc = 0xFFFFFFFFFFFFFFFF
    for b in data:
        crc ^= b << 56
        for _ in range(8):
            if crc & 0x8000000000000000:
                crc = (crc << 1) ^ poly
            else:
                crc <<= 1
            crc &= 0xFFFFFFFFFFFFFFFF
    return format(crc ^ 0xFFFFFFFFFFFFFFFF, "016x")

def run_test(password):
    """Connect, handshake, and verify ACMI header reception."""
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(5)
    s.connect((HOST, PORT))

    handshake = recv_until_null(s)
    fields = handshake.split(b"\n")
    assert len(fields) == 4, f"Expected 4 fields, got {len(fields)}"
    assert fields[0] == b"XtraLib.Stream.0"
    assert fields[1] == b"Tacview.RealTimeTelemetry.0"
    print(f"[PASS] Host handshake: {fields[2].decode()}")

    pwd_hash = crc64_ecma(password)
    s.sendall(
        f"XtraLib.Stream.0\nTacview.RealTimeTelemetry.0\nTestClient\n{pwd_hash}\0"
        .encode()
    )

    time.sleep(0.3)
    buf = recv_until_lines(s, min_lines=5, timeout=3)

    assert b"FileType=text/acmi/tacview" in buf, "Missing FileType"
    assert b"FileVersion=2.2" in buf, "Missing FileVersion"
    assert b"ReferenceTime" in buf, "Missing ReferenceTime"
    lines = buf.decode().strip().split("\n")
    print(f"[PASS] ACMI header received ({len(lines)} lines)")
    for line in lines[:5]:
        print(f"  > {line}")
    s.close()
    return True

def run_test_with_hash(pwd_hash):
    """Connect and handshake with a specific hash (for no-password mode testing)."""
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(5)
    s.connect((HOST, PORT))

    handshake = recv_until_null(s)
    fields = handshake.split(b"\n")
    assert fields[0] == b"XtraLib.Stream.0"
    assert fields[1] == b"Tacview.RealTimeTelemetry.0"

    s.sendall(
        f"XtraLib.Stream.0\nTacview.RealTimeTelemetry.0\nTestClient\n{pwd_hash}\0"
        .encode()
    )

    time.sleep(0.3)
    buf = recv_until_lines(s, min_lines=3, timeout=3)
    ok = b"FileType=text/acmi/tacview" in buf
    s.close()
    if ok:
        print(f"[PASS] Hash '{pwd_hash}' accepted (no-password mode)")
    else:
        print(f"[FAIL] Hash '{pwd_hash}' rejected unexpectedly")
    return ok

def test_wrong_password():
    """Wrong password should cause clean disconnect."""
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(3)
    s.connect((HOST, PORT))
    recv_until_null(s)
    s.sendall(b"XtraLib.Stream.0\nTacview.RealTimeTelemetry.0\nBadClient\nDEADBEEF\0")
    time.sleep(0.3)
    try:
        resp = s.recv(1024)
        if resp:
            print("[FAIL] Wrong password: got data (expected disconnect)")
            return False
        print("[PASS] Wrong password: clean close")
    except (socket.timeout, ConnectionResetError, ConnectionAbortedError):
        print("[PASS] Wrong password: disconnected as expected")
    finally:
        s.close()
    return True

def recv_until_null(sock):
    data = b""
    while True:
        b = sock.recv(1)
        if b == b"\0" or not b:
            break
        data += b
    return data

def recv_until_lines(sock, min_lines, timeout=3):
    buf = b""
    deadline = time.monotonic() + timeout
    lines_seen = 0
    while time.monotonic() < deadline:
        try:
            chunk = sock.recv(4096)
            if not chunk:
                break
            buf += chunk
            lines_seen = buf.count(b"\n")
            if lines_seen >= min_lines:
                break
        except socket.timeout:
            break
    return buf

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--password", default="", help="Server password")
    args = ap.parse_args()

    ok = True

    if args.password:
        print("=== Password mode: correct hash ===")
        ok &= run_test(args.password)

        print("\n=== Password mode: wrong hash ===")
        ok &= test_wrong_password()
    else:
        print("=== No-password mode: hash 0 accepted ===")
        ok &= run_test("")

        print("\n=== No-password mode: arbitrary hash accepted ===")
        ok &= run_test_with_hash("DEADBEEF")

    sys.exit(0 if ok else 1)
