# Roadmap

เอกสารนี้บันทึกแผนพัฒนาต่อยอด `LockscreenGif` ที่ตกลงกันไว้ รวมถึงการตัดสินใจสำคัญ (UI framework, โครงสร้างโปรเจกต์, การ fork/rename) และสถานะความคืบหน้าปัจจุบัน จะอัปเดตไฟล์นี้ทุกครั้งที่มีการตัดสินใจใหม่หรือ phase คืบหน้า

## ลำดับความสำคัญที่ตกลงกันไว้

1. **Architecture** — ทำโค้ดดูแล/ทดสอบง่ายขึ้นก่อน (ย้าย logic ออกจาก code-behind, DI-friendly services, unit tests)
2. **CI/CD** — ปรับ pipeline ให้รันเทสต์และมีคุณภาพเป็นระบบมากขึ้น
3. **Features** — เพิ่มฟีเจอร์ใหม่ให้ผู้ใช้
4. **Other / Exploratory** — งานที่ยังไม่เร่งด่วนหรือรอ prerequisite จาก phase อื่น (การย้าย UI framework, ระบบ custom window shape/skin)

## การตัดสินใจสำคัญ

### 1) จะย้าย UI framework (WinUI3 → อื่น) ตอนนี้ไหม?

**ตัดสินใจ: ยังไม่ย้ายตอนนี้ แต่เตรียมทางไว้**

- แอปนี้พึ่งพา Windows โดยกำเนิด (แก้ไฟล์ระบบ, Registry, `takeown`/`icacls`, `WindowsDisplayAPI`) จึงไม่มีประโยชน์จากการย้ายเพื่อ "cross-platform"
- ปัญหาจริงของ WinUI3 คือความยืดหยุ่นด้าน custom styling/theming และภาระ deployment (Windows App SDK runtime, MSIX)
- ถ้าจะย้ายจริงในอนาคต ตัวเลือกที่แนะนำคือ **Avalonia UI + FluentAvalonia** (styling ยืดหยุ่นกว่า, ไม่ผูก WinAppSDK runtime, หน้าตา Fluent ใกล้เคียงเดิม)
- **เงื่อนไขก่อนตัดสินใจย้ายจริง**: ต้องแยก business logic ออกจาก WinUI/WinRT ให้หมดก่อน (ดู Phase 1/4) เพื่อให้การย้าย UI เหลือแค่ "เขียน UI project ใหม่" ไม่ใช่ "เขียนแอปใหม่ทั้งหมด"
- จุดตัดสินใจซ้ำ: หลัง Phase 1 เสร็จ (Core/Infrastructure แยกจาก UI แล้ว)

### 2) ระบบ custom window shape / "อนิเมะ" branding identity

ผู้ใช้ต้องการให้หน้าต่างหลักของแอปมีเอกลักษณ์ทางภาพที่ชัดเจน (โปร่งใสบางส่วน + สไตล์อนิเมะ) และเปิดกว้างถึงขั้นให้ผู้ใช้ปลายทาง (end user) ปรับแต่งรูปทรง/สกินเองได้ — นี่คือ **"Custom Window Skin System"** ไม่ใช่แค่การปรับ CSS/สไตล์เล็กน้อย

**ตัดสินใจ: จัดเป็น Phase 4 (หลัง Architecture เสร็จ)** เหตุผล:
- ต้องมี custom title bar/drag/resize ของตัวเองทั้งหมด (เสีย Windows Snap Layouts, ต้องทำ hit-testing เอง)
- ถ้าทำตอนนี้ (ก่อน Phase 1) จะเพิ่ม coupling กับ WinUI3 chrome/HWND มากขึ้นไปอีก สวนทางกับเป้าหมาย "แยก UI ออกจาก logic" — ทำให้ทั้งการรีแฟกเตอร์และการย้าย UI framework (ถ้าเลือกทำ) ยากขึ้น
- เป็น feature ที่ควรออกแบบเป็น "ระบบ skin ที่ data-driven" (ไม่ใช่ hardcode รูปทรงเดียว) ซึ่งควรทำตอนที่ UI layer แยกจาก business logic ชัดเจนแล้ว — จังหวะเดียวกับที่พิจารณาย้าย Avalonia พอดี (ข้อ 1 กับข้อนี้เป็นงานที่เกื้อกูลกัน)
- เทคนิคที่จะใช้ (บันทึกไว้สำหรับตอนถึง phase นี้): โปร่งใสบางส่วนด้วย transparent background + `Path`/`Polygon` ผสมกับ `SetWindowRgn` (P/Invoke บน HWND) สำหรับ "ทรงจริง" ที่คลิกทะลุมุมได้

### 3) โครงสร้างโซลูชันเป้าหมาย

```
LockscreenGif.sln
├── Core/                        # ✅ สร้างแล้ว (Phase 1) — pure C#, ไม่มี WinRT/WinUI ref
├── LockScreenGif/               # WinUI3 App: Views, ViewModels, Services (DI), Contracts
├── Logger/                      # Logging + crash dump (คงเดิม)
├── Tests/
│   └── LockscreenGif.Core.Tests/  # ✅ สร้างแล้ว (Phase 1) — xUnit
├── Installer/
├── Demos/
└── docs/
```

`Infrastructure/` (แยก services ที่แตะ OS ออกจากตัว App project) และการแตกเป็นหลาย project แบบเต็มรูปแบบ จะทำใน **Phase 4** เมื่อรู้ทิศทาง UI framework ชัดเจนแล้ว — ไม่ทำ big-bang ตอนนี้เพื่อลดความเสี่ยง

### 4) เรื่อง fork / เปลี่ยนชื่อ / เครดิต

- License เดิมเป็น **MIT** (`Copyright (c) 2026 Leapward-Koex`) — อนุญาตให้เปลี่ยนชื่อ/รีแบรนด์ได้เต็มที่ ขอแค่เก็บไฟล์ `LICENSE.txt`/`.rtf` เดิมไว้
- Repo นี้ตั้ง `upstream` remote ไว้ถูกต้องแล้ว (เชื่อมกับ `Leapward-Koex/LockscreenGif`) — การ rename repo บน GitHub ไม่ตัดความสัมพันธ์นี้
- **แนะนำ**: เพิ่มส่วน "Credits" ใน README อ้างอิงต้นทาง เมื่อพร้อม rebrand
- **แนะนำ**: ยังไม่ต้อง rename ตอนนี้ — รอจน Phase 1 เสร็จและทิศทาง (โดยเฉพาะเรื่อง UI framework/skin) ชัดเจน จะได้ rename + อัปเดต identity (`Package.appxmanifest`, namespace, `Installer/*.aip`) ครั้งเดียวจบ

## ไอเดียฟีเจอร์ใหม่ (backlog สำหรับ Phase 3)

| ฟีเจอร์ | เหตุผล |
| --- | --- |
| สำรอง/กู้คืน lockscreen เดิมก่อนเขียนทับ | ตอนนี้ "remove" ลบไฟล์ dimmed อย่างเดียว ไม่ได้เก็บของเดิมจริง ๆ — เสี่ยงข้อมูลหาย |
| Library/ประวัติ GIF ที่เคยสร้าง/ตั้ง พร้อม thumbnail | ลดเวลาสลับ GIF โดยไม่ต้อง generate ใหม่ |
| Preview GIF สดก่อน apply | ตอนนี้เห็น preview หลัง generate เสร็จเท่านั้น |
| ตั้ง GIF ต่างกันต่อจอ/ความละเอียด (multi-monitor) | รองรับผู้ใช้หลายจอ |
| Auto re-apply ตอนเปิดเครื่อง | กัน Windows Update ทับไฟล์คืน |
| หน้า Settings แยก (default resolution/fps/quality, log retention) | รวมการตั้งค่าที่กระจัดกระจาย |
| Auto-update (เช่น Velopack) | ลดภาระผู้ใช้โหลด artifact เอง |
| แปลงหลายคลิปพร้อมกัน (batch) | เพิ่มความสะดวก |
| ขยายภาษา (ตอนนี้มีแค่ `en-us`) | เข้าถึงผู้ใช้กว้างขึ้น |

## แผนพัฒนาแบ่งเฟส

| Phase | เป้าหมาย | สถานะ |
| --- | --- | --- |
| **1. Architecture** | ย้าย logic → ViewModel/testable Core, static service → DI interface, เพิ่ม test project + unit tests | ✅ **เสร็จเท่าที่ทำได้อย่างมีเหตุผล** (ดูข้อจำกัดด้านล่าง) — ย้าย backup/restore ไปเป็นงาน Phase 3 เพราะเป็นฟีเจอร์ ไม่ใช่งาน architecture |
| **2. CI/CD** | รัน unit test ใน GitHub Actions ก่อน publish, versioning สม่ำเสมอ, code analyzer/format check | 🟡 **กำลังทำ** (ดูด้านล่าง) |
| **3. Features** | เลือกทำ 2-3 ไอเดียจาก backlog ด้านบน (แนะนำเริ่ม: **backup/restore lockscreen เดิม**, live preview, settings page) | ⬜ ยังไม่เริ่ม |
| **4. Exploratory** | แยก Core/Infrastructure เป็นหลาย project เต็มรูปแบบ, ประเมิน/ทำ spike ย้าย UI → Avalonia+FluentAvalonia, ออกแบบ Custom Window Skin System, พิจารณา rename/rebrand | ⬜ รอ Phase 1 |

### Phase 1 — สรุปผล (เสร็จเท่าที่ทำได้อย่างมีเหตุผล)

- [x] สร้างโปรเจกต์ `Core` (net8.0, pure C#, ไม่มี WinRT/WinUI ref) — อิฐก้อนแรกของสถาปัตยกรรมเป้าหมาย
- [x] สร้างโปรเจกต์ `Tests/LockscreenGif.Core.Tests` (xUnit) — 27 unit tests ผ่านทั้งหมด
- [x] ดึง logic ล้วน ๆ ออกจาก `MainPage.xaml.cs`/`CustomRangeSelector.cs`/`LockscreenService.cs` เข้า `Core`:
  - `MathHelpers.RoundToSigFigs`
  - `TimeFormat` (parse/format เวลา mm:ss.f)
  - `GifFileSizeEstimator` (ประมาณขนาดไฟล์ + severity)
  - `VideoResolutionOptions` / `VideoFpsOptions` (รายการตัวเลือกใน dropdown)
  - `LockscreenDimmedFileNaming` (คำนวณชื่อไฟล์ dimmed ที่ต้องเขียนทับ)
- [x] แปลง `DisplayService`, `FfmpegService`, `GifSkiService` จาก static class → interface (`IDisplayService`, `IFfmpegService`, `IGifSkiService`) + DI-registered instance service
- [x] อัปเดต `LockscreenService` ให้รับ `IDisplayService` ผ่าน constructor แทนการเรียก static
- [x] อัปเดต `MainPage`/`App.xaml.cs` ให้ inject/resolve service ผ่าน DI แทนการเรียก static method ตรง ๆ
- [x] ยืนยัน build ทั้งโซลูชันผ่าน (0 errors) และ unit tests ผ่านทั้งหมด
- [x] ย้าย orchestration logic ของปุ่ม Apply/Remove lockscreen และ pipeline ของปุ่ม Generate เข้า `MainViewModel` (`ApplyLockscreenAsync`, `RemoveLockscreenAsync`, `GenerateGifAsync`) — `MainPage.xaml.cs` เหลือเฉพาะการอ่าน/เขียนค่า XAML controls (progress bar, dialog, IsEnabled) และ UI-thread marshaling เท่านั้น
- [ ] Video scrubbing sync (`MediaPlayer`/`CustomRangeSelector` drag events, polling lockscreen mode) ยังคงอยู่ code-behind โดยตั้งใจ — เป็น UI-control concern ล้วน ๆ แม้ใน MVVM ที่ดีก็มักเก็บไว้ที่ View ไม่จำเป็นต้องย้ายเพิ่ม
- [ ] เพิ่ม unit tests สำหรับ `MainViewModel` (ต้อง mock `ILockscreenService`/`IFfmpegService`/`IGifSkiService`/`IAppNotificationService`) — **ข้อจำกัดที่ทราบแล้ว**: `MainViewModel` อยู่ในโปรเจกต์ WinUI3 (`net9.0-windows10.0.26100.0`) การสร้าง test project อ้างอิงโดยตรงจะต้องพึ่ง Windows App SDK runtime และ UI-thread apartment ซึ่งซับซ้อนกว่า xUnit ธรรมดา (ต้องใช้ WinAppDriver หรือ MSTest แบบตั้ง UI thread apartment) — ยังไม่ได้ทำในรอบนี้เพราะไม่สามารถยืนยันได้ว่ารันได้จริงใน sandbox นี้ — **แนวทางแก้**: เก็บ logic ที่ซับซ้อนจริง ๆ ไว้ใน `Core` (ทดสอบง่ายด้วย xUnit ธรรมดา) และให้ `MainViewModel` เป็นเพียง thin orchestration เท่านั้น

ย้าย "backup/restore lockscreen เดิม" ไปอยู่ใน backlog ของ **Phase 3** แทน (เดิมจัดไว้ใน Phase 1 โดยไม่ถูกต้อง — เป็นฟีเจอร์ใหม่ให้ผู้ใช้ ไม่ใช่งานปรับ architecture ของโค้ดที่มีอยู่แล้ว)

> หมายเหตุ: โปรเจกต์ `Core`/`Tests` ตั้ง `TargetFramework=net8.0` (LTS, ไม่มี Windows-specific dependency) แม้แอปหลักจะเป็น `net9.0-windows10.0.26100.0` — อ้างอิงข้ามแบบนี้ปลอดภัย (แอป net9.0 อ้างอิง library net8.0 ได้ปกติ) และทำให้ `Core` ทดสอบ/รันได้แม้ในเครื่องที่ไม่มี net9.0 runtime ติดตั้งแบบเต็ม

### Phase 2 — ความคืบหน้า

- [x] เพิ่ม step รัน `dotnet test` (โปรเจกต์ `Core.Tests`) ใน `.github/workflows/dotnet-desktop.yml` ก่อนขั้นตอน publish — build จะ fail ทันทีถ้ามีเทสต์พัง
- [x] ติดตั้ง .NET SDK ทั้ง `8.0.x` (สำหรับ `Core`/`Core.Tests`) และ `9.0.x` (สำหรับแอป WinUI3) ใน CI แทนที่จะมีแค่ `9.0.x`
- [x] แก้ความไม่สอดคล้องของ versioning: `Package.appxmanifest` เคยถูกตั้ง `Major.Minor` เป็น `0.x` ขณะที่ตัวติดตั้ง (Advanced Installer) ใช้ `2.1.x` — อัปเดตให้ทั้งสองจุดใช้ `2.1.x` ตรงกันแล้ว
- [ ] Code analyzer/format check (`dotnet format --verify-no-changes` หรือเทียบเท่า) — **ยังไม่ทำโดยตั้งใจ**: โค้ดเดิมยังไม่เคยผ่าน formatter มาก่อน การเปิด gate นี้ทันทีจะทำให้ CI แดงแดงทันทีโดยไม่เกี่ยวกับการเปลี่ยนแปลงครั้งนี้ ต้องรัน `dotnet format` ทั้งโค้ดก่อน (diff กว้างไม่เกี่ยวข้อง) — เสนอให้ทำเป็น PR แยกต่างหากต้องการจริง
