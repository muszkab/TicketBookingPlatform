import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MetaService, VersionInfoResponse } from '../../api';

@Injectable({ providedIn: 'root' })
export class ApiInfoService {
    private readonly api = inject(MetaService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly info = signal<VersionInfoResponse | null>(null);
    private requested = false;

    readonly version = computed(() => this.info()?.version ?? '');

    readonly runtimeMajor = computed(() => this.info()?.runtimeMajor ?? null);

    load(): void {
        if (this.requested) {
            return;
        }
        this.requested = true;

        this.api
            .getVersionInfo()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (info) => this.info.set(info),
                // Deliberately silent: the footer keeps its static fallback when the API is unreachable.
                error: () => undefined
            });
    }
}
