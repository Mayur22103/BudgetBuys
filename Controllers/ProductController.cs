using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Project.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static iTextSharp.text.pdf.AcroFields;
using System.Web.UI.WebControls;
using iText.IO.Image;
using OxyPlot;
using OxyPlot.Series;
//using OxyPlot.SkiaSharp;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using iText.Kernel.Events;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
using System.Globalization;
using iText.Kernel.Font;


namespace Project.Controllers
{
    public class ProductController : Controller
    {
        BudgetBuysEntities4 _context = new BudgetBuysEntities4();

        // GET: Product/Categories

        [HttpGet]
        public JsonResult GetSubcategories(int categoryId)
        {
            var subcategories = _context.Subcategories
                                  .Where(sc => sc.CategoryID == categoryId)
                                  .Select(sc => new
                                  {
                                      sc.SubCategoryId,
                                      sc.SubCategoryName
                                  }).ToList();

            return Json(subcategories, JsonRequestBehavior.AllowGet);
        }


        public ActionResult FilterSoldProducts(string productName, string customerName, string status)
        {
            // Start with the sold products query
            var soldProductsQuery = from oi in _context.OrderItems
                                    join p in _context.Products on oi.ProductId equals p.ProductID
                                    join o in _context.Orders on oi.OrderId equals o.OrderId
                                    join c in _context.Customers on o.CustomerID equals c.CustomerID
                                    select new
                                    {
                                        ProductID = p.ProductID,
                                        ProductName = p.ProductName,
                                        Quantity = oi.Quantity,
                                        UnitPrice = oi.UnitPrice,
                                        TotalPrice = oi.TotalPrice,
                                        OrderDate = o.OrderDate,
                                        CustomerName = c.FullName,
                                        CustomerEmail = c.Email,
                                        Status = o.Status
                                    };

            // Apply Product Name filter if provided
            if (!string.IsNullOrEmpty(productName))
            {
                soldProductsQuery = soldProductsQuery.Where(x => x.ProductName.Contains(productName));
            }

            // Apply Customer Name filter if provided
            if (!string.IsNullOrEmpty(customerName))
            {
                soldProductsQuery = soldProductsQuery.Where(x => x.CustomerName.Contains(customerName));
            }

            // Apply Status filter if provided
            if (!string.IsNullOrEmpty(status))
            {
                soldProductsQuery = soldProductsQuery.Where(x => x.Status.Equals(status));
            }

            // Execute the query and transform into an anonymous object (using ExpandoObject for dynamic property assignment)
            var soldProducts = soldProductsQuery.ToList().Select(x =>
            {
                dynamic expando = new ExpandoObject();
                expando.ProductID = x.ProductID;
                expando.ProductName = x.ProductName;
                expando.Quantity = x.Quantity;
                expando.UnitPrice = x.UnitPrice;
                expando.TotalPrice = x.TotalPrice;
                expando.OrderDate = x.OrderDate;
                expando.CustomerName = x.CustomerName;
                expando.CustomerEmail = x.CustomerEmail;
                expando.Status = x.Status;
                return expando;
            }).ToList();

            // Set filter values in ViewBag to retain them in the view
            ViewBag.ProductName = productName;
            ViewBag.CustomerName = customerName;
            ViewBag.Status = status;

            // Return the filtered sold products to the view
            return View(soldProducts);
        }




        public ActionResult Categories(int category)
        {
            // Fetch products based on the category ID
            var products = _context.Products
                            .Where(p => p.CategoryID == category) // CategoryID is assumed; adjust according to your model
                            .ToList();

            // Optional: Pass the category name to the view for display purposes
            var categoryName = _context.Categories
                                .Where(c => c.CategoryID == category)
                                .Select(c => c.CategoryName)
                                .FirstOrDefault();

            var subcategories = _context.Subcategories
                        .Where(sc => sc.CategoryID == category) // Assuming Subcategory has a CategoryID
                        .ToList();
            ViewBag.Subcategories = subcategories;

            ViewBag.CategoryName = categoryName ?? "Products"; // Default to "Products" if no category name is found

            // Pass the products list to the view
            return View(products);
        }

        [Authorize]
        public ActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return HttpNotFound("Product not found.");
            }

            return View(product);
        }

        public ActionResult AllCategories()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        public ActionResult CategoryFilter(int categoryId, int? subcategoryId)
        {
            // Fetch products based on the selected category ID
            var products = _context.Products.AsQueryable();

            if (subcategoryId.HasValue && subcategoryId.Value > 0)
            {
                // Filter products by the selected subcategory
                products = products.Where(p => p.SubCategoryID == subcategoryId.Value);
            }
            else
            {
                // If no subcategory is selected, fetch all products for the category
                products = products.Where(p => p.CategoryID == categoryId);
            }

            // Fetch category name for display purposes
            var categoryName = _context.Categories
                                .Where(c => c.CategoryID == categoryId)
                                .Select(c => c.CategoryName)
                                .FirstOrDefault();

            ViewBag.CategoryName = categoryName ?? "Products"; // Default to "Products" if no category name is found

            // Fetch subcategories for the selected category
            var subcategories = _context.Subcategories
                                .Where(sc => sc.CategoryID == categoryId)
                                .ToList();
            ViewBag.Subcategories = subcategories;

            // Return the filtered products to the view
            return View("Categories", products.ToList()); // Use the Categories view
        }



        // GET: Product/NewArrivals
        public ActionResult NewArrivals()
        {
            var latestProducts = _context.Products
                               .OrderByDescending(p => p.DateAdded)
                               .Take(8)
                               .ToList();
            return View(latestProducts);
        }

        // GET: Product/FeaturedProducts
        public ActionResult FeaturedProducts()
        {
            // Logic to fetch featured products
            return View(); // Will render Views/Product/FeaturedProducts.cshtml
        }






        public ActionResult AddProduct()
        {
            int SellerID = (int)Session["SellerID"];
            ViewBag.Categories = new SelectList(
                                _context.Categories
                                    .Where(c => c.SellerId == SellerID)
                                    .ToList(),
                                "CategoryID",
                                "CategoryName"
    );
            var genders = new List<SelectListItem>
        {
            new SelectListItem { Value = "Male", Text = "Male" },
            new SelectListItem { Value = "Female", Text = "Female" },
        };
            ViewBag.Genders = new SelectList(genders, "Value", "Text"); // Assuming GetGenders is your method to fetch genders
            return View();
        }


        [HttpPost]
        public ActionResult AddProductData(HttpPostedFileBase productPhoto, string productName, string description, int quantity, long price, int? category, string gender, string sellerName, int? subcategory, string productCondition)
        {
            if (ModelState.IsValid)
            {
                // Check if a photo is uploaded
                if (productPhoto != null && productPhoto.ContentLength > 0)
                {
                    var fileName = System.IO.Path.GetFileName(productPhoto.FileName);
                    var path = System.IO.Path.Combine(Server.MapPath("~/lib/images/Product"), fileName);
                    productPhoto.SaveAs(path);
                }

                // Create a new product instance
                var product = new Product
                {
                    ProductName = productName,
                    Description = description,
                    Quantity = quantity,
                    CategoryID = category,
                    SubCategoryID = subcategory,
                    Gender = gender,
                    Price = price,
                    SellerName = sellerName,
                    DateAdded = DateTime.Now,
                    ProductPhoto = "lib/images/Product/" + System.IO.Path.GetFileName(productPhoto.FileName),
                    ProductCondition = productCondition // Set ProductCondition from the form input
                };

                // Save product to the database
                using (var db = new BudgetBuysEntities4())
                {
                    db.Products.Add(product);
                    db.SaveChanges();
                }

                // Redirect to a success page or product list
                return RedirectToAction("MyProducts");
            }

            // If we got this far, something failed; redisplay the form
            ViewBag.ErrorMessage = "Please correct the errors and try again.";
            return View("AddProduct");
        }


        // GET: Product/Search
        public ActionResult Search(string q)
        {
            // Check if the search query is null, empty, or consists only of white-space characters
            if (string.IsNullOrWhiteSpace(q))
            {
                // Add error message to TempData and redirect to Home/Index
                TempData["Error"] = "Please enter a valid search term.";
                return RedirectToAction("Index", "Home"); // Redirect to the Home controller's Index action
            }

            // Proceed with the search query if it's valid
            var products = _context.Products
                .Where(p => p.ProductName.Contains(q) || p.Description.Contains(q))
                .ToList();

            // If no products are found, display a message to inform the user
            if (products.Count == 0)
            {
                TempData["Error"] = "No products found matching your search criteria.";
                return RedirectToAction("Index", "Home");  // Redirect to the Home controller's Index action
            }

            // Return the partial view with the filtered product list
            return PartialView("_ProductList", products); // Make sure this partial view exists
        }



        public ActionResult MyProducts()
        {
            string sellerName = User.Identity.Name;
            List<Product> products = _context.Products.Where(p => p.SellerName == sellerName).ToList();
            return View(products); // Will render Views/Product/Search.cshtml
        }

        public ActionResult DeleteMyProduct(int id)
        {
            // Log the ID being passed in
            System.Diagnostics.Debug.WriteLine($"Attempting to delete product with ID: {id}");

            var product = _context.Products.SingleOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                System.Diagnostics.Debug.WriteLine($"Product with ID {id} not found.");
                return HttpNotFound(); // Return 404 if product not found
            }

            _context.Products.Remove(product); // Remove the product from the context
            _context.SaveChanges(); // Commit the changes to the database

            return RedirectToAction("MyProducts"); // Redirect to the list of products
        }

        public ActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound("Product not found.");
            }
            return View(product);
        }

        [HttpPost]
        public ActionResult EditMyProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                // Fetch the existing product from the database
                var existingProduct = _context.Products.FirstOrDefault(p => p.ProductID == product.ProductID);
                if (existingProduct != null)
                {
                    // Update product details
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Description = product.Description;
                    existingProduct.Quantity = product.Quantity;
                    existingProduct.Price = product.Price;
                    existingProduct.Gender = product.Gender;
                    existingProduct.ProductCondition = product.ProductCondition;

                    // Save changes to the database
                    _context.SaveChanges();

                    // Redirect to the list or summary page after successful update
                    return RedirectToAction("MyProducts");
                }
            }

            // If something went wrong, return the view with the same model
            return View(product);
        }

        [HttpGet]
        public ActionResult FilterCategories(string sort, decimal? minPrice, decimal? maxPrice, int?[] categories, string[] productCondition)
        {
            // Retrieve all products from the database
            var products = _context.Products.AsQueryable();

            // Filter products by selected categories
            if (categories != null && categories.Length > 0)
            {
                products = products.Where(p => categories.Contains(p.CategoryID));
            }

            // Filter products by price range
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice);
            }

            // Filter products by product condition
            if (productCondition != null && productCondition.Length > 0)
            {
                products = products.Where(p => productCondition.Contains(p.ProductCondition));
            }

            // Sort products based on the selected sorting option
            switch (sort)
            {
                case "NameAsc":
                    products = products.OrderBy(p => p.ProductName);
                    break;
                case "NameDesc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "PriceAsc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "PriceDesc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    break;
            }

            // Return the filtered products to the product list partial view
            return PartialView("_ProductList", products.ToList());
        }



        //public ActionResult GeneratePDFReport()
        //{
        //    var products = _context.Products.ToList(); // Replace with seller-specific query if needed

        //    // Check if products are available
        //    if (products == null || !products.Any())
        //    {
        //        return Content("No products available to generate a report.");
        //    }

        //    // Create a memory stream to hold the PDF file
        //    var stream = new MemoryStream();

        //    // Initialize PDF writer and document
        //    var writer = new PdfWriter(stream);
        //    var pdfDoc = new PdfDocument(writer);
        //    var document = new Document(pdfDoc);

        //    // Add title
        //    document.Add(new Paragraph("Product Report")
        //        .SetTextAlignment(TextAlignment.CENTER)
        //        .SetFontSize(18)
        //        .SetBold());

        //    // Add table with columns
        //    Table table = new Table(UnitValue.CreatePercentArray(5)).UseAllAvailableWidth();
        //    table.AddHeaderCell("Product Name");
        //    table.AddHeaderCell("Description");
        //    table.AddHeaderCell("Quantity");
        //    table.AddHeaderCell("Price");
        //    table.AddHeaderCell("Gender");

        //    // Populate the table with product data
        //    foreach (var product in products)
        //    {
        //        table.AddCell(product.ProductName);
        //        table.AddCell(product.Description);
        //        table.AddCell(product.Quantity.ToString());
        //        table.AddCell(product.Price.ToString("C"));
        //        table.AddCell(!string.IsNullOrEmpty(product.Gender) ? product.Gender : "N/A");
        //    }

        //    document.Add(table);
        //    document.Flush(); // Ensures that all content is written to the stream without closing it

        //    // Reset the position of the stream to the beginning
        //    stream.Position = 0;

        //    // Return PDF file as a downloadable attachment
        //    return File(stream, "application/pdf", "ProductReport.pdf");
        //}





        //public ActionResult GeneratePDFReport()
        //{
        //    int sellerId = (int)Session["SellerID"];
        //    string sellerName = _context.Sellers
        //                        .Where(s => s.SellerID == sellerId)
        //                        .Select(s => s.FullName)
        //                        .FirstOrDefault();

        //    if (string.IsNullOrEmpty(sellerName))
        //    {
        //        return Content("Seller not found.");
        //    }
        //    // Query only products added by the current seller
        //    var products = _context.Products.Where(p => p.SellerName == sellerName).ToList();

        //    // Check if there are any products for this seller
        //    if (products == null || !products.Any())
        //    {
        //        return Content("No products available to generate a report.");
        //    }

        //    // Proceed with PDF generation
        //    var stream = new MemoryStream();

        //    try
        //    {
        //        var writer = new PdfWriter(stream);
        //        var pdfDoc = new PdfDocument(writer);
        //        var document = new Document(pdfDoc);

        //        document.Add(new Paragraph("Product Report")
        //            .SetTextAlignment(TextAlignment.CENTER)
        //            .SetFontSize(20)
        //            .SetBold()
        //            .SetFontColor(ColorConstants.BLUE)
        //            .SetMarginBottom(20));

        //        // Define a table with 7 columns: No., Image, Product Name, Description, Quantity, Price, Gender
        //        var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1, 2, 3, 1, 1, 1 })).UseAllAvailableWidth();

        //        // Add header cells with bold font
        //        var headerCells = new[] { "No.", "Product Name", "Description", "Quantity", "Price", "Gender" };
        //        foreach (var header in headerCells)
        //        {
        //            var cell = new Cell().Add(new Paragraph(header).SetBold()).SetBackgroundColor(ColorConstants.LIGHT_GRAY);
        //            table.AddHeaderCell(cell);
        //        }


        //        int serialNumber = 1;
        //        foreach (var product in products)
        //        {
        //            table.AddCell(new Cell().Add(new Paragraph(serialNumber.ToString())));
        //            serialNumber++;
        //            table.AddCell(new Cell().Add(new Paragraph(product.ProductName)));
        //            table.AddCell(new Cell().Add(new Paragraph(product.Description)));
        //            table.AddCell(new Cell().Add(new Paragraph(product.Quantity.ToString())));
        //            table.AddCell(new Cell().Add(new Paragraph(product.Price.ToString("C"))));
        //            table.AddCell(new Cell().Add(new Paragraph(!string.IsNullOrEmpty(product.Gender) ? product.Gender : "N/A")));
        //        }

        //        document.Add(table);
        //        document.Close();
        //        //document.Flush();
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content("Error generating PDF report: " + ex.Message);
        //    }

        //    //stream.Position = 0;
        //    return File(stream.ToArray(), "application/pdf", "ProductReport.pdf");
        //}





    public ActionResult GeneratePDFReport()
    {
        int sellerId = (int)Session["SellerID"];
        string sellerName = _context.Sellers
                            .Where(s => s.SellerID == sellerId)
                            .Select(s => s.FullName)
                            .FirstOrDefault();

        if (string.IsNullOrEmpty(sellerName))
        {
            return Content("Seller not found.");
        }

        var products = _context.Products.Where(p => p.SellerName == sellerName).ToList();

        if (products == null || !products.Any())
        {
            return Content("No products available to generate a report.");
        }

        var stream = new MemoryStream();

        try
        {
            var writer = new PdfWriter(stream);
            var pdfDoc = new PdfDocument(writer);
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new FooterEventHandler());
            var document = new Document(pdfDoc, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(40, 40, 60, 40);

            // Load a good font family (Helvetica as an example)
            PdfFont font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            // Random invoice number generation
            var random = new Random();
            string invoiceNumber = "2024-BB-" + random.Next(100, 1000);

            // Header section with company branding
            var headerTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1 })).UseAllAvailableWidth();
            var companyName = new Paragraph("BUDGET BUYS")
                .SetFont(boldFont)
                .SetFontSize(24)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(ColorConstants.BLACK);
            var headerInfo = new Paragraph("Seller Product Report")
                .SetFont(font)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(ColorConstants.BLACK)
                .SetMarginBottom(15);
            headerTable.AddCell(new Cell().Add(companyName).SetBorder(Border.NO_BORDER).SetBackgroundColor(new DeviceRgb(220, 248, 198)));
            headerTable.AddCell(new Cell().Add(headerInfo).SetBorder(Border.NO_BORDER).SetBackgroundColor(new DeviceRgb(220, 248, 198)));
            document.Add(headerTable);

            // Info section with report details and seller info
            var infoTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth().SetMarginBottom(20);

            // Left cell with "Bill To" details
            var leftInfo = new Paragraph("Seller: \n")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetFontColor(ColorConstants.BLACK);
            leftInfo.Add(new Paragraph(sellerName).SetFont(font).SetFontSize(10).SetFontColor(ColorConstants.BLACK));
            infoTable.AddCell(new Cell().Add(leftInfo).SetBorder(Border.NO_BORDER));

            // Right cell with report meta-info
            var rightInfo = new Paragraph($"Report Number: {invoiceNumber}\nReport Date: {DateTime.Now:MMMM dd, yyyy}")
                .SetFont(font)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontColor(ColorConstants.BLACK);
            infoTable.AddCell(new Cell().Add(rightInfo).SetBorder(Border.NO_BORDER));

            document.Add(infoTable);

            document.Add(new LineSeparator(new SolidLine()).SetMarginBottom(20));

            // Table headers with styling
            var productTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1, 4, 2, 2, 2 })).UseAllAvailableWidth();
            var headers = new[] { "No.", "Product Name", "Condition", "Quantity", "Price" };

            foreach (var header in headers)
            {
                productTable.AddHeaderCell(new Cell().Add(new Paragraph(header)
                    .SetFont(boldFont)
                    .SetFontColor(ColorConstants.BLACK))
                    .SetBackgroundColor(new DeviceRgb(220, 248, 198))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetPadding(8));
            }

            // Data rows with alternating colors
            int serialNumber = 1;
            bool isAlternate = false;

            foreach (var product in products)
            {
                var rowColor = isAlternate ? ColorConstants.WHITE : new DeviceRgb(240, 240, 240);
                isAlternate = !isAlternate;

                productTable.AddCell(new Cell().Add(new Paragraph(serialNumber.ToString())).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                productTable.AddCell(new Cell().Add(new Paragraph(product.ProductName)).SetFont(font).SetBackgroundColor(rowColor));
                productTable.AddCell(new Cell().Add(new Paragraph(product.ProductCondition)).SetFont(font).SetBackgroundColor(rowColor));
                productTable.AddCell(new Cell().Add(new Paragraph(product.Quantity.ToString())).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                productTable.AddCell(new Cell().Add(new Paragraph("₹ " + product.Price.ToString("0.00"))).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));

                serialNumber++;
            }

            document.Add(productTable);

            // Total summary section at the bottom
            document.Add(new LineSeparator(new SolidLine()).SetMarginTop(20).SetMarginBottom(10));
            var totalSection = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 6, 2 })).UseAllAvailableWidth();

            totalSection.AddCell(new Cell().Add(new Paragraph("Total Amount")).SetFont(boldFont).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));
                totalSection.AddCell(new Cell().Add(new Paragraph("₹ " + products.Sum(p => p.Price).ToString("0.00"))).SetFont(font).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));

                document.Add(totalSection);
            document.Close();
        }
        catch (Exception ex)
        {
            return Content("Error generating PDF report: " + ex.Message);
        }

        return File(stream.ToArray(), "application/pdf", "ProductReport.pdf");
    }



    // Event handler for footer
    private class FooterEventHandler : IEventHandler
        {
            public void HandleEvent(Event @event)
            {
                var pdfDocEvent = (PdfDocumentEvent)@event;
                var pdfDoc = pdfDocEvent.GetDocument();
                var page = pdfDocEvent.GetPage();

                // Get page size
                var pageSize = page.GetPageSize();

                // Create canvas for footer section
                var pdfCanvas = new PdfCanvas(page);
                var canvas = new Canvas(pdfCanvas, new Rectangle(0, 0, pageSize.GetWidth(), 50));

                // Optional: Add a separator line above the footer
                pdfCanvas.MoveTo(pageSize.GetLeft(), pageSize.GetBottom() + 50);
                pdfCanvas.LineTo(pageSize.GetRight(), pageSize.GetBottom() + 50);
                pdfCanvas.Stroke();

                // Set up footer styling
                canvas.SetFontSize(10);
                canvas.SetFontColor(ColorConstants.GRAY);

                // Footer content: centered page number
                int pageNumber = pdfDoc.GetPageNumber(page);
                canvas.ShowTextAligned(new Paragraph("Page " + pageNumber)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.DARK_GRAY),
                    pageSize.GetWidth() / 2, 20, TextAlignment.CENTER);

                // Optional: Add additional footer details (e.g., document title or date)
                canvas.ShowTextAligned(new Paragraph("Generated by Budget Buys Report System")
                    .SetFontSize(8)
                    .SetFontColor(ColorConstants.LIGHT_GRAY),
                    pageSize.GetWidth() / 2, 10, TextAlignment.CENTER);

                canvas.Close();
            }
        }






        public ActionResult SoldProducts()
        {
            // Get the role from the session
            string role = Session["UserType"]?.ToString();
            int? sellerId = null;
            string sellerName = null;

            // If the user is a seller, get their seller ID from the session
            if (role == "Seller" && Session["SellerID"] != null)
            {
                sellerId = (int)Session["SellerID"];
                sellerName = _context.Sellers
                                .Where(s => s.SellerID == sellerId)
                                .Select(s => s.FullName)
                                .FirstOrDefault();
                if (string.IsNullOrEmpty(sellerName))
                {
                    return Content("Seller not found.");
                }
            }

            // Query to get the sold products
            var soldProductsQuery = from oi in _context.OrderItems
                                    join p in _context.Products on oi.ProductId equals p.ProductID
                                    join o in _context.Orders on oi.OrderId equals o.OrderId
                                    join c in _context.Customers on o.CustomerID equals c.CustomerID
                                    where o.Status != "Canceled" // Exclude canceled orders
                                    select new
                                    {
                                        ProductID = p.ProductID,
                                        ProductName = p.ProductName,
                                        Quantity = oi.Quantity,
                                        UnitPrice = oi.UnitPrice,
                                        TotalPrice = oi.TotalPrice,
                                        OrderDate = o.OrderDate,
                                        CustomerName = c.FullName,
                                        CustomerEmail = c.Email,
                                        Status = o.Status,
                                        SellerName = p.SellerName // Include SellerID to filter by seller
                                    };

            // If the user is a seller, filter by their SellerID
            if (role == "Seller" && sellerId.HasValue)
            {
                soldProductsQuery = soldProductsQuery.Where(x => x.SellerName == sellerName);
            }

            // Execute the query and convert to a dynamic object
            var soldProducts = soldProductsQuery
                                .ToList()
                                .Select(x =>
                                {
                                    dynamic expando = new ExpandoObject();
                                    expando.ProductID = x.ProductID;
                                    expando.ProductName = x.ProductName;
                                    expando.Quantity = x.Quantity;
                                    expando.UnitPrice = x.UnitPrice;
                                    expando.TotalPrice = x.TotalPrice;
                                    expando.OrderDate = x.OrderDate;
                                    expando.CustomerName = x.CustomerName;
                                    expando.CustomerEmail = x.CustomerEmail;
                                    expando.Status = x.Status;
                                    return expando;
                                }).ToList();

            // Return the view with the sold products
            return View(soldProducts);
        }

        public ActionResult OrderRevokedList()
        {
            // Get the role from the session
            string role = Session["UserType"]?.ToString();
            int? sellerId = null;
            string sellerName = null;

            // If the user is a seller, get their seller ID from the session
            if (role == "Seller" && Session["SellerID"] != null)
            {
                sellerId = (int)Session["SellerID"];
                sellerName = _context.Sellers
                                  .Where(s => s.SellerID == sellerId)
                                  .Select(s => s.FullName)
                                  .FirstOrDefault();
                if (string.IsNullOrEmpty(sellerName))
                {
                    return Content("Seller not found.");
                }
            }

            // Query to get the canceled orders
            var canceledOrdersQuery = from oi in _context.OrderItems
                                      join p in _context.Products on oi.ProductId equals p.ProductID
                                      join o in _context.Orders on oi.OrderId equals o.OrderId
                                      join c in _context.Customers on o.CustomerID equals c.CustomerID
                                      where o.Status == "Canceled" // Only include canceled orders
                                      select new
                                      {
                                          ProductID = p.ProductID,
                                          ProductName = p.ProductName,
                                          Quantity = oi.Quantity,
                                          UnitPrice = oi.UnitPrice,
                                          TotalPrice = oi.TotalPrice,
                                          OrderDate = o.OrderDate,
                                          CustomerName = c.FullName,
                                          CustomerEmail = c.Email,
                                          Status = o.Status,
                                          SellerName = p.SellerName // Include SellerName to filter by seller
                                      };

            // If the user is a seller, filter by their SellerName
            if (role == "Seller" && sellerId.HasValue)
            {
                canceledOrdersQuery = canceledOrdersQuery.Where(x => x.SellerName == sellerName);
            }

            // Execute the query and convert to a dynamic object
            var canceledOrders = canceledOrdersQuery
                                  .ToList()
                                  .Select(x =>
                                  {
                                      dynamic expando = new ExpandoObject();
                                      expando.ProductID = x.ProductID;
                                      expando.ProductName = x.ProductName;
                                      expando.Quantity = x.Quantity;
                                      expando.UnitPrice = x.UnitPrice;
                                      expando.TotalPrice = x.TotalPrice;
                                      expando.OrderDate = x.OrderDate;
                                      expando.CustomerName = x.CustomerName;
                                      expando.CustomerEmail = x.CustomerEmail;
                                      expando.Status = x.Status;
                                      return expando;
                                  }).ToList();

            // Return the view with the canceled orders
            return View(canceledOrders);
        }





        public ActionResult DownloadSoldProductsReport()
        {
            string role = Session["UserType"]?.ToString();
            int? sellerId = null;
            string sellerName = null;

            if (role == "Seller" && Session["SellerID"] != null)
            {
                sellerId = (int)Session["SellerID"];
                sellerName = _context.Sellers
                                 .Where(s => s.SellerID == sellerId)
                                 .Select(s => s.FullName)
                                 .FirstOrDefault();
                if (string.IsNullOrEmpty(sellerName))
                {
                    return Content("Seller not found.");
                }
            }
            var soldProducts = (from oi in _context.OrderItems
                                join p in _context.Products on oi.ProductId equals p.ProductID
                                join o in _context.Orders on oi.OrderId equals o.OrderId
                                join c in _context.Customers on o.CustomerID equals c.CustomerID
                                where o.Status != "Canceled"  // Exclude canceled orders
                                && (role == "Admin" || p.SellerName == sellerName)  // Show all for Admin, only seller's products for Seller
                                select new
                                {
                                    ProductID = p.ProductID,
                                    ProductName = p.ProductName,
                                    Quantity = oi.Quantity,
                                    UnitPrice = oi.UnitPrice,
                                    TotalPrice = oi.TotalPrice,
                                    OrderDate = o.OrderDate,
                                    CustomerName = c.FullName,
                                    CustomerEmail = c.Email
                                })
                                .ToList();


            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdfDoc = new PdfDocument(writer);
                pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new FooterEventHandler());
                var document = new Document(pdfDoc, iText.Kernel.Geom.PageSize.A4);
                document.SetMargins(40, 40, 60, 40);

                PdfFont font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
                PdfFont boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                var random = new Random();
                string reportNumber = "2024-BB-" + random.Next(100, 1000);

                // Header section with consistent design
                var headerTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1 })).UseAllAvailableWidth();
                var companyName = new Paragraph("BUDGET BUYS")
                    .SetFont(boldFont)
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.BLACK);
                var headerInfo = new Paragraph("Sold Product Report")
                    .SetFont(font)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.BLACK)
                    .SetMarginBottom(15);

                headerTable.AddCell(new Cell().Add(companyName).SetBorder(Border.NO_BORDER).SetBackgroundColor(new DeviceRgb(144, 224, 239)));
                headerTable.AddCell(new Cell().Add(headerInfo).SetBorder(Border.NO_BORDER).SetBackgroundColor(new DeviceRgb(144, 224, 239)));
                document.Add(headerTable);

                // Info section with report details and seller info
                var infoTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth().SetMarginBottom(20);

                // Add seller info only if the role is "Seller"
                if (role == "Seller")
                {
                    var leftInfo = new Paragraph("Seller: \n")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetFontColor(ColorConstants.BLACK);
                    leftInfo.Add(new Paragraph(sellerName).SetFont(font).SetFontSize(10).SetFontColor(ColorConstants.BLACK));
                    infoTable.AddCell(new Cell().Add(leftInfo).SetBorder(Border.NO_BORDER));
                }
                else
                {
                    // Add a placeholder when the role is "Admin"
                    var leftInfo = new Paragraph("Admin ")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetFontColor(ColorConstants.BLACK);
                    leftInfo.Add(new Paragraph().SetFont(font).SetFontSize(10).SetFontColor(ColorConstants.BLACK));
                    infoTable.AddCell(new Cell().Add(leftInfo).SetBorder(Border.NO_BORDER));
                }

                var rightInfo = new Paragraph($"Report Number: {reportNumber}\nReport Date: {DateTime.Now:MMMM dd, yyyy}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontColor(ColorConstants.BLACK);
                infoTable.AddCell(new Cell().Add(rightInfo).SetBorder(Border.NO_BORDER));

                document.Add(infoTable);
                document.Add(new LineSeparator(new SolidLine()).SetMarginBottom(20));

                // Table headers
                var productTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1, 3, 1.5f, 2, 2, 2.5f, 1.5f, 3 })).UseAllAvailableWidth();
                string[] headers = { "No.", "Product Name", "Quantity", "Unit Price", "Total Price", "Order Date", "Customer Name", "Customer Email" };

                foreach (var header in headers)
                {
                    productTable.AddHeaderCell(new Cell().Add(new Paragraph(header)
                        .SetFont(boldFont)
                        .SetFontColor(ColorConstants.BLACK))
                        .SetBackgroundColor(new DeviceRgb(144, 224, 239))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(8));
                }

                // Data rows with alternating colors
                int index = 1;
                bool isAlternate = false;

                foreach (var item in soldProducts)
                {
                    var rowColor = isAlternate ? ColorConstants.WHITE : new DeviceRgb(230, 247, 255);
                    isAlternate = !isAlternate;

                    productTable.AddCell(new Cell().Add(new Paragraph(index.ToString())).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.ProductName)).SetFont(font).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("0.00") ?? "N/A")).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TotalPrice?.ToString("0.00") ?? "N/A")).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.OrderDate?.ToString("dd/MM/yyyy") ?? "N/A")).SetFont(font).SetBackgroundColor(rowColor).SetTextAlignment(TextAlignment.CENTER));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.CustomerName)).SetFont(font).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.CustomerEmail)).SetFont(font).SetBackgroundColor(rowColor));

                    index++;
                }

                document.Add(productTable);
                document.Close();

                return File(stream.ToArray(), "application/pdf", "SoldProductReport.pdf");
            }
        }









    }
}


//table.AddCell(new Cell().Add(new Paragraph(((decimal)item.TotalPrice).ToString("C"))));
//table.AddCell(new Cell().Add(new Paragraph(((DateTime)item.OrderDate).ToString("dd/MM/yyyy"))));