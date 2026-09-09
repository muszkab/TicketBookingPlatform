export interface TicketCanvasData {
  readonly eventTitle: string;
  readonly ticketCategoryName: string;
  readonly location: string;
  readonly startsAt: string;
  readonly code: string;
  readonly status: string;
  readonly isValid: boolean;
}

const WIDTH = 720;
const PADDING = 48;
const QR_SIZE = 320;

const COLORS = {
  background: '#ffffff',
  border: '#d0d0d0',
  title: '#101010',
  label: '#6b6b6b',
  text: '#202020',
  invalid: '#b3261e'
} as const;

export function renderTicketPng(data: TicketCanvasData, qrCanvas: HTMLCanvasElement): string | null {
  const rows: [string, string][] = [
    ['Ticket', data.ticketCategoryName],
    ['Venue', data.location],
    ['Starts at', data.startsAt]
  ];

  const measureCanvas = document.createElement('canvas');
  const measureCtx = measureCanvas.getContext('2d');
  if (!measureCtx) {
    return null;
  }

  const contentWidth = WIDTH - PADDING * 2;
  const titleLines = wrapText(measureCtx, data.eventTitle, `700 34px ${FONT_STACK}`, contentWidth);
  const rowLines = rows.map(([label, value]) => ({
    label,
    lines: wrapText(measureCtx, value, `16px ${FONT_STACK}`, contentWidth - LABEL_WIDTH)
  }));

  const titleHeight = titleLines.length * 42;
  const rowsHeight = rowLines.reduce((sum, row) => sum + Math.max(1, row.lines.length) * 24, 0);
  const invalidHeight = data.isValid ? 0 : 40;
  const height =
    PADDING + titleHeight + 20 + rowsHeight + 32 + QR_SIZE + 28 + 46 + invalidHeight + PADDING;

  const canvas = document.createElement('canvas');
  canvas.width = WIDTH;
  canvas.height = height;

  const ctx = canvas.getContext('2d');
  if (!ctx) {
    return null;
  }

  ctx.fillStyle = COLORS.background;
  ctx.fillRect(0, 0, WIDTH, height);

  ctx.strokeStyle = COLORS.border;
  ctx.lineWidth = 2;
  ctx.strokeRect(1, 1, WIDTH - 2, height - 2);

  ctx.textBaseline = 'top';
  let y = PADDING;

  ctx.fillStyle = COLORS.title;
  ctx.font = `700 34px ${FONT_STACK}`;
  for (const line of titleLines) {
    ctx.fillText(line, PADDING, y);
    y += 42;
  }

  y += 20;

  for (const row of rowLines) {
    ctx.fillStyle = COLORS.label;
    ctx.font = `600 15px ${FONT_STACK}`;
    ctx.fillText(row.label, PADDING, y);

    ctx.fillStyle = COLORS.text;
    ctx.font = `16px ${FONT_STACK}`;
    let lineY = y;
    for (const line of row.lines) {
      ctx.fillText(line, PADDING + LABEL_WIDTH, lineY);
      lineY += 24;
    }

    y += Math.max(1, row.lines.length) * 24;
  }

  y += 32;

  ctx.save();
  if (!data.isValid) {
    ctx.globalAlpha = 0.35;
  }
  ctx.drawImage(qrCanvas, (WIDTH - QR_SIZE) / 2, y, QR_SIZE, QR_SIZE);
  ctx.restore();

  y += QR_SIZE + 28;

  ctx.textAlign = 'center';
  ctx.fillStyle = COLORS.text;
  ctx.font = `600 26px ${MONO_FONT_STACK}`;
  ctx.fillText(data.code, WIDTH / 2, y);
  y += 46;

  if (!data.isValid) {
    ctx.fillStyle = COLORS.invalid;
    ctx.font = `700 20px ${FONT_STACK}`;
    ctx.fillText(`${data.status.toUpperCase()} — NOT VALID`, WIDTH / 2, y);
  }

  return canvas.toDataURL('image/png');
}

const FONT_STACK = 'Roboto, "Helvetica Neue", Arial, sans-serif';
const MONO_FONT_STACK = '"Roboto Mono", Consolas, monospace';
const LABEL_WIDTH = 110;

function wrapText(
  ctx: CanvasRenderingContext2D,
  text: string,
  font: string,
  maxWidth: number
): string[] {
  ctx.font = font;

  const words = text.split(/\s+/).filter((w) => w.length > 0);
  if (words.length === 0) {
    return [''];
  }

  const lines: string[] = [];
  let current = words[0];

  for (const word of words.slice(1)) {
    const candidate = `${current} ${word}`;
    if (ctx.measureText(candidate).width <= maxWidth) {
      current = candidate;
    } else {
      lines.push(current);
      current = word;
    }
  }
  lines.push(current);

  return lines;
}
