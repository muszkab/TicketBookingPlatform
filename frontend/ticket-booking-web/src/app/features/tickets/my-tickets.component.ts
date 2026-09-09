import { DatePipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { PagedResultOfTicketDto, TicketDto, TicketStatus, TicketsService } from '../../api';
import { mapProblemDetails } from '../../core/http/map-error';
import { QrCodeComponent } from '../../core/qr-code/qr-code.component';
import { TicketPdfService } from './ticket-pdf.service';

const DEFAULT_PAGE_SIZE = 20;
const PAGE_SIZE_OPTIONS = [10, 20, 50];
const QR_RENDER_ATTEMPTS = 20;
const QR_RENDER_DELAY_MS = 25;

interface TicketsQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly status: TicketStatus | null;
  readonly orderId: string | null;
}

@Component({
  selector: 'app-my-tickets',
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    QrCodeComponent,
    RouterLink
  ],
  templateUrl: './my-tickets.component.html',
  styleUrl: './my-tickets.component.scss'
})
export class MyTicketsComponent implements OnInit {
  private readonly ticketsService = inject(TicketsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly ticketPdf = inject(TicketPdfService);

  protected readonly tickets = signal<TicketDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  protected readonly status = signal<TicketStatus | null>(null);
  protected readonly orderId = signal<string | null>(null);

  protected readonly displayedColumns = ['eventTitle', 'category', 'code', 'status', 'createdAt', 'actions'];
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  protected readonly statuses = TicketStatus;
  protected readonly statusOptions: readonly TicketStatus[] = [
    TicketStatus.Valid,
    TicketStatus.Used,
    TicketStatus.Cancelled
  ];

  protected readonly hasOrderFilter = computed(() => this.orderId() !== null);

  protected readonly orderEventTitle = computed(() => {
    if (!this.hasOrderFilter()) return null;
    const titles = [...new Set(this.tickets().map((t) => t.eventTitle))];
    return titles.length > 0 ? titles.join(', ') : null;
  });

  protected readonly pdfTicket = signal<TicketDto | null>(null);
  protected readonly downloadingTicketId = signal<string | null>(null);

  private readonly pdfQrCode = viewChild(QrCodeComponent);

  private readonly load$ = new Subject<TicketsQuery>();

  constructor() {
    this.load$
      .pipe(
        switchMap((q) =>
          this.ticketsService.getMyTickets(q.status ?? undefined, q.orderId ?? undefined, q.page, q.pageSize)
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result: PagedResultOfTicketDto) => {
          this.tickets.set(result.items ?? []);
          this.totalCount.set(result.totalCount ?? 0);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          console.error('Failed to load my tickets', err);
          this.error.set(mapProblemDetails(err, 'Failed to load your tickets.'));
          this.loading.set(false);
        }
      });
  }

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const rawPage = Number(params.get('page'));
      const rawPageSize = Number(params.get('pageSize'));
      this.page.set(Number.isFinite(rawPage) && rawPage > 0 ? rawPage : 1);
      this.pageSize.set(
        PAGE_SIZE_OPTIONS.includes(rawPageSize) ? rawPageSize : DEFAULT_PAGE_SIZE
      );
      this.status.set(this.parseStatus(params.get('status')));
      this.orderId.set(params.get('orderId'));
      this.triggerLoad();
    });
  }

  protected onPageChange(event: PageEvent): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: event.pageIndex + 1, pageSize: event.pageSize },
      queryParamsHandling: 'merge'
    });
  }

  protected onStatusChange(status: TicketStatus | null): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { status: status ?? null, page: 1 },
      queryParamsHandling: 'merge'
    });
  }

  protected clearOrderFilter(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { orderId: null, page: 1 },
      queryParamsHandling: 'merge'
    });
  }

  protected statusColor(status: TicketStatus): 'primary' | 'accent' | 'warn' | undefined {
    switch (status) {
      case TicketStatus.Valid:
        return 'primary';
      case TicketStatus.Cancelled:
        return 'warn';
      default:
        return undefined;
    }
  }

  protected trackTicket = (_: number, t: TicketDto): string => t.id;

  protected async download(ticket: TicketDto): Promise<void> {
    if (this.downloadingTicketId() !== null) return;

    this.downloadingTicketId.set(ticket.id);
    this.pdfTicket.set(ticket);
    try {
      await this.ticketPdf.download(ticket, await this.waitForQrCanvas());
    } finally {
      this.pdfTicket.set(null);
      this.downloadingTicketId.set(null);
    }
  }

  private async waitForQrCanvas(): Promise<HTMLCanvasElement | null> {
    for (let attempt = 0; attempt < QR_RENDER_ATTEMPTS; attempt++) {
      await new Promise((resolve) => setTimeout(resolve, QR_RENDER_DELAY_MS));
      const canvas = this.pdfQrCode()?.getCanvas();
      if (canvas && canvas.width > 0) {
        return canvas;
      }
    }

    return null;
  }

  private parseStatus(value: string | null): TicketStatus | null {
    return value && (Object.values(TicketStatus) as string[]).includes(value)
      ? (value as TicketStatus)
      : null;
  }

  private triggerLoad(): void {
    this.loading.set(true);
    this.error.set(null);
    this.load$.next({
      page: this.page(),
      pageSize: this.pageSize(),
      status: this.status(),
      orderId: this.orderId()
    });
  }
}
