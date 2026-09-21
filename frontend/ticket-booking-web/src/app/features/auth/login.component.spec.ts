import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let auth: { login: ReturnType<typeof vi.fn> };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    auth = { login: vi.fn() };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('does not submit when the form is invalid', () => {
    component['submit']();

    expect(auth.login).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('logs in and navigates to the default returnUrl on success', () => {
    auth.login.mockReturnValue(of({ accessToken: 'token', expiresAt: '2999-01-01' }));
    component['form'].setValue({ email: 'user@example.com', password: 'secret' });

    component['submit']();

    expect(auth.login).toHaveBeenCalledWith({ email: 'user@example.com', password: 'secret' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/events');
    expect(component['loading']()).toBe(false);
  });

  it('sets an error message when login fails with 401', () => {
    auth.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    component['form'].setValue({ email: 'user@example.com', password: 'wrong' });

    component['submit']();

    expect(component['error']()).toBe('Invalid email or password.');
    expect(component['loading']()).toBe(false);
  });

  it('sets a network error message when the server is unreachable', () => {
    auth.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    component['form'].setValue({ email: 'user@example.com', password: 'secret' });

    component['submit']();

    expect(component['error']()).toBe(
      'Cannot reach the server. Please check your connection and try again.'
    );
  });
});
