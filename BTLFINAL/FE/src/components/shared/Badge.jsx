const variantStyles = {
  available: "bg-emerald-50 text-emerald-700 border-emerald-200",
  reserved: "bg-amber-50 text-amber-700 border-amber-200",
  renting: "bg-blue-50 text-blue-700 border-blue-200",
  maintenance: "bg-red-50 text-red-700 border-red-200",
  inactive: "bg-gray-100 text-gray-500 border-gray-200",
  pending: "bg-amber-50 text-amber-700 border-amber-200",
  approved: "bg-emerald-50 text-emerald-700 border-emerald-200",
  rejected: "bg-red-50 text-red-700 border-red-200",
  completed: "bg-violet-50 text-violet-700 border-violet-200",
  cancelled: "bg-gray-100 text-gray-500 border-gray-200",
  in_progress: "bg-blue-50 text-blue-700 border-blue-200",
  online: "bg-cyan-50 text-cyan-700 border-cyan-200",
  offline: "bg-orange-50 text-orange-700 border-orange-200",
  draft: "bg-gray-100 text-gray-600 border-gray-200",
  sent: "bg-blue-50 text-blue-700 border-blue-200",
  paid: "bg-emerald-50 text-emerald-700 border-emerald-200",
  admin: "bg-violet-50 text-violet-700 border-violet-200",
  staff: "bg-blue-50 text-blue-700 border-blue-200",
  customer: "bg-cyan-50 text-cyan-700 border-cyan-200",
  verified: "bg-emerald-50 text-emerald-700 border-emerald-200",
  unverified: "bg-amber-50 text-amber-700 border-amber-200",
};

const labelMap = {
  available: "Có sẵn",
  reserved: "Đã đặt",
  renting: "Đang thuê",
  maintenance: "Bảo trì",
  inactive: "Ngừng HĐ",
  pending: "Chờ duyệt",
  approved: "Đã duyệt",
  rejected: "Từ chối",
  completed: "Hoàn tất",
  cancelled: "Đã hủy",
  in_progress: "Đang thuê",
  online: "ONLINE",
  offline: "OFFLINE",
  draft: "Nháp",
  sent: "Đã gửi",
  paid: "Đã thanh toán",
  admin: "Admin",
  staff: "Nhân viên",
  customer: "Khách hàng",
  verified: "Đã xác minh",
  unverified: "Chưa xác minh",
};

export default function Badge({ variant, label, className = "" }) {
  const key = variant.toLowerCase();
  const style = variantStyles[key] || "bg-gray-100 text-gray-600 border-gray-200";
  const text = label || labelMap[key] || variant;

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${style} ${className}`}>
      {text}
    </span>
  );
}
