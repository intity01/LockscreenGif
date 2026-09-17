# คู่มือการพัฒนา (Development Guide)

## สิ่งที่ต้องมีก่อนเริ่ม (Prerequisites)

- Windows 10/11 (แอปนี้เป็น Windows-only, ใช้ WinUI 3 + Windows App SDK)
- [.NET 9 SDK](https://dotnet.microsoft.com/) (target framework: `net9.0-windows10.0.26100.0`)
- Visual Studio 2022 พร้อม workload:
  - **.NET Desktop Development**
  - **Windows App SDK / WinUI application development**
- Windows SDK เวอร์ชัน `10.0.26100.0` ขึ้นไป (`SupportedOSPlatformVersion` ในไฟล์ `.csproj`)
- (ทางเลือก) [Advanced Installer](https://www.advancedinstaller.com/) หากต้องการ build ไฟล์ติดตั้ง `.msi` จาก `Installer/LockscreenGif.aip` เอง

## โครงสร้างโซลูชัน

```
LockscreenGif.sln
├── LockScreenGif/   (โปรเจกต์แอปหลัก, WinExe, x64)
└── Logger/          (Class library, ใช้ร่วมกัน)
```

## Build & Run

เปิด `LockscreenGif.sln` ด้วย Visual Studio แล้วกด Run (F5) โดยตั้ง `LockScreenGif` เป็น Startup Project หรือใช้ CLI:

```sh
cd LockScreenGif
dotnet build -c Debug
dotnet run -c Debug
```

> หมายเหตุ: ฟีเจอร์ apply/remove lockscreen เรียก `takeown`/`icacls` แบบ elevated (`runas`) ทุกครั้ง ดังนั้นเวลาทดสอบฟีเจอร์นี้จะมี UAC prompt ขึ้นมาเสมอ แม้ตัวแอปหลักจะรันแบบ non-admin ก็ตาม

### รัน Unit Tests

Logic ล้วน ๆ (ไม่ผูกกับ WinUI/WinRT) ถูกแยกไว้ที่โปรเจกต์ `Core` และมี unit test อยู่ที่ `Tests/LockscreenGif.Core.Tests` (xUnit) รันด้วย:

```sh
dotnet test Tests/LockscreenGif.Core.Tests/LockscreenGif.Core.Tests.csproj
```

`Core`/`Core.Tests` ตั้ง `TargetFramework=net8.0` โดยตั้งใจ (LTS, ไม่มี Windows-specific dependency) เพื่อให้รันได้เร็วและไม่ต้องพึ่ง Windows App SDK runtime — แอปหลัก (`net9.0-windows10.0.26100.0`) อ้างอิง `Core` ได้ตามปกติเพราะ TFM ที่สูงกว่าสามารถอ้างอิง library ที่ต่ำกว่าได้

เมื่อเพิ่ม logic ใหม่ใน `MainPage.xaml.cs`/`MainViewModel` หากส่วนไหนเป็น pure logic (ไม่ต้องพึ่ง XAML controls/WinRT) ควรพิจารณาจะดึงออกมาไว้ใน `Core` แล้วเขียน test คู่กัน ตามแนวทางที่ทำไว้ใน Phase 1 (ดู [`ROADMAP.md`](./ROADMAP.md))

### ไบนารีภายนอกที่ต้องมี (Vendor)

โปรเจกต์อ้างอิงไบนารีที่ต้องอยู่ในตำแหน่งต่อไปนี้ (ถูก copy ไปยัง output directory ตอน build ตามที่ตั้งค่าไว้ใน `LockscreenGif.csproj`):

- `Vendor/FFMPEG/ffmpeg.exe`
- `Vendor/gifski/gifski.dll`

หากไฟล์เหล่านี้หายไปจากรีโป (เช่น ถูก `.gitignore` เพราะไฟล์ไบนารีขนาดใหญ่) จะต้องดาวน์โหลดมาวางเองก่อน build:

- FFmpeg: https://www.ffmpeg.org/
- Gifski: https://gif.ski/ (ใช้ผ่าน NuGet package `Gifski.Net` ที่ต้องมี native `gifski.dll` คู่กัน)

## Publish

โปรเจกต์มี publish profile ที่อ้างอิงใน `.csproj`:

```
Properties\PublishProfiles\win10-$(Platform).pubxml
```

คำสั่งที่ CI ใช้จริง (ดู `.github/workflows/dotnet-desktop.yml`):

```sh
cd LockScreenGif
dotnet publish -c Release -p:PublishProfile=FolderProfile.pubxml
```

ผลลัพธ์จะอยู่ที่ `LockScreenGif/bin/Release/net9.0-windows10.0.26100.0/win-x64/`

## การสร้างตัวติดตั้ง (Installer)

`Installer/LockscreenGif.aip` เป็นโปรเจกต์ของ **Advanced Installer** ใช้แพ็กเกจแอปที่ publish แล้วเป็นไฟล์ `.msi` โดย CI จะเรียกผ่าน GitHub Action `caphyon/advinst-github-action` (ต้องมี Advanced Installer license/automation enabled)

หากต้องการ build ในเครื่อง สามารถเปิดไฟล์ `.aip` ด้วยโปรแกรม Advanced Installer แล้วกด build ได้โดยตรง

โปรเจกต์ยังรองรับการแพ็กเกจแบบ MSIX ผ่าน `EnableMsixTooling`/`Package.appxmanifest` แต่ `WindowsPackageType` ถูกตั้งเป็น `None` (ไม่ได้บังคับ build เป็น MSIX โดยดีฟอลต์)

## CI/CD (`.github/workflows/dotnet-desktop.yml`)

Workflow นี้ทำงานเมื่อ push/PR เข้า branch `master` หรือสั่งรันเองผ่าน `workflow_dispatch`:

1. **Checkout** โค้ด
2. **แก้เวอร์ชัน** ใน `Package.appxmanifest` อัตโนมัติด้วย regex replace (`Version="0.<run_number>.<run_attempt>.0"`)
3. ติดตั้ง **.NET 9 SDK** และ **MSBuild**
4. ถอดรหัส signing certificate จาก secret `BASE64_ENCODED_PFX` (เขียนเป็นไฟล์ `.pfx`)
5. `dotnet publish` โปรเจกต์หลักแบบ Release
6. Build ตัวติดตั้งจาก `Installer/LockscreenGif.aip` ด้วย Advanced Installer action → ได้ `installLockscreenGif.msi`
7. ลบไฟล์ `.pfx` ทิ้ง
8. Upload 2 artifacts: แอปแบบ unpackaged (โฟลเดอร์ publish) และไฟล์ `.msi`

ขั้นตอนสร้าง GitHub Release ถูก comment ปิดไว้ (ยังไม่ใช้งานจริง)

### Secrets ที่ workflow นี้ต้องการ

| Secret | ใช้ทำอะไร |
| --- | --- |
| `BASE64_ENCODED_PFX` | Signing certificate สำหรับเซ็นแอป (เข้ารหัส Base64) |
| `Pfx_Key` (ตามคอมเมนต์ในไฟล์) | รหัสผ่านของ certificate — ปัจจุบันยังไม่เห็นใช้จริงใน step ที่มีอยู่ ควรตรวจสอบก่อนใช้งานจริงหากต้อง sign ไฟล์ที่ publish ออกมา |

## การ Logging และการ Debug

- Log ไฟล์อยู่ที่ `%LocalAppData%\LockscreenGif\app_yyyy-MM-dd.log` (เขียนผ่าน `Logger.Info/Warn/Error/Fatal`)
- Log ไฟล์เก่ากว่า 7 วันจะถูกลบอัตโนมัติตอนแอปสตาร์ท
- หากแอป crash แบบไม่ได้ตั้งใจ จะมีไฟล์ `CrashDump.dmp` ถูกสร้างไว้ในโฟลเดอร์เดียวกัน (สร้างผ่าน `DumpCreator` ในโปรเจกต์ `Logger`)
- ไฟล์ชั่วคราวจากการแปลงวิดีโอ/สร้าง GIF อยู่ที่ `%LocalAppData%\LockscreenGif\Temp\` (ล้างอัตโนมัติ)

## ข้อจำกัดที่ควรรู้ก่อนแก้ไข/ทดสอบ

- รองรับเฉพาะไฟล์ `.gif` จริงเท่านั้นเป็น input โดยตรง (ไม่รองรับไฟล์วิดีโอที่เปลี่ยนนามสกุลเป็น .gif) — ต้องผ่านการแปลงในแอปก่อนเสมอ
- ฟีเจอร์หลัก (apply/remove lockscreen) แก้ไขไฟล์ในโฟลเดอร์ระบบและเปลี่ยนสิทธิ์ไฟล์อย่างถาวร ควรทดสอบในเครื่องที่ยอมรับความเสี่ยงนี้ได้ (แนะนำ VM)
- โหมดหน้าจอล็อกแบบ **Windows Spotlight** หรือ **Slideshow** อาจทำให้ผลลัพธ์ไม่เป็นไปตามคาด เพราะ Windows จะสลับภาพเองเป็นระยะ ผู้ใช้ควรเปลี่ยนเป็นโหมด "Picture" ก่อนใช้แอปนี้
