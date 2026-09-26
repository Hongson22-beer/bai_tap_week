import { useEffect, useState } from "react";
import { ChevronLeft, Star, Users, Fuel, Settings, Calendar, MapPin, Shield, ChevronRight, Heart } from "lucide-react";
import Badge from "../../components/shared/Badge";
import { evaluationsApi } from "../../services/api";

export default function CarDetailPage({ car, onNavigate }) {
  const [activeImg, setActiveImg] = useState(0);
  const [pickupDate, setPickupDate] = useState("");
  const [returnDate, setReturnDate] = useState("");
  const [pickupTime, setPickupTime] = useState("09:00");
  const [returnTime, setReturnTime] = useState("09:00");
  const [liked, setLiked] = useState(false);
  const [vehicleReviews, setVehicleReviews] = useState([]);
  const [reviewsLoading, setReviewsLoading] = useState(true);

  useEffect(() => {
    let alive = true;
    const idXe = Number(car?.id);
    if (!idXe) { setVehicleReviews([]); setReviewsLoading(false); return; }
    setReviewsLoading(true);
    evaluationsApi.byVehicle(idXe)
      .then(data => { if (alive) setVehicleReviews(Array.isArray(data) ? data : (data?.items || data?.data || [])); })
      .catch(() => { if (alive) setVehicleReviews([]); })
      .finally(() => { if (alive) setReviewsLoading(false); });
    return () => { alive = false; };
  }, [car?.id]);

  // Dữ liệu xe từ backend không bắt buộc có gallery/đánh giá/năm/màu.
  // Dùng fallback để trang chi tiết không bị crash khi các field này thiếu.
  const images = Array.isArray(car?.images) ? car.images : [];
  const allImages = [car?.image, ...images.filter(i => i && i !== car?.image)].filter(Boolean);
  const status = String(car?.status || "AVAILABLE");
  const reviews = vehicleReviews.length;
  const rating = reviews ? vehicleReviews.reduce((sum, r) => sum + Number(r.diemDanhGia || 0), 0) / reviews : 0;
  const pricePerDay = Number(car?.pricePerDay ?? 0);

  const days = pickupDate && returnDate
    ? Math.max(1, Math.ceil((new Date(returnDate).getTime() - new Date(pickupDate).getTime()) / 86400000))
    : 0;
  const rentalFee = days * pricePerDay;
  const deposit = Math.round(rentalFee * 0.30);
  const total = rentalFee;

  return (
    <div className="cp-customer cp-car-detail min-h-screen bg-[#F6F8FB]">
      {/* Top bar */}
      <div className="sticky top-0 z-40 bg-white border-b border-gray-100 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-14 flex items-center gap-3">
          <button onClick={() => onNavigate("browse")} className="flex items-center gap-1.5 text-gray-500 hover:text-[#0D1B3E] text-sm font-medium transition-colors">
            <ChevronLeft size={18} /> Danh sách xe
          </button>
          <ChevronRight size={14} className="text-gray-300" />
          <span className="text-sm text-gray-700 font-medium truncate">{car?.name || "Chi tiết xe"}</span>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 py-8">
        <div className="grid lg:grid-cols-2 gap-8">
          {/* Left: Gallery */}
          <div>
            <div className="relative rounded-2xl overflow-hidden h-72 lg:h-96 bg-gray-200">
              {allImages[activeImg] ? <img src={allImages[activeImg]} alt={car?.name || "Xe"} className="w-full h-full object-cover" /> : <div className="w-full h-full flex items-center justify-center text-gray-400">Chưa có ảnh xe</div>}
              <div className="absolute top-4 left-4">
                <Badge variant={status.toLowerCase()} />
              </div>
              <button
                onClick={() => setLiked(!liked)}
                className={`absolute top-4 right-4 w-10 h-10 rounded-full bg-white/90 backdrop-blur-sm flex items-center justify-center shadow-md transition-colors ${liked ? "text-red-500" : "text-gray-400"}`}
              >
                <Heart size={18} className={liked ? "fill-red-500" : ""} />
              </button>
            </div>
            {allImages.length > 1 && (
              <div className="flex gap-3 mt-3">
                {allImages.map((img, i) => (
                  <button
                    key={i}
                    onClick={() => setActiveImg(i)}
                    className={`w-20 h-16 rounded-xl overflow-hidden border-2 transition-all ${activeImg === i ? "border-[#00B4D8]" : "border-transparent"}`}
                  >
                    <img src={img} alt="" className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>
            )}

            {/* Car info */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 mt-5">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h1 className="text-2xl font-bold text-[#0D1B3E]">{car?.name || "Xe"}</h1>
                  <p className="text-gray-500 mt-1">{car?.brand || "—"} · {car?.type || "—"}</p>
                </div>
                <div className="text-right">
                  <div className="text-3xl font-bold text-[#0D1B3E]">{(pricePerDay / 1000).toLocaleString()}K</div>
                  <div className="text-gray-400 text-sm">/ngày</div>
                </div>
              </div>

              <div className="flex items-center gap-2 mb-5">
                <div className="flex items-center gap-1">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <Star key={i} size={14} className={i < Math.floor(rating) ? "fill-amber-400 text-amber-400" : "text-gray-200 fill-gray-200"} />
                  ))}
                </div>
                <span className="font-semibold text-[#0D1B3E] text-sm">{rating || "Chưa có"}</span>
                <span className="text-gray-400 text-sm">({reviews} đánh giá)</span>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                {[
                  { icon: Users, label: "Số chỗ", value: `${car?.seats ?? "—"} chỗ` },
                  { icon: Settings, label: "Hộp số", value: car?.transmission || "—" },
                  { icon: Fuel, label: "Nhiên liệu", value: car?.fuel || "—" },
                  { icon: Calendar, label: "Năm SX", value: car?.year ? String(car.year) : "Chưa cập nhật" },
                  { icon: Shield, label: "Màu xe", value: car?.color || "Chưa cập nhật" },
                  { icon: MapPin, label: "Biển số", value: car?.plate || "Chưa cập nhật" },
                ].map((item) => (
                  <div key={item.label} className="bg-[#F6F8FB] rounded-xl p-3">
                    <div className="flex items-center gap-1.5 text-gray-400 text-xs mb-1">
                      <item.icon size={12} />{item.label}
                    </div>
                    <div className="font-semibold text-[#0D1B3E] text-sm">{item.value}</div>
                  </div>
                ))}
              </div>

              <div className="mt-5 pt-5 border-t border-gray-100">
                <h3 className="font-bold text-[#0D1B3E] mb-3">Mô tả xe</h3>
                <p className="text-gray-600 text-sm leading-relaxed">{car?.description || "Thông tin mô tả xe đang được cập nhật."}</p>
              </div>
            </div>

            {/* Policy */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 mt-4">
              <h3 className="font-bold text-[#0D1B3E] mb-4">Chính sách thuê xe</h3>
              <div className="space-y-3 text-sm text-gray-600">
                {[
                  "Người thuê phải có CCCD/CMND hợp lệ và đủ 18 tuổi",
                  "Tiền cọc bằng 30% giá trị tiền thuê cơ bản; thanh toán sau khi xác nhận hợp đồng",
                  "Phí phát sinh nếu trả xe trễ: 10% giá thuê/ngày/giờ",
                  "Xe phải được trả trong tình trạng sạch sẽ, nguyên vẹn",
                  "Bồi thường 100% nếu xe bị hư hỏng do lỗi người thuê",
                ].map((p, i) => (
                  <div key={i} className="flex gap-2">
                    <span className="text-[#00B4D8] font-bold shrink-0">•</span>
                    <span>{p}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Reviews thật từ backend */}
            <div className="bg-white rounded-2xl border border-gray-100 p-6 mt-4">
              <div className="flex items-center justify-between gap-3 mb-4">
                <h3 className="font-bold text-[#0D1B3E]">Đánh giá từ khách hàng</h3>
                <span className="text-xs font-semibold text-gray-400">{reviews} đánh giá đã xác thực</span>
              </div>
              {reviewsLoading ? (
                <div className="py-8 text-center text-sm text-gray-400">Đang tải đánh giá...</div>
              ) : vehicleReviews.length === 0 ? (
                <div className="py-8 text-center text-sm text-gray-400">Xe này chưa có đánh giá từ hợp đồng đã hoàn tất.</div>
              ) : (
                <div className="space-y-4">
                  {vehicleReviews.map((r) => {
                    const name = r.tenKhachHang || `Khách hàng #${r.idKhachHang}`;
                    return (
                      <div key={r.id} className="border-b border-gray-100 pb-4 last:border-0 last:pb-0">
                        <div className="flex items-center justify-between gap-3 mb-2">
                          <div className="flex items-center gap-2">
                            <div className="w-8 h-8 rounded-full bg-[#0D1B3E] flex items-center justify-center text-white text-xs font-bold">{name.charAt(0).toUpperCase()}</div>
                            <div>
                              <div className="font-semibold text-[#0D1B3E] text-sm">{name}</div>
                              <div className="text-gray-400 text-xs">Đã thuê xe · {new Date(r.thoiGianTao).toLocaleDateString("vi-VN")}</div>
                            </div>
                          </div>
                          <div className="flex items-center gap-0.5">
                            {Array.from({ length: 5 }).map((_, i) => (
                              <Star key={i} size={13} className={i < Number(r.diemDanhGia) ? "fill-amber-400 text-amber-400" : "fill-gray-100 text-gray-200"} />
                            ))}
                          </div>
                        </div>
                        <p className="text-gray-600 text-sm whitespace-pre-wrap">{r.nhanXet || "Khách hàng không để lại nhận xét."}</p>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>

          {/* Right: Booking panel */}
          <div className="lg:sticky lg:top-20 lg:self-start">
            <div className="bg-white rounded-2xl border border-gray-100 p-6 shadow-sm">
              <h2 className="font-bold text-[#0D1B3E] text-lg mb-5">Đặt xe ngay</h2>

              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold text-gray-500 mb-1.5">NGÀY NHẬN</label>
                    <input
                      type="date"
                      value={pickupDate}
                      onChange={(e) => setPickupDate(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-colors"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-gray-500 mb-1.5">GIỜ NHẬN</label>
                    <input
                      type="time"
                      value={pickupTime}
                      onChange={(e) => setPickupTime(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-colors"
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold text-gray-500 mb-1.5">NGÀY TRẢ</label>
                    <input
                      type="date"
                      value={returnDate}
                      onChange={(e) => setReturnDate(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-colors"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-gray-500 mb-1.5">GIỜ TRẢ</label>
                    <input
                      type="time"
                      value={returnTime}
                      onChange={(e) => setReturnTime(e.target.value)}
                      className="w-full px-3 py-2.5 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-colors"
                    />
                  </div>
                </div>
              </div>

              {days > 0 && (
                <div className="mt-5 bg-[#F6F8FB] rounded-xl p-4 space-y-3">
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Số ngày thuê</span>
                    <span className="font-semibold text-[#0D1B3E]">{days} ngày</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Tiền thuê ({(pricePerDay / 1000).toLocaleString()}K × {days})</span>
                    <span className="font-semibold text-[#0D1B3E]">{(rentalFee / 1000).toLocaleString()}K</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Tiền cọc (30%)</span>
                    <span className="font-semibold text-[#0D1B3E]">{(deposit / 1000).toLocaleString()}K</span>
                  </div>
                  <div className="flex justify-between text-sm font-bold border-t border-gray-200 pt-3 mt-1">
                    <span className="text-[#0D1B3E]">Giá trị thuê cơ bản</span>
                    <span className="text-[#00B4D8] text-base">{(total / 1000).toLocaleString()}K</span>
                  </div>
                </div>
              )}

              {status === "AVAILABLE" ? (
                <button
                  onClick={() => onNavigate("checkout", { ...car, pickupDate, returnDate, pickupTime, returnTime })}
                  className="w-full mt-5 bg-[#0D1B3E] text-white py-4 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] transition-colors"
                >
                  Thuê xe ngay
                </button>
              ) : (
                <div className="w-full mt-5 bg-gray-100 text-gray-400 py-4 rounded-xl font-bold text-sm text-center">
                  Xe không khả dụng
                </div>
              )}

              <div className="mt-4 flex items-center gap-2 text-xs text-gray-400">
                <Shield size={12} />
                <span>Xe được bảo hiểm đầy đủ theo quy định pháp luật</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
