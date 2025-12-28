using Microsoft.AspNetCore.Mvc;
using CookComputing.XmlRpc;
using MyWebShop.Models;

namespace MyWebShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly string _url = "http://localhost:8069/xmlrpc/2/";
        private readonly string _db = "dbWebShop";
        private readonly string _user = "testEmail@hotmail.com";
        private readonly string _pass = "testMdp"; // Idéalement une clé API Odoo

        [HttpGet]
        public IActionResult GetProducts()
        {
            try
            {
                // 1. Connexion au point d'entrée "common" pour s'authentifier
                var commonProxy = XmlRpcProxyGen.Create<IOdooProxy>();
                commonProxy.Url = _url + "common";
                int uid = commonProxy.Authenticate(_db, _user, _pass, new object[] { });

                // 2. Connexion au point d'entrée "object" pour lire les données
                var objectProxy = XmlRpcProxyGen.Create<IOdooProxy>();
                objectProxy.Url = _url + "object";

                // 3. Paramètres pour chercher les produits (on demande l'ID, le Nom et le Prix)
                var fields = new string[] { "id", "name", "list_price" };
                var filter = new object[] { }; // On prend tout
                
                var options = new XmlRpcStruct { { "fields", fields } };

                var results = (object[])objectProxy.ExecuteKw(_db, uid, _pass, 
                    "product.template", "search_read", new object[] { filter }, options);

                // 4. On transforme le résultat d'Odoo en une liste C# propre
                var products = results.Select(r => {
                    var dict = (XmlRpcStruct)r;
                    return new Product {
                        Id = (int)dict["id"],
                        Name = (string)dict["name"],
                        Price = Convert.ToDouble(dict["list_price"])
                    };
                });

                return Ok(products);
            }
            catch (System.Exception ex)
            {
                return Unauthorized("L'authentification Odoo a échoué. Vérifiez vos identifiants.");
            }
            
        }
    }
}