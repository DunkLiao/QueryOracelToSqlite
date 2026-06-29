# Oracle To SQLite 匯出工具 🧰

這是一個 Windows 桌面工具，可以把一整個資料夾裡的 Oracle SQL 查詢結果，批次匯出成 SQLite `.db` 檔案。

適合用在這些情境：

- 📊 把 Oracle 報表 SQL 匯成可攜帶的 SQLite 資料庫
- 📁 一次執行多個 `.sql` 檔案
- 🔁 用 SQL 檔名自動建立對應的 SQLite 資料表
- 🧪 把 Oracle 查詢結果交給其他工具做檢查、分析或保存

## ✨ 目前支援的功能

- 🖥️ Windows WPF 圖形介面
- 🔌 Oracle 連線欄位：Host、Port、Service name、Username、Password
- ✅ Oracle 連線測試
- 📂 選擇 SQL 資料夾後，批次執行第一層的 `*.sql` 檔案
- 🗃️ 匯出成單一 SQLite `.db` 檔
- 🏷️ SQLite table name 由 SQL 檔名自動產生
- 🔐 連線資訊會保存在本機使用者設定中，密碼使用 Windows DPAPI 加密
- 🧩 支援 Oracle SQL 參數：
  - `:THIS_MONTH_SS_SEQ`
  - `&THIS_MONTH_SS_SEQ`
- 🧹 自動移除 SQL 檔尾端單一分號 `;`
- ⚠️ 單一 SQL 檔失敗時，會繼續執行其他 SQL 檔，最後彙總成功與失敗結果
- 🧪 已有自動化測試覆蓋核心匯出流程

## 🚀 一般使用方式

### 1. 開啟程式

開發模式可從專案根目錄執行：

```powershell
dotnet run --project src/OracleToSqlite.App
```

若已產生 release 版本，直接執行：

```text
src\OracleToSqlite.App\bin\Release\net8.0-windows\win-x64\publish\OracleToSqlite.App.exe
```

### 2. 填寫 Oracle 連線資訊 🔌

畫面左側填入：

- `Host`：Oracle 主機 IP 或網域
- `Port`：通常是 `1521`
- `Service name`：Oracle service name
- `Username`：Oracle 帳號
- `Password`：Oracle 密碼

可以先按 `Test connection` 測試連線是否成功。

### 3. 選擇 SQL 資料夾 📂

在 `SQLite output` 區塊選擇：

- `SQL folder`：放 `.sql` 檔案的資料夾
- `Output file`：要輸出的 SQLite `.db` 檔案路徑

程式只會讀取 SQL 資料夾第一層的 `*.sql` 檔案，不會遞迴讀取子資料夾。

### 4. 輸入 SQL 參數 🧩

如果 SQL 內有參數，例如：

```sql
SELECT *
FROM BIS.RPT_CR6010
WHERE SS_SEQ = :THIS_MONTH_SS_SEQ;
```

或：

```sql
SELECT *
FROM BIS.RPT_CR6010
WHERE SS_SEQ = &THIS_MONTH_SS_SEQ;
```

請在 `SQL parameters` 輸入：

```text
THIS_MONTH_SS_SEQ=202405
```

也可以寫成：

```text
:THIS_MONTH_SS_SEQ=202405
```

或：

```text
&THIS_MONTH_SS_SEQ=202405
```

多個參數請一行一個：

```text
THIS_MONTH_SS_SEQ=202405
REPORT_CODE=CR6010
```

### 5. 執行匯出 ▶️

按 `Run` 開始匯出。

執行時會顯示：

- 已匯入筆數
- 輸出檔案位置
- 成功或失敗狀態
- 每個失敗 SQL 檔案的錯誤訊息

## 📌 匯出規則

### SQL 檔名會變成 SQLite table name

例如 SQL 資料夾內有：

```text
001_客戶資料.sql
002_交易明細.sql
005_BACVA.sql
```

匯出後 SQLite 會建立：

```text
001_客戶資料
002_交易明細
005_BACVA
```

### 目標 table 會被覆蓋重建

如果 SQLite 檔案裡已經有同名 table，程式會先刪除再重新建立。

### 單一 SQL 失敗不會中斷整批

如果 10 個 SQL 檔案中有 1 個失敗，其他 9 個仍會繼續執行。最後畫面會顯示成功幾個、失敗幾個，以及失敗原因。

## 🔐 連線資訊保存

程式會保存 Oracle 連線資訊，方便下次開啟時自動帶入。

保存位置：

```text
%LOCALAPPDATA%\OracleToSqlite\connection-settings.json
```

安全性說明：

- Host、Port、Service name、Username 會存在設定檔
- Password 會用 Windows DPAPI 依目前 Windows 使用者加密
- 設定檔不能直接拿到另一台電腦或另一個 Windows 使用者帳號解密使用
- 按 `Clear` 會清空畫面欄位，也會刪除已保存的連線資訊

## 🧯 常見問題

### ORA-00933：SQL 命令的結束有問題

常見原因是 SQL 檔尾端有 SQL Developer / SQL*Plus 習慣使用的分號 `;`。

目前程式會自動移除檔尾單一分號。如果仍發生，請檢查 SQL 是否包含 Oracle 不支援的語法，或是否貼入了非查詢指令。

### 缺少 SQL 參數

如果 SQL 使用：

```sql
WHERE SS_SEQ = :THIS_MONTH_SS_SEQ
```

但 `SQL parameters` 沒有填：

```text
THIS_MONTH_SS_SEQ=202405
```

該 SQL 檔會失敗，錯誤訊息會列出缺少的參數名稱。

### 匯出的 SQLite 檔打不開

請確認：

- 其他程式沒有鎖住該 `.db` 檔
- 輸出資料夾有寫入權限
- 磁碟空間足夠

## 🛠️ 開發者指令

從專案根目錄執行：

```powershell
dotnet restore
dotnet build
dotnet test
```

建立 Windows 免安裝 release 版本：

```powershell
.\build_release.bat
```

或手動發布：

```powershell
dotnet publish src\OracleToSqlite.App\OracleToSqlite.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true
```

發布後的執行檔位置：

```text
src\OracleToSqlite.App\bin\Release\net8.0-windows\win-x64\publish\OracleToSqlite.App.exe
```

## 🧱 專案結構

```text
.
├── src/
│   ├── OracleToSqlite.App/      # WPF 桌面程式
│   └── OracleToSqlite.Core/     # Oracle 查詢、SQLite 匯出、批次流程
├── tests/
│   └── OracleToSqlite.Tests/    # 自動化測試
├── test/                        # Oracle 測試資料準備文件
├── rfp/                         # 需求與規劃文件
├── build_release.bat            # 發布用批次檔
├── init_dev.md                  # 開發環境準備
└── TODO.md                      # 實作待辦與驗收清單
```

## 🧰 技術棧

- C# / .NET 8
- WPF
- MVVM
- `Oracle.ManagedDataAccess.Core`
- `Microsoft.Data.Sqlite`
- `CommunityToolkit.Mvvm`
- xUnit
- FluentAssertions

## ✅ 驗收建議

正式使用前，建議至少測試：

- Oracle 正確帳密可連線
- Oracle 錯誤密碼會顯示錯誤
- 簡單 SQL 可匯出
- 含 `:PARAM` 或 `&PARAM` 的 SQL 可匯出
- 多個 SQL 檔案批次執行
- 其中一個 SQL 失敗時，其他 SQL 仍會繼續
- 匯出的 SQLite `.db` 可用 SQLite 工具開啟查詢

## ⚠️ 注意事項

- 請不要把真實帳密、客戶資料、產出的 SQLite `.db` 檔提交到 Git。
- 目前工具假設 SQL 來源是可信任的內部 SQL。
- 本工具目標是匯出查詢結果，不是資料同步或排程系統。
