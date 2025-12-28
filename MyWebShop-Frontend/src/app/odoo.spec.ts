import { TestBed } from '@angular/core/testing';

import { Odoo } from './odoo';

describe('Odoo', () => {
  let service: Odoo;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Odoo);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
