# Backend ASP.NET Core MVC

ASP.NET Core 10 MVC controllers cung cấp JSON API cho giao diện Next.js. Identity dùng EF Core và SQL Server; Razor không tham gia render giao diện chính.

## Cấu hình

- Cài .NET SDK 10 và SQL Server có hỗ trợ backup encryption (xem `ops/`).
- Cung cấp `ConnectionStrings__DefaultConnection` bằng secret store hoặc biến môi trường. Dùng TLS hợp lệ (`Encrypt=True;TrustServerCertificate=False`), không commit mật khẩu.
- Khi triển khai trên hostname khác localhost, cấu hình `AllowedHosts` thành hostname thực tế.
- Tài khoản SQL ứng dụng chỉ có quyền dữ liệu/schema cần thiết; không dùng tài khoản restore hoặc sysadmin.

## Khởi tạo lần đầu

Chạy trong thư mục `backend`:

```sh
dotnet restore
dotnet tool install --tool-path ../.tools dotnet-ef --version 10.0.14
# InitialCreate đã nằm trong repository; review migration trước khi áp dụng.
dotnet run -- --initialize
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5000
```

Lệnh HTTP trên chỉ phục vụ backend nội bộ trong Development, phía trình duyệt vẫn dùng Next.js HTTPS cùng origin để cookie Secure hoạt động. Production dùng reverse proxy TLS có chứng chỉ hợp lệ và backend HTTPS hoặc forwarded headers được cấu hình chỉ tin proxy cụ thể; không tắt TLS verification. Chỉ chạy migration bằng danh tính được phép thay đổi schema; tài khoản chạy ứng dụng thường ngày không cần quyền đó.

Sau khi đăng ký tài khoản đầu tiên, operator có thể dừng app và chạy `dotnet run -- --promote-admin` với `Bootstrap__AdminEmail` là email tài khoản đã đăng ký. Không cung cấp API công khai chọn role và không có mật khẩu quản trị mặc định. Khởi động lại app sau thao tác này, rồi đăng xuất/đăng nhập để nhận role mới. Role Staff có thể cấp qua công cụ quản trị Identity được operator kiểm soát; giao diện quản trị hiện chỉ liệt kê người dùng.

Để cấp Staff cho tài khoản đã đăng ký, operator có thể chạy `dotnet run -- --assign-role` với `Bootstrap__AdminEmail` và `Bootstrap__Role=Staff`. Lệnh chỉ chấp nhận Administrator, Staff, User, bổ sung role và không thu hồi role hiện tại. Đăng nhập lại để cập nhật cookie.

## API và bảo mật

- `GET /api/auth/csrf` trả `{token}` và cookie antiforgery. Gửi `X-CSRF-TOKEN` với mọi POST/PUT/DELETE, kể cả login/register. Lấy token mới sau login/logout vì danh tính đã thay đổi.
- `POST /api/auth/register`, `POST /api/auth/login`: `{email,password}`. Password tối thiểu 12 ký tự có hoa/thường/số/ký tự đặc biệt. Đăng ký luôn nhận role User.
- `POST /api/auth/logout`; `GET /api/auth/me`: `{id,email,roles}`.
- `GET /api/products`, `GET /api/categories`: yêu cầu đăng nhập. POST, PUT `/{id}`, DELETE `/{id}` chỉ Administrator/Staff.
- Product body: `{name,price,categoryId,stock?}`. Category body: `{name}`. Category đang có sản phẩm không được xóa.
- `GET /api/admin/users`: chỉ Administrator (MVC Area `Administrator`).
- `GET /health/live`: tiến trình; `GET /health/ready`: kết nối SQL Server.

Cookie HttpOnly/Secure/SameSite=Lax. Ưu tiên Next.js proxy cùng origin, dùng HTTPS; backend không bật CORS rộng. Persist ASP.NET Data Protection keys bên ngoài container bằng cơ chế lưu trữ có mã hóa/phân quyền nếu triển khai nhiều instance hoặc cần duy trì cookie sau restart.

Backend xử lý `X-Forwarded-Proto` và `X-Forwarded-For` với giới hạn một proxy, chỉ tin các địa chỉ loopback mặc định của ASP.NET Core. Next.js HTTPS chạy cùng máy được nhận diện đúng là HTTPS dù kết nối nội bộ tới backend là HTTP. Production phải cấu hình địa chỉ proxy đáng tin cậy cụ thể và đúng số hop; không xóa danh sách trust để tin mọi nguồn. Reverse proxy public phải ghi đè header forwarding do client gửi và backend nội bộ phải được giới hạn truy cập. Request HTTP không qua proxy HTTPS sẽ không được phát cookie antiforgery Secure.

## Giới hạn hiện tại

Đây là bản nền để phát triển. Phân trang catalog, audit log, email xác thực/phục hồi mật khẩu, UI cấp/thu hồi role và khóa tài khoản cần task tiếp theo trước khi đưa lên production. Backup/restore là tác vụ vận hành riêng, không được cấp cho tài khoản web thông thường.
