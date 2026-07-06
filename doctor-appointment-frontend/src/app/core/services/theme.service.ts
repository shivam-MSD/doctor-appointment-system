import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private activeTheme: 'light' | 'dark' = 'dark';

  constructor() {
    const cachedTheme = localStorage.getItem('theme') as 'light' | 'dark';
    if (cachedTheme) {
      this.activeTheme = cachedTheme;
    }
    this.applyTheme(this.activeTheme);
  }

  getCurrentTheme(): 'light' | 'dark' {
    return this.activeTheme;
  }

  toggleTheme(): void {
    this.activeTheme = this.activeTheme === 'dark' ? 'light' : 'dark';
    localStorage.setItem('theme', this.activeTheme);
    this.applyTheme(this.activeTheme);
  }

  private applyTheme(theme: 'light' | 'dark'): void {
    const body = document.body;
    if (theme === 'light') {
      body.classList.add('light-theme');
    } else {
      body.classList.remove('light-theme');
    }
  }
}
