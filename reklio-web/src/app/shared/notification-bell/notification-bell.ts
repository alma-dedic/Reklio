import { Component, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { switchMap, timer } from 'rxjs';
import { NotificationService } from '../../core/notifications/notification.service';
import { AppNotification } from '../../core/notifications/notification.models';

const POLL_MS = 20000;

@Component({
  selector: 'app-notification-bell',
  imports: [],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.scss',
})
export class NotificationBell {
  private readonly service = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly unread = signal(0);
  protected readonly open = signal(false);
  protected readonly items = signal<AppNotification[]>([]);
  protected readonly loading = signal(false);

  constructor() {
    // Polling broja nepročitanih dok je komponenta živa.
    timer(0, POLL_MS)
      .pipe(
        switchMap(() => this.service.getUnreadCount()),
        takeUntilDestroyed(),
      )
      .subscribe({
        next: (r) => this.unread.set(r.count),
        error: () => {},
      });
  }

  protected toggle(): void {
    const next = !this.open();
    this.open.set(next);
    if (next) {
      this.loadItems();
    }
  }

  protected close(): void {
    this.open.set(false);
  }

  private loadItems(): void {
    this.loading.set(true);
    this.service.getMine().subscribe({
      next: (list) => {
        this.items.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected onItem(n: AppNotification): void {
    if (!n.isRead) {
      this.service.markRead(n.id).subscribe({ next: () => {}, error: () => {} });
      this.items.update((list) =>
        list.map((x) => (x.id === n.id ? { ...x, isRead: true } : x)),
      );
      this.unread.update((c) => Math.max(0, c - 1));
    }
    this.close();
    this.router.navigate(['/kupac/reklamacija', n.claimId]);
  }

  protected markAllRead(): void {
    this.service.markAllRead().subscribe({ next: () => {}, error: () => {} });
    this.items.update((list) => list.map((x) => ({ ...x, isRead: true })));
    this.unread.set(0);
  }

  protected timeAgo(iso: string): string {
    const min = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
    if (min < 1) return 'upravo';
    if (min < 60) return `prije ${min} min`;
    const h = Math.floor(min / 60);
    if (h < 24) return `prije ${h} h`;
    return `prije ${Math.floor(h / 24)} d`;
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.close();
  }
}
