using MyWebShop.Models;
using System.Collections.Generic;

namespace MyWebShop.Services
{
    public interface IOdooService
    {
       
        List<Product> GetProducts(); 
        int CreateProduct(Product product);
    }
}