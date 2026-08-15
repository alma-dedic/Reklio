import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthLayout } from '../auth-layout/auth-layout';
import { AuthService } from '../../../core/auth/auth.service';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
}

// Ista pravila kao ASP.NET Identity na backendu (T1.2).
function passwordPolicy(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';
  const ok =
    value.length >= 8 &&
    /[a-z]/.test(value) &&
    /[A-Z]/.test(value) &&
    /\d/.test(value) &&
    /[^a-zA-Z0-9]/.test(value);
  return ok ? null : { weakPassword: true };
}

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, AuthLayout],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly showPassword = signal(false);
  protected readonly showConfirm = signal(false);

  protected togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  protected toggleConfirm(): void {
    this.showConfirm.update((v) => !v);
  }

  protected readonly form = this.fb.nonNullable.group(
    {
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, passwordPolicy]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { fullName, email, password } = this.form.getRawValue();

    this.auth.register({ fullName, email, password }).subscribe({
      next: () => this.router.navigate(['/registracija-uspjesna']),
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(this.mapError(err));
      },
    });
  }

  private mapError(err: HttpErrorResponse): string {
    if (err.status === 409) {
      return 'Korisnik sa ovim emailom već postoji.';
    }
    if (err.status === 0) {
      return 'Server nije dostupan. Provjerite da je backend pokrenut.';
    }
    const serverErrors = err.error?.errors;
    if (Array.isArray(serverErrors) && serverErrors.length > 0) {
      return serverErrors.join(' ');
    }
    return 'Registracija nije uspjela. Pokušajte ponovo.';
  }
}