import { useState } from "react";
import { Car, Eye, EyeOff, CheckCircle } from "lucide-react";
import Toast from "../../components/shared/Toast";

export default function RegisterPage({ onLogin, onNavigate, toast, onCloseToast, showToast }) {
  const [form, setForm] = useState({ name: "", email: "", phone: "", password: "", confirm: "" });
  const [agreed, setAgreed] = useState(false);
  const [showPass, setShowPass] = useState(false);
  const [errors, setErrors] = useState({});

  const update = (k, v) => {
    setForm(prev => ({ ...prev, [k]: v }));
    setErrors(prev => ({ ...prev, [k]: "" }));
  };

  const validate = () => {
    const e = {};
    if (!form.name.trim()) e.name = "Vui lòng nhập họ và tên";
    if (!form.email.includes("@")) e.email = "Email không hợp lệ";
    if (!/^0\d{9}$/.test(form.phone.replace(/\s/g, ""))) e.phone = "Số điện thoại không hợp lệ (10 số, bắt đầu 0)";
    if (form.password.length < 6) e.password = "Mật khẩu tối thiểu 6 ký tự";
    if (form.password !== form.confirm) e.confirm = "Mật khẩu xác nhận không khớp";
    if (!agreed) e.agreed = "Vui lòng đồng ý với điều khoản";
    return e;
  };

  const handleSubmit = () => {
    const e = validate();
    if (Object.keys(e).length > 0) {
      setErrors(e);
      showToast("Vui lòng kiểm tra lại thông tin", "error");
      return;
    }
    showToast("Backend hiện chưa có API đăng ký được xác nhận. Vui lòng dùng tài khoản đã cấp để đăng nhập.", "error");
    setTimeout(() => onNavigate("login"), 900);
  };

  const inputCls = (field) =>
    `w-full px-3.5 py-3 rounded-xl border text-sm focus:outline-none transition-all ${errors[field] ? "border-red-400 focus:border-red-500 bg-red-50" : "border-gray-200 focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/10"}`;

  return (
    <div className="min-h-screen bg-[#F6F8FB] flex">
      {/* Left panel */}
      <div className="hidden lg:flex lg:w-1/2 bg-[#0D1B3E] flex-col justify-between p-12">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-[#00B4D8] rounded-xl flex items-center justify-center"><Car size={20} className="text-white" /></div>
          <span className="text-white font-bold text-xl tracking-tight">CARRENT PRO</span>
        </div>
        <div>
          <h1 className="text-4xl font-bold text-white leading-tight mb-4">Bắt đầu hành trình<br />của bạn cùng chúng tôi</h1>
          <p className="text-white/60 text-base">Đăng ký miễn phí — truy cập hơn 50 mẫu xe sang trọng, đặt xe trong vài phút.</p>
          <div className="mt-8 space-y-3">
            {["Xe cao cấp, bảo dưỡng định kỳ", "Hợp đồng minh bạch, rõ ràng", "Hỗ trợ 24/7 trong suốt hành trình"].map(b => (
              <div key={b} className="flex items-center gap-3 text-white/80 text-sm">
                <CheckCircle size={16} className="text-[#00B4D8] shrink-0" />{b}
              </div>
            ))}
          </div>
        </div>
        <p className="text-white/30 text-xs">© 2024 CARRENT PRO. All rights reserved.</p>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center p-6">
        <div className="w-full max-w-md">
          <div className="mb-8">
            <div className="flex items-center gap-2 mb-6 lg:hidden">
              <div className="w-8 h-8 bg-[#0D1B3E] rounded-lg flex items-center justify-center"><Car size={16} className="text-white" /></div>
              <span className="font-bold text-[#0D1B3E] text-lg">CARRENT PRO</span>
            </div>
            <h2 className="text-2xl font-bold text-[#0D1B3E]">Tạo tài khoản</h2>
            <p className="text-gray-500 text-sm mt-1">Điền thông tin bên dưới để đăng ký</p>
          </div>

          <div className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1.5">HỌ VÀ TÊN</label>
              <input value={form.name} onChange={e => update("name", e.target.value)} placeholder="Nguyễn Văn An" className={inputCls("name")} />
              {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name}</p>}
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1.5">EMAIL</label>
              <input type="email" value={form.email} onChange={e => update("email", e.target.value)} placeholder="email@example.com" className={inputCls("email")} />
              {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email}</p>}
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1.5">SỐ ĐIỆN THOẠI</label>
              <input type="tel" value={form.phone} onChange={e => update("phone", e.target.value)} placeholder="0901 234 567" className={inputCls("phone")} />
              {errors.phone && <p className="text-red-500 text-xs mt-1">{errors.phone}</p>}
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1.5">MẬT KHẨU</label>
              <div className="relative">
                <input type={showPass ? "text" : "password"} value={form.password} onChange={e => update("password", e.target.value)} placeholder="Tối thiểu 6 ký tự" className={`${inputCls("password")} pr-10`} />
                <button type="button" onClick={() => setShowPass(p => !p)} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                  {showPass ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
              {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password}</p>}
              {form.password && (
                <div className="mt-1.5 flex gap-1">
                  {[1, 2, 3, 4].map(i => (
                    <div key={i} className={`h-1 flex-1 rounded-full transition-all ${form.password.length >= i * 2 ? i <= 2 ? "bg-red-400" : i === 3 ? "bg-amber-400" : "bg-emerald-500" : "bg-gray-200"}`} />
                  ))}
                </div>
              )}
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-500 mb-1.5">XÁC NHẬN MẬT KHẨU</label>
              <input type="password" value={form.confirm} onChange={e => update("confirm", e.target.value)} placeholder="Nhập lại mật khẩu" className={inputCls("confirm")} />
              {errors.confirm && <p className="text-red-500 text-xs mt-1">{errors.confirm}</p>}
            </div>

            <label className="flex items-start gap-2.5 cursor-pointer select-none">
              <input type="checkbox" checked={agreed} onChange={e => { setAgreed(e.target.checked); setErrors(p => ({ ...p, agreed: "" })); }} className="w-4 h-4 accent-[#0D1B3E] mt-0.5 shrink-0" />
              <span className="text-xs text-gray-500 leading-relaxed">
                Tôi đồng ý với{" "}
                <button type="button" className="text-[#00B4D8] hover:underline font-medium">Điều khoản dịch vụ</button>
                {" "}và{" "}
                <button type="button" className="text-[#00B4D8] hover:underline font-medium">Chính sách bảo mật</button>
              </span>
            </label>
            {errors.agreed && <p className="text-red-500 text-xs -mt-2">{errors.agreed}</p>}
          </div>

          <button
            onClick={handleSubmit}
            className="w-full mt-6 bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] active:scale-[0.99] transition-all"
          >
            Đăng ký ngay
          </button>

          <p className="text-center text-sm text-gray-500 mt-5">
            Đã có tài khoản?{" "}
            <button onClick={() => onNavigate("login")} className="text-[#00B4D8] font-semibold hover:text-[#0D1B3E] transition-colors">
              Đăng nhập
            </button>
          </p>
        </div>
      </div>

      {toast && <Toast message={toast.message} type={toast.type} onClose={onCloseToast} />}
    </div>
  );
}
