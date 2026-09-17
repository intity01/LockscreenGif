# Lockscreen Gif — เอกสารประกอบโปรเจกต์

เอกสารชุดนี้สรุปผลการวิเคราะห์โค้ดในรีโปนี้ เพื่อช่วยให้ผู้พัฒนาใหม่เข้าใจภาพรวม สถาปัตยกรรม และวิธีการทำงานของแอปได้อย่างรวดเร็ว

## แอปนี้คืออะไร

**LockscreenGif** เป็นแอป Windows Desktop (WinUI 3) ที่ให้ผู้ใช้ตั้งค่า **GIF แบบเคลื่อนไหว** (หรือวิดีโอที่แปลงเป็น GIF) เป็นภาพหน้าจอล็อก (Lock Screen) ของ Windows ซึ่งปกติแล้ว Windows Settings ไม่รองรับการตั้งค่านี้โดยตรง แอปนี้จึงอาศัยการดัดแปลงไฟล์แคชภาพหน้าจอล็อกของระบบ (`C:\ProgramData\Microsoft\Windows\SystemData\...`) เพื่อ "หลอก" ให้ Windows แสดง GIF แทนภาพนิ่ง

รายละเอียดเชิงลึกของกลไกนี้อยู่ที่ [`HOW-IT-WORKS.md`](./HOW-IT-WORKS.md)

## เอกสารในชุดนี้

| ไฟล์ | เนื้อหา |
| --- | --- |
| [`ARCHITECTURE.md`](./ARCHITECTURE.md) | ภาพรวมสถาปัตยกรรม, โครงสร้างโปรเจกต์/โซลูชัน, การทำงานของ Dependency Injection |
| [`HOW-IT-WORKS.md`](./HOW-IT-WORKS.md) | กลไกเบื้องหลังการปลอมภาพหน้าจอล็อก และไปป์ไลน์แปลงวิดีโอเป็น GIF |
| [`SERVICES.md`](./SERVICES.md) | อ้างอิงคลาส/บริการ (services) หลักทั้งหมด พร้อมหน้าที่ความรับผิดชอบ |
| [`DEVELOPMENT.md`](./DEVELOPMENT.md) | วิธีติดตั้งเครื่องมือ, build/run/publish/test, และรายละเอียด CI/CD |
| [`ROADMAP.md`](./ROADMAP.md) | แผนพัฒนาต่อยอด, การตัดสินใจสำคัญ (UI framework, custom window skin, fork/rename), และสถานะความคืบหน้าปัจจุบัน |

## สรุปเทคโนโลยีที่ใช้

| หมวด | เทคโนโลยี |
| --- | --- |
| Framework | .NET 9 (`net9.0-windows10.0.26100.0`), WinUI 3 (Windows App SDK) |
| MVVM | CommunityToolkit.Mvvm (`ObservableRecipient`) |
| DI / Hosting | `Microsoft.Extensions.Hosting` (Generic Host) |
| แปลงวิดีโอ → เฟรม | FFMpegCore + `ffmpeg.exe` (bundled ใน `Vendor/FFMPEG`) |
| สร้างไฟล์ GIF | Gifski.Net + `gifski.dll` (bundled ใน `Vendor/gifski`) |
| UI Controls เพิ่มเติม | `CommunityToolkit.WinUI.Controls.RangeSelector`, `Microsoft.Xaml.Behaviors.WinUI.Managed` |
| การอ่านความละเอียดจอ | `WindowsDisplayAPI` |
| ติดตั้ง/แพ็กเกจ | Advanced Installer (`Installer/LockscreenGif.aip`) → สร้างเป็น `.msi`, และ MSIX ผ่าน `EnableMsixTooling` |
| CI/CD | GitHub Actions (`.github/workflows/dotnet-desktop.yml`) |

## โครงสร้างโฟลเดอร์ระดับบนสุด

```
LockscreenGif/
├── LockScreenGif/      # โปรเจกต์แอปหลัก (WinUI 3)
├── Logger/             # Class library สำหรับ logging + crash dump แบบ static
├── Demos/              # วิดีโอตัวอย่างการใช้งาน
├── Installer/          # โปรเจกต์ Advanced Installer (.aip) สำหรับสร้างไฟล์ .msi
├── .github/workflows/  # CI pipeline (GitHub Actions)
└── docs/               # เอกสารชุดนี้
```

ดูรายละเอียดเพิ่มเติมในแต่ละไฟล์เอกสารด้านบน
