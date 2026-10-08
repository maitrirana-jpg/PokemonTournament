import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IndividualCardsComponent } from './individual-cards.component';

describe('IndividualCardsComponent', () => {
  let component: IndividualCardsComponent;
  let fixture: ComponentFixture<IndividualCardsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IndividualCardsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(IndividualCardsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('pokemon', { id: 25, name: 'pikachu', type: 'electric', wins: 3, losses: 1, ties: 0 });
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows no rank or round result by default (classic view)', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.rank-chip')).toBeNull();
    expect(element.querySelector('.result-chip')).toBeNull();
  });

  it('shows rank and this round result when given', () => {
    fixture.componentRef.setInput('rank', 2);
    fixture.componentRef.setInput('lastResult', 'W');
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.rank-chip')?.textContent).toContain('#2');
    expect(element.querySelector('.result-chip')?.textContent).toContain('W');
  });
});
