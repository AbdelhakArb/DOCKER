import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OdooService } from '../odoo';
import { CartService } from '../services/cart.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cart.html'
})
export class CartComponent {

  // On injecte les deux services dans le constructeur
  constructor(
    public cartService: CartService, 
    private odooService: OdooService
  ) {}

  confirmOrder() {
    // Vérifier si le panier n'est pas vide
    if (this.cartService.items().length === 0) {
      alert("Votre panier est vide !");
      return;
    }

    const payload = {
      partnerId: 7,
      items: this.cartService.items().map(item => ({
        productId: item.product.id,
        quantity: item.quantity
      }))
    };

    this.odooService.createOrder(payload).subscribe({
      next: (res: any) => {
        alert("Succès ! Commande Odoo n°" + res.id + " créée.");
        this.cartService.clearCart(); // On vide le panier après succès
      },
      error: (err) => {
        console.error(err);
        alert("Erreur lors de la création : " + err.message);
      }
    });
  }
}