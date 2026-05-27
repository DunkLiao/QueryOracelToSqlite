# Oracle 測試範例資料庫準備步驟

本文件記錄如何準備免費 Oracle 測試資料庫，用來驗證本專案的「Oracle 查詢結果轉 SQLite」功能。

## 1. 下載 Oracle Database Free

建議使用 **Oracle Database Free** 作為本機測試資料庫。

官方下載頁：

- https://www.oracle.com/database/free/

可選擇的安裝方式：

- Docker container
- VirtualBox VM
- Linux RPM

若只是要測試本工具，建議優先使用 Docker container 或 VM，方便建立與重置環境。

## 2. 下載 Oracle 官方 Sample Schemas

Oracle 官方提供免費範例 schema，可用來建立測試資料表。

官方 GitHub：

- https://github.com/oracle-samples/db-sample-schemas

官方文件：

- https://docs.oracle.com/en/database/oracle/oracle-database/19/comsc/installing-sample-schemas.html

目前已下載到本專案：

```text
test/oracle-db-sample-schemas-main.zip
```

## 3. 建議先使用 HR Schema

初期測試建議先匯入 `HR` schema，因為資料表簡單、欄位型別清楚，適合驗證：

- Oracle 連線
- SQL 查詢
- 欄位 schema 讀取
- SQLite 建表
- SQLite 寫入資料

建議測試 SQL：

```sql
select * from hr.employees;
select * from hr.departments;
select * from hr.jobs;
```

## 4. 大量資料測試

若要測試 10,000 筆以上資料，可考慮：

- 匯入 `SH` schema。
- 使用 `CO` schema。
- 自行建立測試資料表並產生大量資料。

建議大量資料測試項目：

- 匯出速度。
- UI 是否凍結。
- 進度與筆數顯示是否正確。
- SQLite 檔案是否可正常開啟。
- 取消匯出是否有效。

## 5. 建議測試順序

1. 安裝或啟動 Oracle Database Free。
2. 匯入 Oracle 官方 sample schemas。
3. 先使用 `HR` schema 測試小量資料。
4. 使用以下 SQL 驗證基本匯出：

```sql
select * from hr.employees;
```

5. 確認 SQLite `.db` 檔案已建立。
6. 確認 SQLite 內有對應資料表與資料列。
7. 再使用 `SH` 或自建資料表測試大量資料。

## 6. 本專案測試工具的驗收重點

使用 Oracle sample schemas 測試時，應確認：

- 不需要安裝 Oracle Client。
- 可以用帳號密碼連線 Oracle Database Free。
- 可以執行使用者輸入的 SQL。
- Oracle 欄位型別可以正確轉為 SQLite 欄位型別。
- Oracle `DATE` / `TIMESTAMP` 可以寫入 SQLite `TEXT`。
- Oracle `NUMBER` 可以依精度與小數位寫入 SQLite `INTEGER`、`REAL` 或 `NUMERIC`。
- 匯出完成後會顯示筆數、耗時與輸出檔案位置。
- 發生錯誤時會顯示可讀的錯誤訊息。
