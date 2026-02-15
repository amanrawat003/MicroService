using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public ProductsController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            return Ok(await _context.Products.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == name);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

    }
}
