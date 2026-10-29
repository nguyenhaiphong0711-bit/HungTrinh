# Kết quả kiểm tra bản đầu

Môi trường chạy: Debian Linux, .NET SDK 10.0.403/runtime 10.0.14 và Node 24.19.0. Không có SQL Server hoặc Windows NTFS trong môi trường kiểm tra này.

## Đã chạy
- Backend `dotnet build`: thành công, 0 warning, 0 error.
- NuGet audit bao gồm dependency gián tiếp: không phát hiện package có lỗ hổng tại thời điểm chạy. Đã cập nhật dependency Pkcs được cảnh báo ở lần restore đầu.
- Tạo migration `InitialCreate` cho SQL Server thành công. Tạo migration không có nghĩa là đã áp dụng vào SQL Server thật.
- Frontend `npm run build` bằng webpack: thành công. Turbopack bị giới hạn IPC của môi trường nên dùng tùy chọn webpack chính thức.
- Frontend `npm run typecheck`: thành công.
- Frontend production HTTP: trả 200 và có giao diện đăng nhập trong HTML. Đây không phải kiểm tra đăng nhập bằng trình duyệt.
- Backend Kestrel smoke: `/health/live` 200; khách gọi `/api/products` và `/api/admin/users` nhận 401. Backend chưa có SQL Server để kiểm tra readiness database.
- Bộ test Identity dùng TestServer và EF InMemory cuối cùng: 16 đạt, 0 thất bại, 0 bỏ qua. Bao gồm role matrix, CSRF, chống tự chọn role đăng ký, CRUD, dữ liệu không hợp lệ, sai mật khẩu và hủy phiên sau logout; proxy loopback được tin cậy và IP không tin cậy.

- Đã sửa lỗi CSRF phía backend khi TLS kết thúc ở Next.js: middleware chỉ nhận forwarded headers từ proxy loopback mặc định. Smoke qua frontend trả `/api/auth/csrf` 200, có token và cookie Secure; `/api/admin/users` 401 cho khách. Smoke dùng header mô phỏng TLS termination, chưa phải trình duyệt HTTPS. Edge proxy production phải ghi đè forwarded headers do client gửi.
- `npm audit --omit=dev --audit-level=moderate`: 0 vulnerabilities tại thời điểm chạy.

## Chưa nghiệm thu
- Migration, CRUD và Identity trên SQL Server thực.
- Luồng đăng ký/đăng nhập/CRUD hoàn chỉnh bằng trình duyệt qua HTTPS và SQL Server thật.
- Các BK-01 đến BK-08 trong `ops/README.vi.md`: backup AES-256, NTFS không đọc/copy trái phép, thiếu certificate/sai mật khẩu private key, restore hợp lệ và sử dụng lại dữ liệu.
- Deploy production, audit vận hành, retention và kiểm tra snapshot môi trường mới.

## Bước tiếp theo
Chuẩn bị SQL Server hỗ trợ encrypted backup và một Windows test host; cung cấp connection string qua secret store. Áp dụng migration/seed bằng danh tính triển khai, chạy app bằng danh tính quyền tối thiểu, sau đó thực hiện smoke test và BK-01..08. Không đánh dấu hoàn tất nghiệm thu cho đến khi có kết quả thực tế. Không cần gửi mật khẩu hoặc khóa riêng qua chat.
