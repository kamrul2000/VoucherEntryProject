using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoucherProject.Models;
using VoucherProject.Models.ViewModels;

namespace VoucherProject.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }
        private readonly string _connectionString;
        public AdminController()
        {

            _connectionString = ConfigurationManager.ConnectionStrings["VoucherProjectConnection"].ConnectionString;
        }




        public ActionResult VoucherList(DateTime? searchDate, int page = 1, int pageSize = 10)
        {
            string userEmail = User.Identity.Name;
            int userId = GetUserIdByEmail(userEmail);

            if (userId <= 0)
            {
                ViewBag.ErrorMessage = "Invalid user. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            List<AdminVoucherList> adminVoucherLists = new List<AdminVoucherList>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"
                SELECT 
                    u.Id AS UserId, u.Name AS UserName, 
                    v.Id AS VoucherId, v.Particular, v.Remarks, 
                    v.Amount, v.SubmitDate, v.ParticularDate, v.Status 
                FROM Users u
                INNER JOIN Voucher v ON u.Id = v.UserId";

                    if (!UserIsAdmin(userId))
                    {
                        query += " WHERE v.UserId = @UserId";
                    }

                    if (searchDate.HasValue)
                    {
                        query += " AND CAST(v.SubmitDate AS DATE) = @SearchDate";
                    }

                    query += " ORDER BY v.SubmitDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (!UserIsAdmin(userId))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                        }
                        if (searchDate.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@SearchDate", searchDate.Value.Date);
                        }

                        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = Convert.ToInt32(reader["UserId"]),
                                    Name = reader["UserName"].ToString()
                                };

                                var voucher = new VoucherViewModel
                                {
                                    Id = Convert.ToInt32(reader["VoucherId"]),
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    SubmitDate = Convert.ToDateTime(reader["SubmitDate"]),
                                    ParticularDate = Convert.ToDateTime(reader["ParticularDate"]),
                                    Particular = reader["Particular"].ToString(),
                                    Remarks = reader["Remarks"].ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    Status = reader["Status"].ToString()
                                };

                                adminVoucherLists.Add(new AdminVoucherList
                                {
                                    User = user,
                                    VoucherViewModel = voucher
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching vouchers: {ex.Message}");
            }

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = GetTotalVoucherCount(searchDate, userId);

            return View(adminVoucherLists);
        }

        // Get total count of vouchers for pagination
        private int GetTotalVoucherCount(DateTime? searchDate, int userId)
        {
            int count = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT COUNT(*) FROM Voucher WHERE 1=1";

                    if (!UserIsAdmin(userId))
                    {
                        query += " AND UserId = @UserId";
                    }

                    if (searchDate.HasValue)
                    {
                        query += " AND CAST(SubmitDate AS DATE) = @SearchDate";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (!UserIsAdmin(userId))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                        }
                        if (searchDate.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@SearchDate", searchDate.Value.Date);
                        }

                        conn.Open();
                        count = (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching total count: {ex.Message}");
            }
            return count;
        }


        [HttpPost]
        public ActionResult UpdateVoucherStatus(int voucherId, string status)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "UPDATE Voucher SET Status = @Status WHERE Id = @VoucherId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@VoucherId", voucherId);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Voucher status updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating voucher status: " + ex.Message;
            }

            return RedirectToAction("VoucherList");
        }


        [HttpPost]
        public ActionResult DeleteVoucher(int voucherId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM Voucher WHERE Id = @VoucherId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@VoucherId", voucherId);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Voucher deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting voucher: " + ex.Message;
            }

            return RedirectToAction("VoucherList");
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
        private bool UserIsAdmin(int userId)
        {
            bool isAdmin = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT IsAdmin FROM Users WHERE Id = @UserId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        conn.Open();
                        isAdmin = (bool)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking admin status: {ex.Message}");
            }
            return isAdmin;
        }



    }
}