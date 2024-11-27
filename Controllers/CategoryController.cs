using Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using static Project.Controllers.AdminController;


namespace Project.Controllers
{
    public class CategoryController : Controller
    {
        BudgetBuysEntities4 _context = new BudgetBuysEntities4();

        // GET: Category/AddCategory
        public ActionResult AddCategory()
        {
            return View();
        }





        [HttpPost]
        public ActionResult AddCategoryAndSubcategories(string CategoryName, List<string> SubCategoryNames)
        {
            int SellerID = (int)Session["SellerID"];
            if (ModelState.IsValid)
            {
                // Check if the category already exists for this seller
                var category = _context.Categories.FirstOrDefault(c => c.CategoryName == CategoryName && c.SellerId == SellerID);
                if (category == null)
                {
                    // Create new category if it doesn't exist
                    category = new Category
                    {
                        CategoryName = CategoryName,
                        SellerId = SellerID // Set the SellerID
                    };
                    _context.Categories.Add(category);
                    _context.SaveChanges();
                }

                // Add each subcategory that doesn't already exist
                foreach (var subCategoryName in SubCategoryNames)
                {
                    if (!string.IsNullOrWhiteSpace(subCategoryName) &&
                        !_context.Subcategories.Any(sc => sc.SubCategoryName == subCategoryName && sc.CategoryID == category.CategoryID))
                    {
                        var subcategory = new Subcategory
                        {
                            SubCategoryName = subCategoryName,
                            CategoryID = category.CategoryID // Associate with the correct category
                        };

                        _context.Subcategories.Add(subcategory);
                    }
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Category and Subcategories added successfully!";
                return RedirectToAction("CategoryList", "Category"); // Redirect after success
            }

            return View();
        }


        // POST: Category/DeleteCategory
        [HttpPost]
        public ActionResult DeleteCategory(int id)
        {
            // Find the category by ID
            var category = _context.Categories.Include(c => c.Subcategories) // Include the subcategories
                                               .FirstOrDefault(c => c.CategoryID == id);

            if (category != null)
            {
                // Delete all associated subcategories
                if (category.Subcategories != null)
                {
                    // Remove all subcategories linked to the category
                    _context.Subcategories.RemoveRange(category.Subcategories);
                }

                // Now remove the category itself
                _context.Categories.Remove(category);

                // Save changes to the database
                _context.SaveChanges();
            }

            return RedirectToAction("CategoryList");
        }


        // GET: Category/CategoryList
        public ActionResult CategoryList()
        {
            string sellerName = User.Identity.Name; // Get the current seller's name (username)

            // Check if the user is an admin by comparing with the Admin model
            bool isAdmin = _context.Admins.Any(a => a.FullName == sellerName); // Adjust based on your Admin model

            IEnumerable<Category> categories;

            if (isAdmin)
            {
                // If admin, retrieve all categories with their subcategories
                categories = _context.Categories
                    .Include(c => c.Subcategories) // Include subcategories
                    .ToList();
            }

            else
            {
                int SellerID = (int)Session["SellerID"];
                // If seller, retrieve only categories that have products belonging to this seller
                categories = _context.Categories
                    .Where(p => p.SellerId == SellerID) // Filter by seller's products
                    .Include(c => c.Subcategories) // Include subcategories
                    .ToList();

                // If you're using SellerID instead of SellerName, you can do this:
                // int sellerID = _context.Sellers.Where(s => s.SellerName == sellerName).Select(s => s.SellerID).FirstOrDefault();
                // categories = _context.Categories
                //     .Where(c => c.Products.Any(p => p.SellerID == sellerID)) // Filter by seller's products
                //     .Include(c => c.Subcategories)
                //     .ToList();
            }

            return View(categories);
        }

        [HttpGet]
        public ActionResult EditCategory(int id)
        {
            var category = _context.Categories.Include("Subcategories").FirstOrDefault(c => c.CategoryID == id);
            if (category == null)
            {
                return HttpNotFound();
            }
            return View(category);
        }
        [HttpPost]
        public ActionResult EditCategoryList(Category category, string NewSubcategoryName)
        {
            if (ModelState.IsValid)
            {
                // Fetch the existing category and its subcategories from the database
                var existingCategory = _context.Categories.Include("Subcategories").FirstOrDefault(c => c.CategoryID == category.CategoryID);
                if (existingCategory != null)
                {
                    // Update Category Name
                    existingCategory.CategoryName = category.CategoryName;

                    // Update or remove existing subcategories
                    foreach (var submittedSubcategory in category.Subcategories)
                    {
                        // Find the subcategory in the database
                        var existingSubcategory = existingCategory.Subcategories.FirstOrDefault(s => s.SubCategoryId == submittedSubcategory.SubCategoryId);

                        if (existingSubcategory != null)
                        {
                            // Update subcategory name if it exists
                            existingSubcategory.SubCategoryName = submittedSubcategory.SubCategoryName;
                        }
                    }

                    // Check for new subcategories
                    if (!string.IsNullOrEmpty(NewSubcategoryName))
                    {
                        existingCategory.Subcategories.Add(new Subcategory
                        {
                            SubCategoryName = NewSubcategoryName,
                            CategoryID = existingCategory.CategoryID
                        });
                    }

                    // Save changes to the database
                    _context.SaveChanges();

                    // Redirect to a list or summary page after successful update
                    return RedirectToAction("CategoryList");
                }
            }

            // If something went wrong, return the view with the same model
            return View(category);
        }


        public ActionResult DeleteSubcategory(int id)
        {
            var subcategory = _context.Subcategories.Find(id);
            if (subcategory != null)
            {
                _context.Subcategories.Remove(subcategory);
                _context.SaveChanges();
            }

            return RedirectToAction("EditCategory", new { id = subcategory.CategoryID });
        }

        public ActionResult FilterCategory(string categoryName, string subcategoryName)
        {
            // Start with all categories in the context
            var categories = _context.Categories.AsQueryable();

            // Apply Category Name filter if provided
            if (!string.IsNullOrEmpty(categoryName))
            {
                categories = categories.Where(c => c.CategoryName.Contains(categoryName));
            }

            // Apply Subcategory filter if provided
            if (!string.IsNullOrEmpty(subcategoryName))
            {
                categories = categories.Where(c => c.Subcategories.Any(s => s.SubCategoryName.Contains(subcategoryName)));
            }

            // Set filter values in ViewBag to retain them in the view
            ViewBag.CategoryName = categoryName;
            ViewBag.SubcategoryName = subcategoryName;

            // Return filtered categories to the view
            return View(categories.ToList());
        }




        public ActionResult DownloadCategoryReport()
        {
            List<Category> categories;

            // Check if user is a seller or an admin
            if (Session["SellerID"] != null)
            {
                int sellerId = (int)Session["SellerID"];
                categories = _context.Categories
                    .Include(c => c.Subcategories)
                    .Where(c => c.SellerId == sellerId) // Filter by SellerID
                    .ToList();
            }
            else
            {
                // Admin view: get all categories
                categories = _context.Categories
                    .Include(c => c.Subcategories)
                    .ToList();
            }

            using (var memoryStream = new MemoryStream())
            {
                // Create a new PDF document with custom margins
                var document = new Document(PageSize.A4, 40, 40, 80, 60);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                // Generate random report ID and add current date
                var random = new Random();
                string reportId = $"#2024-BB-{random.Next(100, 1000)}";
                string currentDate = DateTime.Now.ToString("MMMM dd, yyyy");

                // Add a page event for header/footer
                writer.PageEvent = new SimplePageEventHelper(
                    "BUDGET BUYS",
                    "Category and Subcategory Report",
                    $"Report Date: {currentDate}",
                    $"Report ID: {reportId}",
                    "Generated by Budget Buys Report System"
                );

                document.Open();

                // Add space above the title
                var spaceAbove = new Paragraph("\n") { SpacingAfter = 20 };
                document.Add(spaceAbove);

                // Set title font and add title to PDF
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK);
                var title = new Paragraph("Category and Subcategory Data", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Define header font and background color
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                var headerBackground = new BaseColor(0, 102, 153); // Fresh blue color for headers

                // Create a table with appropriate columns
                PdfPTable table = new PdfPTable(3) { WidthPercentage = 100 };

                // Set custom column widths
                float[] columnWidths = { 1f, 2f, 4f };
                table.SetWidths(columnWidths);

                // Add header cells with styling
                table.AddCell(new PdfPCell(new Phrase("Category ID", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase("Category Name", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase("Subcategories", headerFont)) { BackgroundColor = headerBackground, Padding = 5 });

                // Add category and subcategory data rows
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
                foreach (var category in categories)
                {
                    table.AddCell(new PdfPCell(new Phrase(category.CategoryID.ToString(), bodyFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(category.CategoryName, bodyFont)) { Padding = 5 });

                    // Add subcategories (joined as a single comma-separated string)
                    var subcategoryNames = category.Subcategories != null ? string.Join(", ", category.Subcategories.Select(s => s.SubCategoryName)) : "No subcategories";
                    table.AddCell(new PdfPCell(new Phrase(subcategoryNames, bodyFont)) { Padding = 5 });
                }

                // Add table to PDF
                document.Add(table);
                document.Close();

                // Return PDF as a downloadable file
                byte[] bytes = memoryStream.ToArray();
                return File(bytes, "application/pdf", "CategoryReport.pdf");
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


    }
}
