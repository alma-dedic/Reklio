import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../core/chat/chat.service';
import { ChatMessage } from '../../core/chat/chat.models';

interface ChatItem {
  role: 'user' | 'assistant';
  content: string;
  source?: string | null;
}

const EXAMPLES = [
  'Koliko traje garancija na telefon?',
  'Šta nije pokriveno garancijom?',
  'Kako podnosim reklamaciju?',
];

@Component({
  selector: 'app-chat-widget',
  imports: [FormsModule],
  templateUrl: './chat-widget.html',
  styleUrl: './chat-widget.scss',
})
export class ChatWidget {
  private readonly chat = inject(ChatService);
  private readonly messagesEl = viewChild<ElementRef<HTMLElement>>('messages');

  protected readonly open = signal(false);
  protected readonly items = signal<ChatItem[]>([]);
  protected readonly input = signal('');
  protected readonly sending = signal(false);
  protected readonly examples = EXAMPLES;

  protected toggle(): void {
    this.open.update((o) => !o);
  }

  protected close(): void {
    this.open.set(false);
  }

  protected onSubmit(): void {
    this.ask(this.input());
  }

  protected ask(text: string): void {
    const q = text.trim();
    if (!q || this.sending()) {
      return;
    }

    // Istorija = razgovor do sada (bez novog pitanja, koje ide kao `message`).
    const history: ChatMessage[] = this.items().map((m) => ({ role: m.role, content: m.content }));

    this.items.update((l) => [...l, { role: 'user', content: q }]);
    this.input.set('');
    this.sending.set(true);
    this.scrollDown();

    this.chat.ask(q, history).subscribe({
      next: (r) => {
        this.items.update((l) => [...l, { role: 'assistant', content: r.answer, source: r.citedArticle }]);
        this.sending.set(false);
        this.scrollDown();
      },
      error: () => {
        this.items.update((l) => [
          ...l,
          { role: 'assistant', content: 'Izvinjavam se, trenutno ne mogu odgovoriti. Pokušajte ponovo.' },
        ]);
        this.sending.set(false);
        this.scrollDown();
      },
    });
  }

  private scrollDown(): void {
    setTimeout(() => {
      const el = this.messagesEl()?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }
}
