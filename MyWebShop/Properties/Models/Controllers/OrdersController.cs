using Microsoft.AspNetCore.Mvc;
using MyWebShop.Models;
using MyWebShop.Services;
using System;

namespace MyWebShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // La route sera : api/Orders
    public class OrdersController : ControllerBase
    {
        private readonly IOdooService _odooService;

        // L'injection de dépendances récupère le service déjà configuré dans Program.cs
        public OrdersController(IOdooService odooService)
        {
            _odooService = odooService;
        }

        [HttpPost]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                return BadRequest("La commande doit contenir au moins un article.");
            }

            try
            {
                // Note : On peut forcer un PartnerId (Client) par défaut si non fourni
                // Dans Odoo, l'ID 1 est souvent le client "Public" ou l'entreprise elle-même
                if (request.PartnerId <= 0) 
                {
                    request.PartnerId = 1; 
                }

                int orderId = _odooService.CreateOrder(request);
                
                return Ok(new { 
                    id = orderId, 
                    message = "Commande créée avec succès dans Odoo !",
                    date = DateTime.Now 
                });
            }
            catch (Exception ex)
            {
                // Retourne l'erreur précise si Odoo refuse la création
                return StatusCode(500, $"Erreur Odoo : {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetStatus(int id)
        {
            try {
                var status = _odooService.GetOrderStatus(id);
                if (status == null) return NotFound("Commande introuvable");
                return Ok(status);
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
        
    }
}