using Microsoft.AspNetCore.Mvc; 
using CookComputing.XmlRpc;
using MyWebShop.Models;
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

}