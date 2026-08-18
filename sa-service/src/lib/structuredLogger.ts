export function logStep(event: {
  level: 'info' | 'warn' | 'error';
  runId?: string;
  tenantId?: string;
  projectId?: string;
  eventId?: string;
  stepName?: string;
  elapsedMs?: number;
  message: string;
  extra?: Record<string, unknown>;
}): void {
  const line = JSON.stringify({
    ts: new Date().toISOString(),
    service: 'sa-service',
    ...event,
  });
  if (event.level === 'error') {
    console.error(line);
  } else if (event.level === 'warn') {
    console.warn(line);
  } else {
    console.log(line);
  }
}
