# HungTrinh — Quản lý sản phẩm và danh mục

Bản đầu dùng ASP.NET Core 10 MVC/C# + Identity + SQL Server, Next.js/React/TypeScript/Tailwind CSS. Next.js làm phần View; MVC controllers cung cấp JSON API. Ảnh tham khảo là template MVC, không phải mẫu thiết kế giao diện.

- [Kế hoạch, task, phụ thuộc và điều kiện nghiệm thu](docs/PROJECT_PLAN.vi.md)
- [Backend: cấu hình, khởi tạo và API](backend/README.md)
- [Frontend: chạy và build](frontend/README.md)
- [Backup mã hóa, ACL NTFS và nghiệm thu restore](ops/README.vi.md)
- [Kiểm thử tích hợp](tests/README.md)

## Phân quyền
| Vai trò | Xem sản phẩm/danh mục | CRUD sản phẩm/danh mục | Khu quản trị tài khoản |
|---|---|---|---|
| Chưa đăng nhập | Không | Không | Không |
| User | Có | Không | Không |
| Staff | Có | Có | Không |
| Administrator | Có | Có | Có |

Đăng ký luôn tạo User. Cấp quyền bằng CLI vận hành được kiểm soát, không nhận role từ form đăng ký. Administrator web không có quyền restore SQL hoặc đọc backup.

## Chạy tại máy phát triển
1. Cài .NET SDK 10, Node.js 22+ và SQL Server hỗ trợ backup encryption. Chạy lệnh từ checkout có sẵn; không cần tạo worktree.
2. Theo `backend/README.md`, cung cấp connection string qua secret store/biến môi trường; áp dụng migration và seed role bằng `--initialize`. Không có tài khoản/mật khẩu mặc định. Migration dùng danh tính triển khai riêng với tài khoản app.
3. Backend nghe HTTP nội bộ tại `http://localhost:5000` trong Development. Frontend `BACKEND_URL` trỏ địa chỉ này. Truy cập frontend bằng HTTPS để trình duyệt gửi cookie Secure; xem hướng dẫn HTTPS trong `frontend/README.md`. Production phải dùng TLS và cấu hình proxy đáng tin cậy phù hợp; không mở cổng HTTP backend ra Internet.
4. Đăng ký tài khoản, cấp quyền cần thiết bằng CLI backend rồi đăng nhập lại. Không commit connection string hoặc file backup/certificate/private key.

## Phạm vi nghiệm thu
Build và test tự động không thay thế nghiệm thu với SQL Server thực. EF InMemory không kiểm tra SQL migrations, FK hoặc SQL backup. NTFS và restore certificate cần Windows/SQL Server thử nghiệm. Xem `docs/VALIDATION.vi.md` để biết chính xác kiểm tra nào đã chạy và việc còn lại. Đây là bản nền phát triển, chưa phải hệ thống đã nghiệm thu production.
