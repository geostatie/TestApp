import { Component, signal } from '@angular/core';
import { Clinics } from './clinics/clinics';

@Component({
  imports: [Clinics],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('TestApp');
}
