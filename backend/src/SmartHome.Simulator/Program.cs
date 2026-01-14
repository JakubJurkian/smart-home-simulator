using System.Net.Http.Json;
using System.Text.Json; // JSON to send structured data
using MQTTnet;
using MQTTnet.Client;

// CONFIGURATION
const string API_URL = "http://localhost:5187/api/devices/all-system";
const string BROKER_HOST = "test.mosquitto.org"; // Public test broker
const int BROKER_PORT = 1883;

Console.WriteLine("--- IoT Device Simulator (Thermometer) ---");

// Create a factory to generate clients
var mqttFactory = new MqttFactory();
using var mqttClient = mqttFactory.CreateMqttClient();
using var httpClient = new HttpClient();

// Configure connection options
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithTcpServer(BROKER_HOST, BROKER_PORT)
    .WithClientId($"Simulator-{Guid.NewGuid()}") // Unique ID for this device
    .WithCleanSession()
    .Build();

// Connect to the broker
try
{
    Console.Write($"Connecting to {BROKER_HOST}...");
    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
    Console.WriteLine(" Connected!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n Connection failed: {ex.Message}");
    return;
}

// Simulation Loop (Send data every 5 seconds)
var random = new Random();
while (true)
{

    try
    {
        // download all devices (without accessing userId)
        var devices = await httpClient.GetFromJsonAsync<List<DeviceDto>>(API_URL);

        if (devices != null)
        {
            var sensors = devices.Where(d => d.Type == "TemperatureSensor").ToList();
            Console.WriteLine($"Found {sensors.Count} sensors globally.");

            foreach (var sensor in sensors)
            {
                // Wygeneruj dane
                double temp = Math.Round(20.0 + (random.NextDouble() * 5.0), 2);
                string json = JsonSerializer.Serialize(new { temperature = temp });

                // send to unique channel: smarthome/devices/{GUID}/temp
                string topic = $"smarthome/devices/{sensor.Id}/temp";

                await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(topic).WithPayload(json).Build());
            }
        }
    }
    catch (Exception ex) { Console.WriteLine($"⚠️ Error: {ex.Message}"); }

    await Task.Delay(5000); // wait 5 sec
}

public class DeviceDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}