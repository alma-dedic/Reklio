import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthLayout } from '../auth-layout/auth-layout';

@Component({
  selector: 'app-register-success',
  imports: [RouterLink, AuthLayout],
  templateUrl: './register-success.html',
  styleUrl: './register-success.scss',
})
export class RegisterSuccess {}