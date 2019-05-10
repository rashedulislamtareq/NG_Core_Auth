using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NG_Core_Auth.Data;
using NG_Core_Auth.Models;


namespace NG_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }


        [HttpGet("[action]")]
        [Authorize(Policy = "RequireLoggedIn")]
        public IActionResult GetProducts()
        {
            return Ok(_db.Product.ToList());
        }

        [HttpPost("[action]")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<IActionResult> AddProduct([FromBody]ProductModel productModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _db.Product.AddAsync(productModel);
                await _db.SaveChangesAsync();

                return Ok(productModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [HttpPut("[action]/{id}")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody]ProductModel productModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var productToUpdate = _db.Product.FirstOrDefault(x => x.ProductId == id);
                if (productToUpdate != null)
                {
                    productToUpdate.Description = productModel.Description;
                    productToUpdate.ImageUrl = productModel.ImageUrl;
                    productToUpdate.Name = productModel.Name;
                    productToUpdate.OutOfStock = productModel.OutOfStock;
                    productToUpdate.Price = productModel.Price;

                    _db.Entry(productToUpdate).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    return Ok(new JsonResult("Product with Id "+id+" is Updated"));
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [HttpDelete("[action]/{id}")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var productToDelete = _db.Product.FirstOrDefault(x => x.ProductId == id);
                if (productToDelete != null)
                {
                    _db.Product.Remove(productToDelete);
                    await _db.SaveChangesAsync();

                    return Ok(new JsonResult("Product with Id " + id + " is Deleted"));
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
