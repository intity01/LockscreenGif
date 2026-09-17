# อ้างอิง Services และคลาสสำคัญ

> อัปเดตตาม Phase 1 (ดู [`ROADMAP.md`](./ROADMAP.md)): `DisplayService`, `FfmpegService`, `GifSkiService` ถูกแปลงจาก static class เป็น instance service ที่ลงทะเบียนผ่าน DI (มี interface แล้ว) และมีการดึง logic ล้วน ๆ ออกไปอยู่ที่โปรเจกต์ `Core` ใหม่ (ดูหัวข้อถัดไป)

## `Core/` (โปรเจกต์ `LockscreenGif.Core`, ใหม่)

โปรเจกต์ class library เปล่า (`net8.0`, ไม่มี WinUI/WinRT reference) เก็บ logic ล้วน ๆ ที่ทดสอบได้ง่าย มี unit tests ครบทุกคลาสอยู่ที่ `Tests/LockscreenGif.Core.Tests`

| คลาส | หน้าที่ | ดึงมาจาก |
| --- | --- | --- |
| `MathHelpers` | `RoundToSigFigs` — ปัดเศษตัวเลขตามจำนวนเลขนัยสำคัญ | `MainPage.RoundToSigFigs` (เดิม) |
| `TimeFormat` | Parse/format เวลารูปแบบ `mm:ss.f` / `m:ss.f` | `MainPage.TryParseTime` + การ format `ToString(@"mm\:ss\.f")` ที่กระจายอยู่หลายจุด, และ `CustomRangeSelector.ToMinSec` (เดิม) |
| `GifFileSizeEstimator` | ประมาณขนาดไฟล์ GIF ที่จะเกิดขึ้น (MB) + ระดับความรุนแรง (`FileSizeSeverity`) | `MainPage.UpdateFileSizeWarning` (เดิม) |
| `VideoResolutionOptions` | สร้างรายการความละเอียดเอาต์พุตให้เลือก (ไม่ upscale เกินต้นฉบับ) | `MainPage.PopulateResolutionList` (เดิม) |
| `VideoFpsOptions` | สร้างรายการ FPS เอาต์พุตให้เลือก | `MainPage.PopulateFpsList` (เดิม) |
| `LockscreenDimmedFileNaming` | คำนวณชื่อไฟล์ "dimmed" ที่ต้องเขียนทับ (ล้วน, ไม่แตะไฟล์ระบบ/registry) | `LockscreenService.GetDimmedDestFileNames` (เดิม) |

## `Services/` (โปรเจกต์ `LockScreenGif`)

| คลาส | Interface | หน้าที่หลัก |
| --- | --- | --- |
| `LockscreenService` | `ILockscreenService` | หัวใจของแอป — apply/remove GIF เป็นภาพหน้าจอล็อกจริง โดยจัดการสิทธิ์ไฟล์ระบบ (`takeown`/`icacls`), ตรวจโหมดหน้าจอล็อก (Spotlight/Slideshow/Picture) ผ่าน Registry — รับ `IDisplayService` ผ่าน constructor ดู [`HOW-IT-WORKS.md`](./HOW-IT-WORKS.md) |
| `FfmpegService` | `IFfmpegService` | ห่อหุ้ม FFMpegCore เพื่อ trim วิดีโอ, ทำ `-movflags +faststart`, และ extract เฟรมเป็น PNG sequence โดยใช้ `ffmpeg.exe` ที่ bundle มาใน `Vendor/FFMPEG`. ลงทะเบียนเป็น `Singleton` ใน DI |
| `GifSkiService` | `IGifSkiService` | ห่อหุ้ม Gifski.Net เพื่อเข้ารหัสชุดภาพ PNG ให้เป็นไฟล์ GIF คุณภาพสูง โดยใช้ `gifski.dll` ที่ bundle มาใน `Vendor/gifski`. ลงทะเบียนเป็น `Singleton` ใน DI |
| `DisplayService` | `IDisplayService` | ดึงความละเอียดของจอที่ต่ออยู่ทั้งหมดผ่าน `WindowsDisplayAPI` เพื่อคำนวณชื่อไฟล์ dimmed lockscreen ที่ถูกต้อง |
| `TempDirectoryService` | — (static) | คืนค่า root path สำหรับไฟล์ชั่วคราวของแอป (`%LocalAppData%\LockscreenGif\Temp`) ใช้ร่วมกันโดย `FfmpegService`/`GifSkiService` (ยังคงเป็น static เพราะเป็น pure helper ไม่มี state) |
| `ActivationService` | `IActivationService` | จัดลำดับขั้นตอนตอนแอป launch: init theme → ตั้ง content ของ `MainWindow` → หา `IActivationHandler` ที่เหมาะสม → activate window → ตั้ง requested theme |
| `AppNotificationService` | `IAppNotificationService` | จัดการ toast notification ของ Windows (App Notifications) |
| `NavigationService` | `INavigationService` | นำทางระหว่างหน้าใน `Frame` (ตาม pattern ของ Template Studio แม้แอปนี้จะมีแค่หน้าเดียว) |
| `PageService` | `IPageService` | Map ระหว่างชื่อ page กับ `Type` ของ View สำหรับ `NavigationService` |
| `ThemeSelectorService` | `IThemeSelectorService` | จัดการ theme (light/dark/system) และ persist การตั้งค่าธีมของผู้ใช้ |

## `Activation/`

| คลาส | หน้าที่ |
| --- | --- |
| `IActivationHandler` / `ActivationHandler<T>` | สัญญากลางสำหรับ handler ที่ประมวลผล activation args ประเภทต่าง ๆ |
| `DefaultActivationHandler` | Handler เริ่มต้นเมื่อแอปถูกเปิดแบบปกติ (double-click / launch) — navigate ไป `MainPage` |
| `AppNotificationActivationHandler` | Handler เมื่อแอปถูกเปิดจากการคลิก toast notification |

## `CustomControls/`

| คลาส | หน้าที่ |
| --- | --- |
| `CustomRangeSelector` | ขยาย `RangeSelector` (Community Toolkit) ให้ยิง event `RangeDragging` ต่อเนื่องระหว่างลาก thumb (ไม่ใช่แค่ตอนปล่อยเมาส์) และอัปเดต tooltip เป็นรูปแบบเวลา `m:ss.f` — ใช้เป็นตัวเลือกช่วง trim วิดีโอใน `MainPage` |

## `Helpers/`

| ไฟล์ | หน้าที่ |
| --- | --- |
| `FrameExtensions.cs` | Extension methods เกี่ยวกับ `Frame` (นำทาง) |
| `ResourceExtensions.cs` | `"key".GetLocalized()` — ดึงข้อความจาก resource file ตามภาษา |
| `RuntimeHelper.cs` | ตรวจว่าแอปกำลังรันแบบ packaged (MSIX) หรือ unpackaged |
| `TitleBarHelper.cs` | ปรับสีปุ่ม caption (minimize/maximize/close) ให้ตรงกับธีมระบบ |

## `Models/`

| คลาส | หน้าที่ |
| --- | --- |
| `LocalSettingsOptions` | Bind จาก section `LocalSettingsOptions` ใน `appsettings.json` — กำหนดชื่อโฟลเดอร์/ไฟล์สำหรับ local settings |

## โปรเจกต์ `Logger` (แยกเป็น class library ใช้ร่วมกัน)

| คลาส | หน้าที่ |
| --- | --- |
| `Logger` (static) | เขียน log ลงไฟล์ `%LocalAppData%\LockscreenGif\app_yyyy-MM-dd.log` ระดับ `Info/Warn/Error/Fatal`, ลบไฟล์ log เก่ากว่า 7 วันอัตโนมัติ (`CleanupOldLogFiles`) |
| `DumpCreator` | เรียก Win32 API `MiniDumpWriteDump` เพื่อสร้างไฟล์ `CrashDump.dmp` เมื่อแอป crash แบบไม่ได้ตั้งใจ |

## Views/ViewModels

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Views/MainPage.xaml` + `.xaml.cs` | UI เดียวของทั้งแอป: เลือกไฟล์ (GIF/วิดีโอ), preview วิดีโอ + ตัดช่วงเวลา, ตั้งค่าความกว้าง/FPS, ปุ่ม generate GIF, ปุ่ม apply/remove lockscreen, แสดงสถานะโหมดหน้าจอล็อกปัจจุบัน (polling ผ่าน `DispatcherQueueTimer`) |
| `ViewModels/MainViewModel.cs` | จัดการ orchestration logic ของ flow หลัก: `ApplyLockscreenAsync`, `RemoveLockscreenAsync`, `GenerateGifAsync` (trim to extract-frames to encode-gif) — รับ `ILockscreenService`/`IFfmpegService`/`IGifSkiService`/`IAppNotificationService` ผ่าน constructor ไม่อ้างอิง XAML controls โดยตรง (progress ส่งกลับเป็น `Action<double>` callback ให้ `MainPage` จัดการ UI-thread marshaling เอง) ส่วน UI-control concern ล้วน ๆ (video scrubbing, drag events, lockscreen-mode polling) ยังอยู่ที่ code-behind ของ `MainPage` ตามเจตนา |
