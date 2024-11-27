using iTextSharp.text.pdf;
using Project.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iText.Kernel.Colors;
using iText.Layout.Properties;

namespace Project.Controllers
{
    
    public class AdminController : Controller
    {
        BudgetBuysEntities4 _context = new BudgetBuysEntities4();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var userId = User.Identity.Name; // Get the current user ID (username/email)

            // Check if the user is an admin
            bool isAdmin = _context.Admins.Any(a => a.FullName == userId); // Adjust based on your Admin model

            if (!isAdmin)
            {
                TempData["ErrorMessage"] = "You do not have permission to access this page."; // Set alert message
                filterContext.Result = new RedirectResult("~/Home/Index"); // Redirect to the home page or the previous page
            }

            base.OnActionExecuting(filterContext); // Continue with the action
        }
        // GET: Admin
        public ActionResult Products()
        {
            var products = _context.Products.ToList(); // Fetch all products from the database
            return View(products); // Pass the list of products to the view
        }

        public ActionResult Seller()
        {
            var sellers = _context.Sellers.ToList(); // Fetch all sellers from the database
            ViewBag.BusinessNames = _context.Sellers
                                     .Select(s => s.BusinessName)
                                     .Distinct()
                                     .ToList();
            return View(sellers); // Pass the list of sellers to the view
        }

        public ActionResult Customers()
        {
            var customer = _context.Customers.ToList(); // Fetch all sellers from the database
            return View(customer); // Pass the list of sellers to the view
        }

        public ActionResult FilterSeller(string FullName, string Email, string BusinessName, string PhoneNumber, DateTime? MinCreatedAt, DateTime? MaxCreatedAt)
        {
            // Store the filter values to keep them in the modal when the page reloads
            ViewBag.FullName = FullName;
            ViewBag.Email = Email;
            ViewBag.BusinessName = BusinessName;
            ViewBag.PhoneNumber = PhoneNumber;
            ViewBag.MinCreatedAt = MinCreatedAt;
            ViewBag.MaxCreatedAt = MaxCreatedAt;

            var sellers = _context.Sellers.AsQueryable();

            // Apply filters based on user input
            if (!string.IsNullOrEmpty(FullName))
            {
                sellers = sellers.Where(s => s.FullName.Contains(FullName));
            }
            if (!string.IsNullOrEmpty(Email))
            {
                sellers = sellers.Where(s => s.Email.Contains(Email));
            }
            if (!string.IsNullOrEmpty(BusinessName))
            {
                sellers = sellers.Where(s => s.BusinessName.Contains(BusinessName));
            }
            if (!string.IsNullOrEmpty(PhoneNumber))
            {
                sellers = sellers.Where(s => s.PhoneNumber.Contains(PhoneNumber));
            }
            if (MinCreatedAt.HasValue)
            {
                sellers = sellers.Where(s => s.CreatedAt >= MinCreatedAt.Value);
            }
            if (MaxCreatedAt.HasValue)
            {
                sellers = sellers.Where(s => s.CreatedAt <= MaxCreatedAt.Value);
            }

            return View(sellers.ToList()); // Ensure you are returning to the SellersList view
        }


        public ActionResult FilterCustomer(string fullName, string email, string phoneNumber, DateTime? createdAt)
        {
            var customers = _context.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(fullName))
                customers = customers.Where(c => c.FullName.Contains(fullName));

            if (!string.IsNullOrEmpty(email))
                customers = customers.Where(c => c.Email.Contains(email));

            if (!string.IsNullOrEmpty(phoneNumber))
                customers = customers.Where(c => c.PhoneNumber.Contains(phoneNumber));

           
                // Use DateTime.Date to compare only the date part, ignoring the time
                if (createdAt.HasValue)
                {
                    // Use DbFunctions.TruncateTime to ignore time and only compare the date part
                    customers = customers.Where(c => System.Data.Entity.SqlServer.SqlFunctions.DatePart("day", c.CreatedAt) == createdAt.Value.Day &&
                                                     System.Data.Entity.SqlServer.SqlFunctions.DatePart("month", c.CreatedAt) == createdAt.Value.Month &&
                                                     System.Data.Entity.SqlServer.SqlFunctions.DatePart("year", c.CreatedAt) == createdAt.Value.Year);
                }
        

            // Set ViewBag values to retain filter values in the view
            ViewBag.FullName = fullName;
            ViewBag.Email = email;
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.CreatedAt = createdAt;

            return View(customers.ToList());
        }



        public ActionResult FilterProduct(string ProductName, string ProductCondition, decimal? MinPrice, decimal? MaxPrice, int? MinQuantity, int? MaxQuantity)
        {
            // Store the filter values to keep them in the modal when the page reloads
            ViewBag.ProductName = ProductName;
            ViewBag.ProductCondition = ProductCondition;
            ViewBag.MinPrice = MinPrice;
            ViewBag.MaxPrice = MaxPrice;
            ViewBag.MinQuantity = MinQuantity;
            ViewBag.MaxQuantity = MaxQuantity;

            var products = _context.Products.AsQueryable();

            // Apply filters based on user input
            if (!string.IsNullOrEmpty(ProductName))
            {
                products = products.Where(p => p.ProductName.Contains(ProductName));
            }
            if (!string.IsNullOrEmpty(ProductCondition))
            {
                products = products.Where(p => p.ProductCondition == ProductCondition);
            }
            if (MinPrice.HasValue)
            {
                products = products.Where(p => p.Price >= MinPrice.Value);
            }
            if (MaxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= MaxPrice.Value);
            }
            if (MinQuantity.HasValue)
            {
                products = products.Where(p => p.Quantity >= MinQuantity.Value);
            }
            if (MaxQuantity.HasValue)
            {
                products = products.Where(p => p.Quantity <= MaxQuantity.Value);
            }

            return View(products.ToList());
        }





        // POST: Confirm deletion

        public ActionResult DeleteProduct(int id)
        {
            var product = _context.Products.SingleOrDefault(p => p.ProductID == id);
            if (product == null)
            {
                return HttpNotFound(); // Return 404 if product not found
            }

            _context.Products.Remove(product); // Remove the product from the context
            _context.SaveChanges(); // Commit the changes to the database
            return RedirectToAction("Products"); // Redirect to the list of products
        }


        public ActionResult DeleteCustomer(int id)
        {
            // Find the customer in the database
            var customer = _context.Customers.SingleOrDefault(c => c.CustomerID == id);
            if (customer == null)
            {
                return HttpNotFound(); // Return 404 if customer not found
            }
            _context.Customers.Remove(customer); // Remove the customer from the context
            _context.SaveChanges(); // Commit the changes to the database

            return RedirectToAction("Customers");
        }

        public ActionResult DeleteSeller(int id)
        {
            var seller = _context.Sellers.SingleOrDefault(s => s.SellerID == id);
            if (seller == null)
            {
                return HttpNotFound();
            }

            _context.Sellers.Remove(seller);
            _context.SaveChanges(); // Commit the deletion to the database
            return RedirectToAction("Seller");
        }


        //public actionresult generateproductreport()
        //{
        //    var products = _context.products.tolist();

        //    using (var memorystream = new memorystream())
        //    {
        //        // create a new pdf document with specific page size and margins
        //        var document = new document(pagesize.a4, 40, 40, 40, 40);
        //        pdfwriter.getinstance(document, memorystream);
        //        document.open();

        //        // add a styled title
        //        var titlefont = fontfactory.getfont(fontfactory.helvetica_bold, 16, basecolor.black);
        //        var title = new paragraph("product report", titlefont)
        //        {
        //            alignment = element.align_center,
        //            spacingafter = 20
        //        };
        //        document.add(title);

        //        // create a table with column widths and add headers with styling
        //        pdfptable table = new pdfptable(5) { widthpercentage = 100 };
        //        table.setwidths(new float[] { 10, 30, 15, 10, 35 });

        //        // header cells with background color
        //        var headerfont = fontfactory.getfont(fontfactory.helvetica_bold, 10, basecolor.white);
        //        var headerbackground = new basecolor(40, 40, 40); // dark grey color

        //        string[] headers = { "product id", "product name", "price", "quantity", "description" };
        //        foreach (var header in headers)
        //        {
        //            var cell = new pdfpcell(new phrase(header, headerfont))
        //            {
        //                backgroundcolor = headerbackground,
        //                horizontalalignment = element.align_center,
        //                padding = 5
        //            };
        //            table.addcell(cell);
        //        }

        //        // add product data to the table
        //        var bodyfont = fontfactory.getfont(fontfactory.helvetica, 10, basecolor.black);
        //        foreach (var product in products)
        //        {
        //            table.addcell(new pdfpcell(new phrase(product.productid.tostring(), bodyfont)) { padding = 5 });
        //            table.addcell(new pdfpcell(new phrase(product.productname, bodyfont)) { padding = 5 });
        //            table.addcell(new pdfpcell(new phrase(product.price.tostring("c"), bodyfont)) { padding = 5 });
        //            table.addcell(new pdfpcell(new phrase(product.quantity.tostring(), bodyfont)) { padding = 5 });
        //            table.addcell(new pdfpcell(new phrase(product.description, bodyfont)) { padding = 5 });
        //        }

        //        // add table to document
        //        document.add(table);
        //        document.close();

        //        // return pdf as a downloadable file
        //        byte[] bytes = memorystream.toarray();
        //        return file(bytes, "application/pdf", "productreport.pdf");
        //    }
        //}


        public ActionResult GenerateProductReport()
        {
            var products = _context.Products.ToList();

            using (var memoryStream = new MemoryStream())
            {
                // Create a new PDF document with margins
                var document = new Document(PageSize.A4, 40, 40, 80, 60);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                // Generate random report ID and add current date
                var random = new Random();
                string reportId = $"2024-BB-{random.Next(100, 1000)}";
                string currentDate = DateTime.Now.ToString("MMMM dd, yyyy");

                writer.PageEvent = new SimplePageEventHelper(
                    "BUDGET BUYS", // Company Name
                    "Sold Product Report", // Report Title
                    $"Report Number: {reportId}", // Report ID
                    $"Report Date: {currentDate}", // Current Date
                    "Generated by Budget Buys Report System" // Footer Text
                );

                document.Open();

                // Add vertical space before the title
                var spaceAbove = new Paragraph("\n") { SpacingAfter = 10 };
                document.Add(spaceAbove);

                // Title for the report content
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                var title = new Paragraph("Product Data", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Define a table with headers and column widths
                PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 16, 30, 15, 15, 25, 35 });

                // Add headers with new color scheme (light blue background for headers)
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                var headerBackground = new BaseColor(173, 216, 230); // Light blue color for the headers

                string[] headers = { "Product ID", "Product Name", "Price", "Quantity", "Product Condition", "Description" };
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = headerBackground,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    table.AddCell(cell);
                }

                // Populate table with product data
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
                bool isAlternate = false;
                foreach (var product in products)
                {
                    var rowColor = isAlternate ? BaseColor.WHITE : new BaseColor(230, 247, 255);
                    isAlternate = !isAlternate;

                    table.AddCell(new PdfPCell(new Phrase(product.ProductID.ToString(), bodyFont)) { Padding = 5, BackgroundColor = rowColor });
                    table.AddCell(new PdfPCell(new Phrase(product.ProductName, bodyFont)) { Padding = 5, BackgroundColor = rowColor });
                    table.AddCell(new PdfPCell(new Phrase(product.Price.ToString("0.00"), bodyFont)) { Padding = 5, BackgroundColor = rowColor });
                    table.AddCell(new PdfPCell(new Phrase(product.Quantity.ToString(), bodyFont)) { Padding = 5, BackgroundColor = rowColor });
                    table.AddCell(new PdfPCell(new Phrase(product.ProductCondition, bodyFont)) { Padding = 5, BackgroundColor = rowColor });
                    table.AddCell(new PdfPCell(new Phrase(product.Description, bodyFont)) { Padding = 5, BackgroundColor = rowColor });
                }

                // Add the table to the document
                document.Add(table);

                document.Close();

                // Return PDF as a downloadable file
                byte[] bytes = memoryStream.ToArray();
                return File(bytes, "application/pdf", "ProductReport.pdf");
            }
        }





        // Simple page event helper for header and footer
        public class SimplePageEventHelper : PdfPageEventHelper
        {
            private readonly string _companyName;
            private readonly string _reportTitle;
            private readonly string _dateRange;
            private readonly string _reportId;
            private readonly string _footerNote;

            public SimplePageEventHelper(string companyName, string reportTitle, string dateRange, string reportId, string footerNote)
            {
                _companyName = companyName;
                _reportTitle = reportTitle;
                _dateRange = dateRange;
                _reportId = reportId;
                _footerNote = footerNote;
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContent;

                // Company Name with Background Color, Padding, and Margin
                PdfPTable headerTable = new PdfPTable(1)
                {
                    TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin
                };

                var headerCell = new PdfPCell(new Phrase(_companyName, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE)))
                {
                    BackgroundColor = new BaseColor(0, 102, 204),  // Dark blue background color
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    PaddingTop = 10,
                    PaddingBottom = 15,
                    Border = Rectangle.NO_BORDER
                };

                headerTable.AddCell(headerCell);

                headerTable.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 20, cb);  // Positioning near the top of the page

                float gapAfterCompanyName = 20; // Adjust this value to control the gap size
                float reportInfoYPosition = document.PageSize.Height - 55 - gapAfterCompanyName;

                // Report Info Below Company Name with Additional Gap
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.BLACK);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase(_reportTitle, smallFont), document.LeftMargin, reportInfoYPosition, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_RIGHT, new Phrase(_reportId, smallFont), document.PageSize.Width - document.RightMargin, reportInfoYPosition, 0);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase(_dateRange, smallFont), document.LeftMargin, reportInfoYPosition - 10, 0);
              


                float spaceAbove = 20; // Space above the line
                float spaceBelow = 10; // Space below the line
                float lineYPosition = document.PageSize.Height - 80;

                cb.MoveTo(document.LeftMargin, lineYPosition - spaceAbove);
                cb.LineTo(document.PageSize.Width - document.RightMargin, lineYPosition - spaceAbove);
                cb.Stroke();

                // To add space below the line, update the content position for the next element.
                float nextContentYPosition = lineYPosition - spaceAbove - spaceBelow;


                // Footer Content
                var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
                ColumnText.ShowTextAligned(cb, Element.ALIGN_CENTER, new Phrase(_footerNote + " - Page " + writer.PageNumber, footerFont), document.PageSize.Width / 2, document.BottomMargin - 10, 0);

                // Draw Horizontal Line above footer
                cb.MoveTo(document.LeftMargin, document.BottomMargin + 10);
                cb.LineTo(document.PageSize.Width - document.RightMargin, document.BottomMargin + 10);
                cb.Stroke();
            }
        }





        public ActionResult GenerateSellerReport()
        {
            var sellers = _context.Sellers.ToList();

            using (var memoryStream = new MemoryStream())
            {
                // Create a new PDF document with margins
                var document = new Document(PageSize.A4, 40, 40, 80, 60);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                // Generate random report ID and add current date
                var random = new Random();
                string reportId = $"#2024-BB-{random.Next(100, 1000)}";
                string currentDate = DateTime.Now.ToString("MMMM dd, yyyy");

                // Add a page event for header/footer (similar to the Product Report)
                writer.PageEvent = new SimplePageEventHelper(
                    "BUDGET BUYS",
                    "Seller Report",
                    $"Report ID: {reportId}",
                    $"Date: {currentDate}",
                    "Generated by Budget Buys Report System"
                );

                document.Open();

                // Add space above the title (to mimic the space in the product report)
                var spaceAbove = new Paragraph("\n") { SpacingAfter = 10 };
                document.Add(spaceAbove);

                // Add styled title for the report content below the header line
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                var title = new Paragraph("Seller Data", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Create a table with column widths and add headers
                PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 10, 20, 25, 20, 25, 20 });

                // Add header cells with styling
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                var headerBackground = new BaseColor(0, 102, 153); // Fresh blue color for headers

                string[] headers = { "Seller ID", "Full Name", "Email", "Phone Number", "Business Name", "Created At" };
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = headerBackground,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        Padding = 5
                    };
                    table.AddCell(cell);
                }

                // Add seller data to the table
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
                foreach (var seller in sellers)
                {
                    table.AddCell(new PdfPCell(new Phrase(seller.SellerID.ToString(), bodyFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(seller.FullName, bodyFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(seller.Email, bodyFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(seller.PhoneNumber, bodyFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(seller.BusinessName, bodyFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(seller.CreatedAt.HasValue ? seller.CreatedAt.Value.ToString("MM/dd/yyyy") : "N/A", bodyFont)) { Padding = 5 });
                }

                // Add the table to the document
                document.Add(table);
                document.Close();

                // Return PDF as a downloadable file
                byte[] bytes = memoryStream.ToArray();
                return File(bytes, "application/pdf", "SellerReport.pdf");
            }
        }





       public ActionResult GenerateCustomerReport()
{
    var customers = _context.Customers.ToList();

    using (var memoryStream = new MemoryStream())
    {
        // Create a new PDF document with custom margins
        var document = new Document(PageSize.A4, 40, 40, 80, 60);
        var writer = PdfWriter.GetInstance(document, memoryStream);

        // Generate random report ID and add current date
        var random = new Random();
        string reportId = $"#2024-BB-{random.Next(100, 1000)}";
        string currentDate = DateTime.Now.ToString("MMMM dd, yyyy");

        // Add a page event for header/footer (similar to the other reports)
        writer.PageEvent = new SimplePageEventHelper(
            "BUDGET BUYS",
            "Customer Report",
            $"Report ID: {reportId}",
            $"Date: {currentDate}",
            "Generated by Budget Buys Report System"
        );

        document.Open();

        // Add space above the title (to mimic the space in the product report)
        var spaceAbove = new Paragraph("\n") { SpacingAfter = 10 };
        document.Add(spaceAbove);

        // Add styled title for the report content below the header line
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
        var title = new Paragraph("Customer Data", titleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20
        };
        document.Add(title);

        // Define header font and background color
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
        var headerBackground = new BaseColor(0, 102, 153); // Fresh blue color for headers

        // Create a table with appropriate columns
        PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };

        // Set custom column widths: ID column is smaller than the others
        float[] columnWidths = { 1f, 2f, 4f, 4f, 2.5f, 2f };
        table.SetWidths(columnWidths);

        // Add header cells with styling
        table.AddCell(new PdfPCell(new Phrase("ID", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
        table.AddCell(new PdfPCell(new Phrase("Full Name", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
        table.AddCell(new PdfPCell(new Phrase("Email", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
        table.AddCell(new PdfPCell(new Phrase("Address", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
        table.AddCell(new PdfPCell(new Phrase("Phone Number", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
        table.AddCell(new PdfPCell(new Phrase("Created At", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });

        // Add customer data rows
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
        foreach (var customer in customers)
        {
            table.AddCell(new PdfPCell(new Phrase(customer.CustomerID.ToString(), bodyFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(customer.FullName, bodyFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(customer.Email, bodyFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(customer.Address, bodyFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(customer.PhoneNumber, bodyFont)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(customer.CreatedAt?.ToString("MM/dd/yyyy") ?? "", bodyFont)) { Padding = 5 });
        }

        // Add table to PDF
        document.Add(table);
        document.Close();

        // Return PDF as a downloadable file
        byte[] bytes = memoryStream.ToArray();
        return File(bytes, "application/pdf", "CustomerReport.pdf");
    }
}






    }
}