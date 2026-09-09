import type { jsPDF } from 'jspdf';

import { ROBOTO_BOLD_BASE64 } from './fonts/roboto-bold.base64';
import { ROBOTO_REGULAR_BASE64 } from './fonts/roboto-regular.base64';

export interface TicketPdfData {
  readonly eventTitle: string;
  readonly ticketCategoryName: string;
  readonly location: string;
  readonly startsAt: string;
  readonly code: string;
  readonly status: string;
  readonly isValid: boolean;
}

const PAGE_WIDTH = 595.28;
const PAGE_HEIGHT = 841.89;
const MARGIN = 56;
const CONTENT_WIDTH = PAGE_WIDTH - MARGIN * 2;
const LABEL_WIDTH = 92;
const QR_SIZE = 240;

const FONT_NAME = 'Roboto';

const COLORS = {
  title: [16, 16, 16],
  label: [107, 107, 107],
  text: [32, 32, 32],
  border: [208, 208, 208],
  invalid: [179, 38, 30]
} as const;

export function renderTicketPdf(
  doc: jsPDF,
  data: TicketPdfData,
  qrCanvas: HTMLCanvasElement
): void {
  registerFonts(doc);

  doc.setDrawColor(...COLORS.border);
  doc.setLineWidth(1);
  doc.roundedRect(MARGIN / 2, MARGIN / 2, PAGE_WIDTH - MARGIN, PAGE_HEIGHT - MARGIN, 8, 8);

  let y = MARGIN + 18;

  doc.setFont(FONT_NAME, 'bold');
  doc.setFontSize(24);
  doc.setTextColor(...COLORS.title);
  const titleLines = doc.splitTextToSize(data.eventTitle, CONTENT_WIDTH) as string[];
  for (const line of titleLines) {
    doc.text(line, MARGIN, y);
    y += 30;
  }

  y += 16;

  const rows: readonly [string, string][] = [
    ['Ticket', data.ticketCategoryName],
    ['Venue', data.location],
    ['Starts at', data.startsAt],
    ['Status', data.status]
  ];

  for (const [label, value] of rows) {
    doc.setFont(FONT_NAME, 'bold');
    doc.setFontSize(10);
    doc.setTextColor(...COLORS.label);
    doc.text(label.toUpperCase(), MARGIN, y);

    doc.setFont(FONT_NAME, 'normal');
    doc.setFontSize(12);
    doc.setTextColor(...COLORS.text);
    const valueLines = doc.splitTextToSize(value, CONTENT_WIDTH - LABEL_WIDTH) as string[];
    let lineY = y;
    for (const line of valueLines) {
      doc.text(line, MARGIN + LABEL_WIDTH, lineY);
      lineY += 17;
    }

    y += Math.max(1, valueLines.length) * 17 + 7;
  }

  y += 24;

  const qrX = (PAGE_WIDTH - QR_SIZE) / 2;
  if (data.isValid) {
    doc.addImage(qrCanvas.toDataURL('image/png'), 'PNG', qrX, y, QR_SIZE, QR_SIZE);
  } else {
    doc.saveGraphicsState();
    doc.setGState(doc.GState({ opacity: 0.35 }));
    doc.addImage(qrCanvas.toDataURL('image/png'), 'PNG', qrX, y, QR_SIZE, QR_SIZE);
    doc.restoreGraphicsState();
  }

  y += QR_SIZE + 40;

  doc.setFont(FONT_NAME, 'bold');
  doc.setFontSize(20);
  doc.setTextColor(...COLORS.text);
  doc.text(data.code, PAGE_WIDTH / 2, y, { align: 'center' });

  if (!data.isValid) {
    y += 34;
    doc.setFontSize(14);
    doc.setTextColor(...COLORS.invalid);
    doc.text(`${data.status.toUpperCase()} \u2014 NOT VALID`, PAGE_WIDTH / 2, y, {
      align: 'center'
    });
  }
}

function registerFonts(doc: jsPDF): void {
  doc.addFileToVFS('Roboto-Regular.ttf', ROBOTO_REGULAR_BASE64);
  doc.addFont('Roboto-Regular.ttf', FONT_NAME, 'normal');
  doc.addFileToVFS('Roboto-Bold.ttf', ROBOTO_BOLD_BASE64);
  doc.addFont('Roboto-Bold.ttf', FONT_NAME, 'bold');
}
