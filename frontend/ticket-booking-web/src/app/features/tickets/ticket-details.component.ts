import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { TicketDto, TicketStatus, TicketsService } from '../../api';
import { mapProblemDetails } from '../../core/http/map-error';
import { QrCodeComponent } from '../../core/qr-code/qr-code.component';
import { TicketPdfService } from './ticket-pdf.service';

interface StatusPresentation {
  readonly icon: string;
  readonly color: 'primary' | 'accent' | 'warn' | undefined;
  readonly label: string;
}

@Component({
  selector: 'app-ticket-details',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    QrCodeComponent,
    RouterLink
  ],
  templateUrl: './ticket-details.component.html',
  styleUrl: './ticket-details.component.scss'
})
export class TicketDetailsComponent implements OnInit {
  private readonly ticketsService = inject(TicketsService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly ticketPdf = inject(TicketPdfService);

  protected readonly ticket = signal<TicketDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);

  protected readonly statusPresentation = computed<StatusPresentation | null>(() => {
    const t = this.ticket();
    if (!t) return null;
    switch (t.status) {
      case TicketStatus.Valid:
        return { icon: 'check_circle', color: 'primary', label: t.status };
      case TicketStatus.Used:
        return { icon: 'history', color: undefined, label: t.status };
      case TicketStatus.Cancelled:
        return { icon: 'block', color: 'warn', label: t.status };
      default:
        return { icon: 'help', color: undefined, label: t.status };
    }
  });

  protected readonly isActive = computed(() => this.ticket()?.status === TicketStatus.Valid);

  @ViewChild(QrCodeComponent) private qrCode?: QrCodeComponent;

  protected readonly downloading = signal(false);

  protected async download(): Promise<void> {
    const t = this.ticket();
    if (!t || this.downloading()) return;

    this.downloading.set(true);
    try {
      await this.ticketPdf.download(t, this.qrCode?.getCanvas() ?? null);
    } finally {
      this.downloading.set(false);
    }
  }

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return EMPTY;
          }
          this.loading.set(true);
          this.error.set(null);
          this.notFound.set(false);
          return this.ticketsService.getTicketById(id).pipe(
            catchError((err: HttpErrorResponse) => {
              if (err.status === 404) {
                this.notFound.set(true);
              } else {
                this.error.set(mapProblemDetails(err, 'Failed to load the ticket.'));
              }
              this.loading.set(false);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((ticket) => {
        this.ticket.set(ticket);
        this.loading.set(false);
      });
  }
}
