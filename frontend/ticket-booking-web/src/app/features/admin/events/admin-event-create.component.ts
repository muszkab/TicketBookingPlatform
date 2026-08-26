import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatOptionModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';

import {
  CreateEventRequest,
  EventCategory,
  EventsService,
  LocationDto,
  LocationsService,
  ProblemDetails
} from '../../../api';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  EVENT_CATEGORY_OPTIONS,
  eventCategoryLabel
} from '../../events/shared/event-labels';

const LOCATION_PAGE_SIZE = 100;

function dateRangeValidator(startKey: string, endKey: string): ValidatorFn {
  return (group): ValidationErrors | null => {
    const start = group.get(startKey)?.value as string | null;
    const end = group.get(endKey)?.value as string | null;
    if (!start || !end) {
      return null;
    }
    return Date.parse(end) > Date.parse(start) ? null : { dateRange: true };
  };
}

@Component({
  selector: 'app-admin-event-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatOptionModule,
    MatProgressBarModule,
    MatSelectModule
  ],
  templateUrl: './admin-event-create.component.html',
  styleUrl: './admin-event-create.component.scss'
})
export class AdminEventCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly eventsService = inject(EventsService);
  private readonly locationsService = inject(LocationsService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly categoryOptions = EVENT_CATEGORY_OPTIONS;
  protected readonly categoryLabel = eventCategoryLabel;

  protected readonly locations = signal<LocationDto[]>([]);
  protected readonly locationsLoading = signal(true);
  protected readonly locationsError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form: FormGroup = this.fb.nonNullable.group(
    {
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(2000)]],
      category: [EventCategory.Concert, [Validators.required]],
      startsAt: ['', [Validators.required]],
      endsAt: ['', [Validators.required]],
      locationId: ['', [Validators.required]]
    },
    { validators: [dateRangeValidator('startsAt', 'endsAt')] }
  );

  ngOnInit(): void {
    this.loadLocations();
  }

  protected reloadLocations(): void {
    this.loadLocations();
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue() as {
      title: string;
      description: string;
      category: EventCategory;
      startsAt: string;
      endsAt: string;
      locationId: string;
    };

    const request: CreateEventRequest = {
      title: raw.title.trim(),
      description: raw.description ?? '',
      category: raw.category,
      startsAt: new Date(raw.startsAt).toISOString(),
      endsAt: new Date(raw.endsAt).toISOString(),
      locationId: raw.locationId
    };

    this.submitting.set(true);
    this.formError.set(null);

    this.eventsService
      .createEvent(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.submitting.set(false);
          this.notifications.success(`Event "${created.title}" created as Draft.`);
          this.router.navigate(['/admin/events']);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          console.error('Create event failed', err);
          this.formError.set(this.mapError(err));
        }
      });
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
