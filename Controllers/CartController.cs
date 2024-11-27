using Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf.draw;
using Newtonsoft.Json;

namespace Project.Controllers
{
    public class CartController : Controller
    {
       
        BudgetBuysEntities4 _context = new BudgetBuysEntities4();
        // Fetch cart data for the logged-in user

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            base.OnActionExecuting(filterContext);
        }


        [Authorize]
        public ActionResult MyCart()
        {
            var userId = Convert.ToInt32(Session["UserID"]);
            var cartItems = _context.Carts
                                    .Include(c => c.Product) // Ensure Product entity is included
                                    .Where(c => c.CustomerID == userId)
                                    .ToList();

            foreach (var cartItem in cartItems)
            {
                Console.WriteLine($"CartID: {cartItem.CartID}, ProductID: {cartItem.ProductID}, ProductPhoto: {cartItem.Product?.ProductPhoto}");
            }


            return View(cartItems); // Pass cartItems as the model
        }



        [HttpPost]
        public ActionResult AddToCart(int id, int quantity = 1)
        {
            var customerID = GetCustomerID(); // Get the current customer ID
            var product = _context.Products.FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return HttpNotFound("Product not found."); // Handle product not found case
            }

            // Retrieve the cart from the session, or create a new one if it doesn't exist
            var cart = Session["Cart"] as List<Cart> ?? new List<Cart>();

            // Check if the item already exists in the cart
            var existingItem = _context.Carts.FirstOrDefault(c => c.ProductID == id && c.CustomerID == customerID);

            if (existingItem != null)
            {
                // Update the quantity if the product already exists
                existingItem.Quantity += quantity;
            }
            else
            {
                // If the item does not exist, create a new cart item
                var newCartItem = new Cart
                {
                    ProductID = id,
                    ProductName = product.ProductName,
                    ProductPrice = product.Price,
                    Quantity = quantity,
                    CustomerID = customerID, // Use actual customer logic
                    AddedAt = DateTime.Now
                };

                // Add the new item to the cart
                //cart.Add(newCartItem);

                // Save to the database (if needed)
                _context.Carts.Add(newCartItem);

                // Reduce the product quantity if necessary
                //product.Quantity -= quantity;
                 // Commit changes to the database
            }
            _context.SaveChanges();
            // Save the updated cart back to the session
            Session["Cart"] = cart;

            // Redirect to the cart page
            return RedirectToAction("myCart", "Cart");
        }

        [HttpPost]
        [Authorize]
        public ActionResult RemoveFromCart(int cartId)
        {
            var customerId = GetCustomerID(); // Get the current customer ID

            // Find the cart item for this user with the specific cart item ID
            var cartItem = _context.Carts.FirstOrDefault(c => c.CartID == cartId && c.CustomerID == customerId);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }
            else
            {
                // If the cart item doesn't exist, you could add an error message
                TempData["Error"] = "Cart item not found!";
            }

            return RedirectToAction("MyCart");
        }


        private int GetCustomerID()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userName = User.Identity.Name; // Get the logged-in user's username
                var customer = _context.Customers.FirstOrDefault(c => c.FullName == userName);

                if (customer != null)
                {
                    return customer.CustomerID;
                }
            }

            throw new Exception("Customer not found or not logged in");
        }






        //==========================================================

        [HttpPost]
        public ActionResult CancelOrder(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Ensure the order can be canceled (add your own logic here)
            if (order.Status != "Delivered")  // Example: Only cancel orders that are not shipped
            {
                order.Status = "Canceled";  // Update order status to "Canceled"
                _context.SaveChanges();

                TempData["Message"] = "Your order has been canceled successfully!";
            }
            else
            {
                TempData["Message"] = "This order cannot be canceled because it has already been shipped.";
            }

            return RedirectToAction("MyOrders");
        }


        //=================================================



        [Authorize]
        public ActionResult Checkout()
        {
            var customerId = GetCustomerID(); // Get the customer ID of the logged-in user
            var cartItems = _context.Carts.Where(c => c.CustomerID == customerId).ToList(); // Get the cart items for the logged-in user



            if (cartItems == null || !cartItems.Any())
            {
                ViewBag.Message = "Your cart is empty!";
                return View(); // Return to a different view or show a message if the cart is empty
            }

            // Fetch the customer details (Email and Phone) based on the customer ID
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == customerId);

            // If the customer is found, pass the data to the view via ViewBag
            if (customer != null)
            {
                ViewBag.Email = customer.Email;
                ViewBag.Phone = customer.PhoneNumber;
            }


            return View(cartItems);
        }



        public ActionResult ProcessOrder(string FirstName, string LastName, string Email, string Phone, string Address,
                     string City, string State, string ZipCode, string PaymentMethod)
        {
            // Step 1: Save User Information
            var user = new UserAddress
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Phone = Phone,
                Address = Address,
                City = City,
                State = State,
                ZipCode = ZipCode,
                CustomerID = GetCustomerID()
            };

            // Save user information
            _context.UserAddresses.Add(user);
            _context.SaveChanges(); // Ensure UserID is generated
            var userId = user.UserId; // Retrieve the generated UserId

            // Step 2: Retrieve Cart Items
            var cartItems = GetCartItems(); // Fetch from the database

            // Check if cartItems is null or empty
            if (cartItems == null || cartItems.Count == 0)
            {
                ModelState.AddModelError("", "Your cart is empty. Please add items before placing an order.");
                return View(); // Return to the view with an error message
            }

            // Calculate total amount
            decimal totalAmount = cartItems.Sum(item => item.Quantity * item.ProductPrice);

            // Step 3: Create Order Information
            var order = new Order
            {
                UserId = userId, // Use the correct UserId here
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                PaymentMethod = PaymentMethod,
                CustomerID= GetCustomerID(),
                DeliveryDate = DateTime.Now.AddDays(7), // Set default delivery date
                Status = "InProgress" // Set default status
            };

            // Save order information
            _context.Orders.Add(order);

            try
            {
                _context.SaveChanges(); // Ensure order ID is generated
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your order: " + ex.Message);
                return View();
            }

            // Step 4: Save Order Items and Update Product Quantities
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId, // Reference to the created order
                    ProductId = item.ProductID, // Ensure correct mapping
                    Quantity = item.Quantity,
                    UnitPrice = item.ProductPrice
                };

                _context.OrderItems.Add(orderItem); // Add order item to the context

                // Step 5: Update Product Quantity
                var product = _context.Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                if (product != null)
                {
                    product.Quantity -= item.Quantity; // Decrease the product quantity
                                                       // Optional: Check if quantity goes below zero
                    if (product.Quantity < 1)
                    {
                        ModelState.AddModelError("", "Not enough stock for " + product.ProductName);
                        return View();
                    }
                }
            }

            // Save all order items and updated product quantities
            try
            {
                _context.SaveChanges(); // Save Order Items and updated Product Quantities
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving order items: " + ex.Message);
                return View();
            }

            // Step 6: Clear the cart
            ClearCart();

            // Step 7: Redirect to an order confirmation page
            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }



        // Example of GetCartItems() method
        private List<Cart> GetCartItems()
        {
            var customerId = GetCustomerID(); // Ensure you have the correct customer ID
            return _context.Carts.Where(c => c.CustomerID == customerId).ToList(); // Get cart items from the database
        }


        // Example of ClearCart() method
        private void ClearCart()
        {
            var id = GetCustomerID();
            // Session["Cart"] = null; // Clear the session cart
            var cartItems = _context.Carts.Where(c => c.CustomerID == id).ToList(); // Get cart items for the user
            if (cartItems.Any())
            {
                _context.Carts.RemoveRange(cartItems); // Remove all items from the cart
                _context.SaveChanges(); // Commit changes to the database
            }
        }

        public ActionResult OrderConfirmation(int orderId)
        {
            // Check if Orders exists
            if (_context.Orders == null)
            {
                return HttpNotFound("Orders table is null");
            }

            // Include OrderItems
            var order = _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return HttpNotFound("Order not found");
            }

            // Check if OrderItems is null or empty
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                return HttpNotFound("No order items found for this order.");
            }

            return View(order);
        }

        public ActionResult MyOrders()
        {
            // Assuming you are using Identity for authentication
            var userId = GetCustomerID(); // Get the current user's ID

            // Fetch orders for the current user
            var orders = _context.Orders
                                 .Where(o => o.CustomerID == userId)
                                 .Include(o => o.OrderItems)
                                 .Include(o => o.OrderItems.Select(oi => oi.Product))
                                 .ToList();

            // Check if orders list is null, set to empty list if so
            if (orders == null)
            {
                orders = new List<Order>();
            }

            return View(orders); // Pass the orders to the view
        }

        public ActionResult OrderDetails(int orderId)
        {
            var order = _context.Orders.Include(o => o.OrderItems) // Include order items or related entities
                                 .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);  // Pass the order details to the view
        }





        //[Authorize]
        //public ActionResult Checkout()
        //{
        //    var customerId = GetCustomerID();
        //    var cartItems = _context.CartItems.Where(c => c.CustomerID == customerId).ToList(); // Get the user's cart items
        //    return View(cartItems);
        //}


        //[HttpPost]
        //[Authorize]
        //public ActionResult ProcessOrder(string address, string phone)
        //{
        //    var customerId = GetCustomerID();
        //    var cartItems = _context.CartItems.Where(c => c.CustomerID == customerId).ToList();

        //    // Here, you should implement the logic to process the order, such as:
        //    // 1. Create an order in the Orders table
        //    // 2. Clear the cart items for the user after processing the order

        //    if (cartItems.Any())
        //    {
        //        // Assuming you have an Order model and Orders table in your database
        //        var order = new Order
        //        {
        //            CustomerID = customerId,
        //            Address = address,
        //            Phone = phone,
        //            OrderDate = DateTime.Now,
        //            TotalAmount = cartItems.Sum(item => item.Price * item.Quantity)
        //        };

        //        _context.Orders.Add(order);
        //        _context.SaveChanges();

        //        // Remove cart items after order is placed
        //        _context.CartItems.RemoveRange(cartItems);
        //        _context.SaveChanges();
        //    }

        //    // Redirect to a confirmation page or back to the cart
        //    return RedirectToAction("OrderConfirmation");
        //}

        //[Authorize]
        //public ActionResult OrderConfirmation()
        //{
        //    return View();
        //}


        public ActionResult DownloadReceipt(int orderId)
        {
            // Fetch the order details
            var order = _context.Orders
                                .Include(o => o.OrderItems.Select(oi => oi.Product))
                                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return HttpNotFound("Order not found");
            }

            // Create a memory stream to store the PDF document
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 60, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                // Fonts for different sections
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                Font regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DARK_GRAY);
                Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);

                // Set up header section with a distinct background color for "BUDGET BUYS" and improved layout for company info
                PdfPTable headerTable = new PdfPTable(1) { WidthPercentage = 100 };
                headerTable.DefaultCell.Border = Rectangle.NO_BORDER;

                // "BUDGET BUYS" title with a bolder background color and center alignment
                PdfPCell headerCell = new PdfPCell(new Phrase("BUDGET BUYS", titleFont))
                {
                    BackgroundColor = new BaseColor(60, 130, 190), // Darker blue background for emphasis
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 15f,
                    Border = Rectangle.NO_BORDER
                };
                headerTable.AddCell(headerCell);

                // Add company information in a separate cell, with improved alignment and spacing
                PdfPTable companyInfoTable = new PdfPTable(1) { WidthPercentage = 100 };
                companyInfoTable.DefaultCell.Border = Rectangle.NO_BORDER;

                PdfPCell companyInfoCell = new PdfPCell(new Phrase(
                    "111, Nexus-7, College Road\nNadiad, 387001\nEmail: info.budgetbuys@gmail.com\nPhone: +91 234 567 88, +91 234 567 89",
                    regularFont))
                {
                    BackgroundColor = new BaseColor(240, 240, 240), // Light gray background for contrast
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    Padding = 10f,
                    Border = Rectangle.NO_BORDER
                };
                companyInfoTable.AddCell(companyInfoCell);

                // Add both the title and company info to the document
                headerTable.AddCell(companyInfoTable);
                document.Add(headerTable);

                document.Add(new Paragraph("\n")); // Spacer

                // Separator line under header
                document.Add(new Paragraph(new Chunk(new LineSeparator(1f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2))));

                document.Add(new Paragraph("\n")); // Spacer

                // Add Order Receipt title with spacing
                Paragraph title = new Paragraph("Order Receipt", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                document.Add(title);

                // Customer and order summary with better spacing and alignment adjustments
                PdfPTable infoTable = new PdfPTable(2) { WidthPercentage = 100 };
                infoTable.SetWidths(new float[] { 50, 50 });

                // Customer details cell with background color and left alignment
                PdfPCell customerInfoCell = new PdfPCell(new Phrase(
                    $"Customer:\n{order.UserAddress.FirstName} {order.UserAddress.LastName}\n{order.UserAddress.Address}\n" +
                    $"{order.UserAddress.City}, {order.UserAddress.State} {order.UserAddress.ZipCode}",
                    regularFont))
                {
                    BackgroundColor = new BaseColor(245, 245, 245), // Light gray background for contrast
                    Border = Rectangle.NO_BORDER,
                    Padding = 10f,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };
                infoTable.AddCell(customerInfoCell);

                // Creating a nested table to right-align content in the order details cell
                PdfPTable orderDetailsTable = new PdfPTable(1) { WidthPercentage = 100 };
                orderDetailsTable.DefaultCell.Border = Rectangle.NO_BORDER;
                orderDetailsTable.DefaultCell.HorizontalAlignment = Element.ALIGN_RIGHT;

                // Add order details content as a right-aligned paragraph
                PdfPCell orderDetailsCell = new PdfPCell(new Phrase(
                    $"Order ID: {order.OrderId}\nOrder Date: {order.OrderDate:dd/MM/yyyy}\n" +
                    $"Total Amount: Rs. {order.TotalAmount:F2}\nPayment Method: {order.PaymentMethod}",
                    regularFont))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 5f
                };
                orderDetailsTable.AddCell(orderDetailsCell);

                // Adding the nested table to the main cell
                PdfPCell orderInfoCell = new PdfPCell(orderDetailsTable)
                {
                    BackgroundColor = new BaseColor(245, 245, 245), // Light gray background for consistency
                    Border = Rectangle.NO_BORDER,
                    Padding = 10f
                };
                infoTable.AddCell(orderInfoCell);

                document.Add(infoTable);
                document.Add(new Paragraph("\n")); // Spacer

                // Table header for order items
                PdfPTable table = new PdfPTable(4) { WidthPercentage = 100, SpacingBefore = 10f };
                table.SetWidths(new float[] { 40, 20, 20, 20 });

                // Header row with background color
                PdfPCell[] headers = {
                    new PdfPCell(new Phrase("Product Name", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5f, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase("Unit Price", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5f, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase("Quantity", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5f, HorizontalAlignment = Element.ALIGN_CENTER },
                    new PdfPCell(new Phrase("Total", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5f, HorizontalAlignment = Element.ALIGN_CENTER }
                };
                foreach (var header in headers)
                {
                    table.AddCell(header);
                }

                // Add order items
                foreach (var item in order.OrderItems)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.Product.ProductName, regularFont)) { Padding = 5f });
                    table.AddCell(new PdfPCell(new Phrase($"Rs. {item.UnitPrice:F2}", regularFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
                    table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), regularFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 5f });
                    table.AddCell(new PdfPCell(new Phrase($"Rs. {(item.UnitPrice * item.Quantity):F2}", regularFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
                }

                document.Add(table);


                document.Add(new Paragraph("\n")); // Spacer
                // Separator line before footer
                document.Add(new Paragraph(new Chunk(new LineSeparator(1f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2))));

                // Footer (Total amount and thank you note)
                Paragraph totalAmount = new Paragraph($"Total Amount: Rs. {order.TotalAmount:F2}", headerFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 10f
                };
                document.Add(totalAmount);

                document.Add(new Paragraph("\n")); // Spacer
                document.Add(new Paragraph("\n")); // Spacer

                Paragraph thankYouNote = new Paragraph("Thank you for shopping with us!", regularFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 20f,
                    SpacingAfter = 20f
                };
                document.Add(thankYouNote);

                document.Close();
                writer.Close();

                return File(ms.ToArray(), "application/pdf", $"OrderReceipt_{order.OrderId}.pdf");
            }
        }



        //public ActionResult DownloadOrderDetailsPdf(int orderId)
        //{
        //    var order = _context.Orders.Include("OrderItems.Product") // Load related data as needed
        //                          .FirstOrDefault(o => o.OrderId == orderId);

        //    if (order == null)
        //        return HttpNotFound();

        //    return new Rotativa.ViewAsPdf("OrderDetails", order) // Specify your view and model
        //    {
        //        FileName = $"Order_{orderId}_Details.pdf"
        //    };
        //}


    }
}