import { EventCategory, EventStatus } from '../../../api';

export const EVENT_CATEGORY_OPTIONS: { value: EventCategory; label: string }[] = [
  { value: EventCategory.Concert, label: 'Concert' },
  { value: EventCategory.Theater, label: 'Theater' },
  { value: EventCategory.Conference, label: 'Conference' },
  { value: EventCategory.Other, label: 'Other' }
];

export const EVENT_STATUS_OPTIONS: { value: EventStatus; label: string }[] = [
  { value: EventStatus.Draft, label: 'Draft' },
  { value: EventStatus.OnSale, label: 'On sale' },
  { value: EventStatus.SoldOut, label: 'Sold out' },
  { value: EventStatus.Cancelled, label: 'Cancelled' },
  { value: EventStatus.Completed, label: 'Completed' }
];

export function eventCategoryLabel(value: EventCategory): string {
  return EVENT_CATEGORY_OPTIONS.find((o) => o.value === value)?.label ?? value;
}

export function eventStatusLabel(value: EventStatus): string {
  return EVENT_STATUS_OPTIONS.find((o) => o.value === value)?.label ?? value;
}

export function isEventCategory(value: string | null): value is EventCategory {
  return (
    value !== null &&
    EVENT_CATEGORY_OPTIONS.some((o) => o.value === (value as EventCategory))
  );
}

export function isEventStatus(value: string | null): value is EventStatus {
  return (
    value !== null &&
    EVENT_STATUS_OPTIONS.some((o) => o.value === (value as EventStatus))
  );
}

export const SUPPORTED_CURRENCIES = ['HUF', 'EUR', 'USD'] as const;
export type SupportedCurrency = (typeof SUPPORTED_CURRENCIES)[number];
