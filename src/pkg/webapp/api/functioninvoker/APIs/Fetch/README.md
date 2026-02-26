# Fetch API

Synchronous `fetch()` for making HTTP requests from MyCroCloud functions.

## Usage

```js
const res = fetch('https://api.example.com/data');
const data = res.json();

const res2 = fetch('https://api.example.com/users', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ name: 'Alice' })
});
```

## Options

| Property  | Type   | Default | Description |
|-----------|--------|---------|-------------|
| `method`  | string | `GET`   | HTTP method |
| `headers` | object | `{}`    | Request headers |
| `body`    | string | —       | Request body |

## Response Object

| Property/Method | Type     | Description |
|-----------------|----------|-------------|
| `status`        | number   | HTTP status code |
| `statusText`    | string   | HTTP reason phrase |
| `ok`            | boolean  | `true` if 200–299 |
| `headers`       | object   | Response headers (lowercase keys) |
| `text()`        | function | Body as string |
| `json()`        | function | Body parsed as JSON |

All calls are synchronous — no Promises or `await`.

## Limits

| Limit | Value |
|-------|-------|
| Max requests/execution | 50 |
| Request body | 1 MB |
| Response body | 5 MB |
| Total bandwidth | 10 MB |
| Per-request timeout | 5s |
| Max redirects | 5 |

## Security

- Only `http://` and `https://` schemes allowed
- Private/internal IPs blocked (SSRF protection)
- `Host`, `Cookie`, `Set-Cookie` headers stripped from requests
- Fixed `User-Agent: MycroCloud-FunctionInvoker/1.0`
