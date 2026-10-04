import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './core/auth/auth.service';
import { ConfirmDialogComponent, ConfirmDialogData } from './core/dialogs/confirm-dialog.component';

const SIGN_OUT_CONFIRMATION: ConfirmDialogData = {
  title: 'Sign out?',
  message: 'You will be signed out of your account. You can sign in again at any time.',
  confirmLabel: 'Sign out',
  cancelLabel: 'Stay signed in'
};

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly userName = this.auth.userName;
  protected readonly isAdmin = this.auth.isAdmin;

  // Dismissible per view only: deliberately NOT persisted, so the notice reappears on every load.
  protected readonly showDemoNotice = signal(true);

  protected dismissDemoNotice(): void {
    this.showDemoNotice.set(false);
  }

  protected logout(): void {
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: SIGN_OUT_CONFIRMATION
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }
        this.auth.logout();
        this.router.navigate(['/events']);
      });
  }
}
