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
            var lines = File.ReadAllLines("produits.csv").Skip(1);

            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var data = line.Split(',');
                string name = data[0].Trim();
                double price = double.Parse(data[1], CultureInfo.InvariantCulture);
                double cost = double.Parse(data[2], CultureInfo.InvariantCulture);
                string attrName = data[3].Trim();
                string attrVal = data[4].Trim();
                double stockQty = data.Length >= 6 ? double.Parse(data[5], CultureInfo.InvariantCulture) : 0.0;

                int attrId = GetOrCreate(proxy, uid, "product.attribute", attrName);
                int valId = GetOrCreate(proxy, uid, "product.attribute.value", attrVal, attrId);

                int templateId = GetProductTemplateByName(proxy, uid, name);
                if (templateId == 0) {
                    templateId = CreateProductTemplate(proxy, uid, name, price, cost);
                }

                LinkAttributeToTemplate(proxy, uid, templateId, attrId, valId);

                int variantId = GetVariantId(proxy, uid, templateId, valId);
                if (variantId > 0) {
                    proxy.execute_kw(DB, uid, PASS, "product.product", "write", new object[] {
                        new int[] { variantId }, new { lst_price = price, standard_price = cost }
                    }, new { });

                    UpdateStock(proxy, uid, variantId, stockQty);
                    Console.WriteLine($"{name} ({attrVal}) traité avec succès.");
                }
            }
            Console.WriteLine("\nImportation terminée !");
        } catch (Exception ex) { Console.WriteLine($"Erreur : {ex.Message}"); }
    }

    static void UpdateStock(IOdooProxy proxy, int uid, int variantId, double qty) {
    try {
        // 1. Création de la ligne de stock
        int quantId = Convert.ToInt32(proxy.execute_kw(DB, uid, PASS, "stock.quant", "create", new object[] {
            new { product_id = variantId, location_id = MY_LOCATION_ID, inventory_quantity = qty }
        }, new { }));

        // 2. Validation de l'inventaire
        try {
            proxy.execute_kw(DB, uid, PASS, "stock.quant", "action_apply_inventory", new object[] { 
                new int[] { quantId } 
            }, new { });
        } catch (Exception ex) when (ex.Message.Contains("marshal None")) {
            // On ignore cette erreur car c'est juste Odoo qui renvoie une réponse vide après succès
        }
        
        Console.WriteLine($"Stock mis à jour : {qty}");
    } catch (Exception ex) {
        Console.WriteLine($"Erreur réelle sur le stock : {ex.Message}");
    }
}

    static int CreateProductTemplate(IOdooProxy proxy, int uid, string name, double price, double cost) {
        string[] types = { "storable", "consu", "product" };
        foreach (var t in types) {
            try {
                return Convert.ToInt32(proxy.execute_kw(DB, uid, PASS, "product.template", "create", new object[] {
                    new { name = name, list_price = price, standard_price = cost, type = t }
                }, new { }));
            } catch { continue; }
        }
        return Convert.ToInt32(proxy.execute_kw(DB, uid, PASS, "product.template", "create", new object[] {
            new { name = name, list_price = price, standard_price = cost }
        }, new { }));
    }

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