/**
 * T11: k6 Performance Benchmark for JNPF Logging Pipeline
 *
 * Sends 1000 requests with 50 virtual users to measure P50/P95/P99 latency.
 * Use this for accurate load testing instead of the PowerShell script.
 *
 * Prerequisites:
 *   - k6 installed: https://k6.io/docs/getting-started/installation/
 *   - JNPF API running at the configured BASE_URL
 *
 * Usage:
 *   k6 run t11_k6_benchmark.js
 *   k6 run --env BASE_URL=http://localhost:5000 t11_k6_benchmark.js
 *   k6 run --vus 100 --duration 30s t11_k6_benchmark.js
 *
 * References:
 *   - RequestActionFilter: backend/modularity/common/JNPF.Common.Core/Filter/RequestActionFilter.cs
 *   - TraceIdMiddleware: backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs
 */

import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// ── Configuration ──────────────────────────────────────────────────────────

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";

// Endpoints to cycle through (read-only, no side effects)
const ENDPOINTS = [
  "/api/system/TechnicalLog/errors",
  "/api/system/TechnicalLog/slow-requests",
  "/api/system/TechnicalLog/trace?traceId=benchmark-test",
];

// Custom metrics
const traceIdPresent = new Rate("trace_id_present");
const latencyTrend = new Trend("req_latency", true);

// ── Test Options ───────────────────────────────────────────────────────────

export const options = {
  // Scenario: 50 VUs sending a total of 1000 requests
  scenarios: {
    benchmark: {
      executor: "shared-iterations",
      vus: 50,
      iterations: 1000,
      maxDuration: "2m",
    },
  },

  // Thresholds — the test fails if these are breached
  thresholds: {
    // P99 must be under 100ms (generous baseline)
    http_req_duration: ["p(99)<100"],
    // 99% of requests must succeed
    http_req_failed: ["rate<0.01"],
    // TraceId header must be present on all responses
    trace_id_present: ["rate>0.99"],
  },
};

// ── Setup ──────────────────────────────────────────────────────────────────

export function setup() {
  // Warmup: send 10 requests to prime JIT caches
  console.log(`Warming up against ${BASE_URL} ...`);
  for (let i = 0; i < 10; i++) {
    http.get(`${BASE_URL}/api/system/TechnicalLog/errors`);
  }
  console.log("Warmup complete.");
  return { baseUrl: BASE_URL };
}

// ── Main Test ──────────────────────────────────────────────────────────────

export default function (data) {
  const endpoint = ENDPOINTS[__ITER % ENDPOINTS.length];
  const url = `${data.baseUrl}${endpoint}`;

  const params = {
    headers: {
      Accept: "application/json",
      "User-Agent": "k6-benchmark/1.0",
    },
    timeout: "30s",
  };

  const res = http.get(url, params);

  // Record custom metrics
  latencyTrend.add(res.timings.duration);
  traceIdPresent.add(res.headers["X-Trace-Id"] !== undefined);

  // Standard checks
  check(res, {
    "status is 2xx or 4xx (not 5xx)": (r) => r.status < 500,
    "X-Trace-Id header present": (r) => r.headers["X-Trace-Id"] !== undefined,
    "response time < 200ms": (r) => r.timings.duration < 200,
    "response body is not empty": (r) => r.body && r.body.length > 0,
  });

  // Small pause to avoid overwhelming the server (remove for max throughput)
  sleep(0.01);
}

// ── Teardown ───────────────────────────────────────────────────────────────

export function teardown(data) {
  console.log("Benchmark complete.");
}

// ── Summary Handler ────────────────────────────────────────────────────────

export function handleSummary(data) {
  const p50 = data.metrics.http_req_duration.values["p(50)"];
  const p95 = data.metrics.http_req_duration.values["p(95)"];
  const p99 = data.metrics.http_req_duration.values["p(99)"];
  const avg = data.metrics.http_req_duration.values["avg"];
  const max = data.metrics.http_req_duration.values["max"];
  const min = data.metrics.http_req_duration.values["min"];
  const total = data.metrics.http_reqs.values.count;
  const rps = data.metrics.http_reqs.values.rate;
  const failed = data.metrics.http_req_failed.values.rate * 100;

  const summary = `
================================================================================
  T11: k6 Benchmark Results
================================================================================

  Endpoint Base:    ${BASE_URL}
  Total Requests:   ${total}
  Requests/sec:     ${rps.toFixed(1)}
  Failed:           ${failed.toFixed(2)}%

  Latency Distribution:
  ─────────────────────────────────────
  Min:     ${min.toFixed(2)} ms
  P50:     ${p50.toFixed(2)} ms
  P95:     ${p95.toFixed(2)} ms
  P99:     ${p99.toFixed(2)} ms
  Max:     ${max.toFixed(2)} ms
  Avg:     ${avg.toFixed(2)} ms

  Thresholds:
  ─────────────────────────────────────
  P99 < 100ms:      ${p99 < 100 ? "PASS" : "FAIL"} (${p99.toFixed(2)} ms)
  Error rate < 1%:  ${failed < 1 ? "PASS" : "FAIL"} (${failed.toFixed(2)}%)
  TraceId present:  ${data.metrics.trace_id_present.values.rate > 0.99 ? "PASS" : "FAIL"}

================================================================================
`;

  console.log(summary);

  // Write JSON summary to file for CI integration
  return {
    "docs/verification/t11_k6_summary.json": JSON.stringify(
      {
        timestamp: new Date().toISOString(),
        baseUrl: BASE_URL,
        totalRequests: total,
        rps: rps,
        failedPercent: failed,
        latency: { min, p50, p95, p99, max, avg },
        thresholds: {
          p99Under100: p99 < 100,
          errorRateUnder1: failed < 1,
          traceIdPresent: data.metrics.trace_id_present.values.rate > 0.99,
        },
      },
      null,
      2
    ),
    stdout: summary,
  };
}
