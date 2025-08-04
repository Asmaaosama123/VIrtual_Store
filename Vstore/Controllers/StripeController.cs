using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Vstore.Services;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Vstore.Models;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly AppDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration configuration;

        public StripeController(UserManager<User> userManager, IConfiguration configuration, AppDBContext context, NotificationService notificationService)
        {
            _userManager = userManager;
            this.configuration = configuration;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost("checkout-session/{orderId}")]
        public async Task<IActionResult> CreateCheckoutSession(int orderId)
        {
            var order = await _context.Orders
                .Where(o => o.Order_Id == orderId)
                .Include(o => o.Order_Products)
                .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound("Order not found.");

            StripeConfiguration.ApiKey = "sk_test_51Qw6e9FPcMZyJkf0R9AI9rOu3trMC4JWPjxocDKI7xEH1qFFjqOu63EhxqAA765nx7XhWZJn9kkPPchv529Y8jx300Oojr5M4b";

            // حساب السعر بعد الخصم لكل منتج
            var lineItems = order.Order_Products.Select(op =>
            {
                var priceAfterDiscount = op.Product.Product_Price - (op.Product.Product_Price * op.Product.Sale_Percentage / 100);
                return new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        UnitAmount = (long)(priceAfterDiscount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = op.Product.Product_Name
                        }
                    },
                    Quantity = op.Quantity
                };
            }).ToList();

            // حساب إجمالي السعر بعد الخصم
            var totalAmountAfterDiscount = order.Order_Products.Sum(op =>
            {
                var priceAfterDiscount = op.Product.Product_Price - (op.Product.Product_Price * op.Product.Sale_Percentage / 100);
                return priceAfterDiscount * op.Quantity;
            });

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = "http://vstore.runasp.net/api/stripe/checkout-success?session_id={CHECKOUT_SESSION_ID}",
                LineItems = lineItems
            };

            var service = new SessionService();
            Session session = service.Create(options);

            Payment payment = new()
            {
                Amount = (long)(totalAmountAfterDiscount * 100), // بالقروش
                Currency = "egp",
                Status = "pending",
                StripeSessionId = session.Id,
                OrderId = orderId,
                CreatedAt = DateTime.Now,
            };
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return Ok(new { SessionId = session.Id, Url = session.Url, TotalAfterDiscount = totalAmountAfterDiscount });
        }

        [HttpPost("webhook/stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            const string endpointSecret = "whsec_CVgfRC4DbeuMEAfANCPEGU3QZYklfYxp";

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);

                    if (payment != null)
                    {
                        payment.Status = "success";
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest($"Stripe webhook error: {e.Message}, Type: {e.GetType().Name}");
            }
        }

        [HttpGet("checkout-success")]
        public IActionResult CheckoutSuccess(string session_id)
        {
            var deepLinkUrl = $"vstore://checkout-success?session_id={session_id}";
            return Redirect(deepLinkUrl);
        }

        [HttpGet("GetPaymentStatus/{OrderId}")]
        public async Task<IActionResult> GetPaymentStatus(int OrderId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == OrderId);

            if (payment == null)
                return NotFound("Payment not found for this order.");

            return Ok(new
            {
               
                PaymentStatus = payment.Status,
               
            });
        }

    }
}
