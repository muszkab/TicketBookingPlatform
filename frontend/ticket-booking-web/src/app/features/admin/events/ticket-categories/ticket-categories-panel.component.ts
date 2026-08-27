import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  Output,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatOptionModule } from '@angular/material/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter, switchMap } from 'rxjs';

import {
  AddTicketCategoryRequest,
  EventsService,
  ProblemDetails,
  TicketCategoryDto
} from '../../../../api';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../../core/dialogs/confirm-dialog.component';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { SUPPORTED_CURRENCIES } from '../../../events/shared/event-labels';

@Component({
  selector: 'app-ticket-categories-panel',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatOptionModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './ticket-categories-panel.component.html',
  styleUrl: './ticket-categories-panel.component.scss'
})
export class TicketCategoriesPanelComponent {
  private readonly fb = inject(FormBuilder);
  private readonly eventsService = inject(EventsService);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  @Input({ required: true }) eventId!: string;
  @Input() ticketCategories: TicketCategoryDto[] = [];
  @Input() locationCapacity: number | null = null;
  @Input() readOnly = false;

  @Output() readonly changed = new EventEmitter<void>();

  protected get remainingCapacity(): number | null {
    if (this.locationCapacity === null) {
      return null;
    }
    const used = this.ticketCategories.reduce((sum, tc) => sum + tc.totalQuantity, 0);
    return Math.max(this.locationCapacity - used, 0);
  }

  protected readonly currencies = SUPPORTED_CURRENCIES;
  protected readonly submitting = signal(false);
  protected readonly acting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly displayedColumns = [
    'name',
    'price',
    'quantity',
    'actions'
  ] as const;

  protected readonly form: FormGroup = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    price: [0, [Validators.required, Validators.min(0)]],
    currency: [SUPPORTED_CURRENCIES[0], [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]]
  });

  protected addCategory(): void {
    if (this.readOnly || this.submitting()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue() as {
      name: string;
      price: number;
      currency: string;
      quantity: number;
    };

    const request: AddTicketCategoryRequest = {
      name: raw.name.trim(),
      price: raw.price,
      currency: raw.currency,
      quantity: raw.quantity
    };

    this.submitting.set(true);
    this.formError.set(null);
    this.eventsService
      .addTicketCategory(this.eventId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.notifications.success(`Ticket category "${request.name}" added.`);
          this.form.reset({
            name: '',
            price: 0,
            currency: SUPPORTED_CURRENCIES[0],
            quantity: 1
          });
          this.changed.emit();
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          console.error('Add ticket category failed', err);
          this.formError.set(this.mapError(err, 'Failed to add ticket category.'));
        }
      });
  }

  protected removeCategory(category: TicketCategoryDto): void {
    if (this.readOnly) {
      return;
    }
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: {
          title: 'Remove ticket category',
          message: `Remove "${category.name}"? This cannot be undone.`,
          confirmLabel: 'Remove',
          cancelLabel: 'Keep',
          confirmColor: 'warn'
        },
        width: '420px'
      })
      .afterClosed()
      .pipe(
        filter((ok) => ok === true),
        switchMap(() => {
          this.acting.set(true);
          return this.eventsService.removeTicketCategory(this.eventId, category.id);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.acting.set(false);
          this.notifications.success(`"${category.name}" removed.`);
          this.changed.emit();
        },
        error: (err: unknown) => {
          this.acting.set(false);
          console.error('Remove ticket category failed', err);
          this.notifications.error(
            this.mapError(err, 'Failed to remove ticket category.')
          );
        }
      });
  }

  private mapError(err: unknown, fallback: string): string {
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
    return fallback;
  }
}
