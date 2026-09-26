import { useEffect, useState } from "react";
import { Search, SlidersHorizontal, Star, Users, ChevronLeft, X, Car, Grid, List } from "lucide-react";
import { vehiclesApi } from "../../services/api";
import Badge from "../../components/shared/Badge";

export default function BrowseCarsPage({ onNavigate }) {
  const [cars, setCars] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [search, setSearch] = useState("");
  const [selectedBrands, setSelectedBrands] = useState([]);
  const [selectedTypes, setSelectedTypes] = useState([]);
  const [maxPrice, setMaxPrice] = useState(5000000);
  const [sortBy, setSortBy] = useState("popular");
  const [showFilter, setShowFilter] = useState(false);
  const [viewMode, setViewMode] = useState("grid");

  useEffect(() => {
    vehiclesApi.list().then(setCars).catch(e => setLoadError(e.message)).finally(() => setLoading(false));
  }, []);

  const allBrands = [...new Set(cars.map(c => c.brand))];
  const allTypes = [...new Set(cars.map(c => c.type))];

  const toggleBrand = (b) => setSelectedBrands(prev => prev.includes(b) ? prev.filter(x => x !== b) : [...prev, b]);
  const toggleType = (t) => setSelectedTypes(prev => prev.includes(t) ? prev.filter(x => x !== t) : [...prev, t]);

  const filtered = cars
    .filter(c => String(c.status || "").toUpperCase() === "AVAILABLE")
    .filter(c => !search || String(c.name || '').toLowerCase().includes(search.toLowerCase()) || String(c.brand || '').toLowerCase().includes(search.toLowerCase()))
    .filter(c => selectedBrands.length === 0 || selectedBrands.includes(c.brand))
    .filter(c => selectedTypes.length === 0 || selectedTypes.includes(c.type))
    .filter(c => c.pricePerDay <= maxPrice)
    .sort((a, b) => {
      if (sortBy === "price-asc") return a.pricePerDay - b.pricePerDay;
      if (sortBy === "price-desc") return b.pricePerDay - a.pricePerDay;
      if (sortBy === "rating") return Number(b.rating || 0) - Number(a.rating || 0);
      return Number(b.reviews || 0) - Number(a.reviews || 0);
    });

  const StatusBadge = ({ status }) => {
    const map = {
      AVAILABLE: "available", RESERVED: "reserved", RENTING: "renting",
    };
    return <Badge variant={map[status] || "inactive"} />;
  };

  return (
    <div className="cp-customer premium-browse min-h-screen">
      {/* Top bar */}
      <div className="premium-glassbar sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-14 flex items-center gap-4">
          <button onClick={() => onNavigate("landing")} className="flex items-center gap-1.5 text-gray-500 hover:text-[#0D1B3E] text-sm font-medium transition-colors shrink-0">
            <ChevronLeft size={18} /><span className="hidden sm:inline">Trang chủ</span>
          </button>
          <div className="relative flex-1 max-w-md">
            <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Tìm xe, hãng xe..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-colors"
            />
          </div>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="text-sm border border-gray-200 rounded-xl px-3 py-2 focus:outline-none focus:border-[#00B4D8] bg-white text-gray-700"
          >
            <option value="popular">Phổ biến</option>
            <option value="price-asc">Giá thấp → cao</option>
            <option value="price-desc">Giá cao → thấp</option>
            <option value="rating">Đánh giá cao</option>
          </select>
          <div className="flex items-center gap-1.5 border border-gray-200 rounded-xl p-1">
            <button onClick={() => setViewMode("grid")} className={`p-1.5 rounded-lg transition-colors ${viewMode === "grid" ? "bg-[#0D1B3E] text-white" : "text-gray-400 hover:text-gray-700"}`}>
              <Grid size={14} />
            </button>
            <button onClick={() => setViewMode("list")} className={`p-1.5 rounded-lg transition-colors ${viewMode === "list" ? "bg-[#0D1B3E] text-white" : "text-gray-400 hover:text-gray-700"}`}>
              <List size={14} />
            </button>
          </div>
          <button onClick={() => setShowFilter(!showFilter)} className="sm:hidden flex items-center gap-1.5 border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-600">
            <SlidersHorizontal size={15} /> Lọc
          </button>
        </div>
      </div>

      <div className="premium-shell max-w-[1500px] mx-auto px-4 sm:px-8 py-8 flex gap-7">
        {/* Sidebar filter */}
        <aside className={`${showFilter ? "fixed inset-0 z-50 bg-black/50 sm:relative sm:bg-transparent sm:inset-auto" : "hidden sm:block"} sm:w-64 shrink-0`}>
          <div className={`${showFilter ? "absolute left-0 top-0 h-full w-72 bg-white overflow-y-auto p-5" : ""} sm:bg-white sm:rounded-2xl sm:border sm:border-gray-100 sm:p-5 sm:sticky sm:top-20 sm:max-h-[calc(100vh-6rem)] sm:overflow-y-auto`}>
            <div className="flex items-center justify-between mb-5">
              <h3 className="font-bold text-[#0D1B3E] flex items-center gap-2"><SlidersHorizontal size={16} /> Bộ lọc</h3>
              {showFilter && <button onClick={() => setShowFilter(false)} className="sm:hidden text-gray-400"><X size={20} /></button>}
            </div>

            <div className="space-y-6">
              <div>
                <h4 className="font-semibold text-[#0D1B3E] text-sm mb-3">Hãng xe</h4>
                <div className="space-y-2">
                  {allBrands.map(b => (
                    <label key={b} className="flex items-center gap-2.5 cursor-pointer group">
                      <input
                        type="checkbox"
                        checked={selectedBrands.includes(b)}
                        onChange={() => toggleBrand(b)}
                        className="w-4 h-4 accent-[#0D1B3E] rounded"
                      />
                      <span className="text-sm text-gray-600 group-hover:text-[#0D1B3E] transition-colors">{b}</span>
                    </label>
                  ))}
                </div>
              </div>

              <div>
                <h4 className="font-semibold text-[#0D1B3E] text-sm mb-3">Loại xe</h4>
                <div className="space-y-2">
                  {allTypes.map(t => (
                    <label key={t} className="flex items-center gap-2.5 cursor-pointer group">
                      <input
                        type="checkbox"
                        checked={selectedTypes.includes(t)}
                        onChange={() => toggleType(t)}
                        className="w-4 h-4 accent-[#0D1B3E] rounded"
                      />
                      <span className="text-sm text-gray-600 group-hover:text-[#0D1B3E] transition-colors">{t}</span>
                    </label>
                  ))}
                </div>
              </div>

              <div>
                <h4 className="font-semibold text-[#0D1B3E] text-sm mb-3">Giá tối đa / ngày</h4>
                <input
                  type="range"
                  min={500000}
                  max={5000000}
                  step={100000}
                  value={maxPrice}
                  onChange={(e) => setMaxPrice(Number(e.target.value))}
                  className="w-full accent-[#00B4D8]"
                />
                <div className="flex justify-between text-xs text-gray-500 mt-1">
                  <span>500K</span>
                  <span className="font-semibold text-[#0D1B3E]">{(maxPrice / 1000).toLocaleString()}K</span>
                  <span>5.000K</span>
                </div>
              </div>

              <div className="flex gap-2">
                <button
                  onClick={() => { setSelectedBrands([]); setSelectedTypes([]); setMaxPrice(5000000); }}
                  className="flex-1 py-2 border border-gray-200 rounded-xl text-sm text-gray-600 hover:bg-gray-50 transition-colors font-medium"
                >
                  Đặt lại
                </button>
                <button
                  onClick={() => setShowFilter(false)}
                  className="flex-1 py-2 bg-[#0D1B3E] text-white rounded-xl text-sm font-semibold hover:bg-[#1A3A6E] transition-colors"
                >
                  Áp dụng
                </button>
              </div>
            </div>
          </div>
        </aside>

        {/* Main content */}
        <main className="flex-1 min-w-0">
          <section className="catalog-hero mb-7">
            <div className="catalog-orb catalog-orb-a" />
            <div className="catalog-orb catalog-orb-b" />
            <div className="relative z-10 max-w-2xl">
              <div className="catalog-kicker">CARRENT PRO COLLECTION</div>
              <h1>Chọn chiếc xe<br/><span>xứng tầm hành trình.</span></h1>
              <p>Đội xe được tuyển chọn kỹ, giá minh bạch và quy trình thuê xe số hóa từ đặt xe đến bàn giao.</p>
              <div className="catalog-pills"><span>✓ Xe xác minh</span><span>✓ Giá rõ ràng</span><span>✓ Hỗ trợ 24/7</span></div>
            </div>
            <div className="catalog-stat"><strong>{filtered.length}</strong><span>xe đang sẵn sàng</span></div>
          </section>
          <div className="flex items-center justify-between mb-5">
            <div>
              <h1 className="text-xl font-bold text-[#0D1B3E]">Danh sách xe</h1>
              <p className="text-sm text-gray-500 mt-0.5">Tìm thấy <span className="font-semibold text-[#0D1B3E]">{filtered.length}</span> xe phù hợp</p>
            </div>
          </div>

          {loading ? (<div className="bg-white rounded-2xl p-12 text-center text-gray-500">Đang tải xe từ backend...</div>) : loadError ? (<div className="bg-red-50 border border-red-200 rounded-2xl p-6 text-red-700">Không tải được xe: {loadError}</div>) : filtered.length === 0 ? (
            <div className="bg-white rounded-2xl border border-gray-100 p-16 text-center">
              <Car size={48} className="text-gray-300 mx-auto mb-4" />
              <h3 className="font-bold text-gray-700 text-lg mb-2">Không tìm thấy xe</h3>
              <p className="text-gray-400 text-sm">Hãy thử thay đổi bộ lọc hoặc tìm kiếm khác.</p>
            </div>
          ) : viewMode === "grid" ? (
            <div className="premium-car-grid grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              {filtered.map((car) => (
                <div key={car.id} className="bg-white rounded-2xl border border-gray-100 overflow-hidden hover:shadow-lg transition-all group">
                  <div className="relative h-48 overflow-hidden bg-gray-100">
                    <img src={car.image} alt={car.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
                    <div className="absolute top-3 left-3"><StatusBadge status={car.status} /></div>
                    <div className="absolute top-3 right-3 bg-white/90 backdrop-blur-sm rounded-lg px-2 py-1 flex items-center gap-1">
                      <Star size={12} className="fill-amber-400 text-amber-400" />
                      <span className="text-xs font-semibold text-gray-700">{car.rating}</span>
                    </div>
                  </div>
                  <div className="p-4">
                    <h3 className="font-bold text-[#0D1B3E]">{car.name}</h3>
                    <p className="text-gray-400 text-sm">{car.brand} · {car.type}</p>
                    <div className="flex items-center gap-3 py-3 border-t border-gray-50 mt-3 text-xs text-gray-500">
                      <span className="flex items-center gap-1"><Users size={11} />{car.seats} chỗ</span>
                      <span>{car.transmission}</span>
                      <span>{car.fuel}</span>
                    </div>
                    <div className="flex items-center justify-between">
                      <div>
                        <span className="text-lg font-bold text-[#0D1B3E]">{Number(car.pricePerDay || 0).toLocaleString('vi-VN')}đ</span>
                        <span className="text-gray-400 text-xs">/ngày</span>
                      </div>
                      <div className="flex gap-2">
                        <button onClick={() => onNavigate("car-detail", car)} className="text-xs px-3 py-1.5 border border-[#0D1B3E] text-[#0D1B3E] rounded-lg font-medium hover:bg-gray-50 transition-colors">
                          Chi tiết
                        </button>
                        {car.status === "AVAILABLE" && (
                          <button onClick={() => onNavigate("checkout", car)} className="text-xs px-3 py-1.5 bg-[#0D1B3E] text-white rounded-lg font-medium hover:bg-[#1A3A6E] transition-colors">
                            Thuê xe
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="space-y-3">
              {filtered.map((car) => (
                <div key={car.id} className="bg-white rounded-2xl border border-gray-100 p-4 flex gap-4 items-center hover:shadow-md transition-all group">
                  <div className="w-32 h-24 rounded-xl overflow-hidden bg-gray-100 shrink-0">
                    <img src={car.image} alt={car.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <div className="flex items-center gap-2 mb-0.5">
                          <h3 className="font-bold text-[#0D1B3E] text-sm">{car.name}</h3>
                          <StatusBadge status={car.status} />
                        </div>
                        <p className="text-gray-400 text-xs">{car.brand} · {car.type} · {car.seats} chỗ · {car.transmission}</p>
                      </div>
                      <div className="text-right shrink-0">
                        <div className="text-lg font-bold text-[#0D1B3E]">{(car.pricePerDay / 1000).toLocaleString()}K</div>
                        <div className="text-gray-400 text-xs">/ngày</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-2 mt-3">
                      <div className="flex items-center gap-1">
                        <Star size={12} className="fill-amber-400 text-amber-400" />
                        <span className="text-xs font-semibold text-gray-700">{car.rating}</span>
                        <span className="text-xs text-gray-400">({car.reviews})</span>
                      </div>
                      <div className="flex-1" />
                      <button onClick={() => onNavigate("car-detail", car)} className="text-xs px-3 py-1.5 border border-gray-200 text-gray-600 rounded-lg font-medium hover:border-[#0D1B3E] hover:text-[#0D1B3E] transition-colors">
                        Chi tiết
                      </button>
                      {car.status === "AVAILABLE" && (
                        <button onClick={() => onNavigate("checkout", car)} className="text-xs px-3 py-1.5 bg-[#0D1B3E] text-white rounded-lg font-medium hover:bg-[#1A3A6E] transition-colors">
                          Thuê xe
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {filtered.length > 0 && (
            <div className="flex items-center justify-center gap-2 mt-8">
              {[1, 2, 3].map((p) => (
                <button key={p} className={`w-9 h-9 rounded-xl text-sm font-medium transition-colors ${p === 1 ? "bg-[#0D1B3E] text-white" : "bg-white border border-gray-200 text-gray-600 hover:border-[#0D1B3E]"}`}>
                  {p}
                </button>
              ))}
            </div>
          )}
        </main>
      </div>
    </div>
  );
}

function StatusBadge({ status }) {
  const map = {
    AVAILABLE: "available", RESERVED: "reserved", RENTING: "renting",
  };
  return <Badge variant={map[status] || "inactive"} />;
}
