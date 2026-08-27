import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatOptionModule } from '@angular/material/core';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';

import { EventDto, EventStatus } from '../../../api';
import { ErrorCardComponent } from '../../../core/error-card/error-card.component';
import { createEventsListState } from '../../events/shared/events-list-state';
import { AdminEventActionsService } from './shared/admin-event-actions.service';

@Component({
  selector: 'app-admin-events-list',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatMenuModule,
    MatOptionModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ErrorCardComponent
  ],
  templateUrl: './admin-events-list.component.html',
  styleUrl: './admin-events-list.component.scss'
})
export class AdminEventsListComponent implements OnInit {
  private readonly actions = inject(AdminEventActionsService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly state = createEventsListState({
    errorLogPrefix: 'Failed to load admin events'
  });

  protected readonly events = this.state.events;
  protected readonly totalCount = this.state.totalCount;
  protected readonly loading = this.state.loading;
  protected readonly error = this.state.error;

  protected readonly category = this.state.category;
  protected readonly status = this.state.status;
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
  protected readonly onPageChange = this.state.onPageChange;

  protected readonly acting = signal(false);

  protected readonly displayedColumns = [
    'title',
    'category',
    'status',
    'startsAt',
    'actions'
  ] as const;

  ngOnInit(): void {
    this.state.initFromRoute();
  }

  protected canPublish(evt: EventDto): boolean {
    return evt.status === EventStatus.Draft;
  }

  protected canCancel(evt: EventDto): boolean {
    return (
      evt.status !== EventStatus.Cancelled && evt.status !== EventStatus.Completed
    );
  }

  protected publish(evt: EventDto): void {
    this.actions
      .publish(evt, this.acting)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.state.triggerLoad());
  }

  protected cancelEvent(evt: EventDto): void {
    this.actions
      .cancel(evt, this.acting)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.state.triggerLoad());
  }
}
