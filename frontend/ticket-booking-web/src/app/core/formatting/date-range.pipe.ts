import { formatDate } from '@angular/common';
import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';

/** Example: `2025, Apr 12, 20:30` */
export const DATE_TIME_FORMAT = 'y, MMM d, HH:mm';

type DateInput = string | Date | null | undefined;

/** Example: `2025, Apr 12, 20:30 → 23:30`; the end repeats the date only when it falls on another day. */
@Pipe({ name: 'dateRange' })
export class DateRangePipe implements PipeTransform {
    private readonly locale = inject(LOCALE_ID);

    transform(start: DateInput, end: DateInput): string {
        if (!start) {
            return '';
        }

        const from = formatDate(start, DATE_TIME_FORMAT, this.locale);
        if (!end) {
            return from;
        }

        const sameDay =
            formatDate(start, 'yMMdd', this.locale) === formatDate(end, 'yMMdd', this.locale);
        const to = formatDate(end, sameDay ? 'HH:mm' : DATE_TIME_FORMAT, this.locale);
        return `${from} → ${to}`;
    }
}
