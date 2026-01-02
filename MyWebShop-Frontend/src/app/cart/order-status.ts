import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { OdooService } from '../odoo';

@Component({
  selector: 'app-order-status',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container mt-5">
      <div class="card shadow">
        <div class="card-header bg-info text-white">Suivi de commande</div>
        <div class="card-body">
          <div class="input-group mb-3">
            <input type="number" #orderId class="form-control" placeholder="Entrez votre N° de commande">
            <button class="btn btn-primary" (click)="checkStatus(orderId.value)">Rechercher</button>
          </div>

          @if (order(); as details) {
            <div class="mt-4 border-top pt-3">
              <h5>Commande : {{ details.name }}</h5>
              <p>Date : {{ details.date_order }}</p>
              <p>Total : <strong>{{ details.amount_total }} €</strong></p>
              
              <div class="alert" [ngClass]="{
                'alert-secondary': details.state === 'draft',
                'alert-success': details.state === 'sale',
                'alert-danger': details.state === 'cancel'
              }">
                Statut actuel : <strong>{{ details.state }}</strong>
              </div>
            </div>
          }
        </div>
      </div>
        <button class="btn btn-outline-secondary mt-3" routerLink="/shop">
            Retour au catalogue
        </button>
    </div>
  `
})
export class OrderStatusComponent {
  order = signal<any>(null);

  // On injecte le service, pas le client HTTP directement
  constructor(private odooService: OdooService) {} 

  checkStatus(id: string) {
    this.odooService.getOrderStatus(id).subscribe({
      next: (res) => this.order.set(res),
      error: () => alert("Commande non trouvée")
    });
  }
}