using CookComputing.XmlRpc;

namespace MyWebShop.Services
{
    // Cette interface permet à C# de parler à l'API Odoo via XML-RPC
    public interface IOdooProxy : IXmlRpcProxy
    {
        [XmlRpcMethod("authenticate")]
        int authenticate(string db, string user, string pass, object[] args);

        [XmlRpcMethod("execute_kw")]
        object execute_kw(string db, int uid, string pass, string model, string method, object[] args);

        [XmlRpcMethod("execute_kw")]
        object execute_kw(string db, int uid, string pass, string model, string method, object[] args, object kwargs);
    }
}