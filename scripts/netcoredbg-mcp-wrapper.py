#!/usr/bin/env python3
"""
netcoredbg-mcp wrapper — auto-attach to running JNPF backend process.
Scans for JNPF.API.Entry.exe and launches netcoredbg-mcp with the correct PID.
"""
import subprocess
import sys
import os

def find_jnpf_pid():
    """Find JNPF.API.Entry.exe process ID."""
    try:
        # Windows: use tasklist
        output = subprocess.check_output(
            ['tasklist', '/FI', 'IMAGENAME eq JNPF.API.Entry.exe', '/FO', 'CSV', '/NH'],
            encoding='utf-8', errors='ignore'
        )
        for line in output.strip().split('\n'):
            if 'JNPF.API.Entry.exe' in line:
                pid = line.split(',')[1].strip('"')
                return pid
    except Exception:
        pass

    # Fallback: try netstat on port 5000
    try:
        output = subprocess.check_output(
            ['netstat', '-ano', '-p', 'tcp'],
            encoding='utf-8', errors='ignore'
        )
        for line in output.split('\n'):
            if ':5000' in line and 'LISTENING' in line:
                pid = line.strip().split()[-1]
                return pid
    except Exception:
        pass

    return None

if __name__ == '__main__':
    pid = find_jnpf_pid()
    if not pid:
        print("ERROR: JNPF.API.Entry.exe not found running. Start the backend first.", file=sys.stderr)
        sys.exit(1)

    print(f"Attaching to JNPF.API.Entry.exe (PID={pid})", file=sys.stderr)

    # Launch netcoredbg-mcp with the found PID
    args = [
        sys.executable, '-m', 'netcoredbg_mcp',
        '--process-id', pid,
        *sys.argv[1:]  # pass through any extra args
    ]
    os.execv(sys.executable, args)
