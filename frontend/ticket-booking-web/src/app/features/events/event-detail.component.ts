import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { EMPTY, catchError, forkJoin, of, switchMap } from 'rxjs';

import {
  EventDto,
  EventsService,
  LocationDto,
  LocationsService,
  TicketCategoryDto
} from '../../api';
import { eventCategoryLabel, eventStatusLabel } from './shared/event-labels';

@Component({
  selector: 'app-event-detail',
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss'
})
export class EventDetailComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly locationsService = inject(LocationsService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly event = signal<EventDto | null>(null);
  protected readonly location = signal<LocationDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);

  protected readonly ticketColumns = ['name', 'price', 'available'];

  protected readonly categoryLabel = eventCategoryLabel;
  protected readonly statusLabel = eventStatusLabel;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          this.loading.set(true);
          this.error.set(null);
          this.notFound.set(false);
          this.event.set(null);
          this.location.set(null);

          if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return EMPTY;
          }

          return this.eventsService.getEventById(id).pipe(
            switchMap((evt) =>
              forkJoin({
                event: of(evt),
                location: this.locationsService
                  .getLocationById(evt.locationId)
                  .pipe(catchError(() => of<LocationDto | null>(null)))
              })
            ),
            catchError((err: unknown) => {
              if (err instanceof HttpErrorResponse && err.status === 404) {
                this.notFound.set(true);
              } else {
                console.error('Failed to load event', err);
                this.error.set('Failed to load event. Is the backend running on https://localhost:5001?');
              }
              this.loading.set(false);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ event, location }) => {
        this.event.set(event);
        this.location.set(location);
        this.loading.set(false);
      });
  }

  protected trackTicket = (_: number, item: TicketCategoryDto): string => item.id;
}
