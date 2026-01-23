namespace SmartHome.Domain.Entities;

public class TemperatureSensor(string name, Guid roomId) : Device(name, roomId, "TemperatureSensor")
{
    public double CurrentTemperature { get; private set; } = 21.0;

    /// <summary>
    /// Simulates downloading data from a sensor.
    /// In real world here would be code which connect with a physical device.
    /// </summary>
    public double GetReading()
    {
        return CurrentTemperature;
    }

    public void SetTemperature(double temp)
    {
        CurrentTemperature = temp;
    }
}