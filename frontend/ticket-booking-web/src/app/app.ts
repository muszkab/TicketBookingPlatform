import { Component, DestroyRef, VERSION, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { environment } from '../environments/environment';
import { ApiInfoService } from './core/api-info/api-info.service';
import { AuthService } from './core/auth/auth.service';
import { ConfirmDialogComponent, ConfirmDialogData } from './core/dialogs/confirm-dialog.component';

const SIGN_OUT_CONFIRMATION: ConfirmDialogData = {
  title: 'Sign out?',
  message: 'You will be signed out of your account. You can sign in again at any time.',
  confirmLabel: 'Sign out',
  cancelLabel: 'Stay signed in'
};

const CONTACT_EMAIL = 'm1musbal@gmail.com';
const GITHUB_REPO_URL = 'https://github.com/muszkab/TicketBookingPlatform';

const API_FRAMEWORK_LABEL = 'ASP.NET Core';
const API_FRAMEWORK_FALLBACK = `${API_FRAMEWORK_LABEL} 9`;

/** `2026-10-04T09:33:11.817Z` -> `2026-10-04 09:33 UTC` (locale-independent on purpose). */
function formatBuildTime(isoDate: string): string {
  const date = new Date(isoDate);

  if (Number.isNaN(date.getTime())) {
    return isoDate;
  }

  const pad = (value: number) => String(value).padStart(2, '0');
  const day = `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`;
  const time = `${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}`;

  return `${day} ${time} UTC`;
}

/** Extra build identity for the tooltip: a bare patch version says little in a bug report. */
function buildVersionTooltip(): string {
  const parts: string[] = [];

  if (environment.appCommit) {
    parts.push(`commit ${environment.appCommit}`);
  }

  if (environment.appBuildDate) {
    parts.push(`built ${formatBuildTime(environment.appBuildDate)}`);
  }

  return parts.join(' · ');
}

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
  private readonly apiInfo = inject(ApiInfoService);

  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly userName = this.auth.userName;
  protected readonly isAdmin = this.auth.isAdmin;

  protected readonly contactEmail = CONTACT_EMAIL;
  protected readonly githubUrl = GITHUB_REPO_URL;
  protected readonly currentYear = new Date().getFullYear();
  protected readonly appVersion = environment.appVersion;
  protected readonly angularVersion = VERSION.major;
  protected readonly apiFramework = computed(() => {
    const major = this.apiInfo.runtimeMajor();
    return major === null ? API_FRAMEWORK_FALLBACK : `${API_FRAMEWORK_LABEL} ${major}`;
  });
  protected readonly versionTitle = computed(() => {
    const apiVersion = this.apiInfo.version();
    return apiVersion ? `${buildVersionTooltip()} · API ${apiVersion}` : buildVersionTooltip();
  });

  // Dismissible per view only: deliberately NOT persisted, so the notice reappears on every load.
  protected readonly showDemoNotice = signal(true);

  constructor() {
    this.apiInfo.load();
  }

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
