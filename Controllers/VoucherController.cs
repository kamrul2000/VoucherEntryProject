using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Configuration;
using System.Diagnostics; // For logging purposes

namespace VoucherProject.Controllers
{
    public class VoucherReportController : Controller
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["VoucherProjectConnection"].ConnectionString;

        public ActionResult GenerateVoucherReport()
        {
            try
            {
                ReportDocument reportDocument = new ReportDocument();
                string reportPath = Server.MapPath("~/CrystalReport/VoucherListReport.rpt");

                if (!System.IO.File.Exists(reportPath))
                {
                    throw new FileNotFoundException("Report file not found", reportPath);
                }

                reportDocument.Load(reportPath);

                // Fetch data using ADO.NET
                DataTable voucherData = GetVoucherData();
                reportDocument.SetDataSource(voucherData);

                // Export to PDF
                Stream stream = reportDocument.ExportToStream(ExportFormatType.PortableDocFormat);
                return File(stream, "application/pdf", "VoucherListReport.pdf");
            }
            catch (Exception ex)
            {
                // Log the error to a file or system event log
                System.IO.File.WriteAllText(Server.MapPath("~/App_Data/ErrorLog.txt"), ex.ToString());

                // Return a friendly error message
                return new HttpStatusCodeResult(500, "An error occurred while generating the report. Please try again later.");
            }
        }

        private DataTable GetVoucherData()
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, ParticularDate, Particular, Remarks, Amount, SubmitDate, Status FROM Voucher";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }
    }
}
