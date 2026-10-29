# Kiểm thử tích hợp

Chạy từ thư mục gốc repository:

```sh
dotnet test tests/HungTrinh.IntegrationTests/HungTrinh.IntegrationTests.csproj
```

Suite sử dụng ASP.NET Core TestServer, Identity thật, cookie và antiforgery thật; thay SQL Server bằng EF Core InMemory trong môi trường test. Có 16 test cases: ma trận truy cập Administrator (4), đăng ký không tự nâng quyền (1), CSRF thiếu/sai (1), quyền đọc/ghi và CRUD đầy đủ sản phẩm/danh mục (3), khách chưa đăng nhập (1), mật khẩu sai/đăng xuất và hủy phiên (1), giá hoặc danh mục sản phẩm không hợp lệ (3), chỉ tin cậy HTTPS forwarded từ proxy loopback (2). Một test có thể kiểm tra nhiều tình huống liên tiếp.

InMemory không xác nhận migration SQL Server, khóa ngoại hay tính đồng thời của SQL Server. Các kiểm tra backup/restore/certificate và NTFS bắt buộc chạy riêng trên SQL Server/Windows theo runbook trong `ops`; suite này không thay thế các kiểm tra đó.
