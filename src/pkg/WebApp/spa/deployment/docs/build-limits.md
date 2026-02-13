# Build Limits

This document describes the resource limits applied to build jobs.

## Plan-Based Limits

Resource limits are determined by your account's subscription plan. Limits are passed with each build job and enforced by the worker.

| Resource | Free | Pro | Enterprise |
|----------|------|-----|------------|
| Memory | 1 GB | 2 GB | 4 GB |
| CPU | 1 core | 2 cores | 4 cores |
| Build timeout | 10 min | 30 min | 60 min |
| Artifact size | 100 MB | 500 MB | 1 GB |

## System Maximums

These are absolute limits that cannot be exceeded regardless of plan:

| Resource | Maximum |
|----------|---------|
| Memory | 4 GB |
| CPU | 4 cores (400%) |
| Build timeout | 60 minutes |
| Artifact size | 1 GB |
| Processes (PIDs) | 512 |

## Input Validation Limits

| Field | Limit |
|-------|-------|
| Environment variables | 50 max |
| Env var key length | 256 chars |
| Env var value length | 32 KB |
| Directory path length | 512 chars |
| Command length | 4 KB |
| Node version length | 32 chars |

## Security Restrictions

### Allowed Git Hosts

Only the following hosts are allowed for cloning repositories:

- `github.com`
- `gitlab.com`
- `bitbucket.org`

### Path Validation

- Path traversal (`..`) is not allowed
- Absolute paths are not allowed
- Only alphanumeric, dash, underscore, dot, and slash characters

### Container Security

- **Non-root user**: Builds run as `builder` user (UID 1001), not root
- Privileged mode disabled
- Capabilities dropped (only essential ones added)
- No privilege escalation (`no-new-privileges`)
- PID limit enforced

## Soft Limits (Warnings)

Soft limits trigger warnings but don't fail the build:

| Resource | Warning Threshold |
|----------|-------------------|
| Memory | 75% of limit |
| Artifact size | 50% of limit |

## API Message Format

Limits are passed in the build message:

```json
{
  "build_id": "uuid",
  "limits": {
    "memory_mb": 2048,
    "cpu_percent": 200,
    "build_timeout_s": 1800,
    "artifact_size_mb": 500
  }
}
```

If `limits` is omitted, default (free tier) limits apply.
