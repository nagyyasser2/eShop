using eShop.Core.DTOs;
using System;
using System.Linq;

namespace eShop.Core.Services.Implementations
{
    public static class OrderEmailTemplate
    {
        public static string GenerateOrderConfirmationEmail(OrderDto order)
        {
            var itemsHtml = string.Join("", order.OrderItems.Select(item =>
                $@"<tr style='border-bottom: 1px solid #e5e7eb;'>
                    <td style='padding: 12px 16px; color: #374151;'>{item.ProductName}</td>
                    <td style='padding: 12px 16px; text-align: center; color: #374151;'>{item.Quantity}</td>
                    <td style='padding: 12px 16px; text-align: right; color: #374151;'>{item.UnitPrice:C}</td>
                    <td style='padding: 12px 16px; text-align: right; font-weight: 600; color: #3b82f6;'>{item.TotalPrice:C}</td>
                </tr>"));

            return $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Order Confirmation</title>
                </head>
                <body style='margin: 0; padding: 10px; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 50%, #cbd5e1 100%); min-height: 100vh;'>
                    <div style='max-width: 600px; margin: 40px auto; background: #ffffff;  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);'>
        
                        <!-- Header -->
                        <div style='padding: 24px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 28px; font-weight: 700; background: linear-gradient(90deg, #3b82f6, #8b5cf6, #ec4899); -webkit-background-clip: text; background-clip: text; color: transparent;'>
                                ShopHub
                            </h1>
                            <p style='margin: 8px 0 0 0; color: #64748b; font-size: 14px;'>Your Modern Shopping Experience</p>
                        </div>

                        <!-- Main Content -->
                        <div style='padding: 24px;'>
            
                            <!-- Order Confirmation Header -->
                            <div style='text-align: center; margin-bottom: 32px;'>
                                <div style='display: inline-block; padding: 10px 20px; background: linear-gradient(90deg, #3b82f6, #8b5cf6); border-radius: 20px;'>
                                    <span style='color: white; font-weight: 600; font-size: 16px;'>Order Confirmed</span>
                                </div>
                                <h2 style='margin: 12px 0 8px 0; font-size: 24px; font-weight: 700; color: #1f2937;'>
                                    Order #{order.OrderNumber}
                                </h2>
                                <p style='margin: 0; color: #6b7280; font-size: 14px;'>
                                    {order.CreatedAt:MMMM dd, yyyy 'at' HH:mm}
                                </p>
                            </div>

                            <!-- Greeting -->
                            <div style='margin-bottom: 24px; padding: 20px; background: #f8fafc; border-radius: 8px; border: 1px solid #e5e7eb;'>
                                <h3 style='margin: 0 0 10px 0; font-size: 18px; background: linear-gradient(90deg, #8b5cf6, #ec4899); -webkit-background-clip: text; background-clip: text; color: transparent; font-weight: 700;'>
                                    Dear {order.ShippingFirstName} {order.ShippingLastName},
                                </h3>
                                <p style='margin: 0; color: #4b5563; line-height: 1.5; font-size: 14px;'>
                                    Thank you for choosing ShopHub! Your order is confirmed and being processed.
                                </p>
                            </div>

                            <!-- Order Items -->
                            <div style='margin-bottom: 24px;'>
                                <h3 style='margin: 0 0 16px 0; font-size: 18px; color: #1f2937; font-weight: 700;'>
                                    Order Items
                                </h3>
                                <table style='width: 100%; border-collapse: collapse; background: #ffffff; border-radius: 8px; border: 1px solid #e5e7eb;'>
                                    <thead>
                                        <tr style='background: #f8fafc;'>
                                            <th style='padding: 12px 16px; text-align: left; color: #1f2937; font-weight: 600; border-bottom: 2px solid #3b82f6;'>Product</th>
                                            <th style='padding: 12px 16px; text-align: center; color: #1f2937; font-weight: 600; border-bottom: 2px solid #8b5cf6;'>Qty</th>
                                            <th style='padding: 12px 16px; text-align: right; color: #1f2937; font-weight: 600; border-bottom: 2px solid #ec4899;'>Unit Price</th>
                                            <th style='padding: 12px 16px; text-align: right; color: #1f2937; font-weight: 600; border-bottom: 2px solid #3b82f6;'>Total</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {itemsHtml}
                                    </tbody>
                                </table>
                            </div>

                            <!-- Order Summary -->
                            <div style='margin-bottom: 24px;'>
                                <h3 style='margin: 0 0 16px 0; font-size: 18px; color: #1f2937; font-weight: 700;'>
                                    Order Summary
                                </h3>
                                <div style='background: #ffffff; border-radius: 8px; padding: 20px; border: 1px solid #e5e7eb;'>
                                    <div style='display: flex; justify-content: space-between; margin-bottom: 10px;'>
                                        <span style='color: #6b7280;'>Subtotal:</span>
                                        <span style='color: #374151; font-weight: 600;'>{order.SubTotal:C}</span>
                                    </div>
                                    <div style='display: flex; justify-content: space-between; margin-bottom: 10px;'>
                                        <span style='color: #6b7280;'>Tax:</span>
                                        <span style='color: #374151; font-weight: 600;'>{order.TaxAmount:C}</span>
                                    </div>
                                    <div style='display: flex; justify-content: space-between; margin-bottom: 10px;'>
                                        <span style='color: #6b7280;'>Shipping:</span>
                                        <span style='color: #374151; font-weight: 600;'>{order.ShippingAmount:C}</span>
                                    </div>
                                    <div style='display: flex; justify-content: space-between; margin-bottom: 10px;'>
                                        <span style='color: #6b7280;'>Discount:</span>
                                        <span style='color: #059669; font-weight: 600;'>-{order.DiscountAmount:C}</span>
                                    </div>
                                    <div style='display: flex; justify-content: space-between; padding: 12px; background: linear-gradient(90deg, #3b82f6, #8b5cf6); border-radius: 8px;'>
                                        <span style='color: white; font-size: 16px; font-weight: 700;'>Total:</span>
                                        <span style='color: white; font-size: 16px; font-weight: 700;'>{order.TotalAmount:C}</span>
                                    </div>
                                </div>
                            </div>

                            <!-- Shipping Address -->
                            <div style='margin-bottom: 24px;'>
                                <h3 style='margin: 0 0 16px 0; font-size: 18px; color: #1f2937; font-weight: 700;'>
                                    Shipping Address
                                </h3>
                                <div style='background: #ffffff; border-radius: 8px; padding: 20px; border: 1px solid #e5e7eb;'>
                                    <div style='color: #6b7280; line-height: 1.5; font-size: 14px;'>
                                        <div style='font-weight: 600; color: #1f2937; margin-bottom: 8px;'>{order.ShippingFirstName} {order.ShippingLastName}</div>
                                        <div>{order.ShippingAddress}</div>
                                        <div>{order.ShippingCity}, {order.ShippingState} {order.ShippingZipCode}</div>
                                        <div>{order.ShippingCountry}</div>
                                    </div>
                                </div>
                            </div>

                            <!-- Footer Message -->
                            <div style='text-align: center; padding: 24px; background: #f8fafc; border-radius: 8px; border: 1px solid #e5e7eb;'>
                                <h3 style='margin: 0 0 12px 0; font-size: 20px; background: linear-gradient(90deg, #3b82f6, #8b5cf6, #ec4899); -webkit-background-clip: text; background-clip: text; color: transparent; font-weight: 700;'>
                                    Thank You!
                                </h3>
                                <p style='margin: 0 0 12px 0; color: #4b5563; line-height: 1.5; font-size: 14px;'>
                                    We appreciate your business. You'll receive a shipping notification once your order is on its way.
                                </p>
                                <div style='display: inline-block; padding: 10px 20px; background: linear-gradient(90deg, #8b5cf6, #ec4899); border-radius: 20px;'>
                                    <span style='color: white; font-weight: 600; font-size: 14px;'>Happy Shopping! 🛍️</span>
                                </div>
                            </div>
                        </div>

                        <!-- Footer -->
                        <div style='padding: 24px; text-align: center; background: #f8fafc; border-top: 1px solid #e5e7eb;'>
                            <p style='margin: 0 0 12px 0; color: #6b7280; font-size: 12px;'>
                                © {DateTime.Now.Year} ShopHub. All rights reserved.
                            </p>
                            <div style='margin-bottom: 12px;'>
                                <span style='color: #6b7280; font-size: 12px; margin-right: 12px;'>📧 support@eshop.com</span>
                                <span style='color: #6b7280; font-size: 12px;'>📞 (+20) 1090312546</span>
                            </div>
                            <p style='margin: 0; color: #9ca3af; font-size: 12px;'>
                                123 E-Shop St, Commerce City, EG
                            </p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}