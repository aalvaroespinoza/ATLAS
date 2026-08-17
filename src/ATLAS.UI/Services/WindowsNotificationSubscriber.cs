using System.Diagnostics;
using ATLAS.Core.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ATLAS.UI.Services;

/// <summary>
/// Subscribes to the AtlasEventBus and triggers native Windows Toast Notifications.
/// Runs as a hosted background service to remain decoupled from the UI thread.
/// </summary>
public class WindowsNotificationSubscriber : IHostedService
{
    private readonly IAtlasEventBus _eventBus;
    private IDisposable? _subscription;

    public WindowsNotificationSubscriber(IAtlasEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _eventBus.SubscribeAll(HandleEventAsync);
        Debug.WriteLine("[WindowsNotificationSubscriber] Iniciado y escuchando eventos.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        Debug.WriteLine("[WindowsNotificationSubscriber] Detenido.");
        return Task.CompletedTask;
    }

    private Task HandleEventAsync(IAtlasEvent atlasEvent)
    {
        try
        {
            switch (atlasEvent)
            {
                case TransactionCreatedEvent txEvent:
                    ShowToast("ATLAS Finanzas", $"💳 Gasto de ${txEvent.Amount:N0} registrado en {txEvent.Category ?? "varios"}.");
                    break;
                case NoteCapturedEvent noteEvent:
                    ShowToast("ATLAS Segundo Cerebro", "📝 Nota capturada correctamente.");
                    break;
                case TransactionsSyncedEvent syncEvent:
                    ShowToast("ATLAS Finanzas", $"💳 Sincronización completa. {syncEvent.NewTransactionsCount} nuevos movimientos guardados de {syncEvent.Source}.");
                    break;
                case HabitCompletedEvent habitEvent:
                    ShowToast("ATLAS Hábitos", $"🏆 Hábito completado: {habitEvent.HabitName}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsNotificationSubscriber] Error sending toast notification: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private void ShowToast(string title, string body)
    {
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsNotificationSubscriber] AppNotificationBuilder failed, fallback to ToastNotificationManager: {ex.Message}");
            
            // Fallback for older Windows 10 versions or if AppNotificationManager is not initialized
            try
            {
                var xmlString = $@"
                    <toast>
                        <visual>
                            <binding template='ToastGeneric'>
                                <text>{System.Security.SecurityElement.Escape(title)}</text>
                                <text>{System.Security.SecurityElement.Escape(body)}</text>
                            </binding>
                        </visual>
                    </toast>";

                var xmlDocument = new Windows.Data.Xml.Dom.XmlDocument();
                xmlDocument.LoadXml(xmlString);

                var toastNotification = new Windows.UI.Notifications.ToastNotification(xmlDocument);
                Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
            }
            catch (Exception fallbackEx)
            {
                Debug.WriteLine($"[WindowsNotificationSubscriber] ShowToast fallback failed: {fallbackEx.Message}");
            }
        }
    }
}
