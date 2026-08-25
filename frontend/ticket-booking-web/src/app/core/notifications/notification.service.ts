import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

const DEFAULT_DURATION = 4000;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string, config?: MatSnackBarConfig): void {
    this.show(message, 'notification--success', config);
  }

  error(message: string, config?: MatSnackBarConfig): void {
    this.show(message, 'notification--error', { duration: 6000, ...config });
  }

  info(message: string, config?: MatSnackBarConfig): void {
    this.show(message, 'notification--info', config);
  }

  private show(message: string, panelClass: string, config?: MatSnackBarConfig): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: DEFAULT_DURATION,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass: [panelClass],
      ...config
    });
  }
}
