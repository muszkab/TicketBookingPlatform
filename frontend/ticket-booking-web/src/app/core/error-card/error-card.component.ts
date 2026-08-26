import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-error-card',
  imports: [MatButtonModule, MatCardModule],
  template: `
    <mat-card class="error-card">
      <mat-card-content>{{ message }}</mat-card-content>
      <mat-card-actions>
        <button mat-button color="primary" type="button" (click)="retry.emit()">
          {{ retryLabel }}
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: `
    :host {
      display: block;
      max-width: 480px;
    }
  `
})
export class ErrorCardComponent {
  @Input({ required: true }) message!: string;
  @Input() retryLabel = 'Retry';
  @Output() readonly retry = new EventEmitter<void>();
}
