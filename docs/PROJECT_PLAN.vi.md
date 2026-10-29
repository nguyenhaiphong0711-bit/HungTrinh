# Kế hoạch web quản lý sản phẩm và danh mục

## Phạm vi và kiến trúc
ASP.NET Core 10 C#, Controllers/Models và EF Core SQL Server; ASP.NET Identity quản lý tài khoản. Next.js App Router + React + Tailwind CSS đảm nhiệm phần View. Đây là kiến trúc MVC backend kết hợp giao diện React, không phải giao diện Razor mặc định trong ảnh. Admin dùng MVC Area được bảo vệ và trả JSON cho Next.js. Frontend chuyển tiếp /api cùng origin tới backend, xác thực cookie HttpOnly và CSRF cho thao tác ghi. Production dùng HTTPS. Không lưu token vào localStorage.

Ba vai trò: User xem dữ liệu; Staff xem/thêm/sửa/xóa sản phẩm và danh mục; Administrator có quyền Staff và xem danh sách tài khoản; operator được tin cậy cấp vai trò qua CLI. Chỉ Administrator truy cập khu quản trị, kiểm tra quyền tại server. Đăng ký công khai luôn tạo User, không nhận quyền từ client. Tài khoản ứng dụng không có quyền backup/restore database.

## Các task và điều kiện nghiệm thu
| ID | Task nhỏ | Phụ trách | Phụ thuộc | Điều kiện nghiệm thu |
|---|---|---|---|---|
| T01 | Chốt phạm vi, role matrix, hợp đồng API | Điều phối | — | Sản phẩm/danh mục được chọn; xác định quyền server và DTO |
| T02 | Tạo backend MVC, cấu hình SQL Server/Identity | Backend | T01 | Restore/build thành công; secret ngoài mã nguồn; endpoint health hoạt động |
| T03 | Models sản phẩm/danh mục và ràng buộc database | Backend | T02 | Giá không âm, tên hợp lệ; quan hệ FK; migration tạo được DB |
| T04 | Đăng ký/đăng nhập/đăng xuất/thông tin hiện tại | Backend | T02 | Cookie an toàn; đăng ký luôn User; đăng nhập sai bị từ chối; logout hủy phiên |
| T05 | CSRF, role seed và CLI cấp vai trò | Backend | T04 | Request ghi thiếu CSRF bị chặn; anon 401; User/Staff bị từ chối admin; không tự tăng quyền |
| T06 | CRUD danh mục | Backend | T03,T05 | Staff/Admin ghi; User chỉ đọc; không xóa danh mục đang được sử dụng |
| T07 | CRUD sản phẩm | Backend | T03,T05,T06 | Dữ liệu hợp lệ lưu được; lỗi nhập liệu rõ; role bị chặn trực tiếp qua API |
| T08 | Khung Next.js/React/Tailwind và API proxy | Frontend | T01 | Build thành công; giao diện responsive; /api chuyển tiếp đúng backend |
| T09 | Trang đăng ký/đăng nhập và phiên người dùng | Frontend | T04,T08 | Xử lý lỗi, trạng thái loading; dùng cookie và CSRF đúng hợp đồng |
| T10 | UI danh mục/sản phẩm | Frontend | T06,T07,T09 | CRUD hoạt động, xác nhận xóa; User không thấy nút ghi |
| T11 | Khu quản trị | Frontend + Backend | T05,T09 | Chỉ Admin dùng được; trực tiếp gọi API vẫn được kiểm tra quyền |
| T12 | Script SQL backup mã hóa AES-256/certificate | Backup | T01 | Backup có checksum; file không mã hóa không được dùng; secret không commit |
| T13 | NTFS và tách certificate/khóa riêng | Backup | T12 | Principal không được cấp quyền không đọc/copy .bak; bản sao certificate độc lập; ACL cert riêng |
| T14 | Quy trình restore trên DB thử nghiệm | Backup | T12,T13 | Thiếu cert bị từ chối; sai mật khẩu import private key bị từ chối; restore hợp lệ đối chiếu dữ liệu |
| T15 | Test tích hợp Identity/CSRF/role/CRUD | QA | T04,T05,T06,T07 | Có test âm và dương; không dùng kết quả zero-test làm bằng chứng |
| T16 | Build, smoke test, review liên kết frontend/backend | Điều phối | T08–T11,T15 | Hai build thành công; luồng qua proxy và database thật được kiểm tra khi có SQL Server |
| T17 | Nghiệm thu Windows/SQL Server và hướng dẫn chạy | Điều phối + DBA | T12–T16 | Thực thi kiểm tra ACL/restore thật; phân biệt rõ passed, failed, chưa chạy |

## Thứ tự và phân công song song
Sau T01: backend T02–T07; frontend T08 rồi tích hợp T09–T11 theo API; backup T12–T14 độc lập; QA chuẩn bị T15 theo hợp đồng API. Điều phối review và hợp nhất T16–T17. Mỗi agent chỉ sửa thư mục được giao, không tự push hoặc deploy.

## Bắt buộc và tùy chọn
Bắt buộc: build hai phần; test Identity/CSRF/role; SQL schema; test chức năng với SQL Server; backup encryption, NTFS và restore trên Windows thử nghiệm. Linux cloud có thể build/test API bằng DB thay thế nhưng không chứng minh NTFS hoặc SQL Server restore. Các kiểm tra chưa chạy vẫn là việc tồn đọng.
Tùy chọn sau MVP: tìm kiếm/phân trang nâng cao, audit trail đầy đủ, MFA, phục hồi theo thời điểm, lịch backup và retention. Không đặt ước lượng ngày khi chưa đo tốc độ và có hạ tầng SQL/Windows.

## Quy tắc bảo mật backup
Dùng BACKUP DATABASE WITH ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = ...), không giả định .bak có mật khẩu. Password bảo vệ bản xuất private key, cần cho IMPORT certificate ở máy phục hồi. SQL Server service cần đọc/ghi thư mục theo tác vụ; app pool, User, Staff không được cấp quyền. Tách backup operator và restore DBA; hạn chế sysadmin/dbcreator. NTFS không ngăn được Windows administrator đã nắm quyền hệ thống. Certificate/private key giữ nơi độc lập với .bak và sao lưu an toàn. Không cấp quyền SQL backup/restore qua role Administrator của web.
