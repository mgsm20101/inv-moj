using MediatR;
using WIMS.Application.Features.Alerts;

namespace WIMS.WebApi.Services;

/// <summary>
/// محرّك الإنذارات الخلفي — يشغّل جولة فحص دورية (نقطة الطلب/الحد الأدنى/الصلاحية/الركود)
/// وينشئ الإنذارات ويرسل البريد للحرِج. الفاصل الزمني من الإعداد "Alerts:ScanIntervalMinutes".
/// </summary>
public sealed class AlertsBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AlertsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = configuration.GetValue("Alerts:ScanIntervalMinutes", 60);
        var interval = TimeSpan.FromMinutes(Math.Max(1, minutes));

        // تأخير أولي بسيط ليكتمل الإقلاع والبذر.
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var summary = await sender.Send(new RunAlertScanCommand(), stoppingToken);
                if (summary.IsSuccess)
                    logger.LogInformation(
                        "جولة فحص الإنذارات: أُنشئ {Created}، أُغلِق {Resolved}، حرِج جديد {Critical}.",
                        summary.Value.Created, summary.Value.Resolved, summary.Value.CriticalNew);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "فشل جولة فحص الإنذارات — ستُعاد المحاولة في الجولة التالية.");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
