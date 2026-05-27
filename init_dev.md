# 開發環境初始化清單

本專案建議使用 **C# .NET 8 + WPF + ODP.NET Managed + SQLite** 開發 Windows 免安裝桌面應用程式。

## 必裝工具

### 1. .NET 8 SDK

用途：

- 建立、編譯、測試、發布 WPF 專案。
- 發布 self-contained 免安裝 Windows 執行檔。

下載：

- https://dotnet.microsoft.com/download

安裝後確認：

```powershell
dotnet --version
```

## 2. Visual Studio 2022 Community

用途：

- 提供 WPF/Windows Desktop 專案所需 build tools。
- 可選擇性用來除錯、調整 XAML UI。

下載：

- https://visualstudio.microsoft.com/

安裝時勾選 workload：

- `.NET desktop development`

建議加選元件：

- `.NET 8 SDK`
- `MSBuild`
- `Windows 10 SDK` 或 `Windows 11 SDK`

> 開發時不一定需要開啟 Visual Studio IDE；Codex 可以用命令列建立、編譯與修改專案。

## 3. Git

用途：

- 版本管理。
- 方便追蹤 Codex 修改內容。

下載：

- https://git-scm.com/download/win

安裝後確認：

```powershell
git --version
```

## 專案 NuGet 套件

以下套件不需要事先手動安裝到系統；建立專案後用 `dotnet add package` 加入專案即可。

### 核心套件

```powershell
dotnet add package Oracle.ManagedDataAccess
dotnet add package Microsoft.Data.Sqlite
dotnet add package CommunityToolkit.Mvvm
```

用途：

- `Oracle.ManagedDataAccess`: Oracle managed driver，不需安裝 Oracle Client。
- `Microsoft.Data.Sqlite`: SQLite ADO.NET provider。
- `CommunityToolkit.Mvvm`: WPF MVVM binding、command、observable property 輔助。

### 設定與記錄套件

```powershell
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Logging.Debug
```

用途：

- 管理 appsettings.json 或本機設定。
- 提供錯誤與執行紀錄基礎。

### 測試套件

若建立 xUnit 測試專案，加入：

```powershell
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package FluentAssertions
```

用途：

- 單元測試 Oracle 型別轉 SQLite 型別。
- 測試 SQLite 建表 SQL 與資料寫入流程。

## 不需要另外安裝

- Oracle Client：預設使用 `Oracle.ManagedDataAccess` managed driver。
- SQLite 桌面程式或服務：SQLite library 會由 NuGet 套件帶入。
- .NET Runtime：正式發布時可使用 self-contained 模式打包。

## 建議發布指令

建立專案後，可用類似以下指令發布免安裝版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true
```

發布成果會位於：

```text
bin\Release\net8.0-windows\win-x64\publish\
```
