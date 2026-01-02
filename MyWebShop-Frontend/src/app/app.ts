import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ProductListComponent } from './product-list/product-list';
import { CartComponent } from './cart/cart';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CartComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App{}