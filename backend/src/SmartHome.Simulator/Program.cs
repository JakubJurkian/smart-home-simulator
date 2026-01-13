using System.Text;
using System.Text.Json; // JSON to send structured data
using MQTTnet;
using MQTTnet.Client;

// CONFIGURATION
const string BROKER_HOST = "test.mosquitto.org"; // Public test broker
const int BROKER_PORT = 1883;

// Use a unique topic so we don't mix with other people testing!
const string TOPIC = "smarthome/device/livingroom/temp"; 

Console.WriteLine("--- IoT Device Simulator (Thermometer) ---");

// Create a factory to generate clients
var mqttFactory = new MqttFactory();
using var mqttClient = mqttFactory.CreateMqttClient();

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
    // Generate random temperature between 20.0 and 25.0
    double temp = Math.Round(20.0 + (random.NextDouble() * 5.0), 2);

    // Create a data object
    var payloadObj = new
    {
        temperature = temp,
        timestamp = DateTime.UtcNow,
        unit = "C"
    };

    // Serialize to JSON string
    string payloadJson = JsonSerializer.Serialize(payloadObj);

    // Build the MQTT message
    var message = new MqttApplicationMessageBuilder()
        .WithTopic(TOPIC)
        .WithPayload(payloadJson)
        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
        .Build();

    // Publish
    await mqttClient.PublishAsync(message, CancellationToken.None);
    
    Console.WriteLine($"[Sent] Topic: {TOPIC} | Payload: {payloadJson}");

    // Wait 5 seconds
    await Task.Delay(5000);
}