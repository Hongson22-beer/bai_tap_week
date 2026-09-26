import { useEffect, useState } from "react";
import { ChevronLeft, CheckCircle, Car, User, CreditCard, FileText, Clock, MapPin, Upload, Camera, X, Eye } from "lucide-react";
import { rentalsApi, customersApi } from "../../services/api";
import VehicleAvailabilityCalendar from "../../components/shared/VehicleAvailabilityCalendar";

const steps = [
  { id: 1, label: "Thời gian", icon: Clock },
  { id: 2, label: "Thông tin", icon: User },
  { id: 3, label: "Xác minh", icon: FileText },
  { id: 4, label: "Xác nhận", icon: CheckCircle },
];

export default function CheckoutPage({ car, onNavigate }) {
  const [step, setStep] = useState(1);
  const [form, setForm] = useState({
    pickupDate: car?.pickupDate || "", returnDate: car?.returnDate || "", pickupTime: car?.pickupTime || "09:00", returnTime: car?.returnTime || "09:00",
    pickupMethod: "TAI_CUA_HANG", deliveryAddress: "", deliveryNote: "",
    fullName: "", phone: "", email: "", address: "",
    cccd: "", cccdName: "", dob: "", cccdAddress: "",
  });
  const [step1Error, setStep1Error] = useState("");
  const [checkingAvailability, setCheckingAvailability] = useState(false);
  const [availabilityOk, setAvailabilityOk] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const [createdRental, setCreatedRental] = useState(null);
  const [profile, setProfile] = useState(null);
  const [profileLoading, setProfileLoading] = useState(true);
  const [profileError, setProfileError] = useState("");

  const update = (k, v) => {
    setForm(prev => ({ ...prev, [k]: v }));
    if (["pickupDate", "returnDate", "pickupTime", "returnTime"].includes(k)) {
      setAvailabilityOk(false);
      setStep1Error("");
    }
  };

  useEffect(() => {
    let alive = true;
    customersApi.me()
      .then(p => {
        if (!alive) return;
        setProfile(p);
        setForm(prev => ({
          ...prev,
          fullName: p?.hoTen || prev.fullName,
          phone: p?.soDienThoai || prev.phone,
          email: p?.email || prev.email,
          address: p?.diaChi || prev.address,
          cccd: p?.soCccd || "",
          cccdName: p?.hoTen || "",
          dob: p?.ngaySinh ? String(p.ngaySinh).slice(0, 10) : "",
          cccdAddress: p?.diaChi || "",
        }));
      })
      .catch(e => alive && setProfileError(e.message || "Không tải được hồ sơ khách hàng."))
      .finally(() => alive && setProfileLoading(false));
    return () => { alive = false; };
  }, []);

  const startAt = form.pickupDate ? new Date(`${form.pickupDate}T${form.pickupTime || "00:00"}:00`) : null;
  const endAt = form.returnDate ? new Date(`${form.returnDate}T${form.returnTime || "00:00"}:00`) : null;
  const days = startAt && endAt && endAt > startAt
    ? Math.max(1, Math.ceil((endAt.getTime() - startAt.getTime()) / 86400000))
    : 3;
  const rentalFee = days * car.pricePerDay;
  const deposit = Math.round(rentalFee * 0.30);
  // Tiền cọc là khoản trả trước, không cộng thêm vào giá trị tiền thuê.
  const total = rentalFee;

  const profileVerified = !!profile?.hoSoDaXacMinh;

  const submitRental = async () => {
    setSubmitError(""); setSubmitting(true);
    try {
      if (form.pickupMethod === "GIAO_TAN_NOI" && (!form.fullName.trim() || !form.phone.trim())) { setSubmitError("Giao tận nơi cần họ tên và số điện thoại người nhận."); setSubmitting(false); return; }
      const thoiGianNhan = new Date(`${form.pickupDate}T${form.pickupTime}:00`).toISOString();
      const thoiGianTraDuKien = new Date(`${form.returnDate}T${form.returnTime}:00`).toISOString();
      const result = await rentalsApi.createOnline({
        idXe: Number(car.id), thoiGianNhan, thoiGianTraDuKien,
        hinhThucNhanXe: form.pickupMethod,
        tenNguoiNhan: form.pickupMethod === "GIAO_TAN_NOI" ? form.fullName : null,
        soDienThoaiNhan: form.pickupMethod === "GIAO_TAN_NOI" ? form.phone : null,
        diaChiGiaoXe: form.pickupMethod === "GIAO_TAN_NOI" ? form.deliveryAddress : null,
        ghiChuGiaoXe: form.deliveryNote || null
      });
      setCreatedRental(result); setStep(5);
    } catch (e) { setSubmitError(e.message || "Không thể gửi yêu cầu thuê xe"); }
    finally { setSubmitting(false); }
  };

  const inputCls = "w-full px-3.5 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/10 transition-all";
  const labelCls = "block text-xs font-semibold text-gray-500 mb-1.5";

  if (step === 5) {
    return (
      <div className="min-h-screen bg-[#F6F8FB] flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl border border-gray-100 p-8 max-w-md w-full text-center shadow-sm">
          <div className="w-20 h-20 bg-emerald-50 rounded-full flex items-center justify-center mx-auto mb-5">
            <CheckCircle size={40} className="text-emerald-500" />
          </div>
          <h2 className="text-2xl font-bold text-[#0D1B3E] mb-2">Yêu cầu đã gửi thành công!</h2>
          <p className="text-gray-500 text-sm mb-6">Nhân viên sẽ xem xét và phản hồi trong vòng 1–2 giờ làm việc.</p>
          <div className="bg-[#F6F8FB] rounded-xl p-4 text-left space-y-2 mb-6">
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Mã yêu cầu</span>
              <span className="font-bold text-[#0D1B3E]">{createdRental?.maYeuCau || createdRental?.id || "Đã tạo"}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Trạng thái</span>
              <span className="text-amber-600 font-semibold bg-amber-50 px-2 py-0.5 rounded-full text-xs">PENDING</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Nguồn</span>
              <span className="font-semibold text-cyan-700">ONLINE</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Thời gian gửi</span>
              <span className="font-semibold text-[#0D1B3E]">{new Date().toLocaleString("vi-VN")}</span>
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => onNavigate("tracking", createdRental)} className="flex-1 bg-[#0D1B3E] text-white py-3 rounded-xl font-semibold text-sm hover:bg-[#1A3A6E] transition-colors">
              Theo dõi yêu cầu
            </button>
            <button onClick={() => onNavigate("landing")} className="flex-1 border border-gray-200 text-gray-600 py-3 rounded-xl font-semibold text-sm hover:bg-gray-50 transition-colors">
              Về trang chủ
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <>
    <div className="min-h-screen bg-[#F6F8FB]">
      {/* Top */}
      <div className="sticky top-0 z-40 bg-white border-b border-gray-100 shadow-sm">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 h-14 flex items-center gap-3">
          <button onClick={() => step > 1 ? setStep(step - 1) : onNavigate("browse")} className="flex items-center gap-1.5 text-gray-500 hover:text-[#0D1B3E] text-sm font-medium transition-colors">
            <ChevronLeft size={18} /> {step > 1 ? "Quay lại" : "Danh sách xe"}
          </button>
          <div className="flex-1 flex items-center justify-center gap-2">
            {steps.map((s, i) => (
              <div key={s.id} className="flex items-center gap-2">
                <div className={`flex items-center gap-1.5 text-xs font-semibold transition-colors ${s.id <= step ? "text-[#0D1B3E]" : "text-gray-300"}`}>
                  <div className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold transition-colors ${s.id < step ? "bg-emerald-500 text-white" : s.id === step ? "bg-[#0D1B3E] text-white" : "bg-gray-200 text-gray-400"}`}>
                    {s.id < step ? <CheckCircle size={12} /> : s.id}
                  </div>
                  <span className="hidden sm:inline">{s.label}</span>
                </div>
                {i < steps.length - 1 && <div className={`w-8 h-px ${s.id < step ? "bg-emerald-400" : "bg-gray-200"}`} />}
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-4 sm:px-6 py-8">
        <div className="grid lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            {/* Step 1 */}
            {step === 1 && (
              <div className="bg-white rounded-2xl border border-gray-100 p-6">
                <h2 className="text-xl font-bold text-[#0D1B3E] mb-6 flex items-center gap-2"><Clock size={20} className="text-[#00B4D8]" /> Chọn thời gian thuê</h2>
                <VehicleAvailabilityCalendar vehicleId={car?.id} />
                <div className="grid sm:grid-cols-2 gap-4">
                  <div>
                    <label className={labelCls}>NGÀY NHẬN XE</label>
                    <input type="date" value={form.pickupDate} onChange={e => update("pickupDate", e.target.value)} className={inputCls} />
                  </div>
                  <div>
                    <label className={labelCls}>GIỜ NHẬN XE</label>
                    <input type="time" value={form.pickupTime} onChange={e => update("pickupTime", e.target.value)} className={inputCls} />
                  </div>
                  <div>
                    <label className={labelCls}>NGÀY TRẢ XE</label>
                    <input type="date" value={form.returnDate} onChange={e => update("returnDate", e.target.value)} className={inputCls} />
                  </div>
                  <div>
                    <label className={labelCls}>GIỜ TRẢ XE</label>
                    <input type="time" value={form.returnTime} onChange={e => update("returnTime", e.target.value)} className={inputCls} />
                  </div>
                </div>
                <div className="mt-5">
                  <label className={labelCls}>HÌNH THỨC NHẬN XE</label>
                  <div className="grid sm:grid-cols-2 gap-3">
                    <button type="button" onClick={() => update("pickupMethod", "TAI_CUA_HANG")} className={`p-4 rounded-xl border-2 text-left ${form.pickupMethod === "TAI_CUA_HANG" ? "border-[#00B4D8] bg-cyan-50" : "border-gray-200"}`}>
                      <div className="font-bold text-[#0D1B3E]">Nhận tại cửa hàng</div><div className="text-xs text-gray-500 mt-1">Đến điểm giao xe của CARRENT PRO để nhận xe.</div>
                    </button>
                    <button type="button" onClick={() => update("pickupMethod", "GIAO_TAN_NOI")} className={`p-4 rounded-xl border-2 text-left ${form.pickupMethod === "GIAO_TAN_NOI" ? "border-[#00B4D8] bg-cyan-50" : "border-gray-200"}`}>
                      <div className="font-bold text-[#0D1B3E]">Giao xe tận nơi</div><div className="text-xs text-gray-500 mt-1">Nhân viên giao xe tới địa chỉ bạn cung cấp.</div>
                    </button>
                  </div>
                  {form.pickupMethod === "GIAO_TAN_NOI" && <div className="mt-4 space-y-3">
                    <div className="relative"><MapPin size={15} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400"/><input value={form.deliveryAddress} onChange={e=>update("deliveryAddress",e.target.value)} placeholder="Địa chỉ giao xe *" className={`${inputCls} pl-9`}/></div>
                    <textarea value={form.deliveryNote} onChange={e=>update("deliveryNote",e.target.value)} placeholder="Ghi chú giao xe (tòa nhà, cổng, thời gian liên hệ...)" rows="2" className={`${inputCls} resize-none`}/>
                    <p className="text-xs text-gray-500">Tên và số điện thoại người nhận sẽ lấy từ thông tin khách hàng ở bước tiếp theo.</p>
                  </div>}
                </div>
                {step1Error && <p className="mt-3 text-red-500 text-sm font-medium bg-red-50 rounded-xl px-4 py-2.5">{step1Error}</p>}
                {availabilityOk && <p className="mt-3 text-emerald-700 text-sm font-semibold bg-emerald-50 border border-emerald-200 rounded-xl px-4 py-2.5">✓ Xe khả dụng trong khoảng thời gian đã chọn.</p>}
                <button disabled={checkingAvailability} onClick={async () => {
                  if (!form.pickupDate) { setStep1Error("Vui lòng chọn ngày nhận xe"); return; }
                  if (!form.returnDate) { setStep1Error("Vui lòng chọn ngày trả xe"); return; }
                  const start = new Date(`${form.pickupDate}T${form.pickupTime}:00`);
                  const end = new Date(`${form.returnDate}T${form.returnTime}:00`);
                  if (end <= start) { setStep1Error("Thời gian trả phải sau thời gian nhận xe"); return; }
                  if (form.pickupMethod === "GIAO_TAN_NOI" && !form.deliveryAddress.trim()) { setStep1Error("Vui lòng nhập địa chỉ giao xe tận nơi"); return; }
                  setCheckingAvailability(true); setStep1Error(""); setAvailabilityOk(false);
                  try {
                    const result = await rentalsApi.checkAvailability(Number(car.id), start.toISOString(), end.toISOString());
                    if (!result?.available) { setStep1Error(result?.message || "Xe đã có lịch thuê trong khoảng thời gian này. Vui lòng chọn thời gian khác."); return; }
                    setAvailabilityOk(true);
                    setStep(2);
                  } catch (e) {
                    setStep1Error(e.message || "Không thể kiểm tra lịch xe. Vui lòng thử lại.");
                  } finally { setCheckingAvailability(false); }
                }} className="w-full mt-6 bg-[#0D1B3E] disabled:opacity-60 text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] transition-colors">
                  {checkingAvailability ? "Đang kiểm tra lịch xe..." : "Kiểm tra khả dụng & Tiếp theo →"}
                </button>
              </div>
            )}

            {/* Step 2 */}
            {step === 2 && (
              <div className="bg-white rounded-2xl border border-gray-100 p-6">
                <h2 className="text-xl font-bold text-[#0D1B3E] mb-6 flex items-center gap-2"><User size={20} className="text-[#00B4D8]" /> Thông tin khách hàng</h2>
                <div className="grid sm:grid-cols-2 gap-4">
                  <div className="sm:col-span-2">
                    <label className={labelCls}>HỌ VÀ TÊN</label>
                    <input type="text" value={form.fullName} onChange={e => update("fullName", e.target.value)} placeholder="Nguyễn Văn A" className={inputCls} />
                  </div>
                  <div>
                    <label className={labelCls}>SỐ ĐIỆN THOẠI</label>
                    <input type="tel" value={form.phone} onChange={e => update("phone", e.target.value)} placeholder="0901 234 567" className={inputCls} />
                  </div>
                  <div>
                    <label className={labelCls}>EMAIL</label>
                    <input type="email" value={form.email} onChange={e => update("email", e.target.value)} placeholder="email@example.com" className={inputCls} />
                  </div>
                  <div className="sm:col-span-2">
                    <label className={labelCls}>ĐỊA CHỈ</label>
                    <input type="text" value={form.address} onChange={e => update("address", e.target.value)} placeholder="Địa chỉ liên hệ" className={inputCls} />
                  </div>
                </div>
                <button onClick={() => setStep(3)} className="w-full mt-6 bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] transition-colors">
                  Tiếp theo →
                </button>
              </div>
            )}

            {/* Step 3 - hồ sơ là nguồn dữ liệu duy nhất */}
            {step === 3 && (
              <div className="bg-white rounded-2xl border border-gray-100 p-6">
                <h2 className="text-xl font-bold text-[#0D1B3E] mb-1 flex items-center gap-2"><FileText size={20} className="text-[#00B4D8]" /> Xác minh hồ sơ thuê xe</h2>
                <p className="text-gray-500 text-sm mb-6">Thông tin CCCD và GPLX được lấy trực tiếp từ Hồ sơ của tôi. Bạn không cần nhập hoặc tải giấy tờ lại khi đặt xe.</p>

                {profileLoading ? (
                  <div className="bg-gray-50 rounded-xl p-4 text-sm text-gray-500">Đang tải hồ sơ khách hàng...</div>
                ) : profileError ? (
                  <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl p-4 text-sm">{profileError}</div>
                ) : (
                  <>
                    <div className={`rounded-xl border p-4 ${profileVerified ? "bg-emerald-50 border-emerald-200" : "bg-amber-50 border-amber-200"}`}>
                      <div className="flex items-start gap-3">
                        <CheckCircle size={20} className={profileVerified ? "text-emerald-600" : "text-amber-600"}/>
                        <div className="flex-1">
                          <div className={`font-bold ${profileVerified ? "text-emerald-800" : "text-amber-800"}`}>{profileVerified ? "Hồ sơ thuê xe đã được xác minh" : "Hồ sơ thuê xe chưa được xác minh đầy đủ"}</div>
                          <div className="text-sm mt-1 text-gray-600">CCCD: {profile?.soCccd ? `********${String(profile.soCccd).slice(-4)}` : "Chưa cung cấp"}</div>
                          <div className="text-sm text-gray-600">GPLX: {profile?.soGplx ? `********${String(profile.soGplx).slice(-4)}` : "Chưa cung cấp"}</div>
                        </div>
                      </div>
                    </div>
                    <div className="grid sm:grid-cols-2 gap-3 mt-4">
                      <div className="border rounded-xl p-4"><div className="text-xs text-gray-400">CCCD</div><div className="font-semibold mt-1">{profile?.anhCccdMatTruoc && profile?.anhCccdMatSau ? "✓ Đã tải đủ 2 mặt" : "Chưa tải đủ ảnh"}</div><div className={`text-xs mt-1 ${profile?.cccdDaXacMinh ? "text-emerald-600" : "text-amber-600"}`}>{profile?.cccdDaXacMinh ? "Đã được nhân viên xác minh" : "Chờ nhân viên xác minh"}</div></div>
                      <div className="border rounded-xl p-4"><div className="text-xs text-gray-400">GPLX ô tô</div><div className="font-semibold mt-1">{profile?.anhGplx ? "✓ Đã tải ảnh GPLX" : "Chưa tải ảnh GPLX"}</div><div className={`text-xs mt-1 ${profile?.gplxDaXacMinh ? "text-emerald-600" : "text-amber-600"}`}>{profile?.gplxDaXacMinh ? "Đã được nhân viên xác minh" : "Chờ nhân viên xác minh"}</div></div>
                    </div>
                    {!profileVerified && <button onClick={() => onNavigate("profile")} className="w-full mt-5 border border-amber-300 bg-amber-50 text-amber-800 py-3 rounded-xl font-bold">Hoàn thiện / xem Hồ sơ của tôi</button>}
                    <button onClick={() => profileVerified && setStep(4)} disabled={!profileVerified} className="w-full mt-3 bg-[#0D1B3E] disabled:bg-gray-200 disabled:text-gray-400 text-white py-3.5 rounded-xl font-bold text-sm disabled:cursor-not-allowed">{profileVerified ? "Tiếp theo →" : "Cần nhân viên xác minh hồ sơ trước khi thuê online"}</button>
                  </>
                )}
              </div>
            )}

            {/* Step 4 */}
            {step === 4 && (
              <div className="bg-white rounded-2xl border border-gray-100 p-6">
                <h2 className="text-xl font-bold text-[#0D1B3E] mb-6 flex items-center gap-2"><CreditCard size={20} className="text-[#00B4D8]" /> Xác nhận yêu cầu</h2>
                <div className="space-y-4">
                  <div className="bg-[#F6F8FB] rounded-xl p-4">
                    <h4 className="font-semibold text-[#0D1B3E] text-sm mb-3">Thông tin xe</h4>
                    <div className="flex gap-3">
                      <img src={car.image} alt={car.name} className="w-16 h-12 rounded-lg object-cover" />
                      <div>
                        <div className="font-bold text-[#0D1B3E]">{car.name}</div>
                        <div className="text-gray-400 text-xs">{car.brand} · Biển số: {car.plate}</div>
                        <div className="text-sm font-semibold text-[#00B4D8] mt-1">{Number(car.pricePerDay || 0).toLocaleString("vi-VN")}đ/ngày</div>
                      </div>
                    </div>
                  </div>
                  <div className="bg-[#F6F8FB] rounded-xl p-4 grid grid-cols-2 gap-2 text-sm">
                    <div><span className="text-gray-400">Nhận xe:</span> <span className="font-semibold">{form.pickupDate || "20/12/2024"} {form.pickupTime}</span></div>
                    <div><span className="text-gray-400">Trả xe:</span> <span className="font-semibold">{form.returnDate || "25/12/2024"} {form.returnTime}</span></div>
                    <div><span className="text-gray-400">Khách hàng:</span> <span className="font-semibold">{form.fullName || "Nguyễn Văn A"}</span></div>
                    <div><span className="text-gray-400">Điện thoại:</span> <span className="font-semibold">{form.phone || "0901 234 567"}</span></div>
                    <div><span className="text-gray-400">CCCD:</span> <span className="font-semibold">{form.cccd || "012345678912"}</span></div>
                    <div><span className="text-gray-400">Trạng thái CCCD:</span> <span className="text-amber-600 font-semibold">Đã cung cấp / Chờ xác minh</span></div>
                  </div>
                  <div className="bg-[#F6F8FB] rounded-xl p-4 space-y-2 text-sm">
                    <div className="flex justify-between"><span className="text-gray-500">Số ngày thuê</span><span className="font-semibold">{days} ngày</span></div>
                    <div className="flex justify-between"><span className="text-gray-500">Tiền thuê</span><span className="font-semibold">{Number(rentalFee || 0).toLocaleString("vi-VN")}đ</span></div>
                    <div className="flex justify-between"><span className="text-gray-500">Tiền cọc</span><span className="font-semibold">{Number(deposit || 0).toLocaleString("vi-VN")}đ</span></div>
                    <div className="flex justify-between font-bold border-t border-gray-200 pt-2 mt-1">
                      <span className="text-[#0D1B3E]">Giá trị thuê dự kiến</span>
                      <span className="text-[#00B4D8] text-base">{Number(total || 0).toLocaleString("vi-VN")}đ</span>
                    </div>
                  </div>
                </div>
                <button onClick={submitRental} disabled={submitting} className="w-full mt-6 bg-[#00B4D8] disabled:opacity-60 text-white py-4 rounded-xl font-bold text-sm hover:bg-[#009bb8] transition-colors">{submitting ? "Đang gửi tới backend..." : "Xác nhận gửi yêu cầu"}</button>
                {submitError && <div className="mt-3 bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm">{submitError}</div>}
                <p className="text-xs text-gray-400 text-center mt-3">Yêu cầu sẽ được xem xét và phản hồi trong vòng 1–2 giờ làm việc</p>
              </div>
            )}
          </div>

          {/* Summary sidebar */}
          <div className="lg:sticky lg:top-20 lg:self-start">
            <div className="bg-white rounded-2xl border border-gray-100 p-5">
              <img src={car.image} alt={car.name} className="w-full h-40 object-cover rounded-xl mb-4" />
              <h3 className="font-bold text-[#0D1B3E]">{car.name}</h3>
              <p className="text-gray-400 text-sm">{car.brand}</p>
              <div className="mt-4 pt-4 border-t border-gray-100 space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-500">Giá/ngày</span>
                  <span className="font-semibold">{Number(car.pricePerDay || 0).toLocaleString("vi-VN")}đ</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">Số ngày</span>
                  <span className="font-semibold">{days}</span>
                </div>
                <div className="flex justify-between font-bold border-t border-gray-100 pt-2 mt-1">
                  <span className="text-[#0D1B3E]">Tiền thuê dự kiến</span>
                  <span className="text-[#00B4D8]">{Number(total || 0).toLocaleString("vi-VN")}đ</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    </>
  );
}
