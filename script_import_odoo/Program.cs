using System;
using System.IO;
using System.Linq;
using System.Globalization;
using CookComputing.XmlRpc;

public interface IOdooProxy : IXmlRpcProxy {
    [XmlRpcMethod("authenticate")]
    int authenticate(string db, string user, string pass, object options);
    [XmlRpcMethod("execute_kw")]
    object execute_kw(string db, int uid, string pass, string obj, string method, object[] args, object kwargs);
}

class Program {
    const string URL = "http://localhost:8069/xmlrpc/2/";
    const string DB = "dbWebShop";
    const string USER = "testEmail@hotmail.com";
    const string PASS = "testMdp";
    const int MY_LOCATION_ID = 8; 

    static void Main(string[] args) {
        var proxy = XmlRpcProxyGen.Create<IOdooProxy>();
        proxy.Url = URL + "object";

        try {
            int uid = Authenticate();
            Console.WriteLine($"Connecté (UID: {uid})");

            var lines = File.ReadAllLines("test03.csv").Skip(1);

            foreach (var line in lines) {
                try {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var data = line.Split(';');
                    if (data.Length < 5) continue;

                    string name = data[0].Trim();
                    double price = double.Parse(data[1], CultureInfo.InvariantCulture);
                    double cost = double.Parse(data[2], CultureInfo.InvariantCulture);
                    string attrName = data[3].Trim();
                    string attrVal = data[4].Trim();
                    double stockQty = data.Length >= 6 ? double.Parse(data[5], CultureInfo.InvariantCulture) : 0.0;

                    int attrId = GetOrCreate(proxy, uid, "product.attribute", attrName);
                    int valId = GetOrCreate(proxy, uid, "product.attribute.value", attrVal, attrId);

                    // 1. Recherche ou Création du Template
                    int templateId = GetProductTemplateByName(proxy, uid, name);
                    if (templateId == 0) {
                        templateId = CreateProductTemplate(proxy, uid, name, price, cost);
                    } else {
                        // On essaie de forcer le type à 'consu' pour les existants
                        try {
                            proxy.execute_kw(DB, uid, PASS, "product.template", "write", 
                                new object[] { new int[] { templateId }, new { type = "consu" } }, new { });
                        } catch { /* Ignore si Odoo refuse le changement de type */ }
                    }

                    // 2. Liaison Attributs
                    LinkAttributeToTemplate(proxy, uid, templateId, attrId, valId);

                    // 3. Récupération Variante
                    int variantId = GetVariantId(proxy, uid, templateId, valId);

                    if (variantId > 0) {
                        // Mise à jour prix
                        proxy.execute_kw(DB, uid, PASS, "product.product", "write", 
                            new object[] { new int[] { variantId }, new { lst_price = price, standard_price = cost } }, new { });

                        // 4. Mise à jour Stock
                        UpdateStock(proxy, uid, variantId, stockQty);
                        Console.WriteLine($" {name} ({attrVal}) traité.");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Erreur sur la ligne [{line}] : {ex.Message}");
                }
            }
            Console.WriteLine("\nTerminé !");
        } catch (Exception ex) { Console.WriteLine($"💥 Erreur critique : {ex.Message}"); }
    }

    static int CreateProductTemplate(IOdooProxy proxy, int uid, string name, double price, double cost) {
    try {
        return Convert.ToInt32(proxy.execute_kw(DB, uid, PASS, "product.template", "create", new object[] {
            new { 
                name = name, 
                list_price = price, 
                standard_price = cost, 
                is_storable = true, //  cette ligne qui coche "Track Inventory"
                type = "consu"      // On garde consu par sécurité
            }
        }, new { }));
    } catch {
        // Si is_storable n'existe pas dans ta version d'Odoo, on crée normalement
        return Convert.ToInt32(proxy.execute_kw(DB, uid, PASS, "product.template", "create", new object[] {
            new { name = name, list_price = price, standard_price = cost }
        }, new { }));
    }
}

    static void UpdateStock(IOdooProxy proxy, int uid, int variantId, double qty) {
    try {
        // 1. On s'assure que le suivi de stock est activé
        proxy.execute_kw(DB, uid, PASS, "product.product", "write", new object[] {
            new int[] { variantId }, new { is_storable = true }
        }, new { });

        // 2. Création ou mise à jour du Quant
        var quantIdObj = proxy.execute_kw(DB, uid, PASS, "stock.quant", "create", new object[] {
            new {
                product_id = variantId,
                location_id = MY_LOCATION_ID,
                inventory_quantity = qty
            }
        }, new { });

        int quantId = Convert.ToInt32(quantIdObj);

        // 3. Validation de l'inventaire
        try {
            proxy.execute_kw(DB, uid, PASS, "stock.quant", "action_apply_inventory", new object[] { 
                new int[] { quantId } 
            }, new { });
            Console.WriteLine($"Stock mis à jour : {qty}");
        } 
        catch (Exception ex) when (ex.Message.Contains("cannot marshal None") || ex.Message.Contains("allow_none")) {
            // C'est l'erreur fantôme d'Odoo : l'action a réussi mais renvoie None.
            // On considère cela comme un SUCCÈS.
            Console.WriteLine($"Stock mis à jour : {qty} (Validation confirmée)");
        }
    } 
    catch (Exception ex) {
        // Ici, on gère les vraies erreurs (ex: déjà un stock existant)
        Console.WriteLine($"Note : Ajustement via écriture directe...");
        HandleExistingQuant(proxy, uid, variantId, qty);
    }
}

static void HandleExistingQuant(IOdooProxy proxy, int uid, int variantId, double qty) {
    try {
        var searchQuant = proxy.execute_kw(DB, uid, PASS, "stock.quant", "search", new object[] {
            new object[] { 
                new object[] { "product_id", "=", variantId },
                new object[] { "location_id", "=", MY_LOCATION_ID }
            }
        }, new { });

        int[] quantIds = SafeConvertToIntArray(searchQuant);
        if (quantIds.Length > 0) {
            proxy.execute_kw(DB, uid, PASS, "stock.quant", "write", new object[] {
                new int[] { quantIds[0] }, new { inventory_quantity = qty }
            }, new { });

            try {
                proxy.execute_kw(DB, uid, PASS, "stock.quant", "action_apply_inventory", new object[] { 
                    new int[] { quantIds[0] } 
                }, new { });
            } catch { /* Ignorer l'erreur marshal None ici aussi */ }
            
            Console.WriteLine($"Stock actualisé à {qty}");
        }
    } catch (Exception fatal) {
        Console.WriteLine($"Erreur réelle : {fatal.Message}");
    }
}

    // --- Méthodes Techniques (Inchangées mais incluses pour que le code compile) ---
    static void LinkAttributeToTemplate(IOdooProxy proxy, int uid, int templateId, int attrId, int valId) {
        var res = proxy.execute_kw(DB, uid, PASS, "product.template.attribute.line", "search", new object[] {
            new object[] { new object[] { "product_tmpl_id", "=", templateId }, new object[] { "attribute_id", "=", attrId } }
        }, new { });
        int[] ids = SafeConvertToIntArray(res);
        if (ids.Length == 0) {
            proxy.execute_kw(DB, uid, PASS, "product.template.attribute.line", "create", new object[] {
                new { product_tmpl_id = templateId, attribute_id = attrId, value_ids = new object[] { new object[] { 6, 0, new int[] { valId } } } }
            }, new { });
        } else {
            proxy.execute_kw(DB, uid, PASS, "product.template.attribute.line", "write", new object[] {
                new int[] { ids[0] }, new { value_ids = new object[] { new object[] { 4, valId, 0 } } }
            }, new { });
        }
    }

    static int GetVariantId(IOdooProxy proxy, int uid, int templateId, int valId) {
        var res = proxy.execute_kw(DB, uid, PASS, "product.product", "search", new object[] {
            new object[] { new object[] { "product_tmpl_id", "=", templateId }, new object[] { "product_template_attribute_value_ids.product_attribute_value_id", "=", valId } }
        }, new { });
        int[] ids = SafeConvertToIntArray(res);
        return ids.Length > 0 ? ids[0] : 0;
    }

    static int GetProductTemplateByName(IOdooProxy proxy, int uid, string name) {
        var res = proxy.execute_kw(DB, uid, PASS, "product.template", "search", new object[] { new object[] { new object[] { "name", "=", name } } }, new { });
        int[] ids = SafeConvertToIntArray(res);
        return ids.Length > 0 ? ids[0] : 0;
    }

    static int GetOrCreate(IOdooProxy proxy, int uid, string model, string name, int? attrId = null) {
        object crit = attrId == null ? new object[] { new object[] { "name", "=", name } } : new object[] { new object[] { "name", "=", name }, new object[] { "attribute_id", "=", attrId } };
        var res = proxy.execute_kw(DB, uid, PASS, model, "search", new object[] { crit }, new { });
        int[] ids = SafeConvertToIntArray(res);
        if (ids.Length > 0) return ids[0];
        var data = attrId == null ? (object)new { name = name } : new { name = name, attribute_id = attrId };
        return Convert.ToInt32(proxy.execute_kw(DB, uid, PASS, model, "create", new object[] { data }, new { }));
    }

    static int[] SafeConvertToIntArray(object result) {
        if (result is int[] i) return i;
        if (result is object[] o) return o.Select(Convert.ToInt32).ToArray();
        return Array.Empty<int>();
    }

    static int Authenticate() {
        var authProxy = XmlRpcProxyGen.Create<IOdooProxy>();
        authProxy.Url = URL + "common";
        return authProxy.authenticate(DB, USER, PASS, new { });
    }
}