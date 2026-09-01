import { WritableSignal } from '@angular/core';
import { MonoTypeOperatorFunction, Observable, defer, finalize } from 'rxjs';

/**
 * Sets `lock` to `true` when the source is subscribed and back to `false`
 * when the stream completes, errors, or is unsubscribed. Use to keep
 * action buttons disabled for the whole duration of an async operation.
 */
export function withActionLock<T>(
  lock: WritableSignal<boolean>
): MonoTypeOperatorFunction<T> {
  return (source: Observable<T>) =>
    defer(() => {
      lock.set(true);
      return source.pipe(finalize(() => lock.set(false)));
    });
}
