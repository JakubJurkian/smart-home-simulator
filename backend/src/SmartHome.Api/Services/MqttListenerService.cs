using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SmartHome.Domain.Interfaces; // Do IDeviceNotifier

namespace SmartHome.Api.Services;

public class MqttListenerService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private IMqttClient? _mqttClient;

    // Configuration like in simulator
    private const string BROKER_HOST = "test.mosquitto.org";
    private const int BROKER_PORT = 1883;
    private const string TOPIC = "smarthome/device/livingroom/temp";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MQTT client configuration
        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(BROKER_HOST, BROKER_PORT)
            .WithClientId($"BackendListener-{Guid.NewGuid()}")
            .WithCleanSession()
            .Build();

        // Obsługa zdarzenia: "Przyszła wiadomość"
        _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

        // Połączenie i subskrypcja
        try
        {
            await _mqttClient.ConnectAsync(mqttOptions, stoppingToken);
            
            var subscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(TOPIC)
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);

            Console.WriteLine("Backend: Connected to MQTT and listening...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Backend MQTT Error: {ex.Message}");
        }

        // Czekaj w nieskończoność (aż aplikacja nie zostanie zamknięta)
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        Console.WriteLine($"📥 Backend received MQTT: {payload}");

        // MAGIA INTEGRACJI
        
        // Musimy utworzyć "Scope", żeby dostać się do serwisów (SignalR, Baza Danych)
        // wewnątrz serwisu działającego w tle.
        using (var scope = _scopeFactory.CreateScope())
        {
            // Parsujemy JSON z symulatora
            try 
            {
                var data = JsonSerializer.Deserialize<TemperatureData>(payload);
                
                // Tutaj normalnie zapisalibyśmy to do bazy danych
                // np. var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
                // deviceService.UpdateTemperature("LivingRoomSensor", data.Temperature);
                // Ale na razie zrobimy skrót -> Od razu SignalR
                
                // Powiadamiamy Reacta przez SignalR
                var notifier = scope.ServiceProvider.GetRequiredService<IDeviceNotifier>();
                await notifier.NotifyDeviceChanged(); 
                
                Console.WriteLine("⚡ SignalR notification sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error processing message: {ex.Message}");
            }
        }
    }
}

// Klasa pomocnicza do odczytu JSON-a
public class TemperatureData
{
    public double temperature { get; set; }
    public DateTime timestamp { get; set; }
}