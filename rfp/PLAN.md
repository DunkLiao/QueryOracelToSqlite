# Oracle 查詢結果轉 SQLite 免安裝桌面工具計畫書

## Summary

建立一個 Windows 桌面應用程式，讓使用者輸入 Oracle 連線資訊與 SQL 查詢，執行後將查詢結果直接轉成 SQLite 資料庫表格。專案目前為空資料夾，建議從零建立 **C# .NET 8 + WPF + ODP.NET Managed + Microsoft.Data.Sqlite** 架構，開發與打包都可用命令列完成，不要求開啟 Visual Studio IDE。

目標成果：

- 產出可雙擊執行的 Windows 桌面程式。
- 不要求使用者安裝 Oracle Client。
- 不要求使用者安裝 SQLite。
- 可發布為 self-contained 免安裝版本。
- 支援 Oracle SQL 查詢結果匯入 SQLite `.db` 檔案中的指定資料表。

## Key Changes

- 建立 .NET 8 WPF 專案，採 MVVM 架構，使用 `CommunityToolkit.Mvvm` 管理 UI 狀態與命令。
- Oracle 連線使用 `Oracle.ManagedDataAccess`，以 managed driver 連線，避免依賴 Oracle Client 安裝。
- SQLite 寫入使用 `Microsoft.Data.Sqlite`，由程式自動建立 `.db` 檔與資料表。
- UI 第一版包含：
  - Oracle host/port/service name 或完整 connection string 輸入。
  - 帳號、密碼輸入。
  - SQL 查詢輸入區。
  - SQLite 輸出路徑選擇。
  - 目標 table name 輸入。
  - 執行、取消、清除按鈕。
  - 進度、筆數、錯誤訊息與完成狀態顯示。
- 匯入流程：
  - 測試 Oracle 連線。
  - 執行使用者 SQL。
  - 讀取結果欄位 schema。
  - 依 Oracle 欄位型別推導 SQLite 欄位型別。
  - 建立或覆蓋目標 SQLite table。
  - 使用 transaction 批次寫入資料。
  - 完成後顯示筆數與輸出檔案位置。

## Public Interfaces / Types

- 主要設定模型：
  - `OracleConnectionSettings`: host、port、service name、username、password、connection string。
  - `ExportJobSettings`: SQL、SQLite file path、target table name、overwrite mode。
  - `ExportResult`: success、row count、elapsed time、output path、error message。
- 主要服務：
  - `IOracleQueryService`: 測試連線、執行查詢、取得欄位資訊。
  - `ISqliteExportService`: 建立 SQLite 檔案、建立資料表、批次寫入資料。
  - `IExportJobRunner`: 串接 Oracle 查詢與 SQLite 匯出，支援取消。
- 第一版預設策略：
  - 目標資料表若存在，採「覆蓋重建」。
  - SQLite 型別簡化為 `INTEGER`、`REAL`、`TEXT`、`BLOB`、`NUMERIC`。
  - Oracle `DATE` / `TIMESTAMP` 寫入 SQLite `TEXT`，格式使用 ISO-like 字串。
  - Oracle `NUMBER` 依精度與小數位推導為 `INTEGER`、`REAL` 或 `NUMERIC`。
  - 欄位名稱保留 Oracle 查詢結果名稱，必要時用 SQLite identifier quote 處理。

## Test Plan

- 建置檢查：
  - `dotnet build`
  - `dotnet publish` 產出 self-contained Windows 可執行檔。
- 單元測試：
  - Oracle 型別到 SQLite 型別轉換。
  - SQLite table name / column name quote。
  - 建表 SQL 產生。
  - null、文字、數字、日期、blob 寫入轉換。
- 整合測試：
  - 使用模擬 schema/result reader 測試完整匯出流程，不依賴真 Oracle。
  - 產出的 SQLite 檔可被重新開啟並查詢資料筆數。
- 手動驗收：
  - 使用真 Oracle 測試簡單查詢：`select * from some_table where rownum <= 100`。
  - 測試大量資料，例如 10,000 筆以上。
  - 測試錯誤 SQL、錯誤密碼、無法連線、輸出檔被鎖定。
  - 測試取消匯出時 UI 不凍結，並顯示明確狀態。

## Assumptions

- 目標平台先以 Windows 為主。
- 第一版使用 WPF，不使用 WinUI 3，因為 WPF 對免安裝內部工具更簡單穩定。
- 第一版不做排程、不做多組任務、不做資料同步差異比對。
- 第一版不儲存密碼；若之後需要記住連線設定，再加入 Windows DPAPI 加密保存。
- 使用者輸入的 SQL 視為可信任內部使用，不額外做 SQL 安全限制。
- Oracle 連線以 username/password + host/port/service name 為主要模式，另提供完整 connection string 作為進階模式。
