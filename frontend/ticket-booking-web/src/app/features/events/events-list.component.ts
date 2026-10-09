import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';

import { ErrorCardComponent } from '../../core/error-card/error-card.component';

import { EventStatus, LocationDto, LocationsService } from '../../api';
import { createEventsListState } from './shared/events-list-state';

@Component({
  selector: 'app-events-list',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    RouterLink,
    ErrorCardComponent
  ],
  templateUrl: './events-list.component.html',
  styleUrl: './events-list.component.scss'
})
export class EventsListComponent implements OnInit {
  private readonly locationsService = inject(LocationsService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly state = createEventsListState({
    includeLocation: true,
    defaultStatus: EventStatus.OnSale
  });

  protected readonly events = this.state.events;
  protected readonly totalCount = this.state.totalCount;
  protected readonly loading = this.state.loading;
  protected readonly error = this.state.error;

  protected readonly category = this.state.category;
  protected readonly status = this.state.status;
  protected readonly locationId = this.state.locationId;
  protected readonly page = this.state.page;
  protected readonly pageSize = this.state.pageSize;

  protected readonly categoryOptions = this.state.categoryOptions;
  protected readonly statusOptions = this.state.statusOptions;
  protected readonly pageSizeOptions = this.state.pageSizeOptions;
  protected readonly categoryLabel = this.state.categoryLabel;
  protected readonly statusLabel = this.state.statusLabel;

  protected readonly reload = this.state.reload;
  protected readonly onCategoryChange = this.state.onCategoryChange;
  protected readonly onStatusChange = this.state.onStatusChange;
  protected readonly onLocationChange = this.state.onLocationChange;
  protected readonly onPageChange = this.state.onPageChange;

  protected readonly locations = signal<LocationDto[]>([]);

  private readonly locationsById = computed(
    () => new Map(this.locations().map((loc) => [loc.id, loc]))
  );

  protected locationLabel(locationId: string): string | null {
    const loc = this.locationsById().get(locationId);
    return loc ? `${loc.name} — ${loc.city}` : null;
  }

  ngOnInit(): void {
    this.locationsService
      .getLocations(undefined, undefined, 1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.locations.set(result.items ?? []),
        error: (err) => console.error('Failed to load locations', err)
      });

    this.state.initFromRoute();
  }
}
