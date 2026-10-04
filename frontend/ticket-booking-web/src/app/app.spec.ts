import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient()],
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

  it('should list register and sign-in actions in the menu for anonymous users', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.app-toolbar__menu-button')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const panel = document.querySelector('.mat-mdc-menu-panel');
    expect(panel).toBeTruthy();
    expect(panel?.textContent).toContain('Register');
    expect(panel?.textContent).toContain('Sign in');
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
});
