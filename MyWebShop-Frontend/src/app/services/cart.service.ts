import { Injectable, signal } from '@angular/core';
//import { Product } from '../../../../MyWebShop/Properties/Models/Product';
@Injectable({ providedIn: 'root' })
export class CartService {
  // On utilise un signal pour une détection de changement ultra-rapide
  items = signal<{ product: any, quantity: number }[]>([]);

  addToCart(product: any) {
    const currentItems = this.items();
    const existingItem = currentItems.find(i => i.product.id === product.id);

    if (existingItem) {
      existingItem.quantity += 1;
      this.items.set([...currentItems]);
    } else {
      this.items.set([...currentItems, { product, quantity: 1 }]);
    }
  }

  clearCart() {
    this.items.set([]);
  }

  getTotal() {
    return this.items().reduce((acc, item) => acc + (item.product.price * item.quantity), 0);
  }
}