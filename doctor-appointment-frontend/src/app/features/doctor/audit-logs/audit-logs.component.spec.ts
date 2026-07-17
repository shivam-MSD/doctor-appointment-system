import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuditLogsComponent } from './audit-logs.component';

describe('AuditLogsComponent', () => {
  let component: AuditLogsComponent;
  let fixture: ComponentFixture<AuditLogsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AuditLogsComponent]
    });
    fixture = TestBed.createComponent(AuditLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
