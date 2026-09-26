# Flow fix - 24/09/2026

## Staff
- Duyệt yêu cầu thuê chỉ chuyển PENDING -> APPROVED.
- Màn Hợp đồng hiển thị riêng các yêu cầu APPROVED chưa có hợp đồng với trạng thái chờ lập hợp đồng.
- Nhấn "Lập hợp đồng" trên từng yêu cầu sẽ mở form và chọn sẵn đúng mã yêu cầu.
- Hợp đồng DRAFT sau khi tạo nằm trong danh sách hợp đồng; nhân viên gửi hợp đồng từ danh sách/chi tiết.

## Customer
- "Theo dõi đơn" đổi thành "Theo dõi thuê xe".
- Trang theo dõi là màn tổng quát cho toàn bộ hành trình: Yêu cầu -> Hợp đồng -> Xác nhận -> Thanh toán -> Sẵn sàng nhận xe -> Nhận xe -> Hoàn tất.
- Khi nhân viên gửi hợp đồng (SENT), khách thấy hợp đồng ngay trên hành trình và có nút Xác nhận/Từ chối.
- Sau CUSTOMER_CONFIRMED, khách có nút Thanh toán ngay, tự điền ID hợp đồng và tổng tiền.
- Trang hiển thị các yêu cầu thuộc tài khoản hiện tại để chọn theo dõi.

## Backend limitation
Backend hiện chưa có GET /api/rentals/my hoặc GET /api/contracts/my. FE dùng quyền ownership của GET by id để dò các bản ghi thuộc khách hàng. Đủ cho demo, nhưng production nên bổ sung 2 endpoint danh sách riêng theo user.
