# FE ↔ Backend mapping (24/09/2026)

Bản này được nối theo backend `btl_thue_xe(5).zip`.

## Luồng khách hàng
- Auth: `/api/Auth/login`
- Xe: `/api/Vehicles`, `/api/Vehicles/{id}`
- Lịch xe: `/api/rentals/vehicle/{idXe}/booked-periods`
- Kiểm tra khả dụng: `/api/rentals/availability`
- Tạo yêu cầu online: `/api/rentals/ONLINE`
- Theo dõi yêu cầu: `/api/rentals/{id}`
- Hồ sơ: `/api/Customers/me`
- Hợp đồng: `/api/contracts/{id}`, confirm/reject
- Thanh toán: `/api/Payments`
- Gia hạn: `/api/Extensions`
- Hủy hợp đồng: `/api/Cancellations`
- Đánh giá: `/api/Evaluations`

## Luồng nhân viên
- Dashboard/yêu cầu: `/api/rentals`
- Duyệt/từ chối: `/api/rentals/{id}/approve|reject`
- Tạo yêu cầu tại quầy: `/api/rentals/offline`
- Khách hàng: `/api/Customers`, verify CCCD
- Xe: `/api/Vehicles`
- Lập hợp đồng: `POST /api/contracts`
- Gửi hợp đồng: `PUT /api/contracts/{id}/send`
- Bàn giao: `/api/Handovers`
- Trả xe: `/api/Returns`
- Duyệt gia hạn: `/api/Extensions/{id}/process`
- Duyệt hủy: `/api/Cancellations/{id}/process`
- Thanh toán theo hợp đồng: `/api/Payments/contract/{idHopDong}`

## Lưu ý backend hiện tại
Backend chưa có endpoint `GET /api/contracts` để lấy danh sách toàn bộ hợp đồng.
Để FE vẫn hiển thị danh sách thật mà không dùng mock, bản này dò các hợp đồng qua `GET /api/contracts/{id}` và dừng sau một chuỗi ID không tồn tại. Với dữ liệu demo nhỏ hiện tại cách này chạy được, nhưng bản production nên bổ sung endpoint danh sách hợp đồng có phân trang.

Không còn sử dụng `mockData.js` trong các màn nghiệp vụ chính.
