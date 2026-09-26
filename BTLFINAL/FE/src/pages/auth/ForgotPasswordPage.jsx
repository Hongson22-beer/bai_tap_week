import { useState, useEffect, useRef } from "react";
import { Car, Mail, ArrowRight, ChevronLeft, Lock, Eye, EyeOff, CheckCircle, RefreshCw, ShieldCheck } from "lucide-react";

export default function ForgotPasswordPage({ onNavigate }) {
  const [step, setStep] = useState("email");
  const [email, setEmail] = useState("");
  const [emailError, setEmailError] = useState("");
  const [emailLoading, setEmailLoading] = useState(false);

  // OTP
  const [otp, setOtp] = useState(["", "", "", "", "", ""]);
  const [otpError, setOtpError] = useState("");
  const [otpLoading, setOtpLoading] = useState(false);
  const [resendTimer, setResendTimer] = useState(60);
  const [resendLoading, setResendLoading] = useState(false);
  const otpRefs = useRef([]);

  // New password
  const [newPass, setNewPass] = useState("");
  const [confirmPass, setConfirmPass] = useState("");
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [passError, setPassError] = useState("");
  const [passLoading, setPassLoading] = useState(false);

  useEffect(() => {
    if (step !== "otp") return;
    if (resendTimer <= 0) return;
    const t = setInterval(() => setResendTimer(p => p - 1), 1000);
    return () => clearInterval(t);
  }, [step, resendTimer]);

  const handleSendEmail = () => {
    setEmailError("");
    if (!email.trim()) { setEmailError("Vui lòng nhập email"); return; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { setEmailError("Email không hợp lệ"); return; }
    setEmailLoading(true);
    setTimeout(() => {
      setEmailLoading(false);
      setStep("otp");
      setResendTimer(60);
    }, 1200);
  };

  const handleOtpChange = (idx, val) => {
    const digit = val.replace(/\D/g, "").slice(-1);
    const next = [...otp];
    next[idx] = digit;
    setOtp(next);
    setOtpError("");
    if (digit && idx < 5) otpRefs.current[idx + 1]?.focus();
  };

  const handleOtpKeyDown = (idx, e) => {
    if (e.key === "Backspace" && !otp[idx] && idx > 0) {
      otpRefs.current[idx - 1]?.focus();
    }
  };

  const handleOtpPaste = (e) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData("text").replace(/\D/g, "").slice(0, 6);
    const next = [...otp];
    pasted.split("").forEach((d, i) => { next[i] = d; });
    setOtp(next);
    otpRefs.current[Math.min(pasted.length, 5)]?.focus();
  };

  const handleVerifyOtp = () => {
    const code = otp.join("");
    if (code.length < 6) { setOtpError("Vui lòng nhập đủ 6 chữ số OTP"); return; }
    setOtpLoading(true);
    setTimeout(() => {
      setOtpLoading(false);
      // accept any 6-digit code for demo
      setStep("newpass");
    }, 1000);
  };

  const handleResend = () => {
    setResendLoading(true);
    setTimeout(() => {
      setResendLoading(false);
      setResendTimer(60);
      setOtp(["", "", "", "", "", ""]);
      setOtpError("");
      otpRefs.current[0]?.focus();
    }, 800);
  };

  const passStrength = (() => {
    if (!newPass) return 0;
    let s = 0;
    if (newPass.length >= 8) s++;
    if (/[A-Z]/.test(newPass)) s++;
    if (/[0-9]/.test(newPass)) s++;
    if (/[^A-Za-z0-9]/.test(newPass)) s++;
    return s;
  })();

  const strengthLabel = ["", "Yếu", "Trung bình", "Khá", "Mạnh"][passStrength];
  const strengthColor = ["", "bg-red-400", "bg-amber-400", "bg-blue-400", "bg-emerald-500"][passStrength];

  const handleSetPassword = () => {
    setPassError("");
    if (newPass.length < 6) { setPassError("Mật khẩu phải có ít nhất 6 ký tự"); return; }
    if (newPass !== confirmPass) { setPassError("Mật khẩu xác nhận không khớp"); return; }
    setPassLoading(true);
    setTimeout(() => {
      setPassLoading(false);
      setStep("done");
    }, 1200);
  };

  const inputCls = "w-full px-4 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/15 transition-all";

  return (
    <div className="min-h-screen flex">
      {/* Left panel */}
      <div className="hidden lg:flex lg:w-5/12 bg-[#0D1B3E] relative overflow-hidden flex-col">
        <img
          src="https://images.unsplash.com/photo-1771210353591-20006eba7ad7?w=800&h=1200&fit=crop&auto=format"
          alt="Luxury car"
          className="absolute inset-0 w-full h-full object-cover opacity-25"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[#0D1B3E]/80 via-transparent to-transparent" />
        <div className="relative z-10 p-10 flex flex-col h-full">
          <button onClick={() => onNavigate("login")} className="flex items-center gap-2.5 w-fit">
            <div className="w-9 h-9 bg-[#00B4D8] rounded-xl flex items-center justify-center shadow-md">
              <Car size={18} className="text-white" />
            </div>
            <span className="text-white font-extrabold text-lg tracking-tight">CARRENT<span className="text-[#00B4D8]"> PRO</span></span>
          </button>

          <div className="flex-1 flex flex-col justify-center">
            <div className="w-16 h-16 bg-[#00B4D8]/20 border border-[#00B4D8]/30 rounded-2xl flex items-center justify-center mb-6">
              <ShieldCheck size={32} className="text-[#00B4D8]" />
            </div>
            <h2 className="text-3xl font-extrabold text-white leading-tight mb-4">
              Khôi phục<br />tài khoản an toàn
            </h2>
            <p className="text-white/55 leading-relaxed mb-8">
              Chúng tôi sẽ gửi mã xác minh OTP đến email của bạn để đặt lại mật khẩu mới một cách bảo mật.
            </p>

            {/* Steps indicator */}
            <div className="space-y-3">
              {[
                { label: "Xác nhận email", done: step !== "email" },
                { label: "Nhập mã OTP", done: step === "newpass" || step === "done" },
                { label: "Tạo mật khẩu mới", done: step === "done" },
              ].map((s, i) => (
                <div key={i} className="flex items-center gap-3">
                  <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold shrink-0 transition-colors ${s.done ? "bg-emerald-500 text-white" : "bg-white/10 text-white/40 border border-white/20"}`}>
                    {s.done ? <CheckCircle size={14} /> : i + 1}
                  </div>
                  <span className={`text-sm font-medium transition-colors ${s.done ? "text-emerald-400" : "text-white/50"}`}>{s.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Right panel */}
      <div className="flex-1 flex items-center justify-center p-6 bg-[#F6F8FB]">
        <div className="w-full max-w-md">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-2 mb-8">
            <div className="w-8 h-8 bg-[#0D1B3E] rounded-lg flex items-center justify-center">
              <Car size={16} className="text-white" />
            </div>
            <span className="text-[#0D1B3E] font-bold text-lg">CARRENT PRO</span>
          </div>

          {/* Back button */}
          {step !== "done" && (
            <button onClick={() => step === "email" ? onNavigate("login") : step === "otp" ? setStep("email") : setStep("otp")}
              className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-[#0D1B3E] font-medium mb-6 transition-colors">
              <ChevronLeft size={16} /> {step === "email" ? "Quay lại đăng nhập" : "Quay lại"}
            </button>
          )}

          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">

            {/* ── STEP 1: Email ── */}
            {step === "email" && (
              <>
                <div className="w-12 h-12 bg-blue-50 rounded-xl flex items-center justify-center mb-5">
                  <Mail size={22} className="text-[#00B4D8]" />
                </div>
                <h2 className="text-2xl font-extrabold text-[#0D1B3E] mb-1">Quên mật khẩu?</h2>
                <p className="text-gray-500 text-sm mb-7">Nhập email tài khoản — chúng tôi sẽ gửi mã OTP 6 chữ số để xác minh.</p>

                <div className="mb-5">
                  <label className="block text-sm font-semibold text-[#0D1B3E] mb-2">Địa chỉ Email</label>
                  <div className="relative">
                    <Mail size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                    <input
                      type="email"
                      value={email}
                      onChange={e => { setEmail(e.target.value); setEmailError(""); }}
                      onKeyDown={e => e.key === "Enter" && handleSendEmail()}
                      placeholder="your@email.com"
                      autoFocus
                      className={`${inputCls} pl-10 ${emailError ? "border-red-400 ring-2 ring-red-400/10" : ""}`}
                    />
                  </div>
                  {emailError && <p className="text-red-500 text-xs mt-1.5 font-medium">{emailError}</p>}
                </div>

                <button onClick={handleSendEmail} disabled={emailLoading}
                  className="w-full bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] disabled:opacity-60 transition-all flex items-center justify-center gap-2">
                  {emailLoading
                    ? <><span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Đang gửi mã...</>
                    : <>Gửi mã OTP <ArrowRight size={16} /></>}
                </button>

                <p className="text-center text-sm text-gray-500 mt-6">
                  Nhớ mật khẩu rồi?{" "}
                  <button onClick={() => onNavigate("login")} className="text-[#00B4D8] font-semibold hover:text-[#0D1B3E] transition-colors">
                    Đăng nhập
                  </button>
                </p>
              </>
            )}

            {/* ── STEP 2: OTP ── */}
            {step === "otp" && (
              <>
                <div className="w-12 h-12 bg-amber-50 rounded-xl flex items-center justify-center mb-5">
                  <ShieldCheck size={22} className="text-amber-500" />
                </div>
                <h2 className="text-2xl font-extrabold text-[#0D1B3E] mb-1">Nhập mã OTP</h2>
                <p className="text-gray-500 text-sm mb-1">
                  Mã xác minh đã gửi đến
                </p>
                <p className="text-[#0D1B3E] font-bold text-sm mb-7">{email}</p>

                {/* OTP inputs */}
                <div className="flex gap-2.5 justify-center mb-5" onPaste={handleOtpPaste}>
                  {otp.map((digit, i) => (
                    <input
                      key={i}
                      ref={el => { otpRefs.current[i] = el; }}
                      type="text"
                      inputMode="numeric"
                      maxLength={1}
                      value={digit}
                      onChange={e => handleOtpChange(i, e.target.value)}
                      onKeyDown={e => handleOtpKeyDown(i, e)}
                      className={`w-12 h-14 text-center text-2xl font-extrabold rounded-xl border-2 outline-none transition-all
                        ${digit ? "border-[#00B4D8] bg-[#00B4D8]/5 text-[#0D1B3E]" : "border-gray-200 bg-gray-50 text-gray-700"}
                        focus:border-[#0D1B3E] focus:ring-2 focus:ring-[#0D1B3E]/10
                        ${otpError ? "border-red-400 bg-red-50" : ""}`}
                    />
                  ))}
                </div>

                {otpError && (
                  <p className="text-red-500 text-xs text-center mb-4 font-medium">{otpError}</p>
                )}

                <button onClick={handleVerifyOtp} disabled={otpLoading || otp.join("").length < 6}
                  className="w-full bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2 mb-5">
                  {otpLoading
                    ? <><span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Đang xác minh...</>
                    : <>Xác minh OTP <ArrowRight size={16} /></>}
                </button>

                {/* Resend */}
                <div className="text-center">
                  <p className="text-sm text-gray-500 mb-2">Không nhận được mã?</p>
                  {resendTimer > 0 ? (
                    <p className="text-sm text-gray-400">
                      Gửi lại sau <span className="font-bold text-[#0D1B3E] tabular-nums">{resendTimer}s</span>
                    </p>
                  ) : (
                    <button onClick={handleResend} disabled={resendLoading}
                      className="text-sm text-[#00B4D8] font-semibold hover:text-[#0D1B3E] transition-colors flex items-center gap-1.5 mx-auto disabled:opacity-50">
                      <RefreshCw size={14} className={resendLoading ? "animate-spin" : ""} />
                      {resendLoading ? "Đang gửi lại..." : "Gửi lại mã OTP"}
                    </button>
                  )}
                </div>

                <div className="mt-5 bg-blue-50 border border-blue-100 rounded-xl px-4 py-3 text-xs text-blue-600">
                  Mã OTP có hiệu lực trong <span className="font-bold">5 phút</span>. Kiểm tra cả hộp thư Spam nếu không thấy.
                </div>
              </>
            )}

            {/* ── STEP 3: New password ── */}
            {step === "newpass" && (
              <>
                <div className="w-12 h-12 bg-emerald-50 rounded-xl flex items-center justify-center mb-5">
                  <Lock size={22} className="text-emerald-500" />
                </div>
                <h2 className="text-2xl font-extrabold text-[#0D1B3E] mb-1">Tạo mật khẩu mới</h2>
                <p className="text-gray-500 text-sm mb-7">Mật khẩu phải có ít nhất 6 ký tự. Nên kết hợp chữ hoa, số và ký tự đặc biệt.</p>

                <div className="space-y-4 mb-5">
                  <div>
                    <label className="block text-sm font-semibold text-[#0D1B3E] mb-2">Mật khẩu mới</label>
                    <div className="relative">
                      <Lock size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                      <input
                        type={showNew ? "text" : "password"}
                        value={newPass}
                        onChange={e => { setNewPass(e.target.value); setPassError(""); }}
                        placeholder="Tối thiểu 6 ký tự"
                        className={`${inputCls} pl-10 pr-10`}
                        autoFocus
                      />
                      <button type="button" onClick={() => setShowNew(!showNew)} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                        {showNew ? <EyeOff size={16} /> : <Eye size={16} />}
                      </button>
                    </div>
                    {/* Strength bar */}
                    {newPass && (
                      <div className="mt-2">
                        <div className="flex gap-1 mb-1">
                          {[1, 2, 3, 4].map(i => (
                            <div key={i} className={`h-1.5 flex-1 rounded-full transition-all duration-300 ${i <= passStrength ? strengthColor : "bg-gray-100"}`} />
                          ))}
                        </div>
                        <p className={`text-xs font-semibold ${["", "text-red-500", "text-amber-500", "text-blue-500", "text-emerald-600"][passStrength]}`}>
                          {strengthLabel}
                        </p>
                      </div>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-[#0D1B3E] mb-2">Xác nhận mật khẩu</label>
                    <div className="relative">
                      <Lock size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                      <input
                        type={showConfirm ? "text" : "password"}
                        value={confirmPass}
                        onChange={e => { setConfirmPass(e.target.value); setPassError(""); }}
                        placeholder="Nhập lại mật khẩu"
                        className={`${inputCls} pl-10 pr-10 ${confirmPass && confirmPass === newPass ? "border-emerald-400 bg-emerald-50/40" : ""}`}
                      />
                      <button type="button" onClick={() => setShowConfirm(!showConfirm)} className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                        {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
                      </button>
                      {confirmPass && confirmPass === newPass && (
                        <CheckCircle size={16} className="absolute right-9 top-1/2 -translate-y-1/2 text-emerald-500" />
                      )}
                    </div>
                  </div>
                </div>

                {passError && (
                  <p className="text-red-500 text-sm bg-red-50 border border-red-200 rounded-xl px-4 py-2.5 mb-4 font-medium">{passError}</p>
                )}

                <button onClick={handleSetPassword} disabled={passLoading}
                  className="w-full bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] disabled:opacity-60 transition-all flex items-center justify-center gap-2">
                  {passLoading
                    ? <><span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Đang cập nhật...</>
                    : <>Đặt mật khẩu mới <ArrowRight size={16} /></>}
                </button>
              </>
            )}

            {/* ── STEP 4: Done ── */}
            {step === "done" && (
              <div className="text-center py-4">
                <div className="w-20 h-20 bg-emerald-50 rounded-full flex items-center justify-center mx-auto mb-6 shadow-sm">
                  <CheckCircle size={40} className="text-emerald-500" />
                </div>
                <h2 className="text-2xl font-extrabold text-[#0D1B3E] mb-2">Đặt lại thành công!</h2>
                <p className="text-gray-500 text-sm mb-2">Mật khẩu mới đã được cập nhật cho tài khoản</p>
                <p className="text-[#0D1B3E] font-bold text-sm mb-8">{email}</p>

                <div className="bg-emerald-50 border border-emerald-200 rounded-2xl p-5 mb-8 text-left space-y-2">
                  {[
                    "Mật khẩu đã được thay đổi thành công",
                    "Các phiên đăng nhập cũ đã bị vô hiệu",
                    "Bạn có thể đăng nhập bằng mật khẩu mới",
                  ].map((t, i) => (
                    <div key={i} className="flex items-center gap-2.5 text-emerald-700 text-sm font-medium">
                      <CheckCircle size={15} className="shrink-0 text-emerald-500" /> {t}
                    </div>
                  ))}
                </div>

                <button onClick={() => onNavigate("login")}
                  className="w-full bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold text-sm hover:bg-[#1A3A6E] transition-all flex items-center justify-center gap-2 mb-3">
                  Đăng nhập ngay <ArrowRight size={16} />
                </button>
                <button onClick={() => onNavigate("landing")} className="text-sm text-gray-400 hover:text-gray-600 transition-colors">
                  Về trang chủ
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
