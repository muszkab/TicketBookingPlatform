import { HttpErrorResponse } from '@angular/common/http';

import { ProblemDetails } from '../../api';

export function mapProblemDetails(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const problem = err.error as ProblemDetails | undefined;
    if (problem?.detail) {
      return problem.detail;
    }
    if (problem?.title) {
      return problem.title;
    }
    if (err.status === 0) {
      return 'Cannot reach the server. Please check your connection and try again.';
    }
  }
  return fallback;
}
