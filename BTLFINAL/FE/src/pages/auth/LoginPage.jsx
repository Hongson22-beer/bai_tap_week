import { useState } from "react";
import { Car, Eye, EyeOff, Mail, Lock, ArrowRight } from "lucide-react";
import { authApi } from "../../services/api";

export default function LoginPage({ onLogin, onNavigate }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [remember, setRemember] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(""); setLoading(true);
    try {
      const user = await authApi.login(email, password, remember);
      onLogin(user);
    } catch (err) {
      setError(err.message || "Đăng nhập thất bại");
    } finally { setLoading(false); }
  };

  const quickLogin = (role) => {
    const creds = {
      customer: { email: "hungtest@gmail.com", pass: "Hung@123456" },
      staff: { email: "staff_test@carrent.vn", pass: "Staff@123456" },
      admin: { email: "admin_test@carrent.vn", pass: "Admin@123456" },
    };
    setEmail(creds[role].email); setPassword(creds[role].pass);
  };

  return (
    <div className="min-h-screen flex">
      {/* Left panel */}
      <div className="hidden lg:flex lg:w-1/2 bg-[#0D1B3E] relative overflow-hidden flex-col">
        <img
          src="https://images.unsplash.com/photo-1780296269675-169390638617?w=1000&h=1200&fit=crop&auto=format"
          alt="Luxury car"
          className="absolute inset-0 w-full h-full object-cover opacity-30"
        />
        <div className="relative z-10 p-10 flex flex-col h-full">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 bg-[#00B4D8] rounded-lg flex items-center justify-center">
              <Car size={20} className="text-white" />
            </div>
            <span className="text-white font-bold text-xl tracking-tight">CARRENT PRO</span>
          </div>
          <div className="flex-1 flex flex-col justify-center">
            <h1 className="text-4xl font-bold text-white leading-tight mb-4">
              Thuê xe dễ dàng –<br />Hành trình trọn vẹn.
            </h1>
            <p className="text-white/60 text-lg leading-relaxed">
              Nền tảng quản lý thuê ô tô chuyên nghiệp với hơn 50+ xe cao cấp sẵn sàng phục vụ.
            </p>
            <div className="mt-10 grid grid-cols-3 gap-4">
              {[
                { label: "Xe cao cấp", value: "50+" },
                { label: "Khách hàng", value: "2.400+" },
                { label: "Đánh giá", value: "4.9★" },
              ].map((s) => (
                <div key={s.label} className="bg-white/10 rounded-xl p-4 backdrop-blur-sm border border-white/10">
                  <div className="text-2xl font-bold text-[#00B4D8]">{s.value}</div>
                  <div className="text-white/60 text-sm mt-1">{s.label}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center p-6 bg-[#F6F8FB]">
        <div className="w-full max-w-md">
          <div className="lg:hidden flex items-center gap-2 mb-8">
            <div className="w-8 h-8 bg-[#0D1B3E] rounded-lg flex items-center justify-center">
              <Car size={16} className="text-white" />
            </div>
            <span className="text-[#0D1B3E] font-bold text-lg">CARRENT PRO</span>
          </div>

          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">
            <h2 className="text-2xl font-bold text-[#0D1B3E] mb-1">Đăng nhập</h2>
            <p className="text-gray-500 text-sm mb-6">Chào mừng trở lại! Nhập thông tin để tiếp tục.</p>

            {/* Quick login chips */}
            <div className="flex gap-2 mb-6">
              {["customer", "staff", "admin"].map((role) => (
                <button
                  key={role}
                  onClick={() => quickLogin(role)}
                  className="flex-1 text-xs py-1.5 px-2 rounded-lg border border-gray-200 text-gray-600 hover:border-[#00B4D8] hover:text-[#0D1B3E] transition-colors font-medium"
                >
                  {role === "customer" ? "Khách hàng" : role === "staff" ? "Nhân viên" : "Quản trị"}
                </button>
              ))}
            </div>

            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm mb-5">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-[#0D1B3E] mb-1.5">Email</label>
                <div className="relative">
                  <Mail size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="your@email.com"
                    required
                    className="w-full pl-10 pr-4 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/20 transition-all"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-[#0D1B3E] mb-1.5">Mật khẩu</label>
                <div className="relative">
                  <Lock size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                  <input
                    type={showPassword ? "text" : "password"}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="••••••••"
                    required
                    className="w-full pl-10 pr-10 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/20 transition-all"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                  >
                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={remember}
                    onChange={(e) => setRemember(e.target.checked)}
                    className="w-4 h-4 accent-[#00B4D8] rounded"
                  />
                  <span className="text-sm text-gray-600">Ghi nhớ đăng nhập</span>
                </label>
                <button type="button" onClick={() => onNavigate("forgot-password")} className="text-sm text-[#00B4D8] hover:text-[#0D1B3E] font-medium transition-colors">
                  Quên mật khẩu?
                </button>
              </div>
              <button
                type="submit"
                disabled={loading}
                className="w-full bg-[#0D1B3E] text-white py-3 rounded-xl font-semibold text-sm hover:bg-[#1A3A6E] disabled:opacity-60 transition-all flex items-center justify-center gap-2 mt-2"
              >
                {loading ? (
                  <span className="flex items-center gap-2">
                    <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    Đang đăng nhập...
                  </span>
                ) : (
                  <>Đăng nhập <ArrowRight size={16} /></>
                )}
              </button>
            </form>

            <p className="text-center text-sm text-gray-500 mt-6">
              Chưa có tài khoản?{" "}
              <button onClick={() => onNavigate("register")} className="text-[#00B4D8] font-semibold hover:text-[#0D1B3E] transition-colors">
                Đăng ký ngay
              </button>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
