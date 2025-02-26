using System;
using System.Data.SqlClient;
using System.Web.Mvc;
using System.Configuration;
using VoucherProject.Models;
using System.Web.Security;
using System.Collections.Generic;

namespace VoucherProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;
        public AuthController()
        {

            _connectionString = ConfigurationManager.ConnectionStrings["VoucherProjectConnection"].ConnectionString;
        }
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Register(string name, string email, string password)
        {
            if (RegisterUser(name, email, password))
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }
        private bool RegisterUser(string name, string email, string password)
        {
            try
            {
                using (SqlConnection conn=new SqlConnection(_connectionString))    
                {
                    string query = "INSERT INTO Users (Name, Email, Password) VALUES (@Name, @Email, @Password)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", password); 

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user registration: {ex.Message}");
                return false;
            }
        }
        // GET: Login Page
        [HttpGet]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                FormsAuthentication.SignOut();

                return RedirectToAction("Login", "Auth");
            }

            return View();
        }



        [HttpPost]
        public ActionResult Login(Models.User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                TempData["Error"] = "Email and Password are required.";
                return RedirectToAction("Login");
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND Password = @Password";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.Parameters.AddWithValue("@Password", user.Password);

                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        int userCount = (result != null) ? Convert.ToInt32(result) : 0;

                        if (userCount > 0)
                        {
                            FormsAuthentication.SetAuthCookie(user.Email, false);

                            if (CheckIsAdmin(user.Email))
                            {
                                return RedirectToAction("VoucherList", "Admin");
                            }

                            return RedirectToAction("VoucherList", "Auth");
                        }
                        else
                        {
                            TempData["Error"] = "Invalid email or password.";
                            return RedirectToAction("Login");  // Redirect back to login with error
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred during login. Please try again later.";
                Console.WriteLine($"Error during login: {ex.Message}");
                return RedirectToAction("Login");
            }
        }



        private bool CheckIsAdmin(string email)
        {
            int userId = 0;
            bool isAdmin = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT IsAdmin FROM Users WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        conn.Open();
                        isAdmin = (bool)cmd.ExecuteScalar();
                        if(isAdmin == true)
                        {
                            isAdmin = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching UserId: {ex.Message}");
            }

            return isAdmin;
        }

        [Authorize]
        public ActionResult Logout()
        {
           
            FormsAuthentication.SignOut();

          
            Session.Clear();
            Session.Abandon();

           
            return RedirectToAction("Login", "Auth");
        }

        // GET: Forget Password Page
        public ActionResult ForgotPassword()
        {
            return View();
        }
        // POST: Forgot Password
        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Please enter your email address.";
                return View();
            }

            // Check if email exists in the database
            bool userExists = CheckIfUserExists(email);

            if (userExists)
            {
                // In a real-world scenario, generate a reset token and send an email with the reset link.
                ViewBag.Message = "If your email is registered, you will receive a password reset link shortly.";
            }
            else
            {
                ViewBag.ErrorMessage = "Email address not found.";
            }

            return View();
        }

        private bool CheckIfUserExists(string email)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        conn.Open();
                        int userCount = (int)cmd.ExecuteScalar();
                        return userCount > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking user existence: {ex.Message}");
                return false;
            }
        }




        [HttpGet]
        [Authorize]
        public ActionResult CreateVoucher()
        {
            string userEmail = User.Identity.Name;  
            int userId = GetUserIdByEmail(userEmail);  

            
            return View("CreateVoucher", new VoucherViewModel { UserId = userId });
        }



        [HttpPost]
        [Authorize]
        public ActionResult CreateVoucher(VoucherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userEmail = User.Identity.Name;
            int userId = GetUserIdByEmail(userEmail);

            if (userId <= 0)
            {
                ViewBag.ErrorMessage = "Invalid user. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            string query = @"
    INSERT INTO Voucher (UserId, ParticularDate, Particular, Remarks, Amount, SubmitDate, Status)
    VALUES (@UserId, @ParticularDate, @Particular, @Remarks, @Amount, @SubmitDate, @Status)";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ParticularDate", model.ParticularDate);
                    command.Parameters.AddWithValue("@Particular", model.Particular);
                    command.Parameters.AddWithValue("@Remarks", model.Remarks);
                    command.Parameters.AddWithValue("@Amount", model.Amount);
                    command.Parameters.AddWithValue("@SubmitDate", model.SubmitDate);
                    command.Parameters.AddWithValue("@Status", "Pending");

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Voucher added successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to add the voucher. Please try again.";
                    }
                }

                return RedirectToAction("VoucherList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving the voucher: " + ex.Message;
                return View(model);
            }
        }


        private int GetUserIdByEmail(string email)
        {
            int userId = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT Id FROM Users WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        conn.Open();
                        userId = (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching UserId: {ex.Message}");
            }

            return userId;
        }

        [HttpGet]
        public ActionResult VoucherList(DateTime? searchDate)
        {
            string userEmail = User.Identity.Name;
            int userId = GetUserIdByEmail(userEmail);

            if (userId <= 0)
            {
                ViewBag.ErrorMessage = "Invalid user. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            List<VoucherViewModel> vouchers = new List<VoucherViewModel>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM Voucher WHERE UserId = @UserId";
                    if (searchDate.HasValue)
                    {
                        query += " AND CAST(SubmitDate AS DATE) = @SearchDate";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        if (searchDate.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@SearchDate", searchDate.Value.Date);
                        }
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                vouchers.Add(new VoucherViewModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    SubmitDate = Convert.ToDateTime(reader["SubmitDate"]),
                                    ParticularDate = Convert.ToDateTime(reader["ParticularDate"]),
                                    Particular = reader["Particular"].ToString(),
                                    Remarks = reader["Remarks"].ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    Status = reader["Status"].ToString(),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching vouchers: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading vouchers.";
            }

            return View(vouchers);
        }


        [HttpGet]
        [Authorize]
        public ActionResult EditVoucher(int id)
        {
            VoucherViewModel voucher = null;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM Voucher WHERE Id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                voucher = new VoucherViewModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    SubmitDate = Convert.ToDateTime(reader["SubmitDate"]),
                                    ParticularDate = Convert.ToDateTime(reader["ParticularDate"]),
                                    Particular = reader["Particular"].ToString(),
                                    Remarks = reader["Remarks"].ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching voucher: {ex.Message}");
            }

            if (voucher == null)
            {
                return HttpNotFound();
            }

            return View(voucher);
        }
        [HttpPost]
        [Authorize]
        public ActionResult EditVoucher(VoucherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"
            UPDATE Voucher
            SET ParticularDate = @ParticularDate,
                Particular = @Particular,
                Remarks = @Remarks,
                Amount = @Amount
            WHERE Id = @Id AND UserId = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ParticularDate", model.ParticularDate);
                        cmd.Parameters.AddWithValue("@Particular", model.Particular);
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks);
                        cmd.Parameters.AddWithValue("@Amount", model.Amount);
                        cmd.Parameters.AddWithValue("@Id", model.Id);
                        cmd.Parameters.AddWithValue("@UserId", model.UserId);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        Console.WriteLine($"Rows affected: {rowsAffected}");

                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Voucher updated successfully.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to update the voucher. It may not exist or you don't have permission.";
                        }
                    }
                }
                return RedirectToAction("VoucherList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating voucher: {ex.Message}";
                return RedirectToAction("VoucherList");
            }
        }


        [HttpPost]
        [Authorize]
        public ActionResult DeleteVoucher(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM Voucher WHERE Id = @Id AND UserId = @UserId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@UserId", GetUserIdByEmail(User.Identity.Name));

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Voucher deleted successfully.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to delete the voucher. Please try again.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting voucher: {ex.Message}";
            }

            return RedirectToAction("VoucherList");
        }




    }
}
