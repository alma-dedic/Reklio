import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppHeader } from '../../../shared/app-header/app-header';

@Component({
  selector: 'app-customer-layout',
  imports: [RouterOutlet, AppHeader],
  template: `
    <app-header homeLink="/kupac" />
    <router-outlet />
  `,
  styles: `
    :host {
      display: block;
      min-height: 100vh;
      background: #f6f7f9;
    }
  `,
})
export class CustomerLayout {}
