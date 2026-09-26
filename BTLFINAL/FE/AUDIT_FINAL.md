# CARRENT PRO - Audit final

## Các lỗi trọng yếu đã sửa
- Hợp đồng: FE không còn giả định không có API danh sách hợp đồng. Dùng GET /api/contracts để đồng bộ chính xác rental -> contract, tránh hiện "Lập hợp đồng" cho yêu cầu đã có hợp đồng.
- Sau khi tạo hợp đồng, Staff Dashboard quay về danh sách hợp đồng và mở hợp đồng vừa tạo thay vì tiếp tục ở form tạo mới.
- Khách hàng có danh sách hợp đồng của chính mình; không còn phải đoán/nhập ID hợp đồng.
- Danh sách yêu cầu thuê của khách dùng GET /api/rentals theo tài khoản; loại bỏ cơ chế scan ID tuần tự.
- Staff/Admin xác minh hồ sơ: phải mở và xem CCCD trước, CCCD sau, GPLX; nút xác minh chỉ bật khi tải đủ 3 ảnh.
- Xác minh cập nhật đồng thời CCCD + GPLX.
- Payment PHI_PHAT_SINH được gắn nhãn "Quyết toán hợp đồng" trên giao diện nhân viên.
- Checkout không còn fallback sang một chiếc Mercedes hard-code khi thiếu dữ liệu xe.
- Xóa file mockData.js không được sử dụng.
- Bổ sung role guard cho Staff/Admin và checkout customer ở App.

## Backend tối thiểu cần thay
1. BtlThueXe.Core/Services/IContractService.cs
2. BtlThueXe.Infrastructure/Services/ContractService.cs
3. BtlThueXe.Api/Controllers/ContractsController.cs
4. BtlThueXe.Api/Controllers/RentalsController.cs

Mục đích: GET /api/contracts trả danh sách theo role (customer chỉ thấy của mình; staff/admin thấy danh sách), và GET /api/rentals cho phép customer/admin theo logic lọc ownership vốn đã có trong RentalService.

## Build trong môi trường tạo gói
- `src/services/api.js`: Node syntax check OK.
- Không thể chạy `npm run build` tại môi trường đóng gói vì dependency chưa có sẵn và `npm install` vượt thời gian môi trường.
- Không thể chạy `dotnet build` vì môi trường đóng gói không cài dotnet CLI.
- Cần chạy hai lệnh build trên máy Windows dự án sau khi thay file.
