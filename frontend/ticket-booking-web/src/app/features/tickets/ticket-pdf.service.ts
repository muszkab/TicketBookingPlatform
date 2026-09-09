import { formatDate } from '@angular/common';
import { Injectable, LOCALE_ID, inject } from '@angular/core';

import { TicketDto, TicketStatus } from '../../api';
import { NotificationService } from '../../core/notifications/notification.service';

@Injectable({ providedIn: 'root' })
export class TicketPdfService {
  private readonly locale = inject(LOCALE_ID);
  private readonly notification = inject(NotificationService);

  async download(ticket: TicketDto, qrCanvas: HTMLCanvasElement | null): Promise<void> {
    try {
      if (!qrCanvas) {
        throw new Error('The QR code could not be rendered.');
      }

      const { jsPDF } = await import('jspdf');
      const { renderTicketPdf } = await import('./ticket-pdf');

      const doc = new jsPDF({ orientation: 'portrait', unit: 'pt', format: 'a4' });
      renderTicketPdf(
        doc,
        {
          eventTitle: ticket.eventTitle,
          ticketCategoryName: ticket.ticketCategoryName,
          location: ticket.location,
          startsAt: formatDate(ticket.startsAt, 'medium', this.locale),
          code: ticket.code,
          status: ticket.status,
          isValid: ticket.status === TicketStatus.Valid
        },
        qrCanvas
      );
      doc.save(`ticket-${ticket.code}.pdf`);
    } catch (err) {
      console.error('Failed to generate the ticket PDF', err);
      this.notification.error('Could not generate the ticket.');
    }
  }
}
