import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';

import {
  CreateEventRequest,
  EventsService,
  LocationDto,
  LocationsService,
  ProblemDetails
} from '../../../api';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  AdminEventFormComponent,
  AdminEventFormValue
} from './admin-event-form.component';

const LOCATION_PAGE_SIZE = 100;

@Component({
  selector: 'app-admin-event-create',
  imports: [
    RouterLink,
    AdminEventFormComponent,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule
  ],
  templateUrl: './admin-event-create.component.html',
  styleUrl: './admin-event-create.component.scss'
})
export class AdminEventCreateComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly locationsService = inject(LocationsService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly locations = signal<LocationDto[]>([]);
  protected readonly locationsLoading = signal(true);
  protected readonly locationsError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadLocations();
  }

  protected reloadLocations(): void {
    this.loadLocations();
  }

  protected onSubmit(value: AdminEventFormValue): void {
    const request: CreateEventRequest = { ...value };
    this.submitting.set(true);
    this.formError.set(null);

    this.eventsService
      .createEvent(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.submitting.set(false);
          this.notifications.success(`Event "${created.title}" created as Draft.`);
          this.router.navigate(['/admin/events', created.id, 'edit']);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          console.error('Create event failed', err);
          this.formError.set(this.mapError(err));
        }
      });
  }

  protected cancel(): void {
    this.router.navigate(['/admin/events']);
  }

  private loadLocations(): void {
    this.locationsLoading.set(true);
    this.locationsError.set(null);
    this.locationsService
      .getLocations(undefined, undefined, 1, LOCATION_PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.locations.set(result.items ?? []);
          this.locationsLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load locations', err);
          this.locationsError.set('Failed to load locations.');
          this.locationsLoading.set(false);
        }
      });
  }

  private mapError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as ProblemDetails | undefined;
      if (problem?.detail) {
        return problem.detail;
      }
      if (problem?.title) {
        return problem.title;
      }
      if (err.status === 0) {
        return 'Cannot reach the server.';
      }
    }
    return 'Failed to create event. Please try again.';
  }
}
