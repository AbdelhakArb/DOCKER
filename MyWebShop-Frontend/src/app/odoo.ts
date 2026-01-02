import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OdooService {
  private apiUrl = 'http://localhost:5030/api/products'; 

  constructor(private http: HttpClient) { }

  getProducts(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  createOrder(orderData: any): Observable<any> {
  return this.http.post('http://localhost:5030/api/Orders', orderData);
}

getOrderStatus(id: string) {
  return this.http.get(`http://localhost:5030/api/Orders/${id}`);
}
}