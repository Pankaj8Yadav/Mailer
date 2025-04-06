using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Text;
using Mailer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace Mailer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public List<SendEmail> GetEmails()
        {
            List<SendEmail> AllEmails = new List<SendEmail>();

            // Get the connection string from appsettings.json
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Use ADO.NET to retrieve data
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Define the query to retrieve data from your table
                string query = "SELECT Id, Email FROM tbl_mailer"; // Update table/column names

                // Execute the query
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SendEmail emails = new SendEmail
                            {
                                Id = reader.GetInt32(0),
                                Email = reader.GetString(1),
                            };

                            AllEmails.Add(emails);
                        }
                    }
                }
            }

            return AllEmails;
        }

        [HttpPost]
        public async Task<JsonResult> SendEmail(string email)
        {
            // Prepare email message
            MailMessage message = new MailMessage();
            message.From = new MailAddress(_configuration["EmailSettings:EmailId"]);
            message.To.Add(email);
            message.Subject = "Greetings from Divyang Career";

            string mailBody = $@"
    Dear User,
    <br /><br />
    Hi! We hope you're doing well.
    <br /><br />
    Thank you for using Divyang Career.
    <br />
    Best regards,
    <br />
    Divyang Career Customer Support Team";

            message.Body = mailBody;
            message.IsBodyHtml = true;
            message.BodyEncoding = Encoding.UTF8;

            // Setup SMTP client
            SmtpClient smtp = new SmtpClient(_configuration["EmailSettings:SmtpClient"]);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(_configuration["EmailSettings:EmailId"], _configuration["EmailSettings:Password"]);
            smtp.Port = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            smtp.EnableSsl = true;

            try
            {
                // Send the email
                await smtp.SendMailAsync(message);  // Async version of Send
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during email sending
                Console.WriteLine("Error sending email: " + ex.Message);
                return Json(new { success = false, message = "Failed to send email. Please try again." });
            }

            // Email sent successfully
            return Json(new { success = true, message = "Email has been sent to your address." });
        }
        public List<string> GetEmailsFromDatabase()
        {
            List<string> emailList = new List<string>();

            string connectionString = _configuration.GetConnectionString("DefaultConnection"); // Your connection string here

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Email FROM tbl_mailer"; // Query to get emails

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            emailList.Add(reader.GetString(0)); // Get each email
                        }
                    }
                }
            }

            return emailList;
        }


        [HttpPost]
        public IActionResult SendBulkEmail()
        {
            // Retrieve the list of emails from the database
            List<string> emails = GetEmailsFromDatabase();

            if (emails.Count == 0)
            {
                return Json(new { success = false, message = "No emails found in the database." });
            }

            // Prepare the email content
            string subject = "Hello from ASP.NET Core";
            string body = "This is a bulk email sent from an ASP.NET Core application.";

            // Set up the SMTP client
            var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpClient"])
            {
                Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                Credentials = new NetworkCredential(_configuration["EmailSettings:EmailId"], _configuration["EmailSettings:Password"]),
                EnableSsl = true,
            };

            // Send the email to each address
            foreach (var email in emails)
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:EmailId"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                message.To.Add(email);

                try
                {
                    smtpClient.Send(message);  // Send the email
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error sending email to " + email + ": " + ex.Message });
                }
            }

            // Return success message after sending all emails
            return Json(new { success = true, message = "Emails sent successfully to all recipients." });
        }



    }
}
