import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes), // Gère la navigation entre Boutique et Suivi
    provideHttpClient()    // Permet à tes services de contacter ton API .NET
  ]
};