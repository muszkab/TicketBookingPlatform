import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { EventsService } from './core/api/events.service';
import { EventDto, PagedResult } from './core/api/api-types';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly eventsService = inject(EventsService);

  protected readonly title = signal('ticket-booking-web');
  protected readonly events = signal<EventDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.eventsService.getAll().subscribe({
      next: (result: PagedResult<EventDto>) => {
        this.events.set(result.items);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Failed to load events', err);
        this.error.set('Failed to load events. Is the backend running on https://localhost:5001?');
        this.loading.set(false);
      }
    });
  }
}
