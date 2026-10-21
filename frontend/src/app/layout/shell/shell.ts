import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth';

interface NavigationItem {
  label: string;
  description: string;
  path: string;
  icon: 'upload' | 'process' | 'quarantine';
}

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Shell {
  readonly auth = inject(AuthService);
  readonly mobileOpen = signal(false);

  readonly navigation: readonly NavigationItem[] = [
    {
      label: 'Cargar CSV',
      description: 'Sube un archivo de comercios',
      path: '/upload',
      icon: 'upload'
    },
    {
      label: 'Procesar fecha',
      description: 'Valida y mueve a cuarentena',
      path: '/process',
      icon: 'process'
    },
    {
      label: 'Cuarentena',
      description: 'Consulta los rechazados',
      path: '/quarantine',
      icon: 'quarantine'
    }
  ];

  readonly initials = computed(() => {
    const username = this.auth.user()?.username ?? 'US';
    return username.slice(0, 2).toUpperCase();
  });

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
  }
}
