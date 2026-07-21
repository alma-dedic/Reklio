import { Component, input } from '@angular/core';

@Component({
  selector: 'app-auth-layout',
  imports: [],
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.scss',
})
export class AuthLayout {
  readonly heading = input.required<string>();
  readonly subheading = input.required<string>();
}