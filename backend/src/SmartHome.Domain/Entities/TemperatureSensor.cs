namespace SmartHome.Domain.Entities;

public class TemperatureSensor(string name, string room) : Device(name, room, "TemperatureSensor")
{
    public double CurrentTemperature { get; private set; } = 21.0;

    /// <summary>
    /// Symuluje pobranie danych z czujnika sprzętowego.
    /// W prawdziwym świecie tutaj byłby kod łączący się z fizycznym urządzeniem.
    /// </summary>
    public double GetReading()
    {
        // Używamy generatora losowości
        var random = new Random();

        // Symulacja wahań temperatury
        // Generujemy liczbę od -0.5 do +0.5
        double fluctuation = (random.NextDouble() * 1.0) - 0.5;

        // Aktualizujemy temperaturę (zmienia się powoli, nie skacze z 20 na 50)
        CurrentTemperature += fluctuation;

        // Zaokrąglamy do 1 miejsca po przecinku (np. 21.3)
        CurrentTemperature = Math.Round(CurrentTemperature, 1);

        return CurrentTemperature;
    }

    public void SetTemperature(double temp)
    {
        CurrentTemperature = temp;
    }
}