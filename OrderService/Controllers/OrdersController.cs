using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Events;
using OrderService.Messaging;
using OrderService.Models;
using System.Net.Http.Json;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdersController(OrderDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _context.Orders.ToListAsync());
        }

        //[HttpPost]
        //public async Task<IActionResult> Create(Order order)
        //{
        //    _context.Orders.Add(order);
        //    await _context.SaveChangesAsync();
        //    return Ok(order);
        //}

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            var client = _httpClientFactory.CreateClient("ProductService");

            var product = await client
                .GetFromJsonAsync<ProductDto>($"/api/products/{order.ProductId}");

            if (product == null)
                return BadRequest("Invalid product");

            if (product.Stock < order.Quantity)
                return BadRequest("Insufficient stock");

            order.TotalAmount = product.Price * order.Quantity;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var publisher = new OrderEventPublisher();

            publisher.PublishOrderCreated(new OrderCreatedEvent
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = order.Quantity
            });

            return Ok(order);
        }

    }
}
