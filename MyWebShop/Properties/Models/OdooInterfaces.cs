using CookComputing.XmlRpc;

namespace MyWebShop
{
    // Cette interface permet de mapper les fonctions d'Odoo vers C#
    public interface IOdooProxy : IXmlRpcProxy
    {
        [XmlRpcMethod("authenticate")]
        int Authenticate(string db, string user, string pass, object[] args);

        [XmlRpcMethod("execute_kw")]
        object ExecuteKw(string db, int uid, string pass, string model, string method, object[] args, object kwargs);
    }
}