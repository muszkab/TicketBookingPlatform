export const USER_ROLES = {
  Admin: 'Admin',
  Organizer: 'Organizer',
  Customer: 'Customer'
} as const;

export type UserRole = (typeof USER_ROLES)[keyof typeof USER_ROLES];
