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
    private const string TOPIC = "smarthome/devices/+/temp";

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
        var topic = e.ApplicationMessage.Topic;
        var segments = topic.Split('/'); // [0]smarthome, [1]devices, [2]GUID, [3]temp
        if (segments.Length > 2 && Guid.TryParse(segments[2], out Guid deviceId))
        {
            using var scope = _scopeFactory.CreateScope();
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var data = JsonSerializer.Deserialize<TemperatureData>(payload);

            if (data != null)
            {
                // Save in db
                var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
                deviceService.UpdateTemperature(deviceId, data.temperature);

                // send to react for live effect
                var notifier = scope.ServiceProvider.GetRequiredService<IDeviceNotifier>();
                await notifier.PushTemperature(deviceId, data.temperature);

                Console.WriteLine($"✅ Processed {deviceId}: {data.temperature}");
            }
        }
    }

    // Klasa pomocnicza do odczytu JSON-a
    public class TemperatureData
{
    public double temperature { get; set; }
}
}