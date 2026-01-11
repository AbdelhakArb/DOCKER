using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookComputing.XmlRpc;
/*
// Interface pour la communication Odoo
public interface IOdooProxy : IXmlRpcProxy {
    [XmlRpcMethod("execute_kw")]
    object execute_kw(string db, int uid, string pass, string obj, string method, object[] args, object kwargs);
}

class Program {
    const string URL = "http://localhost:8069/xmlrpc/2/";
    const string DB = "dbWebShop";
    const string USER = "testEmail@hotmail.com";
    const string PASS = "testMdp";

    static void Main(string[] args) {
        var proxy = XmlRpcProxyGen.Create<IOdooProxy>();
        proxy.Url = URL + "object";

        // 1. Authentification
        int uid = Authenticate();
        Console.WriteLine($"Connecté avec l'UID : {uid}");

        // 2. Lecture du CSV et Import
        string csvPath = "produits.csv";
        var lines = File.ReadAllLines(csvPath).Skip(1); // Ignorer l'en-tête

        foreach (var line in lines) {
            var data = line.Split(',');
            string name = data[0];
            double price = double.Parse(data[1]);
            double cost = double.Parse(data[2]);
            string attrName = data[3];
            string attrVal = data[4];

            Console.WriteLine($"Traitement en cours de : {name}...");

            // --- Gestion des Attributs ---
            int attrId = GetOrCreate(proxy, uid, "product.attribute", attrName);
            int valId = GetOrCreate(proxy, uid, "product.attribute.value", attrVal, attrId);

            // --- Création du Produit ---
            var templateId = proxy.execute_kw(DB, uid, PASS, "product.template", "create", new object[] {
                new {
                    name = name,
                    list_price = price,
                    standard_price = cost,
                    attribute_line_ids = new object[] {
                        new object[] { 0, 0, new { 
                            attribute_id = attrId, 
                            value_ids = new object[] { new object[] { 6, 0, new int[] { valId } } } 
                        } }
                    }
                }
            }, new { });

            Console.WriteLine($" Produit importé avec succès (ID: {templateId})");
        }
    }

    // Méthode générique pour éviter la duplication de code (DRY)
    static int GetOrCreate(IOdooProxy proxy, int uid, string model, string name, int? attrId = null) {
        object criteria = attrId == null 
            ? new object[] { new object[] { "name", "=", name } }
            : new object[] { new object[] { "name", "=", name }, new object[] { "attribute_id", "=", attrId } };

        var searchResult = (object[])proxy.execute_kw(DB, uid, PASS, model, "search", new object[] { criteria }, new { });

        if (searchResult.Length > 0) return (int)searchResult[0];

        var newId = proxy.execute_kw(DB, uid, PASS, model, "create", new object[] {
            attrId == null ? (object)new { name = name } : new { name = name, attribute_id = attrId }
        }, new { });

        return (int)newId;
    }

   static int Authenticate() {
    // On crée un proxy spécifique pour le point d'accès "common"
    var authProxy = XmlRpcProxyGen.Create<IOdooProxy>();
    authProxy.Url = URL + "common";

    try {
        // Appel de la méthode d'authentification
        var result = authProxy.execute_kw(DB, 0, PASS, "common", "authenticate", 
            new object[] { DB, USER, PASS, new { } }, new { });

        if (result is int uid) {
            return uid;
        } else {
            throw new Exception("Identifiants incorrects ou base de données introuvable.");
        }
    }
    catch (Exception ex) {
        Console.WriteLine($"❌ Erreur lors de l'authentification : {ex.Message}");
        throw;
    }
}
}
*/