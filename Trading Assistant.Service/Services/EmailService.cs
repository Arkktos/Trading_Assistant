using System.Text;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using Trading_Assistant.Service.Configuration;
using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailConfig _config;

    public EmailService(ILogger<EmailService> logger, EmailConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendAnalysisReportAsync(AnalysisResult analysisResult, List<MarketData> marketDataList)
    {
        try
        {
            _logger.LogInformation("Preparing analysis report email");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.FromName, _config.FromAddress));
            message.To.Add(new MailboxAddress("", _config.ToAddress));
            message.Subject = $"Trading Assistant - Rapport d'analyse du {analysisResult.AnalysisDate:dd/MM/yyyy HH:mm}";

            var htmlBody = BuildHtmlReport(analysisResult, marketDataList);

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Port 587 uses STARTTLS, port 465 uses SSL/TLS
            var secureSocketOptions = _config.SmtpPort == 465
                ? MailKit.Security.SecureSocketOptions.SslOnConnect
                : MailKit.Security.SecureSocketOptions.StartTls;

            await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions);
            await client.AuthenticateAsync(_config.Username, _config.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Analysis report email sent successfully to {Email}", _config.ToAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending analysis report email");
            throw;
        }
    }

    private string BuildHtmlReport(AnalysisResult analysisResult, List<MarketData> marketDataList)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px; }
            h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
            h2 { color: #34495e; margin-top: 30px; border-bottom: 2px solid #ecf0f1; padding-bottom: 8px; }
            h3 { color: #7f8c8d; margin-top: 20px; }
            .summary { background-color: #ecf0f1; padding: 15px; border-radius: 5px; margin: 20px 0; }
            .market-data { background-color: #f8f9fa; padding: 10px; margin: 10px 0; border-radius: 5px; }
            .positive { color: #27ae60; font-weight: bold; }
            .negative { color: #e74c3c; font-weight: bold; }
            .opportunity { background-color: #fff; border-left: 4px solid #3498db; padding: 15px; margin: 15px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            .opportunity.buy { border-left-color: #27ae60; }
            .opportunity.sell { border-left-color: #e74c3c; }
            .opportunity.hold { border-left-color: #f39c12; }
            .badge { display: inline-block; padding: 3px 8px; border-radius: 3px; font-size: 0.85em; font-weight: bold; margin-right: 5px; }
            .badge.high { background-color: #27ae60; color: white; }
            .badge.medium { background-color: #f39c12; color: white; }
            .badge.low { background-color: #95a5a6; color: white; }
            .observation { padding: 8px; margin: 5px 0; background-color: #f8f9fa; border-left: 3px solid #3498db; }
            .footer { margin-top: 40px; padding-top: 20px; border-top: 2px solid #ecf0f1; font-size: 0.9em; color: #7f8c8d; text-align: center; }
            table { width: 100%; border-collapse: collapse; margin: 10px 0; }
            th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ecf0f1; }
            th { background-color: #34495e; color: white; }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>📊 Rapport d'Analyse de Marché</h1>");
        sb.AppendLine($"<p><strong>Date:</strong> {analysisResult.AnalysisDate:dd MMMM yyyy à HH:mm}</p>");

        sb.AppendLine("<div class='summary'>");
        sb.AppendLine($"<h2>📝 Résumé</h2>");
        sb.AppendLine($"<p>{analysisResult.Summary}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>📈 Données de Marché</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Actif</th><th>Prix</th><th>Var. Jour</th><th>Var. Semaine</th><th>Var. Mois</th><th>Volume</th></tr>");

        foreach (var data in marketDataList)
        {
            var dayClass = data.DayChangePercent >= 0 ? "positive" : "negative";
            var weekClass = data.WeekChangePercent >= 0 ? "positive" : "negative";
            var monthClass = data.MonthChangePercent >= 0 ? "positive" : "negative";

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td><strong>{data.Asset.Name}</strong><br/>({data.Asset.Symbol})</td>");
            sb.AppendLine($"<td>${data.CurrentPrice:F2}</td>");
            sb.AppendLine($"<td class='{dayClass}'>{data.DayChangePercent:+0.00;-0.00}%</td>");
            sb.AppendLine($"<td class='{weekClass}'>{data.WeekChangePercent:+0.00;-0.00}%</td>");
            sb.AppendLine($"<td class='{monthClass}'>{data.MonthChangePercent:+0.00;-0.00}%</td>");
            sb.AppendLine($"<td>{data.Volume:N0}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");

        if (analysisResult.Opportunities.Any())
        {
            sb.AppendLine("<h2>💡 Opportunités Détectées</h2>");

            foreach (var opp in analysisResult.Opportunities)
            {
                var oppClass = opp.Direction.ToLower();
                var confidenceBadge = opp.ConfidenceLevel.ToLower();

                sb.AppendLine($"<div class='opportunity {oppClass}'>");
                sb.AppendLine($"<h3>{opp.Asset.Name} ({opp.Asset.Symbol})</h3>");
                sb.AppendLine($"<p><strong>Direction:</strong> {opp.Direction} ");
                sb.AppendLine($"<span class='badge {confidenceBadge}'>{opp.ConfidenceLevel}</span>");
                sb.AppendLine($"<span class='badge medium'>{opp.Timeframe}</span></p>");
                sb.AppendLine($"<p><strong>Raison:</strong> {opp.Reason}</p>");
                sb.AppendLine("</div>");
            }
        }

        if (!string.IsNullOrEmpty(analysisResult.MarketSentiment))
        {
            sb.AppendLine("<div class='summary'>");
            sb.AppendLine("<h2>🎯 Sentiment de Marché</h2>");
            sb.AppendLine($"<p>{analysisResult.MarketSentiment}</p>");
            sb.AppendLine("</div>");
        }

        if (!string.IsNullOrEmpty(analysisResult.RiskAssessment))
        {
            sb.AppendLine("<div class='summary'>");
            sb.AppendLine("<h2>⚠️ Évaluation des Risques</h2>");
            sb.AppendLine($"<p>{analysisResult.RiskAssessment}</p>");
            sb.AppendLine("</div>");
        }

        if (analysisResult.KeyObservations.Any())
        {
            sb.AppendLine("<h2>🔍 Observations Clés</h2>");
            foreach (var observation in analysisResult.KeyObservations)
            {
                sb.AppendLine($"<div class='observation'>• {observation}</div>");
            }
        }

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p><strong>⚠️ Avertissement:</strong> Ce rapport fournit une analyse factuelle basée sur les données de marché. Il ne constitue pas un conseil financier personnalisé. Vous êtes seul responsable de vos décisions de trading.</p>");
        sb.AppendLine("<p>Généré automatiquement par Trading Assistant</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
