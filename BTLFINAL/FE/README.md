# Car Rental — Giao diện JavaScript (chuyển đổi từ Figma Make TSX)

Đây là bản chuyển đổi **Nhóm A** (giao diện đang thực sự chạy trong `App.tsx` gốc) từ
React + TypeScript sang **React + JavaScript thuần (.jsx/.js)**, giữ nguyên gần như 100%
giao diện, layout, màu sắc, spacing so với source Figma Make ban đầu.

Đã build-test thành công bằng Vite (`npm run build` → 2398 module transformed, không lỗi)
và chạy thử bằng `npm run preview` (HTTP 200, render đúng).

## 1. Cấu trúc thư mục

```
src/
├── App.jsx                     # Root — điều hướng bằng useState (giữ đúng cơ chế gốc)
├── main.jsx                    # Entry point
├── index.css                   # Tailwind v4 + theme màu navy/cyan (copy nguyên vẹn)
├── data/
│   └── mockData.js             # Dữ liệu mẫu (cars, brands, rentalRequests, revenue...)
├── components/
│   └── shared/
│       ├── Badge.jsx
│       └── Toast.jsx
└── pages/
    ├── customer/                # 12 trang: Landing, BrowseCars, CarDetail, Checkout,
    │                             #   Tracking, Payment, RentalHistory, Profile, Contract,
    │                             #   ExtendRental, CancelContract, HandoverTracking, Review
    ├── auth/                     # Login, Register, ForgotPassword
    ├── staff/                    # StaffDashboard + StaffOfflineCreate + StaffContractCreate
    │                             #   + StaffHandoverReturn (nhúng bên trong StaffDashboard)
    └── admin/                    # AdminDashboard (1 file monolithic, tự quản lý điều hướng nội bộ)
```

> Lưu ý: các trang `customer`/`auth` vốn nằm phẳng trong `pages/` ở source gốc — tôi đã
> sắp xếp lại vào thư mục con theo đúng cấu trúc bạn yêu cầu, **không thay đổi bất kỳ
> JSX/style/logic nào**, chỉ cập nhật lại đường dẫn import cho khớp vị trí mới.

## 2. Dependency cần cài trong dự án của bạn

```bash
npm install lucide-react recharts
```

Nếu dự án hiện tại **chưa dùng Tailwind CSS v4**, cần cài thêm:

```bash
npm install -D tailwindcss @tailwindcss/vite
```

và thêm plugin vào `vite.config.js`:

```js
import tailwindcss from "@tailwindcss/vite";
export default defineConfig({
  plugins: [react(), tailwindcss()],
});
```

`index.css` dùng cú pháp Tailwind v4 (`@import "tailwindcss"` + `@theme inline`) — **không
tương thích trực tiếp** với Tailwind v3 (`@tailwind base/components/utilities`). Nếu dự án
bạn đang ở v3, cần nâng cấp lên v4 hoặc tôi có thể viết lại `index.css` sang cú pháp v3
nếu bạn cần.

## 3. Cách tích hợp vào dự án hiện tại

1. Copy toàn bộ thư mục `src/` này đè/merge vào `src/` của dự án bạn (nếu dự án đã có
   file `App.jsx`/`main.jsx` riêng, cần merge thủ công thay vì ghi đè).
2. Cài 2 dependency `lucide-react`, `recharts` (bắt buộc — toàn bộ icon và biểu đồ dùng 2 lib này).
3. Đảm bảo Tailwind v4 đã cấu hình đúng như mục 2.
4. Chạy `npm run dev` và kiểm tra lại từng trang.

## 4. Về routing

Bản này giữ nguyên **đúng cơ chế điều hướng gốc** của Nhóm A: một state `page` trong
`App.jsx`, chuyển trang bằng cách gọi `onNavigate("tên-trang", data)` — **không dùng
react-router**. Đây là lựa chọn có chủ đích để giữ đúng 100% hành vi/giao diện source gốc
theo yêu cầu của bạn. Nếu sau này bạn muốn chuyển sang `react-router-dom` để có URL rõ ràng
và không mất state khi refresh, tôi có thể hỗ trợ riêng — nhưng việc đó sẽ đổi cách các
trang nhận props điều hướng (từ `onNavigate` sang `useNavigate()`), nên cần làm ở bước sau,
tách biệt với việc chuyển đổi ngôn ngữ này.

## 5. Việc chưa làm (do bạn chọn chỉ giữ Nhóm A)

Source Figma Make gốc còn có **Nhóm B** — một bộ giao diện khác dùng `react-router` +
`components/ui/` (Button, Card, Modal, Table, Tabs...) + `CustomerLayout`/`DashboardLayout`
cho cùng các tính năng (Browse Cars, Login, Admin Users/Vehicles/AuditLog, Staff
RentalRequests/RequestDetail). Bộ này **không được đưa vào bản chuyển đổi này** theo đúng
lựa chọn của bạn. Nếu sau này cần dùng đến, cho tôi biết — mã nguồn TSX gốc vẫn còn trong
file `src (1).zip` bạn đã upload.
