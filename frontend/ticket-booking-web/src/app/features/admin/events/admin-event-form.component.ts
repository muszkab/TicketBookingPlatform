import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatOptionModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { EventCategory, LocationDto } from '../../../api';
import { EVENT_CATEGORY_OPTIONS } from '../../events/shared/event-labels';

export interface AdminEventFormValue {
  title: string;
  description: string;
  category: EventCategory;
  startsAt: string;
  endsAt: string;
  locationId: string;
}

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

function toLocalInputValue(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return '';
  }
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

@Component({
  selector: 'app-admin-event-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatOptionModule,
    MatSelectModule
  ],
  templateUrl: './admin-event-form.component.html',
  styleUrl: './admin-event-form.component.scss'
})
export class AdminEventFormComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input() initialValue: Partial<AdminEventFormValue> | null = null;
  @Input() locations: LocationDto[] = [];
  @Input() locationsLoading = false;
  @Input() disableLocation = false;
  @Input() readOnly = false;
  @Input() submitting = false;
  @Input() submitLabel = 'Save';
  @Input() showCancel = true;

  @Output() readonly formSubmit = new EventEmitter<AdminEventFormValue>();
  @Output() readonly cancelled = new EventEmitter<void>();

  protected readonly categoryOptions = EVENT_CATEGORY_OPTIONS;

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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialValue'] && this.initialValue) {
      this.form.patchValue({
        title: this.initialValue.title ?? '',
        description: this.initialValue.description ?? '',
        category: this.initialValue.category ?? EventCategory.Concert,
        startsAt: this.initialValue.startsAt
          ? toLocalInputValue(this.initialValue.startsAt)
          : '',
        endsAt: this.initialValue.endsAt
          ? toLocalInputValue(this.initialValue.endsAt)
          : '',
        locationId: this.initialValue.locationId ?? ''
      });
      this.form.markAsPristine();
    }
    if (changes['readOnly']) {
      if (this.readOnly) {
        this.form.disable({ emitEvent: false });
      } else {
        this.form.enable({ emitEvent: false });
      }
    }
    if (
      changes['disableLocation'] ||
      changes['locationsLoading'] ||
      changes['readOnly']
    ) {
      this.syncLocationDisabled();
    }
  }

  private syncLocationDisabled(): void {
    if (this.readOnly) {
      return;
    }
    const control = this.form.controls['locationId'];
    if (this.disableLocation || this.locationsLoading) {
      if (control.enabled) {
        control.disable({ emitEvent: false });
      }
    } else if (control.disabled) {
      control.enable({ emitEvent: false });
    }
  }

  protected submit(): void {
    if (this.readOnly || this.submitting) {
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

    this.formSubmit.emit({
      title: raw.title.trim(),
      description: raw.description ?? '',
      category: raw.category,
      startsAt: new Date(raw.startsAt).toISOString(),
      endsAt: new Date(raw.endsAt).toISOString(),
      locationId: raw.locationId
    });
  }

  protected cancel(): void {
    this.cancelled.emit();
  }
}
