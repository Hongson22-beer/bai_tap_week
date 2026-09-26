# FE BTL - kết nối backend 3d738a3

API mặc định: `http://localhost:5101/api`. Có thể đổi bằng `VITE_API_URL`.

## Đã nối API thật
- Login JWT + role KHACH_HANG/NHAN_VIEN/ADMIN; lưu token và gửi Bearer tự động.
- Vehicles: danh sách/chi tiết xe.
- Customer: tạo Rental ONLINE; tra cứu Rental theo ID; Payment; Extension; Cancellation; Evaluation.
- Staff: danh sách Rental, approve/reject; duyệt/từ chối Extension và Cancellation; tra cứu Handover/Return theo hợp đồng.
- Admin: Reports summary; Users; khóa/mở tài khoản; Vehicles; AuditLogs.
- Lỗi backend 400/401/403/404/409 được hiển thị message thật trên UI.

## Không giả lập nghiệp vụ
- Payment chuyển khoản chỉ tạo PENDING; FE không tự đổi PAID. Trạng thái PAID do webhook backend xử lý.
- Gia hạn/hủy/đánh giá gọi backend thật nên state machine/ownership vẫn do backend quyết định.
- Register/Forgot Password không giả thành công nếu chưa có endpoint backend được xác nhận.

## Chạy
Backend:
`dotnet run --project BtlThueXe.Api`

Frontend:
`npm install`
`npm run dev`

Mở URL Vite (thường `http://localhost:5173`).

## Tài khoản demo
Customer: hungtest@gmail.com / Hung@123456
Staff: staff_test@carrent.vn / Staff@123456
Admin: admin_test@carrent.vn / Admin@123456

## Ghi chú test
Trong môi trường tạo gói này không tải được npm packages từ registry nên không thể chạy Vite build tại đây. Source đã được kiểm tra đường dẫn/import và cần chạy `npm install && npm run build` trên máy có npm/network trước khi nộp.
