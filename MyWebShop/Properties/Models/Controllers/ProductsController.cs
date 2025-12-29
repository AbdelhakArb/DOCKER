using Microsoft.AspNetCore.Mvc; // C'est celui-ci qui contient ControllerBase
using CookComputing.XmlRpc;      // Pour Odoo
using MyWebShop.Models;// Remplace par le nom de ton dossier Models
using MyWebShop.Services;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IOdooService _odooService;

    // Le contrôleur demande une interface, il ne sait pas comment Odoo fonctionne
    public ProductsController(IOdooService odooService)
    {
        _odooService = odooService;
    }

    [HttpGet]
    public IActionResult Get() => Ok(_odooService.GetProducts());

    [HttpPost]
public IActionResult Post([FromBody] Product product) // On l'appelle 'product'
{
    var id = _odooService.CreateProduct(product); // On utilise 'product'
    return Ok(new { id = id, message = "Produit créé !" });
}
}