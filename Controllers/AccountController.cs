using Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace Project.Controllers
{
    public class AccountController : Controller
    {
        BudgetBuysEntities4 _context = new BudgetBuysEntities4();
  
        // GET: Account
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }


        // POST: Register for Customer or Seller
        public ActionResult RegisterData(string userType,string FullName, string Email, string Password,
                                     string Address, string PhoneNumber, string BusinessName)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.ErrorMessage = "All Fields Are Required.";
                return View("Register"); // Stay on the Register page
            }

            var hashedPassword = HashPassword(Password);

            if (userType == "Seller")
            {
                // Save as Seller
                var seller = new Seller
                {
                    FullName = FullName,
                    Email = Email,
                    PasswordHash = hashedPassword,
                    PhoneNumber = PhoneNumber,
                    BusinessName = BusinessName,
                    Address = Address,
                    CreatedAt = DateTime.Now
                };

                _context.Sellers.Add(seller);
            }
            else
            {
                // Save as Customer
                var customer = new Customer
                {
                    FullName = FullName,
                    Email = Email,
                    PasswordHash = hashedPassword,
                    Address = Address,
                    PhoneNumber = PhoneNumber,
                    CreatedAt = DateTime.Now
                };

                _context.Customers.Add(customer);
            }

            _context.SaveChanges();
            return RedirectToAction("Login");
        }

        // Hash the password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        [HttpPost]
        public ActionResult Login(string email, string password, string userType)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Email and password are required.";
                return View();
            }

            // Hash the entered password
            var hashedPassword = HashPassword(password);

            // Check credentials based on user type
            if (userType == "Seller")
            {
                var seller = _context.Sellers.FirstOrDefault(s => s.Email == email);
                if (seller != null && seller.PasswordHash == hashedPassword)
                {
                    // Successful login for Seller
                    Session["UserName"] = seller.FullName;
                    Session["UserType"] = "Seller";
                    Session["SellerID"] = seller.SellerID;
                    FormsAuthentication.SetAuthCookie(seller.FullName, false);
                    return RedirectToAction("index", "Home"); // Redirect to seller dashboard
                }
            }
            else if (userType == "Customer")
            {
                var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
                if (customer != null && customer.PasswordHash == hashedPassword)
                {
                    // Successful login for Customer
                    Session["UserName"] = customer.FullName;
                    Session["UserType"] = "Customer";
                    Session["UserId"] = customer.CustomerID;
                    FormsAuthentication.SetAuthCookie(customer.FullName, false); 
                    return RedirectToAction("index", "Home"); // Redirect to customer dashboard
                }
            }
            else if (userType == "Admin")
            {
                var admin = _context.Admins.FirstOrDefault(a => a.Email == email);
                if (admin != null && admin.PasswordHash == password)
                {
                    // Successful login for Customer
                    Session["UserName"] = admin.FullName;
                    Session["UserType"] = "Admin";
                    Session["UserId"] = admin.AdminID;
                    FormsAuthentication.SetAuthCookie(admin.FullName, false);
                    return RedirectToAction("index", "Home"); // Redirect to customer dashboard
                }
            }


            // If we reach here, login failed
            ViewBag.ErrorMessage = "Invalid login credentials.";
            return View();
        }


        public ActionResult Logout()
        {
            // Clear the session
            Session.Clear();

            // Abandon the session (optional)
            Session.Abandon();

            // Sign out the user
            FormsAuthentication.SignOut();

            // Redirect to the home page or login page
            return RedirectToAction("Index", "Home"); // Change to your desired redirect
        }



        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: ForgotPassword
        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            // Check if the user exists
            var user = _context.Customers.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "No user found with this email address.";
                return View();
            }

            // Show the reset password form
            ViewBag.Email = email; // Pass the email to the view
            return View("ResetPassword", new {ViewBag.Email});
        }

        // GET: ResetPassword
        public ActionResult ResetPassword(string email)
        {
            ViewBag.Email = email; // Store email in ViewBag for the form
            return View();
        }

        // POST: ResetPassword
        [HttpPost]
        public ActionResult ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                ViewBag.Email = email;
                return View();
            }

            var user = _context.Customers.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "No user found with this email address.";
                return View();
            }

            // Update user's password
            user.PasswordHash = HashPassword(newPassword); // Implement HashPassword method
            _context.SaveChanges();

            return RedirectToAction("Login", "Account");
        }

        // GET: UpdateProfile
        [HttpGet]
        [Authorize] // Ensure the user is authenticated
        public ActionResult UpdateProfile()
        {
            var userId = GetUserId(); // Get the user ID
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == userId);

            if (customer == null)
            {
                return HttpNotFound(); // Return a 404 if the user is not found
            }

            ViewBag.IsSeller = User.IsInRole("Seller");
            
            return View(customer); // Pass the customer data to the view
        }
// Ensure the user is authenticated
        public ActionResult UpdateProfile(Customer model, string businessName)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsSeller = User.IsInRole("Seller");
                return View(model); // Return the view with the current model if validation fails
            }

            var customer = _context.Customers.Find(model.CustomerID); // Retrieve the existing customer record
            if (customer == null)
            {
                return HttpNotFound(); // Return a 404 if the user is not found
            }

            // Update customer properties
            customer.FullName = model.FullName;
            customer.Email = model.Email;
            customer.PhoneNumber = model.PhoneNumber;
            customer.Address = model.Address;

            // Update business name if the user is a seller
            if (User.IsInRole("Seller"))
            {
                var seller = _context.Sellers.FirstOrDefault(s => s.Email == customer.Email);
                if (seller != null)
                {
                    seller.BusinessName = businessName; // Update the business name
                }
            }

            // Save changes to the database
            _context.SaveChanges();

            Response.Write("Profile updated successfully!");
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("UpdateProfile"); // Redirect to the update profile page
        }


        [HttpGet]
        [Authorize] // Restrict access to sellers
        public ActionResult UpdateSellerProfile()
        {
            var userId = GetUserId(); // Get the user ID
            var seller = _context.Sellers.FirstOrDefault(s => s.SellerID == userId);

            if (seller == null)
            {
                return HttpNotFound(); // Return a 404 if the seller is not found
            }

            return View(seller); // Pass the seller data to the view
        }

        [HttpPost]
        public ActionResult UpdateSellerProfile(Seller model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Return the view with the current model if validation fails
            }

            var seller = _context.Sellers.Find(model.SellerID); // Retrieve the existing seller record
            if (seller == null)
            {
                return HttpNotFound(); // Return a 404 if the seller is not found
            }

            // Update seller properties
            seller.FullName = model.FullName;
            seller.Email = model.Email;
            seller.PhoneNumber = model.PhoneNumber;
            seller.Address = model.Address;
            seller.BusinessName = model.BusinessName; // Update business name

            // Save changes to the database
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("UpdateSellerProfile"); // Redirect to the update profile page
        }


        private int GetUserId()
        {
            // Check if the user is a seller and retrieve the SellerID
            if (Session["SellerID"] != null)
            {
                return Convert.ToInt32(Session["SellerID"]);
            }

            // Otherwise, retrieve the UserId for customers/admins
            if (Session["UserId"] != null)
            {
                return Convert.ToInt32(Session["UserId"]);
            }

            // Return an invalid ID or handle the case where neither is set
            throw new Exception("User ID not found in session.");
        }
    }

}