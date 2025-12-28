import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OdooService } from '../odoo';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-list.html'
})
export class ProductListComponent implements OnInit {
  // 2. ICI : Il faut absolument utiliser signal<any[]>([])
  // Si tu as écrit : products: any[] = []; -> C'est ça l'erreur !
  products = signal<any[]>([]); 

  constructor(private odooService: OdooService) {}

  ngOnInit(): void {
    this.odooService.getProducts().subscribe({
      next: (data) => {
        console.log('Données reçues :', data);
        this.products.set(data); // On utilise .set() pour les signals
      },
      error: (err) => console.error('Erreur API :', err)
    });
  }
}