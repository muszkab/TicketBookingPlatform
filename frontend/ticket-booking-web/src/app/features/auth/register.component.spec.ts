import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let component: RegisterComponent;
  let auth: { register: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    auth = { register: vi.fn() };
    router = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('does not submit when the form is invalid', () => {
    component['submit']();

    expect(auth.register).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('registers and navigates to /login on success', () => {
    auth.register.mockReturnValue(of({}));
    component['form'].setValue({
      fullName: 'Test User',
      email: 'user@example.com',
      password: 'secret1'
    });

    component['submit']();

    expect(auth.register).toHaveBeenCalledWith({
      fullName: 'Test User',
      email: 'user@example.com',
      password: 'secret1'
    });
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/events' }
    });
    expect(component['loading']()).toBe(false);
  });

  it('sets a conflict error message when the email is already registered', () => {
    auth.register.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component['form'].setValue({
      fullName: 'Test User',
      email: 'user@example.com',
      password: 'secret1'
    });

    component['submit']();

    expect(component['error']()).toBe(
      'An account with this email address already exists. Please sign in instead.'
    );
    expect(component['loading']()).toBe(false);
  });

  it('sets a network error message when the server is unreachable', () => {
    auth.register.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    component['form'].setValue({
      fullName: 'Test User',
      email: 'user@example.com',
      password: 'secret1'
    });

    component['submit']();

    expect(component['error']()).toBe(
      'Cannot reach the server. Please check your connection and try again.'
    );
  });
});
