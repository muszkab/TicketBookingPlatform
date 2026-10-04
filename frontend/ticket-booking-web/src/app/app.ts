import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './core/auth/auth.service';

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

  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly userName = this.auth.userName;
  protected readonly isAdmin = this.auth.isAdmin;

  // Dismissible per view only: deliberately NOT persisted, so the notice reappears on every load.
  protected readonly showDemoNotice = signal(true);

  protected dismissDemoNotice(): void {
    this.showDemoNotice.set(false);
  }

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/events']);
  }
}
