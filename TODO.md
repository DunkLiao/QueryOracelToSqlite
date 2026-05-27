# TODO List

根據 `rfp/PLAN.md` 拆分的實作待辦清單。

## 1. 開發環境

- [ ] 安裝 .NET 8 SDK。
- [ ] 安裝 Visual Studio 2022 Community。
- [ ] 在 Visual Studio Installer 勾選 `.NET desktop development` workload。
- [ ] 安裝 Git。
- [ ] 確認 `dotnet --version` 可正常執行。
- [ ] 確認 `git --version` 可正常執行。

## 2. 專案骨架

- [ ] 建立 .NET 8 WPF solution。
- [ ] 建立主要 WPF application project。
- [ ] 建立測試 project。
- [ ] 加入 `Oracle.ManagedDataAccess` NuGet 套件。
- [ ] 加入 `Microsoft.Data.Sqlite` NuGet 套件。
- [ ] 加入 `CommunityToolkit.Mvvm` NuGet 套件。
- [ ] 加入設定與 logging 相關 NuGet 套件。
- [ ] 建立 MVVM 基礎結構。

## 3. 設定模型與核心型別

- [ ] 建立 `OracleConnectionSettings`。
- [ ] 建立 `ExportJobSettings`。
- [ ] 建立 `ExportResult`。
- [ ] 建立 Oracle 欄位 schema 描述型別。
- [ ] 建立 SQLite 欄位 schema 描述型別。
- [ ] 定義匯出狀態與錯誤訊息格式。

## 4. Oracle 查詢服務

- [ ] 建立 `IOracleQueryService` 介面。
- [ ] 實作 Oracle 連線字串產生邏輯。
- [ ] 實作 Oracle 連線測試。
- [ ] 實作 SQL 查詢執行。
- [ ] 實作查詢結果欄位 schema 讀取。
- [ ] 支援 host/port/service name 連線模式。
- [ ] 支援完整 connection string 進階模式。
- [ ] 處理連線失敗、帳密錯誤、SQL 錯誤。

## 5. SQLite 匯出服務

- [ ] 建立 `ISqliteExportService` 介面。
- [ ] 實作 SQLite 檔案建立。
- [ ] 實作 SQLite identifier quote。
- [ ] 實作 Oracle 型別到 SQLite 型別轉換。
- [ ] 實作 `CREATE TABLE` SQL 產生。
- [ ] 實作目標 table 存在時覆蓋重建。
- [ ] 實作 transaction 批次寫入。
- [ ] 實作 null、文字、數字、日期、blob 寫入轉換。

## 6. 匯出流程協調

- [ ] 建立 `IExportJobRunner` 介面。
- [ ] 串接 Oracle 查詢與 SQLite 匯出流程。
- [ ] 執行前檢查必要欄位。
- [ ] 匯出過程回報進度與筆數。
- [ ] 支援取消匯出。
- [ ] 完成後回傳筆數、耗時、輸出檔案位置。
- [ ] 失敗時回傳可讀錯誤訊息。

## 7. WPF UI

- [ ] 建立主視窗版面。
- [ ] 建立 Oracle host 輸入欄位。
- [ ] 建立 Oracle port 輸入欄位。
- [ ] 建立 Oracle service name 輸入欄位。
- [ ] 建立完整 connection string 進階輸入欄位。
- [ ] 建立帳號輸入欄位。
- [ ] 建立密碼輸入欄位。
- [ ] 建立 SQL 查詢輸入區。
- [ ] 建立 SQLite 輸出路徑選擇功能。
- [ ] 建立目標 table name 輸入欄位。
- [ ] 建立執行按鈕。
- [ ] 建立取消按鈕。
- [ ] 建立清除按鈕。
- [ ] 顯示執行進度。
- [ ] 顯示已匯入筆數。
- [ ] 顯示錯誤訊息。
- [ ] 顯示完成狀態與輸出檔案位置。
- [ ] 避免匯出期間 UI 凍結。

## 8. 驗證與錯誤處理

- [ ] 驗證 Oracle 連線資訊不可空白。
- [ ] 驗證 SQL 查詢不可空白。
- [ ] 驗證 SQLite 輸出路徑不可空白。
- [ ] 驗證目標 table name 不可空白。
- [ ] 處理輸出檔案被鎖定。
- [ ] 處理查詢結果沒有欄位。
- [ ] 處理查詢結果沒有資料列。
- [ ] 處理欄位名稱重複。
- [ ] 處理不支援或未知 Oracle 型別。

## 9. 自動化測試

- [ ] 測試 Oracle 型別到 SQLite 型別轉換。
- [ ] 測試 SQLite table name quote。
- [ ] 測試 SQLite column name quote。
- [ ] 測試建表 SQL 產生。
- [ ] 測試 null 值寫入。
- [ ] 測試文字寫入。
- [ ] 測試數字寫入。
- [ ] 測試日期與 timestamp 寫入。
- [ ] 測試 blob 寫入。
- [ ] 使用模擬 schema/result reader 測試完整匯出流程。
- [ ] 驗證產出的 SQLite 檔可重新開啟並查詢筆數。

## 10. 手動驗收

- [ ] 使用真 Oracle 測試連線成功。
- [ ] 使用真 Oracle 測試錯誤密碼。
- [ ] 使用真 Oracle 測試錯誤 SQL。
- [ ] 測試簡單查詢：`select * from some_table where rownum <= 100`。
- [ ] 測試 10,000 筆以上大量資料。
- [ ] 測試取消匯出。
- [ ] 測試輸出檔案被鎖定。
- [ ] 確認完成後 SQLite 檔可被外部工具開啟。

## 11. 建置與發布

- [ ] 執行 `dotnet build`。
- [ ] 修正 build warning 與 error。
- [ ] 執行測試。
- [ ] 執行 self-contained publish。
- [ ] 確認發布資料夾內可直接執行程式。
- [ ] 在未安裝 Oracle Client 的環境測試執行。
- [ ] 在未安裝 .NET Runtime 的環境測試執行。
- [ ] 整理發布檔案與使用說明。

## 12. 第一版暫不實作

- [ ] 不做排程功能。
- [ ] 不做多組匯出任務管理。
- [ ] 不做資料同步差異比對。
- [ ] 不儲存密碼。
- [ ] 不限制內部可信任 SQL。
