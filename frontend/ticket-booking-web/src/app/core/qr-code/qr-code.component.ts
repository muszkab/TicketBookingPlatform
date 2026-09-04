import { ChangeDetectionStrategy, Component, ElementRef, ViewChild, input } from '@angular/core';
import { QRCodeComponent } from 'angularx-qrcode';

@Component({
  selector: 'app-qr-code',
  standalone: true,
  imports: [QRCodeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <qrcode
      #qr
      [qrdata]="code()"
      [width]="size()"
      [errorCorrectionLevel]="'M'"
      [margin]="2"
      [elementType]="'canvas'"
      [cssClass]="'app-qr-code__canvas'"
      [ariaLabel]="'QR code for ' + code()" />
  `,
  styles: [
    `
      :host {
        display: inline-block;
        line-height: 0;
      }

      .app-qr-code__canvas {
        display: block;
      }
    `
  ]
})
export class QrCodeComponent {
  readonly code = input.required<string>();
  readonly size = input<number>(256);

  @ViewChild('qr', { static: false }) private qr?: QRCodeComponent;

  /**
   * Returns a PNG data URL of the rendered QR code, or null if not ready yet.
   */
  toPngDataUrl(): string | null {
    const canvas = this.findCanvas();
    return canvas ? canvas.toDataURL('image/png') : null;
  }

  private findCanvas(): HTMLCanvasElement | null {
    const host = (this.qr as unknown as { qrcElement?: ElementRef<HTMLElement> } | undefined)?.qrcElement?.nativeElement;
    return host?.querySelector('canvas') ?? null;
  }
}
