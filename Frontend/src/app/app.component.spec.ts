import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([{ path: 'classic', children: [] }, { path: '', children: [] }])]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it(`should have the 'Frontend' title`, () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('Frontend');
  });

  it('offers a button to the classic version at the end of the page', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const link = (fixture.nativeElement as HTMLElement).querySelector('footer a');

    expect(link?.textContent).toContain('View the classic version');
    expect(link?.getAttribute('href')).toBe('/classic');
  });

  it('links back to round-by-round from the classic version', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    await TestBed.inject(Router).navigateByUrl('/classic');
    fixture.detectChanges();
    const link = (fixture.nativeElement as HTMLElement).querySelector('footer a');

    expect(link?.textContent).toContain('round-by-round');
    expect(link?.getAttribute('href')).toBe('/');
  });
});
