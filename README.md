# MouseBD - Android Touchpad

Aplikacja zamieniająca telefon z Androidem w touchpad dla komputera z Windows. Komunikacja w czasie rzeczywistym przez UDP — minimalne opóźnienia, jak touchpad w laptopie.

## Architektura

```
[ Telefon Android ]  ─── UDP / WiFi ───  [ PC Windows ]
   MouseBD (MAUI)                          MouseBD.Server
   - Touchpad UI                           - UDP listener
   - Gesture detection                     - SendInput (Win32)
   - UDP client                            - System tray icon
```

## Struktura projektu

```
MouseBD.sln
├── MouseBD/              ← Aplikacja Android (MAUI)
├── MouseBD.Server/       ← Serwer na PC (Windows, system tray)
└── MouseBD.Shared/       ← Wspólny protokół UDP
```

## Protokół UDP

Proste binarne pakiety, port 27015 (UDP):

| Typ        | Bajt 0 | Bajt 1-4 | Bajt 5-8 |
|------------|--------|----------|----------|
| Move       | `0x01` | float dx | float dy |
| LeftDown   | `0x02` | —        | —        |
| LeftUp     | `0x03` | —        | —        |
| RightDown  | `0x04` | —        | —        |
| RightUp    | `0x05` | —        | —        |
| Scroll     | `0x06` | float dx | float dy |
| Ping       | `0x07` | —        | —        |
| Pong       | `0x08` | —        | —        |
| MiddleDown | `0x09` | —        | —        |
| MiddleUp   | `0x0A` | —        | —        |

## Instalacja i uruchomienie

### PC (Serwer)

**Wymagania:** .NET 9 SDK, Windows 10/11

```bash
cd MouseBD.Server
dotnet run -c Release
```

Lub skompiluj do .exe:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Serwer uruchomi się w zasobniku systemowym (tray). Kliknij dwukrotnie ikonę aby zobaczyć adres IP.

**Zapora (Firewall):** Odblokuj port UDP 27015 przychodzący:
```
netsh advfirewall firewall add rule name="MouseBD" dir=in action=allow protocol=UDP localport=27015
```

### Android (Aplikacja)

**Wymagania:** .NET 9 MAUI workload, Android SDK

```bash
cd MouseBD
dotnet build -f net9.0-android -c Release
dotnet publish -f net9.0-android -c Release
```

Albo otwórz rozwiązanie w Visual Studio 2022+ i wdróż na urządzenie/emulator.

## Jak używać

1. Uruchom `MouseBD.Server` na PC
2. Upewnij się, że telefon i PC są w tej samej sieci WiFi
3. Otwórz aplikację na telefonie
4. W ustawieniach (⚙) wpisz adres IP komputera
5. Naciśnij "Połącz"

### Gesty touchpada

| Gest | Akcja |
|------|-------|
| 1 palec - przeciągnij | Ruch kursora |
| 1 palec - szybkie tapnięcie | Lewy przycisk myszy |
| 2 palce - przeciągnij | Przewijanie |
| Przycisk LPM | Lewy przycisk (wciśnij i trzymaj) |
| Przycisk PPM | Prawy przycisk |
| Przycisk ● | Środkowy przycisk |

## Wskazówki dotyczące wydajności

- Używaj **WiFi 5 GHz** dla minimalnych opóźnień (< 5ms)
- UDP nie gwarantuje dostarczenia pakietów, ale jest wystarczające dla ruchów myszy
- Sub-pikselowa akumulacja zapewnia płynny ruch nawet przy wolnym WiFi
- Serwer używa `SendInput` — najszybsze możliwe Win32 API dla sterowania myszą

## Wymagania

| Komponent | Wymaganie |
|-----------|-----------|
| PC | Windows 10/11, .NET 9 |
| Telefon | Android 7.0+ (API 24+) |
| Sieć | WiFi (ta sama sieć) |
