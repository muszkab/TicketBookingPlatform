import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { BehaviorSubject } from 'rxjs';

import { EventCategory, EventsService, EventStatus, LocationsService } from '../../api';
import { EventsListComponent } from './events-list.component';

describe('EventsListComponent', () => {
  let fixture: ComponentFixture<EventsListComponent>;
  let eventsService: { getEvents: ReturnType<typeof vi.fn> };
  let locationsService: { getLocations: ReturnType<typeof vi.fn> };
  let queryParamMap$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  beforeEach(async () => {
    eventsService = {
      getEvents: vi.fn().mockReturnValue(
        of({
          items: [
            {
              id: 'e1',
              title: 'Test Concert',
              description: 'desc',
              category: EventCategory.Concert,
              status: EventStatus.OnSale,
              startsAt: '2999-01-01T00:00:00Z',
              endsAt: '2999-01-01T02:00:00Z',
              locationId: 'loc-1',
              ticketCategories: []
            }
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1
        })
      )
    };
    locationsService = {
      getLocations: vi.fn().mockReturnValue(of({ items: [], page: 1, pageSize: 100, totalCount: 0 }))
    };
    queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

    await TestBed.configureTestingModule({
      imports: [EventsListComponent],
      providers: [
        { provide: EventsService, useValue: eventsService },
        { provide: LocationsService, useValue: locationsService },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$, snapshot: {} }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EventsListComponent);
    fixture.detectChanges();
  });

  it('creates the component and loads events on init', () => {
    expect(fixture.componentInstance).toBeTruthy();
    expect(eventsService.getEvents).toHaveBeenCalled();
    expect(fixture.componentInstance['events']()).toHaveLength(1);
    expect(fixture.componentInstance['loading']()).toBe(false);
  });

  it('renders the event title from the loaded data', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Test Concert');
  });

  it('loads the location filter options', () => {
    expect(locationsService.getLocations).toHaveBeenCalledWith(undefined, undefined, 1, 100);
  });

  it('defaults the status filter to On sale', () => {
    expect(eventsService.getEvents).toHaveBeenCalledWith(
      undefined,
      EventStatus.OnSale,
      undefined,
      1,
      20
    );
  });

  it('keeps a status filter explicitly cleared in the url', () => {
    queryParamMap$.next(convertToParamMap({ status: EventStatus.Cancelled }));
    fixture.detectChanges();
    expect(eventsService.getEvents).toHaveBeenLastCalledWith(
      undefined,
      EventStatus.Cancelled,
      undefined,
      1,
      20
    );

    queryParamMap$.next(convertToParamMap({}));
    fixture.detectChanges();
    expect(eventsService.getEvents).toHaveBeenLastCalledWith(
      undefined,
      undefined,
      undefined,
      1,
      20
    );
  });
});
