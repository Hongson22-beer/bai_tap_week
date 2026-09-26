import { useEffect, useState } from "react";
import {
  Car, Search, Star, Shield, Clock, Users, ChevronRight, MapPin,
  Calendar, ArrowRight, CheckCircle, Award, Phone, Menu, X,
  TrendingUp, Zap, HeadphonesIcon, ChevronLeft
} from "lucide-react";
import { vehiclesApi, evaluationsApi } from "../../services/api";

const navLinks = ["Trang chủ", "Danh sách xe", "Hãng xe", "Theo dõi thuê xe", "Giới thiệu"];

export default function LandingPage({ onNavigate, currentUser, onLogout }) {
  const [menuOpen, setMenuOpen] = useState(false);
  const [pickup, setPickup] = useState("");
  const [returnDate, setReturnDate] = useState("");
  const [location, setLocation] = useState("");
  const [activeReview, setActiveReview] = useState(0);
  const [cars, setCars] = useState([]);
  const [realReviews, setRealReviews] = useState([]);
  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const list = await vehiclesApi.list();
        if (!alive) return;
        setCars(list);
        const chunks = await Promise.all(list.map(v => evaluationsApi.byVehicle(v.id).catch(() => [])));
        if (!alive) return;
        const normalized = chunks.flatMap((rows, idx) => (Array.isArray(rows) ? rows : (rows?.items || rows?.data || [])).map(r => ({...r, _vehicle: list[idx]})));
        setRealReviews(normalized.filter(r => r.hienThi !== false));
      } catch { if (alive) { setCars([]); setRealReviews([]); } }
    })();
    return () => { alive = false; };
  }, []);
  const brands = [...new Set(cars.map(c => c.brand).filter(Boolean))].map((name, i) => ({ id: i + 1, name, logo: name.slice(0, 3).toUpperCase(), count: cars.filter(c => c.brand === name).length }));

  const handleSearch = () => {
    onNavigate("browse", { pickup, returnDate, location });
  };

  const featuredCars = cars.filter(c => c.status === "AVAILABLE").slice(0, 3);

  const avgRating = realReviews.length ? realReviews.reduce((sum, r) => sum + Number(r.diemDanhGia || r.rating || 0), 0) / realReviews.length : 0;
  const satisfiedRate = realReviews.length ? Math.round(realReviews.filter(r => Number(r.diemDanhGia || r.rating || 0) >= 4).length * 100 / realReviews.length) : 0;
  const reviews = realReviews.slice(0, 3).map((r, i) => {
    const name = r.tenKhachHang || r.hoTenKhachHang || `Khách hàng #${r.idKhachHang || i + 1}`;
    return {
      id: r.id || i, name, role: r._vehicle?.name || `Xe #${r.idXe || "—"}`,
      text: r.nhanXet || "Khách hàng đã đánh giá dịch vụ.",
      rating: Math.max(1, Math.min(5, Number(r.diemDanhGia || 5))), avatar: name.charAt(0).toUpperCase()
    };
  });
  const brandCount = new Set(cars.map(c => c.brand).filter(Boolean)).size;

  return (
    <div className="cp-customer min-h-screen bg-white font-sans">
      {/* ─── NAVBAR ─── */}
      <header className="cp-public-nav fixed top-0 inset-x-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          <button onClick={() => onNavigate("landing")} className="flex items-center gap-2.5 shrink-0">
            <div className="w-9 h-9 bg-[#0D1B3E] rounded-xl flex items-center justify-center shadow-sm">
              <Car size={17} className="text-white" />
            </div>
            <span className="font-extrabold text-[#0D1B3E] text-lg tracking-tight">CARRENT<span className="text-[#00B4D8]"> PRO</span></span>
          </button>

          <nav className="hidden md:flex items-center gap-1">
            {navLinks.map((link) => (
              <button key={link} onClick={() => {
                if (link === "Trang chủ") window.scrollTo({ top: 0, behavior: "smooth" });
                else if (link === "Danh sách xe") onNavigate("browse");
                else if (link === "Theo dõi thuê xe") onNavigate("tracking");
                else if (link === "Hãng xe") document.getElementById("brands-section")?.scrollIntoView({ behavior: "smooth" });
                else if (link === "Giới thiệu") document.getElementById("why-section")?.scrollIntoView({ behavior: "smooth" });
              }} className="text-sm font-medium text-gray-600 hover:text-[#0D1B3E] transition-colors px-3 py-1.5 rounded-lg hover:bg-gray-50">
                {link}
              </button>
            ))}
          </nav>

          <div className="flex items-center gap-2.5">
            {currentUser ? (
              <div className="flex items-center gap-3">
                <button onClick={() => onNavigate(currentUser.role === "admin" ? "admin" : currentUser.role === "staff" ? "staff" : "profile")} className="hidden sm:flex items-center gap-2 text-sm font-medium text-gray-700 hover:text-[#0D1B3E]">
                  <div className="w-7 h-7 rounded-full bg-[#0D1B3E] flex items-center justify-center text-white text-xs font-bold">{currentUser.name[0]}</div>
                  {currentUser.name}
                </button>
                <button onClick={onLogout} className="text-xs text-gray-400 hover:text-red-500 transition-colors border border-gray-200 px-3 py-1.5 rounded-lg hover:border-red-200">Đăng xuất</button>
              </div>
            ) : (
              <>
                <button onClick={() => onNavigate("login")} className="hidden sm:block text-sm font-medium text-gray-600 hover:text-[#0D1B3E] transition-colors px-3 py-1.5">Đăng nhập</button>
                <button onClick={() => onNavigate("register")} className="text-sm font-bold bg-[#0D1B3E] text-white px-4 py-2 rounded-xl hover:bg-[#1A3A6E] transition-colors shadow-sm">Đăng ký</button>
              </>
            )}
            <button onClick={() => setMenuOpen(!menuOpen)} className="md:hidden p-2 text-gray-600">
              {menuOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>

        {/* Mobile menu */}
        {menuOpen && (
          <div className="md:hidden bg-white border-t border-gray-100 px-4 py-4 space-y-1">
            {navLinks.map(link => (
              <button key={link} onClick={() => { setMenuOpen(false); if (link === "Danh sách xe") onNavigate("browse"); else if (link === "Theo dõi thuê xe") onNavigate("tracking"); }} className="block w-full text-left py-2.5 px-3 text-sm font-medium text-gray-600 hover:text-[#0D1B3E] hover:bg-gray-50 rounded-lg transition-colors">{link}</button>
            ))}
            {!currentUser && <button onClick={() => { setMenuOpen(false); onNavigate("login"); }} className="block w-full text-left py-2.5 px-3 text-sm font-semibold text-[#00B4D8]">Đăng nhập</button>}
          </div>
        )}
      </header>

      {/* ─── HERO ─── */}
      <section className="cp-landing-hero cp-home-v11 relative min-h-screen flex items-center pt-16 overflow-hidden">
        {/* Background texture */}
        <div className="absolute inset-0 opacity-5" style={{ backgroundImage: "radial-gradient(circle at 1px 1px, white 1px, transparent 0)", backgroundSize: "40px 40px" }} />
        <div className="absolute top-0 right-0 w-1/2 h-full bg-gradient-to-l from-[#00B4D8]/8 to-transparent" />

        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 py-12 w-full">
          <div className="grid lg:grid-cols-[1.02fr_.98fr] gap-12 items-center">

            {/* ── Left column ── */}
            <div>
              <div className="inline-flex items-center gap-2 bg-[#00B4D8]/15 border border-[#00B4D8]/30 text-[#00B4D8] px-4 py-1.5 rounded-full text-sm font-semibold mb-7">
                <span className="w-2 h-2 bg-[#00B4D8] rounded-full animate-pulse" />
                Bộ sưu tập xe chọn lọc · Sẵn sàng cho hành trình mới
              </div>

              <h1 className="text-5xl lg:text-6xl font-extrabold text-white leading-[1.1] mb-6 tracking-tight">
                Chọn chiếc xe<br />
                <span className="text-[#55D9F4]">xứng tầm</span> hành trình<br />
                của riêng bạn.
              </h1>

              <p className="text-white/60 text-lg mb-8 leading-relaxed max-w-md">
                Từ chuyến đi cuối tuần đến lịch trình công việc, CARRENT PRO giúp bạn tìm, đặt và quản lý hành trình trên một trải nghiệm liền mạch.
              </p>

              {/* Booking widget */}
              <div className="cp-home-search bg-white rounded-2xl p-5 shadow-2xl shadow-black/30 border border-white/10">
                <p className="text-xs font-bold text-gray-400 uppercase tracking-widest mb-4">Tìm xe phù hợp</p>
                <div className="grid sm:grid-cols-3 gap-3 mb-4">
                  <div className="relative">
                    <MapPin size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#00B4D8]" />
                    <input type="text" placeholder="Địa điểm nhận xe" value={location} onChange={e => setLocation(e.target.value)}
                      className="w-full pl-8 pr-3 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/10 transition-all bg-gray-50 placeholder:text-gray-400" />
                  </div>
                  <div className="relative">
                    <Calendar size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#00B4D8]" />
                    <input type="date" value={pickup} onChange={e => setPickup(e.target.value)}
                      className="w-full pl-8 pr-3 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/10 transition-all bg-gray-50 text-gray-700" />
                  </div>
                  <div className="relative">
                    <Calendar size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#00B4D8]" />
                    <input type="date" value={returnDate} onChange={e => setReturnDate(e.target.value)}
                      className="w-full pl-8 pr-3 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] focus:ring-2 focus:ring-[#00B4D8]/10 transition-all bg-gray-50 text-gray-700" />
                  </div>
                </div>
                <button onClick={handleSearch}
                  className="w-full bg-[#0D1B3E] text-white py-3.5 rounded-xl font-bold flex items-center justify-center gap-2 hover:bg-[#1A3A6E] active:scale-[0.99] transition-all text-sm shadow-md">
                  <Search size={16} /> Tìm xe ngay
                </button>
              </div>

              {/* Trust badges */}
              <div className="flex items-center gap-5 mt-6">
                <div className="flex -space-x-2">
                  {["H", "M", "B", "A"].map((l, i) => (
                    <div key={i} className="w-8 h-8 rounded-full border-2 border-[#0D1B3E] bg-[#1A3A6E] text-white text-xs font-bold flex items-center justify-center">{l}</div>
                  ))}
                </div>
                <div>
                  <div className="flex items-center gap-0.5 mb-0.5">
                    {Array.from({ length: 5 }).map((_, i) => <Star key={i} size={12} className="fill-amber-400 text-amber-400" />)}
                    <span className="text-amber-400 text-xs font-bold ml-1">{realReviews.length ? avgRating.toFixed(1) : "—"}</span>
                  </div>
                  <p className="text-white/50 text-xs">{realReviews.length ? `${realReviews.length} đánh giá thực` : "Dữ liệu đánh giá từ hệ thống"}</p>
                </div>
              </div>
            </div>

            {/* ── Right column ── */}
            <div className="cp-hero-visual cp-home-showcase hidden lg:block relative">
              {/* Main car image */}
              <div className="cp-hero-frame cp-home-car-frame relative rounded-3xl overflow-hidden shadow-2xl shadow-black/40">
                <img
                  src={featuredCars[0]?.image || ""}
                  alt={featuredCars[0]?.name || "Xe cao cấp"}
                  className="w-full h-full object-cover"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-[#0D1B3E]/60 via-transparent to-transparent" />

                {/* Floating car info card */}
                <div className="absolute bottom-4 left-4 right-4 bg-white/10 backdrop-blur-xl border border-white/20 rounded-2xl p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <div className="text-white font-bold text-base">{featuredCars[0]?.name || "CARRENT PRO Premium"}</div>
                      <div className="text-white/60 text-xs mt-0.5">{featuredCars[0]?.brand || "Premium"} · {featuredCars[0]?.type || "Xe cao cấp"}</div>
                    </div>
                    <div className="text-right">
                      <div className="text-[#00B4D8] font-extrabold text-lg">{Number(featuredCars[0]?.pricePerDay || 0).toLocaleString("vi-VN")}đ</div>
                      <div className="text-white/50 text-xs">/ngày</div>
                    </div>
                  </div>
                  <button onClick={() => onNavigate("browse")} className="mt-3 w-full bg-[#00B4D8] text-white text-xs font-bold py-2 rounded-xl hover:bg-[#0099bb] transition-colors flex items-center justify-center gap-1.5">
                    Đặt ngay <ArrowRight size={12} />
                  </button>
                </div>
              </div>

              {/* Stats cards row */}
              <div className="grid grid-cols-3 gap-3 mt-4">
                {[
                  { label: "Xe hệ thống", value: String(cars.length), icon: Car, color: "text-[#00B4D8]" },
                  { label: "Khách hài lòng", value: realReviews.length ? `${satisfiedRate}%` : "—", icon: TrendingUp, color: "text-emerald-400" },
                  { label: "Hỗ trợ liên tục", value: "24/7", icon: Zap, color: "text-amber-400" },
                ].map(s => (
                  <div key={s.label} className="bg-white/8 backdrop-blur-md border border-white/15 rounded-2xl p-4 text-center hover:bg-white/12 transition-colors">
                    <s.icon size={18} className={`${s.color} mx-auto mb-2`} />
                    <div className="text-white font-extrabold text-xl leading-none">{s.value}</div>
                    <div className="text-white/50 text-xs mt-1">{s.label}</div>
                  </div>
                ))}
              </div>

              {/* Floating badge top-right */}
              <div className="absolute -top-4 -right-4 bg-[#00B4D8] text-white rounded-2xl px-4 py-3 shadow-xl shadow-[#00B4D8]/30">
                <div className="text-xs font-semibold opacity-80">Trải nghiệm đặt xe</div>
                <div className="text-xl font-extrabold leading-tight">Nhanh · rõ · tiện</div>
              </div>
            </div>
          </div>
        </div>

        {/* Curved bottom */}
        <div className="absolute bottom-0 left-0 right-0">
          <svg viewBox="0 0 1440 60" className="w-full fill-[#F6F8FB]" preserveAspectRatio="none">
            <path d="M0,60 C360,0 1080,0 1440,60 L1440,60 L0,60 Z" />
          </svg>
        </div>
      </section>

      {/* ─── BRANDS ─── */}
      <section id="brands-section" className="cp-home-brands py-16 bg-[#F6F8FB]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="text-center mb-10">
            <p className="text-xs font-bold uppercase tracking-widest text-[#00B4D8] mb-3">Đối tác thương hiệu</p>
            <h2 className="text-3xl font-extrabold text-[#0D1B3E] mb-3">Hãng xe phổ biến</h2>
            <p className="text-gray-500">Đội xe đa dạng từ các thương hiệu danh tiếng thế giới</p>
          </div>
          <div className="cp-brand-grid grid gap-4">
            {brands.map(brand => (
              <button key={brand.id} onClick={() => onNavigate("browse")}
                className="bg-white rounded-2xl p-5 flex flex-col items-center gap-3 border border-gray-100 hover:border-[#00B4D8] hover:shadow-lg hover:-translate-y-1 transition-all duration-200 group">
                <div className="w-14 h-14 bg-[#0D1B3E] rounded-xl flex items-center justify-center group-hover:bg-[#00B4D8] transition-colors shadow-sm">
                  <span className="text-white text-xs font-extrabold tracking-tight">{brand.logo}</span>
                </div>
                <div>
                  <div className="text-xs font-bold text-gray-700 text-center">{brand.name}</div>
                  <div className="text-xs text-gray-400 text-center">{brand.count} xe</div>
                </div>
              </button>
            ))}
          </div>
        </div>
      </section>

      {/* ─── FEATURED CARS ─── */}
      <section className="cp-home-featured py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="flex items-end justify-between mb-10">
            <div>
              <p className="text-xs font-bold uppercase tracking-widest text-[#00B4D8] mb-2">Lựa chọn hàng đầu</p>
              <h2 className="text-3xl font-extrabold text-[#0D1B3E] mb-1.5">Xe nổi bật</h2>
              <p className="text-gray-500">Những lựa chọn được khách hàng yêu thích nhất</p>
            </div>
            <button onClick={() => onNavigate("browse")} className="hidden sm:flex items-center gap-2 text-sm font-bold text-[#0D1B3E] border-2 border-[#0D1B3E] px-4 py-2 rounded-xl hover:bg-[#0D1B3E] hover:text-white transition-all">
              Xem tất cả <ArrowRight size={15} />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {featuredCars.map((car, i) => (
              <div key={car.id} onClick={() => onNavigate("car-detail", car)} className={`bg-white rounded-3xl border overflow-hidden cursor-pointer group transition-all duration-300 hover:shadow-2xl hover:-translate-y-1 ${i === 0 ? "border-[#00B4D8] ring-2 ring-[#00B4D8]/20" : "border-gray-100"}`}>
                {i === 0 && <div className="bg-[#00B4D8] text-white text-center text-xs font-bold py-1.5 tracking-wide">⭐ PHỔ BIẾN NHẤT</div>}
                <div className="relative overflow-hidden h-52 bg-gray-50">
                  <img src={car.image} alt={car.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
                  <div className="absolute inset-0 bg-gradient-to-t from-black/20 to-transparent" />
                  <div className="absolute top-3 left-3">
                    <span className="bg-emerald-500 text-white text-xs font-bold px-2.5 py-1 rounded-full shadow-sm">● Có sẵn</span>
                  </div>
                  <div className="absolute top-3 right-3 bg-white/95 backdrop-blur-sm rounded-xl px-2.5 py-1.5 flex items-center gap-1 shadow-sm">
                    <Star size={11} className="fill-amber-400 text-amber-400" />
                    <span className="text-xs font-bold text-gray-700">{car.rating || "—"}</span>
                    <span className="text-gray-400 text-xs">({car.reviews || 0})</span>
                  </div>
                </div>
                <div className="p-5">
                  <div className="mb-3">
                    <h3 className="font-extrabold text-[#0D1B3E] text-base">{car.name}</h3>
                    <p className="text-gray-400 text-sm mt-0.5">{car.brand} · {car.type}{car.year ? ` · ${car.year}` : ""}</p>
                  </div>
                  <div className="flex items-center gap-3 py-3 border-y border-gray-50 text-xs text-gray-500 mb-4">
                    <span className="flex items-center gap-1"><Users size={12} className="text-[#00B4D8]" />{car.seats} chỗ</span>
                    <span className="flex items-center gap-1"><Zap size={12} className="text-[#00B4D8]" />{car.transmission}</span>
                    <span className="text-gray-300">·</span>
                    <span>{car.fuel}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <div>
                      <span className="text-2xl font-extrabold text-[#0D1B3E]">{(car.pricePerDay / 1000).toLocaleString()}K</span>
                      <span className="text-gray-400 text-sm font-normal">/ngày</span>
                    </div>
                    <div className="bg-[#0D1B3E] text-white text-sm font-bold px-4 py-2 rounded-xl group-hover:bg-[#00B4D8] transition-colors flex items-center gap-1.5">
                      Đặt xe <ArrowRight size={13} />
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ─── HOW IT WORKS ─── */}
      <section className="cp-home-process py-16 bg-[#F6F8FB]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="text-center mb-12">
            <p className="text-xs font-bold uppercase tracking-widest text-[#00B4D8] mb-3">Quy trình</p>
            <h2 className="text-3xl font-extrabold text-[#0D1B3E] mb-3">Thuê xe chỉ 4 bước</h2>
            <p className="text-gray-500">Đơn giản, nhanh chóng, không rắc rối</p>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 relative">
            {/* connector line */}
            <div className="hidden md:block absolute top-10 left-[12.5%] right-[12.5%] h-px bg-gradient-to-r from-transparent via-[#00B4D8]/40 to-transparent" />
            {[
              { step: "01", title: "Chọn xe", desc: "Duyệt danh sách xe theo hãng, loại, giá phù hợp", icon: Search, color: "bg-blue-50 text-blue-600" },
              { step: "02", title: "Đặt lịch", desc: "Chọn ngày nhận — trả xe, điền thông tin cá nhân", icon: Calendar, color: "bg-violet-50 text-violet-600" },
              { step: "03", title: "Xác nhận", desc: "Ký hợp đồng online và thanh toán tiền cọc", icon: CheckCircle, color: "bg-emerald-50 text-emerald-600" },
              { step: "04", title: "Nhận xe", desc: "Đến điểm hẹn, nhận xe và bắt đầu hành trình", icon: Car, color: "bg-amber-50 text-amber-600" },
            ].map(item => (
              <div key={item.step} className="bg-white rounded-2xl p-6 border border-gray-100 text-center relative hover:shadow-lg transition-all hover:-translate-y-0.5">
                <div className="absolute -top-3.5 left-1/2 -translate-x-1/2 bg-[#0D1B3E] text-white text-xs font-extrabold px-3 py-1 rounded-full tracking-widest">{item.step}</div>
                <div className={`w-14 h-14 ${item.color} rounded-2xl flex items-center justify-center mx-auto mb-4 mt-3`}>
                  <item.icon size={22} />
                </div>
                <h3 className="font-extrabold text-[#0D1B3E] mb-2">{item.title}</h3>
                <p className="text-gray-500 text-sm leading-relaxed">{item.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ─── WHY CHOOSE US ─── */}
      <section id="why-section" className="cp-home-why py-16 bg-[#0D1B3E] overflow-hidden">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="grid lg:grid-cols-2 gap-12 items-center">
            <div>
              <p className="text-xs font-bold uppercase tracking-widest text-[#00B4D8] mb-4">Vì sao chọn chúng tôi</p>
              <h2 className="text-3xl font-extrabold text-white mb-5 leading-tight">
                Trải nghiệm thuê xe<br /><span className="text-[#00B4D8]">đẳng cấp khác biệt</span>
              </h2>
              <p className="text-white/55 mb-8 leading-relaxed">Chúng tôi không chỉ cho thuê xe — chúng tôi mang đến hành trình hoàn hảo với đội ngũ hỗ trợ chuyên nghiệp 24/7.</p>
              <div className="space-y-5">
                {[
                  { icon: Shield, title: "An toàn & Uy tín", desc: "Xe kiểm định định kỳ, bảo hiểm đầy đủ, đội ngũ chuyên nghiệp", color: "bg-blue-500/20 text-blue-400" },
                  { icon: Zap, title: "Nhanh chóng & Tiện lợi", desc: "Trải nghiệm đặt xe 5 phút, giao xe tận nơi hoặc tại địa điểm theo yêu cầu", color: "bg-amber-500/20 text-amber-400" },
                  { icon: Award, title: "Giá minh bạch", desc: "Không phí ẩn, hợp đồng rõ ràng, nhiều hình thức thanh toán", color: "bg-emerald-500/20 text-emerald-400" },
                  { icon: HeadphonesIcon, title: "Hỗ trợ 24/7", desc: "Hotline, chat và hỗ trợ tận nơi — luôn bên bạn mọi lúc", color: "bg-violet-500/20 text-violet-400" },
                ].map(item => (
                  <div key={item.title} className="flex gap-4 group">
                    <div className={`w-11 h-11 ${item.color} rounded-xl flex items-center justify-center shrink-0 group-hover:scale-110 transition-transform`}>
                      <item.icon size={18} />
                    </div>
                    <div>
                      <h4 className="font-bold text-white mb-1">{item.title}</h4>
                      <p className="text-white/50 text-sm leading-relaxed">{item.desc}</p>
                    </div>
                  </div>
                ))}
              </div>
              <button onClick={() => onNavigate("browse")} className="mt-8 flex items-center gap-2 bg-[#00B4D8] text-white font-bold px-6 py-3 rounded-xl hover:bg-[#0099bb] transition-colors shadow-lg shadow-[#00B4D8]/25">
                Khám phá xe ngay <ChevronRight size={16} />
              </button>
            </div>

            <div className="relative">
              <div className="relative rounded-3xl overflow-hidden">
                <img src={featuredCars[0]?.image || cars[0]?.image || ""} alt="Xe nổi bật CARRENT PRO" className="w-full h-[420px] object-cover" />
                <div className="absolute inset-0 bg-gradient-to-t from-[#0D1B3E]/70 via-transparent to-transparent" />
              </div>
              {/* floating rating */}
              <div className="absolute bottom-6 left-6 right-6 bg-white/10 backdrop-blur-xl border border-white/25 rounded-2xl p-5">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 bg-[#00B4D8] rounded-xl flex items-center justify-center shrink-0">
                    <Star size={20} className="fill-white text-white" />
                  </div>
                  <div className="flex-1">
                    <div className="text-white font-extrabold text-lg leading-none mb-1">{realReviews.length ? `${avgRating.toFixed(1)} / 5.0` : "Chưa có đánh giá"}</div>
                    <div className="flex items-center gap-0.5 mb-1">{Array.from({ length: 5 }).map((_, i) => <Star key={i} size={12} className="fill-amber-400 text-amber-400" />)}</div>
                    <div className="text-white/55 text-xs">{realReviews.length ? `Dựa trên ${realReviews.length} đánh giá đang hiển thị` : "Chưa có phản hồi công khai"}</div>
                  </div>
                </div>
              </div>
              {/* badge top right */}
              <div className="absolute -top-4 -right-4 bg-emerald-500 text-white rounded-2xl px-4 py-3 shadow-xl text-center">
                <div className="text-xl font-extrabold leading-none">{realReviews.length ? `${satisfiedRate}%` : "—"}</div>
                <div className="text-xs font-medium opacity-80 mt-0.5">Hài lòng</div>
              </div>
            </div>

          </div>
        </div>
      </section>

      {/* ─── STATS ─── */}
      <section className="cp-home-stats py-14 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-5">
            {[
              { value: String(cars.length), label: "Xe trong hệ thống", sub: `${cars.filter(c => c.status === "AVAILABLE").length} xe sẵn sàng`, icon: Car, color: "bg-blue-50 text-blue-600" },
              { value: String(brandCount), label: "Thương hiệu", sub: "Dữ liệu đội xe thật", icon: Award, color: "bg-violet-50 text-violet-600" },
              { value: String(realReviews.length), label: "Đánh giá", sub: "Đang hiển thị công khai", icon: TrendingUp, color: "bg-emerald-50 text-emerald-600" },
              { value: realReviews.length ? `${avgRating.toFixed(1)}★` : "—", label: "Đánh giá TB", sub: "Tính từ dữ liệu thật", icon: Star, color: "bg-amber-50 text-amber-500" },
            ].map(s => (
              <div key={s.label} className="bg-[#F6F8FB] rounded-2xl p-6 flex items-center gap-4 hover:shadow-md transition-all border border-transparent hover:border-gray-200">
                <div className={`w-12 h-12 ${s.color} rounded-xl flex items-center justify-center shrink-0`}><s.icon size={20} /></div>
                <div>
                  <div className="text-3xl font-extrabold text-[#0D1B3E] leading-none">{s.value}</div>
                  <div className="text-sm font-semibold text-gray-600 mt-1">{s.label}</div>
                  <div className="text-xs text-gray-400">{s.sub}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ─── REVIEWS ─── */}
      <section className="cp-home-reviews py-16 bg-[#F6F8FB]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="text-center mb-10">
            <p className="text-xs font-bold uppercase tracking-widest text-[#00B4D8] mb-3">Phản hồi thực</p>
            <h2 className="text-3xl font-extrabold text-[#0D1B3E] mb-3">Khách hàng nói gì?</h2>
            <p className="text-gray-500">{realReviews.length ? `${realReviews.length} đánh giá thực đang được hiển thị` : "Chưa có đánh giá công khai"}</p>
          </div>

          {/* Desktop grid */}
          <div className="hidden md:grid md:grid-cols-3 gap-6">
            {reviews.map(r => (
              <div key={r.id || r.name} className="bg-white rounded-3xl p-6 border border-gray-100 hover:shadow-xl transition-all hover:-translate-y-0.5">
                <div className="flex items-center gap-1 mb-4">
                  {Array.from({ length: r.rating }).map((_, i) => <Star key={i} size={14} className="fill-amber-400 text-amber-400" />)}
                  <span className="ml-auto text-xs font-bold text-gray-400">{Number(r.rating).toFixed(1)}</span>
                </div>
                <p className="text-gray-600 text-sm leading-relaxed mb-6">"{r.text}"</p>
                <div className="flex items-center gap-3 border-t border-gray-50 pt-4">
                  <div className="w-10 h-10 rounded-full bg-[#0D1B3E] flex items-center justify-center text-white text-sm font-extrabold shadow-md">{r.avatar}</div>
                  <div>
                    <div className="font-extrabold text-[#0D1B3E] text-sm">{r.name}</div>
                    <div className="text-gray-400 text-xs">{r.role}</div>
                  </div>
                  <div className="ml-auto w-8 h-8 bg-emerald-50 rounded-full flex items-center justify-center">
                    <CheckCircle size={16} className="text-emerald-500" />
                  </div>
                </div>
              </div>
            ))}
          </div>
          {reviews.length === 0 && <div className="bg-white rounded-3xl border border-gray-100 p-10 text-center text-gray-400">Chưa có đánh giá công khai. Đánh giá sẽ xuất hiện sau khi khách hoàn thành hợp đồng.</div>}

          {/* Mobile carousel */}
          <div className={`md:hidden ${reviews.length ? "" : "hidden"}`}>
            <div className="bg-white rounded-3xl p-6 border border-gray-100 shadow-sm">
              <div className="flex items-center gap-1 mb-4">
                {Array.from({ length: reviews[activeReview]?.rating || 0 }).map((_, i) => <Star key={i} size={14} className="fill-amber-400 text-amber-400" />)}
              </div>
              <p className="text-gray-600 text-sm leading-relaxed mb-6">"{reviews[activeReview]?.text || ""}"</p>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-[#0D1B3E] flex items-center justify-center text-white text-sm font-bold">{reviews[activeReview]?.avatar || "?"}</div>
                <div>
                  <div className="font-bold text-[#0D1B3E] text-sm">{reviews[activeReview]?.name || ""}</div>
                  <div className="text-gray-400 text-xs">{reviews[activeReview]?.role || ""}</div>
                </div>
              </div>
            </div>
            <div className="flex items-center justify-center gap-3 mt-4">
              <button onClick={() => setActiveReview(p => (p - 1 + reviews.length) % reviews.length)} className="w-9 h-9 rounded-full border border-gray-200 flex items-center justify-center text-gray-600 hover:border-[#0D1B3E]"><ChevronLeft size={16} /></button>
              {reviews.map((_, i) => <div key={i} className={`w-2 h-2 rounded-full transition-all ${i === activeReview ? "bg-[#0D1B3E] w-5" : "bg-gray-300"}`} />)}
              <button onClick={() => setActiveReview(p => (p + 1) % reviews.length)} className="w-9 h-9 rounded-full border border-gray-200 flex items-center justify-center text-gray-600 hover:border-[#0D1B3E]"><ChevronRight size={16} /></button>
            </div>
          </div>
        </div>
      </section>

      {/* ─── CTA ─── */}
      <section className="cp-home-cta py-16 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-[#0D1B3E] via-[#1A3A6E] to-[#00B4D8]" />
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: "radial-gradient(circle at 2px 2px, white 1px, transparent 0)", backgroundSize: "32px 32px" }} />
        <div className="relative z-10 max-w-3xl mx-auto px-4 text-center">
          <div className="inline-flex items-center gap-2 bg-white/15 border border-white/25 text-white px-4 py-1.5 rounded-full text-sm font-semibold mb-6">
            <Zap size={14} className="text-amber-400" /> Đặt xe trực tuyến nhanh chóng
          </div>
          <h2 className="text-4xl font-extrabold text-white mb-4 leading-tight">Sẵn sàng cho<br />hành trình của bạn?</h2>
          <p className="text-white/70 mb-8 text-lg">Chọn xe, gửi yêu cầu và theo dõi toàn bộ hành trình thuê xe ngay trên CARRENT PRO.</p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button onClick={() => onNavigate("browse")} className="bg-white text-[#0D1B3E] font-extrabold px-8 py-4 rounded-2xl hover:bg-gray-50 transition-colors inline-flex items-center justify-center gap-2 shadow-xl text-sm">
              Tìm xe ngay <ChevronRight size={17} />
            </button>
            <button onClick={() => onNavigate("login")} className="border-2 border-white/40 text-white font-bold px-8 py-4 rounded-2xl hover:bg-white/10 transition-colors inline-flex items-center justify-center gap-2 text-sm">
              Đăng nhập ngay
            </button>
          </div>
        </div>
      </section>

      {/* ─── FOOTER ─── */}
      <footer className="bg-[#040d1e] text-white py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="grid md:grid-cols-4 gap-8 mb-10">
            <div>
              <div className="flex items-center gap-2.5 mb-4">
                <div className="w-9 h-9 bg-[#00B4D8] rounded-xl flex items-center justify-center"><Car size={17} className="text-white" /></div>
                <span className="font-extrabold text-lg">CARRENT<span className="text-[#00B4D8]"> PRO</span></span>
              </div>
              <p className="text-white/40 text-sm leading-relaxed">Nền tảng thuê xe trực tuyến uy tín hàng đầu Việt Nam. Thuê xe dễ dàng — Hành trình trọn vẹn.</p>
            </div>
            {[
              { title: "Dịch vụ", links: ["Danh sách xe", "Đặt xe online", "Thuê dài hạn", "Thuê có tài xế"] },
              { title: "Hỗ trợ", links: ["Trung tâm trợ giúp", "Theo dõi thuê xe", "Chính sách hủy", "Liên hệ"] },
              { title: "Công ty", links: ["Về chúng tôi", "Điều khoản", "Bảo mật", "Tuyển dụng"] },
            ].map(col => (
              <div key={col.title}>
                <div className="text-sm font-bold mb-4 text-white/80">{col.title}</div>
                <ul className="space-y-2">
                  {col.links.map(link => <li key={link}><button className="text-white/40 text-sm hover:text-white transition-colors text-left">{link}</button></li>)}
                </ul>
              </div>
            ))}
          </div>
          <div className="border-t border-white/10 pt-8 flex flex-col sm:flex-row items-center justify-between gap-4">
            <p className="text-white/30 text-sm">© 2024 CARRENT PRO. All rights reserved.</p>
            <div className="flex items-center gap-2">
              <Phone size={14} className="text-[#00B4D8]" />
              <span className="text-white/50 text-sm">Hotline: <span className="text-white font-semibold">1800 1234</span></span>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
