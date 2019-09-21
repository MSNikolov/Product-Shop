using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProductShop.Data;
using ProductShop.Models;

namespace ProductShop
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            var context = new ProductShopContext();

            Console.WriteLine(GetUsersWithProducts(context));
        }

        public static string ImportUsers(ProductShopContext context, string inputJson)
        {
            var users = JsonConvert.DeserializeObject<List<User>>(inputJson);

            context.Users.AddRange(users);

            context.SaveChanges();

            return $"Successfully imported {users.Count}";
        }

        public static string ImportProducts(ProductShopContext context, string inputJson)
        {
            var products = JsonConvert.DeserializeObject<List<Product>>(inputJson);

            context.AddRange(products);

            context.SaveChanges();

            return $"Successfully imported {products.Count}";
        }

        public static string ImportCategories(ProductShopContext context, string inputJson)
        {
            var categories = JsonConvert.DeserializeObject<List<Category>>(inputJson).Where(c => c.Name != null);

            context.Categories.AddRange(categories);

            context.SaveChanges();

            return $"Successfully imported {categories.Count()}";
        }

        public static string ImportCategoryProducts(ProductShopContext context, string inputJson)
        {
            var catProducts = JsonConvert.DeserializeObject<List<CategoryProduct>>(inputJson);

            context.CategoryProducts.AddRange(catProducts);

            context.SaveChanges();

            return $"Successfully imported {catProducts.Count}";
        }

        public static string GetProductsInRange(ProductShopContext context)
        {
            var products = context
                .Products
                .Select(p => new
                {
                    name = p.Name,
                    price = p.Price,
                    seller = p.Seller.FirstName + " " + p.Seller.LastName
                })
                .Where(p => p.price >= 500 && p.price <= 1000)
                .OrderBy(p => p.price);

            var productsInRangeJson = JsonConvert.SerializeObject(products, Formatting.Indented);

            return productsInRangeJson.Trim();
        }

        public static string GetSoldProducts(ProductShopContext context)
        {
            var sold = context
                .Users
                .Include(u => u.ProductsSold)
                .ThenInclude(ps => ps.Buyer)
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null))
                .Select(u => new
                {
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    soldProducts = u.ProductsSold
                    .Where(ps => ps.Buyer != null)
                    .Select(ps => new
                    {
                        name = ps.Name,
                        price = ps.Price,
                        buyerFirstName = ps.Buyer.FirstName,
                        buyerLastName = ps.Buyer.LastName
                    })
                })
                .OrderBy(u => u.lastName)
                .ThenBy(u => u.firstName);

            return JsonConvert.SerializeObject(sold, Formatting.Indented);
        }

        public static string GetCategoriesByProductsCount(ProductShopContext context)
        {
            var categories = context
                .Categories
                .Include(c => c.CategoryProducts)
                .ThenInclude(cp => cp.Product)
                .Select(c => new
                {
                    category = c.Name,
                    productsCount = c.CategoryProducts.Count,
                    averagePrice = Math.Round(c.CategoryProducts.Average(cp => cp.Product.Price), 2).ToString(),
                    totalRevenue = c.CategoryProducts.Sum(cp => cp.Product.Price).ToString()
                })
                .OrderByDescending(c => c.productsCount);

            return JsonConvert.SerializeObject(categories, Formatting.Indented);
        }

        public static string GetUsersWithProducts(ProductShopContext context)
        {
            var users = context
                .Users
                .Include(u => u.ProductsSold)
                .ThenInclude(ps => ps.Buyer)
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.Age,
                    soldProducts = new
                    {
                        count = u.ProductsSold.Where(ps => ps.Buyer != null).Count(),
                        products = u.ProductsSold
                        .Where(ps => ps.Buyer != null)
                        .Select(ps => new
                        {
                            name = ps.Name,
                            price = ps.Price
                        })
                    }

                })
                .Where(u => u.soldProducts.count > 0)
                .OrderByDescending(u => u.soldProducts.count);

            var usersWithSold = new
            {
                usersCount = users.Count(),
                users
            };

            return JsonConvert.SerializeObject(usersWithSold, Formatting.Indented);



        }
    }
} 