import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turn an HTTP failure into something actionable.
 *
 * Status 0 means no response was received at all, so the cause is genuinely
 * ambiguous. Naming only CORS here was actively misleading: with the API on
 * Container Apps at minReplicas 0, a dropped connection during scale-down or a
 * cold start produces exactly the same status. Requests have already been
 * retried by retryTransient before reaching this point.
 */
export function describeError(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return (
      'No response from the API after retrying. The cause cannot be determined from ' +
      'the browser alone — it may be starting up from zero replicas, a dropped ' +
      'connection, or a CORS rejection. The browser console shows which.'
    );
  }

  if (err.status >= 500) {
    return `The API failed with ${err.status} ${err.statusText || ''}`.trim();
  }

  return `The API rejected the request: ${err.status} ${err.statusText || ''}`.trim();
}
