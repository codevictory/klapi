using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public record FeedbackRequest(string? Message);

public class EmailOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(WebApplication app)
    {
        app.MapPost("/feedback", async (
            FeedbackRequest request,
            IOptions<EmailOptions> emailOptions,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { Message = "Feedback message is required." });
            }

            var message = request.Message.Trim();

            if (message.Length > 5000)
            {
                return Results.BadRequest(new { Message = "Feedback message is too long." });
            }

            var options = emailOptions.Value;

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(options.FromAddress));
            email.To.Add(MailboxAddress.Parse(options.ToAddress));
            email.Subject = "Klapi – New feedback";
            email.Body = new TextPart("plain") { Text = message };

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(options.SmtpHost, options.SmtpPort, SecureSocketOptions.StartTls);

                if (!string.IsNullOrWhiteSpace(options.Username))
                {
                    await smtp.AuthenticateAsync(options.Username, options.Password);
                }

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send feedback email.");
                return Results.Problem("Failed to send feedback. Please try again later.");
            }

            return Results.Ok(new { Message = "Feedback sent." });
        })
        .RequireCors("FrontendWriteCors")
        .WithName("SubmitFeedback");
    }
}
