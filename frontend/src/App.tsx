import { useState, useEffect } from 'react'

interface Device {
  id: number
  name: string
  room: string
  discriminator: string
  isOn?: boolean
}

function App() {
  const [devices, setDevices] = useState<Device[]>([])

  useEffect(() => {
    fetch('http://localhost:5187/api/devices') 
      .then(response => {
        if (response.ok) {
          return response.json()
        }
        throw new Error("Błąd sieci!")
      })
      .then(data => {
        console.log("Pobrane urządzenia:", data)
        setDevices(data)
      })
      .catch(error => console.error("Błąd pobierania:", error))
  }, [])

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial' }}>
      <h1>🏠 Smart Home Manager</h1>
      
      {/* Jeśli lista jest pusta, wyświetl komunikat */}
      {devices.length === 0 && <p>Ładowanie... (lub brak urządzeń w bazie)</p>}

      <div style={{ display: 'grid', gap: '10px', color: 'black' }}>
        {devices.map(device => (
          <div 
            key={device.id} 
            style={{ 
              border: '1px solid #ccc', 
              padding: '10px', 
              borderRadius: '8px',
              backgroundColor: '#f9f9f9'
            }}
          >
            <h3>{device.name}</h3>
            <p> Pokój: <strong>{device.room}</strong></p>
            <p> Typ: {device.discriminator} (ID: {device.id})</p>
            
            {/* Tutaj sprawdzamy, czy to żarówka i czy jest włączona */}
            {device.discriminator === 'LightBulb' && (
              <p>
                Status: {device.isOn ? '💡 WŁĄCZONA' : '⚫ Wyłączona'}
              </p>
            )}

             {/* Tutaj miejsce na temperaturę dla czujnika (zrobimy za chwilę) */}
          </div>
        ))}
      </div>
    </div>
  )
}

export default App