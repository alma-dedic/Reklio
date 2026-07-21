import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-customer-home',
  imports: [],
  templateUrl: './customer-home.html',
  styleUrl: './customer-home.scss',
})
export class CustomerHome {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.currentUser;

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}