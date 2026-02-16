using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SmartHome.Domain.Interfaces.Devices;

namespace SmartHome.Api.BackgroundServices;

public class MqttListenerService(IServiceScopeFactory scopeFactory, ILogger<MqttListenerService> logger, IConfiguration config) : BackgroundService
{
    private IMqttClient? _mqttClient;
    // Configuration
    private readonly string _host = config.GetValue<string>("MqttSettings:Host") ?? "localhost";
    private readonly int _port = config.GetValue<int>("MqttSettings:Port", 1883);
    private const string TOPIC = "smarthome/devices/+/temp";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithClientId($"BackendListener-{Guid.NewGuid()}")
            .WithCleanSession()
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

        _mqttClient.DisconnectedAsync += async (e) =>
        {
            logger.LogWarning("MQTT Disconnected. Reconnecting...");
            await Task.Delay(5000, stoppingToken);
            try
            {
                await _mqttClient.ConnectAsync(mqttOptions, stoppingToken);
            }
            catch
            { }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await _mqttClient.TryPingAsync())
                {
                    await _mqttClient.ConnectAsync(mqttOptions, stoppingToken);

                    var subscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(TOPIC)
                        .Build();

                    await _mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);

                    logger.LogInformation("MQTT Listener connected on {Host}", _host);
                }

                break;
            }
            catch (Exception ex)
            {
                logger.LogError("MQTT Connection Failed (Retrying in 5s): {Message}", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var segments = topic.Split('/');

            if (segments.Length > 2 && Guid.TryParse(segments[2], out Guid deviceId))
            {
                using var scope = scopeFactory.CreateScope();
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                var data = JsonSerializer.Deserialize<TemperatureData>(payload);

                if (data != null)
                {
                    var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
                    var notifier = scope.ServiceProvider.GetRequiredService<IDeviceNotifier>();

                    await deviceService.UpdateTemperatureAsync(deviceId, data.temperature);

                    await notifier.PushTemperature(deviceId, data.temperature);

                    logger.LogInformation("Processed {DeviceId}: {Temp}°C", deviceId, data.temperature);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing MQTT message");
        }
    }

    public class TemperatureData
    {
        public double temperature { get; set; }
    }
}