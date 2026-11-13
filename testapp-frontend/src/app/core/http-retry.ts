import { HttpErrorResponse } from '@angular/common/http';
import { MonoTypeOperatorFunction, retry, timer } from 'rxjs';

/** Retry schedule for transient failures. Exported so tests can drive the timers. */
export const RETRY_COUNT = 2;
export const RETRY_DELAY_MS = 300;

/**
 * Retries requests that failed for reasons likely to go away on their own.
 *
 * The API runs on Container Apps with minReplicas 0, so a request can
 * legitimately fail while the app is cold-starting, or land on a replica that
 * is being terminated during scale-down. Both surface as status 0. Retrying
 * covers those without masking genuine errors: a 4xx (including a real CORS
 * rejection reaching the app) is rethrown immediately.
 */
export function retryTransient<T>(): MonoTypeOperatorFunction<T> {
  return retry({
    count: RETRY_COUNT,
    delay: (error: HttpErrorResponse, retryCount) => {
      const transient = error.status === 0 || error.status >= 500;
      if (!transient) {
        throw error;
      }
      return timer(retryCount * RETRY_DELAY_MS);
    },
  });
}
