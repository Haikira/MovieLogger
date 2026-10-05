import { vi } from 'vitest';

/**
 * A tiny fetch stub for tests. Register handlers per "METHOD /path"; each receives the parsed
 * request and returns a response. Unhandled requests fail loudly so tests can't silently depend
 * on endpoints they didn't set up. Every request is recorded for assertions.
 */

export interface RecordedRequest {
  method: string;
  path: string;
  query: URLSearchParams;
  body: unknown;
  headers: Record<string, string>;
}

type Handler = (request: RecordedRequest) => Response | Promise<Response>;

export function json(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
  });
}

export function noContent(): Response {
  return new Response(null, { status: 204 });
}

export function validationProblem(errors: Record<string, string[]>): Response {
  return json(400, { title: 'One or more validation errors occurred.', status: 400, errors });
}

export class MockApi {
  readonly requests: RecordedRequest[] = [];
  private readonly handlers = new Map<string, Handler>();

  on(method: string, path: string, handler: Handler | Response | object): this {
    const fn: Handler =
      typeof handler === 'function'
        ? (handler as Handler)
        : handler instanceof Response
          ? () => (handler as Response).clone()
          : () => json(200, handler);
    this.handlers.set(`${method.toUpperCase()} ${path}`, fn);
    return this;
  }

  /** Requests matching a method and path (query string ignored). */
  calls(method: string, path: string): RecordedRequest[] {
    return this.requests.filter((r) => r.method === method.toUpperCase() && r.path === path);
  }

  install(): this {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = new URL(String(input), 'http://localhost');
        const method = (init?.method ?? 'GET').toUpperCase();
        const headers = Object.fromEntries(Object.entries((init?.headers as Record<string, string>) ?? {}));
        const request: RecordedRequest = {
          method,
          path: url.pathname,
          query: url.searchParams,
          body: typeof init?.body === 'string' ? JSON.parse(init.body) : undefined,
          headers,
        };
        this.requests.push(request);
        const handler = this.handlers.get(`${method} ${url.pathname}`);
        if (!handler) {
          throw new Error(`Unhandled API request in test: ${method} ${url.pathname}${url.search}`);
        }
        return handler(request);
      }),
    );
    return this;
  }
}
