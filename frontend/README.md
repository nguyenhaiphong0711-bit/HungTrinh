# Giao diện Next.js

Yêu cầu Node.js 22 hoặc 24. Chạy `npm ci` trong thư mục `frontend`. Phiên bản Next.js 16.4.3, React 19.3.1 và Tailwind 4.3.3 được đối chiếu với npm registry và khóa trong `package-lock.json`.

Identity và antiforgery cookie dùng `Secure=Always`: **trình duyệt phải mở frontend bằng HTTPS**. `npm run dev` mặc định HTTP chỉ dùng kiểm tra giao diện, không phải cấu hình đăng nhập được hỗ trợ.

Trên máy phát triển đã có `mkcert` từ nguồn chính thức, tạo chứng chỉ localhost được máy và trình duyệt tin cậy (giữ private key ngoài Git):

```sh
mkcert -install
mkcert -key-file /tmp/hungtrinh-localhost-key.pem -cert-file /tmp/hungtrinh-localhost.pem localhost 127.0.0.1 ::1
BACKEND_URL=http://localhost:5000 npm run dev -- --experimental-https --experimental-https-key /tmp/hungtrinh-localhost-key.pem --experimental-https-cert /tmp/hungtrinh-localhost.pem
```

Trên Windows, thay đường dẫn `/tmp` bằng thư mục chứng chỉ cục bộ và đặt biến môi trường theo PowerShell. Nếu trình duyệt ở máy khác, chứng chỉ phải có hostname tương ứng và CA phải được tin cậy trên máy trình duyệt. Không tắt xác minh TLS để khắc phục lỗi chứng chỉ.

Backend ASP.NET cần chạy riêng với `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5000 dotnet run` trong thư mục `backend`. HTTP backend chỉ dùng cho chặng nội bộ loopback trên cùng máy; HTTPS kết thúc tại Next.js. Có thể đổi địa chỉ bằng `BACKEND_URL` (biến phía server, đọc khi build). Giao diện gọi `/api` cùng origin qua Next.js rewrite để dùng Identity cookie và CSRF.

Kiểm tra: `npm run typecheck` và `npm run build`. Dự án dùng webpack theo tùy chọn chính thức của Next.js vì Turbopack cần IPC socket mà một số sandbox không cấp quyền. Không bỏ qua TypeScript khi build.

Để chạy bản production, build với `BACKEND_URL` phù hợp rồi `npm start` phía sau reverse proxy HTTPS tin cậy. Reverse proxy phải **ghi đè** `X-Forwarded-Proto`/`X-Forwarded-For` do client gửi; ASP.NET chỉ tin proxy loopback mặc định. Không mở trực tiếp cổng backend ra Internet. Bảo vệ chặng tới backend bằng HTTPS khi không còn trên loopback. Không đưa backend URL chứa mật khẩu vào cấu hình.

Luồng kiểm tra tích hợp: đăng ký User → đăng nhập → xem danh sách; User không thấy nút sửa; Staff có thể tạo danh mục và sản phẩm; Administrator mở mục Quản trị; đăng xuất không còn xem dữ liệu. Backend phải trả 401/403 khi gọi API trái quyền kể cả khi bỏ qua giao diện.
