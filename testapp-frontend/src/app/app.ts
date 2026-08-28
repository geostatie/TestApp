import { Component, signal } from '@angular/core';
import { Weather } from './weather/weather';

@Component({
  imports: [Weather],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('TestApp');
}
