namespace MyWebShop.Models
{
    public class OrderRequest
    {
        public int PartnerId { get; set; } // L'ID du client dans Odoo
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public double Quantity { get; set; }
    }
}