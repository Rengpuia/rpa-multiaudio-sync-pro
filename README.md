# RPA MultiAudio Sync Pro (7.1 USB + Bluetooth Router)

A high-performance audio routing utility and spatial testing suite engineered for:
- **🎬 Cinema Mode Lip-Sync / Audio Sync Engine**: Frame-accurate speech-to-lip synchronization for movie watching with Bluetooth and USB/Wired headphones.
- **🎛️ Dedicated Device Mixer**: Fine-grain volume, pan balance, and hardware chirps in one unified mixing console.
- **👂 Ear Separation Mode**: Per-device feed assignment (`Full Both Ears`, `L-Channel Left Ear`, `R-Channel Right Ear`) for simultaneous translation, multi-listener setups, or accessibility.
- **☰ Arranged Quick Menu**: Cleanly categorized flyout drawer for Routing Profiles, Audio Processing, Latency Presets, and System Settings.
- **🔗 Zero-Echo Headphone Lock**: Automatically delays USB/wired headphones to match Bluetooth wireless latency so both viewers hear audio in simultaneous unison.
- **⚡ 0ms Zero-Lag Gaming Mode**: Instant WASAPI event-driven direct stream with high-pass footstep presence boost (+22% gain).
- **🎛️ 7.1 Channel ON / OFF Switch**: Global and per-device toggle between 8-channel discrete surround sound and 2.0 stereo pass-through.
- **100% Anti-Cheat Safe**: User-mode Ring-3 WASAPI architecture (Vanguard, EAC, Ricochet, BattlEye compliant).
- **⚙️ General Preferences & Background Service**: Launch on Startup, Minimize to System Tray, and Auto-Reconnect.

---

## 👨‍💻 Developer Information

- **Developer**: K Vanlalrengpuia
- **Email**: rengakhiangte@gmail.com
- **Software**: RPA MultiAudio Sync Pro v2.4.2

---

## 🎬 Cinema Mode Lip-Sync & Audio Sync Features

### Why Cinema Lip-Sync is Crucial
Bluetooth headphones inherently introduce wireless buffer latency (typically **50ms to 240ms**). USB or 3.5mm wired headphones have virtually **0ms latency**.
When two people watch a movie together:
1. The Bluetooth listener experiences dialogue delayed behind the actor's lips.
2. The wired listener hears audio instantly, causing an annoying double-audio "reverb/echo" in the room.

### How RPA MultiAudio Sync Pro Solves This:
- **Visual Clapper Flash-and-Beep Calibrator**: A 60 FPS sweeping runner hits the 0ms target crosshair with a synchronized audio pulse. Users can dial in the offset until the visual hit and the sound land at the exact same millisecond.
- **Auto-Match Lock (`🔗 Lock USB to BT`)**: Instantly locks the delay of wired/USB headphones to match the Bluetooth headphone's latency so dialogue hits both listeners at the exact same fraction of a millisecond.
- **Hardware Profile Presets**:
  - `aptX-LL (Low Latency)`: 50 ms
  - `Apple AAC / AirPods`: 130 ms
  - `Sony LDAC / Bose`: 160 ms
  - `Standard SBC / Smart TV`: 220 ms
- **Video Player Integration**: Compatible with media player audio delay hotkeys (VLC: `J`/`K`, MPC-HC: `+`/`-`).

---

## 📁 Project Structure

```
multi-headphone-app/
├── index.html                     # Windows 11 Fluent Web UI with Cinema Lip-Sync & 7.1 Radar
├── RPAMultiAudioSyncPro.csproj     # .NET 8 Windows Forms C# project configuration
├── Program.cs                     # Native C# WASAPI audio routing engine with NAudio
├── run_web.bat                    # One-click launcher for the interactive Web application
├── RPAMultiAudioSyncPro_Setup.iss # Inno Setup Standalone Windows Installer script
├── cert/                          # Code-signing certificate & anti-block trust tools
│   ├── K_Vanlalrengpuia_CodeSigning.cer # Public Certificate (Trusted Publisher & Root)
│   ├── K_Vanlalrengpuia_CodeSigning.pfx # Authenticode Signing Key
│   ├── Install_Certificate.bat    # 1-Click installer to trust developer certificate
│   ├── Sign_Application.bat      # Script to sign .exe, .dll, and installer
│   ├── generate_cert.ps1          # Certificate generator script
│   └── sign_binaries.ps1          # Authenticode signer engine
├── assets/
│   ├── installer_logo.png         # High-resolution installer splash banner
│   ├── installer_logo_banner.svg  # Scalable vector banner
│   ├── installer_logo_square.svg  # Square 512x512 logo / icon asset
│   └── preview_logos.html         # Asset showcase gallery
└── README.md                      # Documentation and usage guide
```

---

## 🚀 How to Run

### Option 1: Instant Web Interface
Double-click `run_web.bat` or open `index.html` in your browser.
- Switch between **🎮 7.1 Gaming Radar** and **🎬 Cinema Lip-Sync Hub**.
- Click **Run Lip-Sync Pulse (Flash + Beep)** to calibrate audio-visual sync.

### Option 2: Native Windows Desktop App (.NET 8 WinForms)
1. Ensure [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) is installed.
2. Open terminal in `multi-headphone-app`:
   ```powershell
   dotnet restore
   dotnet build -c Release
   ```
3. Run:
   ```powershell
   .\bin\Release\net8.0-windows\RPAMultiAudioSyncPro.exe
   ```

---

## 🛡️ Digital Signature & Preventing Windows from Blocking Install

Windows SmartScreen and UAC typically block unknown installers with **"Windows protected your PC"** or **"Unknown Publisher"**. To prevent Windows from blocking the installer and app:

### Step 1: Trust Developer Certificate (1-Click)
1. Navigate to the `cert\` directory.
2. Right-click [`Install_Certificate.bat`](file:///j:/VC%20Directory%20Project/multi-headphone-app/cert/Install_Certificate.bat) and choose **"Run as Administrator"**.
3. This installs `K_Vanlalrengpuia_CodeSigning.cer` into Windows **Trusted Root Certification Authorities** and **Trusted Publishers**.
4. Once installed, Windows recognizes **K Vanlalrengpuia** as a verified publisher.

### Step 2: Sign Binaries and Installer
Run [`Sign_Application.bat`](file:///j:/VC%20Directory%20Project/multi-headphone-app/cert/Sign_Application.bat) (or run `powershell -File cert\sign_binaries.ps1 -FilePath <target.exe>`).
- Digitally signs executables using the RSA 2048 SHA256 Authenticode certificate.
- Windows UAC prompt displays **Verified Publisher: K Vanlalrengpuia** in blue instead of orange/red.
- The Inno Setup installer (`RPAMultiAudioSyncPro_Setup.iss`) automatically signs the uninstaller and registers the developer certificate during installation.
