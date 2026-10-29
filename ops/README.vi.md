# Backup mã hóa và phục hồi SQL Server trên Windows

**Trạng thái:** đã tạo script và rà soát tĩnh; chưa chạy PowerShell, SQL Server, NTFS hay restore trong môi trường Linux hiện tại. Các tiêu chí bên dưới chỉ đạt sau khi thực thi và lưu bằng chứng trên Windows/SQL Server hỗ trợ backup encryption (Developer cho thử nghiệm; kiểm tra edition/license trước triển khai).

## Ranh giới quyền

- ASP.NET Identity `Administrator`, `Staff`, `User` là quyền ứng dụng, không phải quyền Windows hay SQL Server. Administrator web không tự động có quyền đọc `.bak`, export certificate hoặc restore.
- Tài khoản SQL ứng dụng chỉ có quyền DML cần thiết trên `HungTrinh`; không cấp `sysadmin`, `db_owner`, `dbcreator`, `CONTROL SERVER` hay quyền backup/restore. Migration chạy bằng tài khoản triển khai riêng.
- DBA tạo master key/certificate và quản lý restore. Tài khoản tác vụ backup riêng được cấp `BACKUP DATABASE` trên `HungTrinh` và quyền `VIEW DEFINITION` certificate tại master khi cần. DBA kiểm tra quyền tối thiểu theo phiên bản; không dùng tài khoản ứng dụng.
- Windows SQL service cần ghi thư mục backup/certificate lúc export. Nhóm backup chỉ đọc `.bak`; nhóm custodian chỉ đọc certificate/private key. Người dùng web và tài khoản ứng dụng không thuộc các nhóm này. Built-in Administrators/SYSTEM vẫn có quyền quản trị Windows; không thể dùng ACL để chống một quản trị viên máy đã nắm quyền hệ thống.
- Restore được vận hành bằng tài khoản DBA được chỉ định, kiểm soát và audit riêng. Không tạo endpoint web chạy lệnh restore hoặc nhận đường dẫn SQL tùy ý.

## Chuẩn bị và backup

1. Tạo hai thư mục NTFS rỗng, độc lập, ví dụ `D:\HungTrinhBackup` và `E:\HungTrinhCertificateExport`. Không đặt trong web root, thư mục dùng chung hoặc cùng một cây thư mục. Dùng tài khoản vận hành Windows được cấp quyền, chạy `Set-BackupAcl.ps1` với `-WhatIf` trước, sau đó thực thi; script có xác nhận vì thay thế ACL. Kiểm tra bằng `Get-Acl`/`icacls` và kiểm tra các nhóm lồng nhau. Với thư mục hiện có dữ liệu, DBA phải lên kế hoạch ACL/migration riêng; script từ chối để tránh làm mất quyền truy cập bất ngờ.
2. Dùng `sqlcmd` với xác thực Windows `-E`, mã hóa TLS `-N`, máy chủ có certificate TLS đáng tin cậy, và `-b` để lỗi SQL trả exit code khác 0. Không dùng `-C` hoặc tắt xác minh TLS. Tất cả đường dẫn SQL là đường dẫn **trên máy SQL Server**, không phải máy client.
3. Chạy `01-create-certificate.sql` một lần bằng DBA. Cung cấp `MasterKeyPassword`, `CertificateExportPassword`, `CertificateDirectory` qua scripting variables của sqlcmd trong phiên vận hành bảo mật. Không ghi mật khẩu vào repo, command line `-v`, transcript, lịch sử hoặc CI logs; không dùng `-e` để echo SQL. Nếu dùng biến môi trường process để sqlcmd đọc, đặt từ secret manager hoặc nhập `Read-Host -AsSecureString`, chuyển tạm trong bộ nhớ và xóa trong `finally`. SQLCMD thay thế chuỗi thuần: giá trị mật khẩu phải escape dấu `'` thành `''` trước khi gán; từ chối xuống dòng và tất cả chuỗi `$(`. Giá trị đường dẫn/logical name cũng phải được DBA kiểm tra, không nhận input người dùng web. Không tái sử dụng các giá trị đã escape cho thao tác khác.
4. Sao chép `.cer` và `.pvk` sang kho chứng chỉ độc lập/offline có ACL và mã hóa riêng; giữ password trong secret manager độc lập. Xác minh hash file và khả năng import trên máy thử nghiệm trước khi tin vào backup. Không giữ bản duy nhất trên máy chứa `.bak`. Sau export, DBA có thể gỡ quyền SQL service khỏi kho lưu trữ chứng chỉ offline; giữ các quyền cần thiết tại SQL certificate nội bộ.
5. Mỗi lần chạy `02-backup.sql`, truyền `BackupFile` là tên file mới có timestamp duy nhất, ví dụ `D:\HungTrinhBackup\HungTrinh_20261029T090000Z.bak`. Xác nhận file chưa tồn tại trước khi chạy. `COPY_ONLY` tránh ảnh hưởng chuỗi differential backup; `CHECKSUM` và `RESTORE VERIFYONLY` phát hiện lỗi media nhưng **không thay thế restore thật**. Thiết lập lịch backup, retention và giám sát ngoài ứng dụng. Đổi/rotate certificate phải giữ lại certificate/key cũ cho mọi backup còn lưu.

Ví dụ lệnh (chỉ các biến không bí mật đã chuẩn bị trong phiên an toàn):

```powershell
sqlcmd -S $SqlServer -E -N -b -i .\01-create-certificate.sql
if ($LASTEXITCODE -ne 0) { throw 'Certificate provisioning failed' }
sqlcmd -S $SqlServer -E -N -b -i .\02-backup.sql
if ($LASTEXITCODE -ne 0) { throw 'Backup failed' }
```

## Kiểm thử bắt buộc, trên instance thử nghiệm cô lập

| Mã | Thao tác | Điều kiện nghiệm thu |
|---|---|---|
| BK-01 | Đăng nhập Windows bằng tài khoản không có quyền; chạy `Test-UnauthorizedBackupRead.ps1 -BackupFile <file thực sự tồn tại>` | Cả mở đọc và copy đều nhận `UnauthorizedAccessException`; exit 0 chỉ khi cả hai bị từ chối. File không tồn tại/lỗi khác là lỗi kiểm thử, không phải đạt. |
| BK-02 | Kiểm tra ACL và membership dưới tài khoản ứng dụng, Staff/User, nhóm backup và custodian | App/Staff/User không đọc/copy `.bak`; backup không đọc `.pvk`; custodian không có quyền `.bak` nếu không được cấp riêng. Kiểm tra quyền kế thừa/nhóm lồng nhau. |
| BK-03 | Trên SQL instance mới **chưa import certificate**, chạy `RESTORE VERIFYONLY FROM DISK = '<file>' WITH CHECKSUM` | SQL từ chối do thiếu server certificate/thumbprint, sqlcmd `-b` trả khác 0. Không chấp nhận lỗi file/path/ACL thay cho lỗi certificate. |
| BK-04 | Với `.cer`/`.pvk` đúng, chạy `03-import-certificate-test.sql` bằng password export cố ý sai | Import private key bị từ chối; mã thoát khác 0; xác nhận certificate chưa được tạo. Nếu một certificate đã tồn tại, dùng instance thử nghiệm mới, không xóa certificate production. |
| BK-05 | Import lại với password đúng trên instance sạch, rồi `RESTORE FILELISTONLY FROM DISK = '<file>'` | Import thành công; ghi nhận logical name dữ liệu/log. Nếu nhiều file dữ liệu, thêm MOVE cho mọi file vào bản sao script thử nghiệm trước restore. |
| BK-06 | Truyền các biến `BackupFile`, `LogicalDataName`, `LogicalLogName`, `TestDataDirectory`; chạy `04-restore-test.sql` | Tạo được **HungTrinh_RestoreTest**, không ghi đè database hiện có, `DBCC CHECKDB` không lỗi; so khớp row counts và bản ghi ID nghiệp vụ đã ghi nhận ngay trước backup. SQL service có quyền trên thư mục dữ liệu thử nghiệm. |
| BK-07 | Trỏ bản ứng dụng thử nghiệm vào DB restore, dùng tài khoản thử nghiệm đã có trong backup | Đăng nhập được, role đúng và đọc được sản phẩm/danh mục đã biết; Staff/User bị chặn quản trị. Với cookie/data-protection key mới, đăng nhập lại là bình thường. Không gửi thông báo hoặc gọi tích hợp production. |
| BK-08 | Kết nối bằng SQL login ứng dụng; thử backup/restore hoặc truy cập key theo kịch bản DBA chuẩn bị trên môi trường test | Bị từ chối đúng quyền SQL; không thử thao tác có thể sửa production. |

**Phân biệt password:** `.bak` được mã hóa bằng certificate, không có “mật khẩu restore `.bak`”. Password bảo vệ file private key export. Password sai phải làm **import private key thất bại**; thiếu certificate/key phải làm **restore thất bại**. Nếu certificate đúng đã được import trước đó, thay đổi password export không làm restore bị từ chối; phải kiểm thử trên instance sạch.

Lưu bằng chứng từng BK: timestamp, phiên bản/edition SQL, tên instance thử nghiệm đã làm mờ, danh tính/nhóm kiểm thử (không có secrets), exit code, thông báo lỗi đúng loại, kết quả CHECKDB và dữ liệu mẫu. Không đánh dấu đạt khi chỉ có VERIFYONLY hoặc port mở. DBA dọn database thử nghiệm bằng quy trình riêng sau nghiệm thu; các script này không tự động DROP database.
