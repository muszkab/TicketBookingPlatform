import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { MetaService, VersionInfoResponse } from '../../api';
import { ApiInfoService } from './api-info.service';

const INFO: VersionInfoResponse = {
    version: '1.2.3',
    commit: 'abc1234',
    runtimeVersion: '9.0.20',
    runtimeMajor: 10,
    buildDate: '2026-10-04T12:00:00Z',
    environment: 'Production'
};

describe('ApiInfoService', () => {
    let getVersionInfo: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        getVersionInfo = vi.fn(() => of(INFO));

        TestBed.configureTestingModule({
            providers: [{ provide: MetaService, useValue: { getVersionInfo } }],
        });
    });

    it('starts without info so the footer can fall back', () => {
        const service = TestBed.inject(ApiInfoService);

        expect(service.version()).toBe('');
        expect(service.runtimeMajor()).toBeNull();
        expect(getVersionInfo).not.toHaveBeenCalled();
    });

    it('exposes the values returned by the meta endpoint after load()', () => {
        const service = TestBed.inject(ApiInfoService);

        service.load();

        expect(getVersionInfo).toHaveBeenCalledTimes(1);
        expect(service.version()).toBe('1.2.3');
        expect(service.runtimeMajor()).toBe(10);
    });

    it('requests the info only once', () => {
        const service = TestBed.inject(ApiInfoService);

        service.load();
        service.load();

        expect(getVersionInfo).toHaveBeenCalledTimes(1);
    });

    it('keeps the fallback when the request fails', () => {
        getVersionInfo.mockReturnValue(throwError(() => new Error('offline')));
        const service = TestBed.inject(ApiInfoService);

        service.load();

        expect(service.version()).toBe('');
        expect(service.runtimeMajor()).toBeNull();
    });
});
