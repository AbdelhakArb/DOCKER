using MyWebShop.Models;
using MyWebShop.Services;
using CookComputing.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyWebShop.Services
{
    public class OdooService : IOdooService
    {
        // Remplace par tes vraies informations de connexion Odoo
        private readonly string _url = "http://localhost:8069/xmlrpc/2/"; 
        private readonly string _db = "dbWebShop";
        private readonly string _user = "testEmail@hotmail.com";
        private readonly string _pass = "testMdp";
        private readonly int _uid;

        public OdooService()
        {
            try
            {
                var commonProxy = XmlRpcProxyGen.Create<IOdooProxy>();
                commonProxy.Url = _url + "common";
                
                // Authentification pour obtenir l'UID utilisateur
                _uid = commonProxy.authenticate(_db, _user, _pass, new object[] { });
                
                if (_uid == 0) throw new Exception("Échec de l'authentification Odoo.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR ODOO DETECTEE : {ex.Message}");
                throw new Exception($"Erreur de connexion Odoo : {ex.Message}");
            }
        }

       public List<Product> GetProducts()
{
    var rpcClient = XmlRpcProxyGen.Create<IOdooProxy>();
    rpcClient.Url = _url + "object";
    
    var fieldList = new string[] { "id", "name", "list_price" };
    
    var result = (object[])rpcClient.execute_kw(_db, _uid, _pass, "product.template", "search_read", 
        new object[] { new object[] { } }, 
        new { fields = fieldList });

    return result.Select(p => 
    {
        // On caste en XmlRpcStruct au lieu de IDictionary
        var structData = (XmlRpcStruct)p; 
        
        return new Product
        {
            Id = (int)structData["id"],
            Name = structData["name"]?.ToString() ?? "Sans nom",
            Price = Convert.ToDouble(structData["list_price"])
        };
    }).ToList();
}
        public int CreateOrder(OrderRequest order)
{
    var rpcClient = XmlRpcProxyGen.Create<IOdooProxy>();
    rpcClient.Url = _url + "object";

    try
    {
        // 1. Création de l'entête (Utilisation de XmlRpcStruct au lieu de Dictionary)
        var orderData = new XmlRpcStruct();
        orderData.Add("partner_id", order.PartnerId);
        orderData.Add("date_order", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        Console.WriteLine($"Tentative Odoo pour Partner ID: {order.PartnerId}");

        int orderId = (int)rpcClient.execute_kw(_db, _uid, _pass, "sale.order", "create", new object[] { orderData });

        // 2. Création des lignes
        foreach (var item in order.Items)
        {
            var lineData = new XmlRpcStruct();
            lineData.Add("order_id", orderId);
            lineData.Add("product_id", item.ProductId);
            lineData.Add("product_uom_qty", (double)item.Quantity);
            
            rpcClient.execute_kw(_db, _uid, _pass, "sale.order.line", "create", new object[] { lineData });
        }

        Console.WriteLine($"Succès ! Commande Odoo ID: {orderId}");
        return orderId;
    }
    catch (Exception ex)
    {
        Console.WriteLine("--- ERREUR ODOO ---");
        Console.WriteLine(ex.Message);
        throw;
    }
}

public object? GetOrderStatus(int orderId)
{
    var rpcClient = XmlRpcProxyGen.Create<IOdooProxy>();
    rpcClient.Url = _url + "object";

    var fields = new string[] { "id", "name", "state", "amount_total", "date_order" };
    
    var result = (object[])rpcClient.execute_kw(_db, _uid, _pass, "sale.order", "read", 
        new object[] { new int[] { orderId } }, 
        new { fields = fields });

    return result.Length > 0 ? result[0] : null;
}
        public int CreateProduct(Product product)
        {
            var rpcClient = XmlRpcProxyGen.Create<IOdooProxy>();
            rpcClient.Url = _url + "object";

            // Préparation des données pour Odoo
            var data = new Dictionary<string, object>
            {
                { "name", product.Name },
                { "list_price", product.Price },
                { "type", "consu" } // Type 'Consommable' par défaut
            };

            // Appel de la méthode 'create' d'Odoo
            var newId = (int)rpcClient.execute_kw(_db, _uid, _pass, "product.template", "create", 
                new object[] { data });

            return newId;
        }
    }
    
}

