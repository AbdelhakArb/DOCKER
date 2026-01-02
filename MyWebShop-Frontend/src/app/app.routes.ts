import { Routes } from '@angular/router';
import { ProductListComponent } from './product-list/product-list';
import { OrderStatusComponent } from './cart/order-status';

export const routes: Routes = [
  { path: 'shop', component: ProductListComponent },
  { path: 'track', component: OrderStatusComponent },
  { path: '', redirectTo: 'shop', pathMatch: 'full' } 
];