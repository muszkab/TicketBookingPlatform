import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { App } from './app';
import { AuthService } from './core/auth/auth.service';
import { ConfirmDialogComponent } from './core/dialogs/confirm-dialog.component';

describe('App', () => {
  let dialogOpen: ReturnType<typeof vi.fn>;
  let afterClosed: Subject<boolean | undefined>;

  beforeEach(async () => {
    afterClosed = new Subject<boolean | undefined>();
    dialogOpen = vi.fn(() => ({ afterClosed: () => afterClosed.asObservable() }));

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        { provide: MatDialog, useValue: { open: dialogOpen } },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the brand title', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.app-toolbar__brand')?.textContent).toContain(
      'Ticket Booking'
    );
  });

  it('should render a hamburger menu trigger instead of inline action buttons', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const trigger = compiled.querySelector<HTMLButtonElement>(
      '.app-toolbar__menu-button'
    );
    expect(trigger).toBeTruthy();
    expect(trigger?.getAttribute('aria-label')).toBe('Open navigation menu');
    expect(trigger?.textContent).toContain('menu');
    expect(
      compiled.querySelectorAll('.app-toolbar__inner a[mat-button]')
    ).toHaveLength(1);
    expect(
      compiled.querySelector('.app-toolbar__inner button[mat-button]')
    ).toBeNull();
  });

  it('should list sign-in above register in the menu for anonymous users', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.app-toolbar__menu-button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const panel = document.querySelector('.mat-mdc-menu-panel');
    expect(panel).toBeTruthy();

    const labels = Array.from(
      panel!.querySelectorAll('a[mat-menu-item] span')
    ).map((label) => label.textContent?.trim());

    // The panel renders its content more than once in the test environment, so compare
    // the order of first occurrences.
    expect([...new Set(labels)]).toEqual(['Sign in', 'Register']);
  });

  it('asks for confirmation before signing out', () => {
    const logoutSpy = vi.spyOn(TestBed.inject(AuthService), 'logout');
    const navigateSpy = vi
      .spyOn(TestBed.inject(Router), 'navigate')
      .mockResolvedValue(true);

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    fixture.componentInstance['logout']();

    expect(dialogOpen).toHaveBeenCalledTimes(1);
    const [component, config] = dialogOpen.mock.calls[0] as [
      unknown,
      { data: { title: string; confirmLabel: string } }
    ];
    expect(component).toBe(ConfirmDialogComponent);
    expect(config.data.title).toBe('Sign out?');
    expect(config.data.confirmLabel).toBe('Sign out');

    // Nothing happens until the user confirms.
    expect(logoutSpy).not.toHaveBeenCalled();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('signs out and returns to the events list when the confirmation is accepted', () => {
    const logoutSpy = vi.spyOn(TestBed.inject(AuthService), 'logout');
    const navigateSpy = vi
      .spyOn(TestBed.inject(Router), 'navigate')
      .mockResolvedValue(true);

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    fixture.componentInstance['logout']();
    afterClosed.next(true);

    expect(logoutSpy).toHaveBeenCalledTimes(1);
    expect(navigateSpy).toHaveBeenCalledWith(['/events']);
  });

  it('keeps the session when the sign-out confirmation is dismissed', () => {
    const logoutSpy = vi.spyOn(TestBed.inject(AuthService), 'logout');
    const navigateSpy = vi
      .spyOn(TestBed.inject(Router), 'navigate')
      .mockResolvedValue(true);

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    fixture.componentInstance['logout']();
    afterClosed.next(false);
    afterClosed.complete();

    expect(logoutSpy).not.toHaveBeenCalled();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('should show the demo notice on load', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.demo-banner')?.textContent).toContain(
      'This is a demo app'
    );
  });

  it('should hide the demo notice when dismissed without persisting it', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.demo-banner__close')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(compiled.querySelector('.demo-banner')).toBeNull();

    // A fresh instance represents a page reload: the notice must come back.
    const reloaded = TestBed.createComponent(App);
    reloaded.detectChanges();
    expect(
      (reloaded.nativeElement as HTMLElement).querySelector('.demo-banner')
    ).not.toBeNull();
  });

  it('renders contact links, the year and a non-interactive LinkedIn placeholder in the footer', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;

    const email = compiled.querySelector<HTMLAnchorElement>(
      '.app-footer__link[href^="mailto:"]'
    );
    expect(email?.getAttribute('href')).toBe('mailto:m1musbal@gmail.com');

    const github = compiled.querySelector<HTMLAnchorElement>(
      '.app-footer__link[href*="github.com"]'
    );
    expect(github?.getAttribute('href')).toBe(
      'https://github.com/muszkab/TicketBookingPlatform'
    );
    expect(github?.getAttribute('target')).toBe('_blank');
    expect(github?.getAttribute('rel')).toBe('noopener noreferrer');

    // LinkedIn profile is not ready: rendered as a span, not a link.
    const linkedin = compiled.querySelector('.app-footer__link--disabled');
    expect(linkedin?.tagName).toBe('SPAN');
    expect(linkedin?.querySelector('a')).toBeNull();

    expect(
      compiled.querySelector('.app-footer__copyright')?.textContent
    ).toContain(String(new Date().getFullYear()));
  });
});
