import { useEffect, useMemo, useState } from "react";
import {
  LayoutDashboard, Users, UserCircle, Car, Star, BarChart3, ScrollText,
  Bell, Search, ChevronDown, LogOut, Menu, X, TrendingUp, Shield,
  CheckCircle2, XCircle, RefreshCw, Lock, Unlock, Eye, Activity,
  CreditCard, FileText, Clock3, Plus, Pencil, Upload, Save
} from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, LineChart, Line, Legend
} from "recharts";
import {
  adminApi, vehiclesApi, customersApi, evaluationsApi, authApi, categoriesApi, contractsApi, paymentsApi
} from "../../services/api";
import LiveNotificationBell from "../../components/shared/LiveNotificationBell";
import { pollAdminFlow } from "../../services/liveNotifications";

const NAV_GROUPS = [
  { label: "TỔNG QUAN", items: [{ id: "dashboard", label: "Tổng quan", icon: LayoutDashboard }] },
  { label: "HỆ THỐNG", items: [
    { id: "users", label: "Người dùng", icon: Users },
    { id: "customers", label: "Khách hàng", icon: UserCircle },
  ]},
  { label: "ĐOÀN XE", items: [{ id: "vehicles", label: "Xe", icon: Car }] },
  { label: "GIÁM SÁT", items: [
    { id: "reviews", label: "Đánh giá", icon: Star },
    { id: "stats", label: "Thống kê", icon: BarChart3 },
    { id: "audit", label: "Audit Log", icon: ScrollText },
  ]},
];

const fmtMoney = n => `${Number(n || 0).toLocaleString("vi-VN")} ₫`;
const fmtDate = v => v ? new Date(v).toLocaleString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }) : "—";
const arr = v => Array.isArray(v) ? v : (v?.items || v?.data || []);

const vehicleStatusLabel = {
  AVAILABLE: "Sẵn sàng", RESERVED: "Đã giữ chỗ", RENTING: "Đang thuê",
  MAINTENANCE: "Bảo trì", INACTIVE: "Ngừng hoạt động"
};
const vehicleStatusClass = s => {
  if (s === "AVAILABLE") return "bg-emerald-50 text-emerald-700 border-emerald-200";
  if (s === "RENTING") return "bg-blue-50 text-blue-700 border-blue-200";
  if (s === "RESERVED") return "bg-amber-50 text-amber-700 border-amber-200";
  if (s === "MAINTENANCE") return "bg-orange-50 text-orange-700 border-orange-200";
  return "bg-gray-100 text-gray-600 border-gray-200";
};

export default function AdminDashboard({ adminName, onNavigate, onLogout }) {
  const [activeNav, setActiveNav] = useState("dashboard");
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [summary, setSummary] = useState(null);
  const [users, setUsers] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [vehicles, setVehicles] = useState([]);
  const [reviews, setReviews] = useState([]);
  const [audits, setAudits] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedVehicle, setSelectedVehicle] = useState(null);
  const [reviewCustomer, setReviewCustomer] = useState(null);
  const [vehicleEditor, setVehicleEditor] = useState(null);
  const [brands, setBrands] = useState([]);
  const [types, setTypes] = useState([]);
  const [profileMenu, setProfileMenu] = useState(false);
  const [finance, setFinance] = useState({ monthly: [], yearly: [], paidCount: 0, refundCount: 0 });

  const loadFinance = async () => {
    try {
      const contracts = await contractsApi.discover();
      const groups = await Promise.all(contracts.map(c => paymentsApi.byContract(c.id).catch(() => [])));
      const payments = groups.flatMap(x => arr(x));
      const monthMap = new Map();
      const yearMap = new Map();
      let paidCount = 0, refundCount = 0;
      for (const p of payments) {
        const status = String(p.trangThai || p.status || "").toUpperCase();
        if (!["PAID", "REFUNDED"].includes(status)) continue;
        const rawDate = p.thoiGianThanhToan || p.thoiGianCapNhat || p.updatedAt || p.createdAt || p.thoiGianTao;
        const d = rawDate ? new Date(rawDate) : null;
        if (!d || Number.isNaN(d.getTime())) continue;
        const amount = Number(p.soTien || p.amount || p.tongTien || 0);
        const sign = status === "REFUNDED" ? -1 : 1;
        if (status === "PAID") paidCount++; else refundCount++;
        const mk = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,"0")}`;
        const yk = String(d.getFullYear());
        const m = monthMap.get(mk) || { month: `T${d.getMonth()+1}/${d.getFullYear()}`, paid:0, refunded:0, net:0, sort:d.getFullYear()*100+d.getMonth()+1 };
        if (status === "PAID") m.paid += amount; else m.refunded += amount; m.net += sign*amount; monthMap.set(mk,m);
        const y = yearMap.get(yk) || { year: yk, paid:0, refunded:0, net:0 };
        if (status === "PAID") y.paid += amount; else y.refunded += amount; y.net += sign*amount; yearMap.set(yk,y);
      }
      setFinance({ monthly:[...monthMap.values()].sort((a,b)=>a.sort-b.sort).slice(-12), yearly:[...yearMap.values()].sort((a,b)=>a.year.localeCompare(b.year)), paidCount, refundCount });
    } catch (e) { console.warn("Không tải được thống kê tài chính chi tiết", e); }
  };

  const load = async (silent = false) => {
    try {
      silent ? setRefreshing(true) : setLoading(true);
      setError("");
      const [s, u, c, v, r, a, b, t] = await Promise.all([
        adminApi.reports(), adminApi.users(), customersApi.list(), vehiclesApi.list(),
        evaluationsApi.list(), adminApi.audits(), categoriesApi.brands(), categoriesApi.types()
      ]);
      setSummary(s || {});
      setUsers(arr(u));
      setCustomers(arr(c));
      setVehicles(arr(v));
      setReviews(arr(r));
      setAudits(arr(a));
      setBrands(arr(b)); setTypes(arr(t));
      loadFinance();
    } catch (e) {
      setError(`${e.status ? `HTTP ${e.status} · ` : ""}${e.message || "Không tải được dữ liệu quản trị."}`);
    } finally {
      setLoading(false); setRefreshing(false);
    }
  };

  useEffect(() => { load(); }, []);

  const notify = (text) => { setMessage(text); setTimeout(() => setMessage(""), 2600); };


  const setReviewVisibility = async (review, hienThi) => {
    try {
      const updated = await evaluationsApi.setVisibility(review.id, hienThi);
      setReviews(prev => prev.map(x => Number(x.id) === Number(review.id) ? { ...x, ...updated } : x));
      notify(hienThi ? "Đã hiển thị lại đánh giá trên trang chi tiết xe." : "Đã ẩn đánh giá khỏi trang chi tiết xe.");
      return updated;
    } catch (e) {
      setError(`${e.status ? `HTTP ${e.status} · ` : ""}${e.message || "Không cập nhật được trạng thái đánh giá."}`);
      throw e;
    }
  };
  const updateUserStatus = async user => {
    try {
      await adminApi.setStatus(user.id, !user.dangHoatDong);
      notify(user.dangHoatDong ? `Đã khóa tài khoản #${user.id}` : `Đã bật tài khoản #${user.id}`);
      await load(true);
    } catch (e) { setError(e.message); }
  };

  const updateUserRole = async (user, role) => {
    try {
      await adminApi.setRoles(user.id, [role]);
      notify(`Đã đổi vai trò người dùng #${user.id} thành ${role}`);
      setSelectedUser(null);
      await load(true);
    } catch (e) { setError(e.message); }
  };

  const verifyCustomer = async customer => {
    try {
      const next = !(customer.cccdDaXacMinh && customer.gplxDaXacMinh);
      await customersApi.verifyDocuments(customer.idKhachHang, next, next);
      notify(next ? "Đã xác minh CCCD và GPLX" : "Đã hủy xác minh hồ sơ");
      await load(true);
    } catch (e) { setError(e.message); }
  };

  const updateVehicleStatus = async (vehicle, status) => {
    try {
      await vehiclesApi.setStatus(vehicle.id, status, "Cập nhật từ Admin Dashboard");
      notify(`Đã cập nhật xe #${vehicle.id} → ${vehicleStatusLabel[status] || status}`);
      setSelectedVehicle(null);
      await load(true);
    } catch (e) { setError(e.message); }
  };

  const saveVehicle = async (form, imageFile) => {
    try {
      setError("");
      let saved;
      if (form.id) saved = await vehiclesApi.update(form.id, form);
      else saved = await vehiclesApi.create(form);
      const id = saved?.id || form.id;
      if (imageFile && id) await vehiclesApi.uploadImage(id, imageFile);
      notify(form.id ? `Đã cập nhật xe #${id}` : `Đã thêm xe #${id}`);
      setVehicleEditor(null);
      await load(true);
    } catch (e) { setError(e.message || "Không lưu được xe."); }
  };

  const filteredUsers = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return users;
    return users.filter(u => `${u.id} ${u.email} ${u.hoTen} ${(u.roles || []).join(" ")}`.toLowerCase().includes(q));
  }, [users, search]);

  const filteredCustomers = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return customers;
    return customers.filter(c => `${c.idKhachHang} ${c.hoTen} ${c.email} ${c.soDienThoai} ${c.soCccd}`.toLowerCase().includes(q));
  }, [customers, search]);

  const filteredVehicles = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return vehicles;
    return vehicles.filter(v => `${v.id} ${v.name} ${v.plate} ${v.brand} ${v.type} ${v.status}`.toLowerCase().includes(q));
  }, [vehicles, search]);

  const vehiclePie = [
    { name: "Sẵn sàng", value: summary?.xeAvailable || 0, color: "#10B981" },
    { name: "Giữ chỗ", value: summary?.xeReserved || 0, color: "#F59E0B" },
    { name: "Đang thuê", value: summary?.xeRenting || 0, color: "#3B82F6" },
    { name: "Bảo trì", value: summary?.xeMaintenance || 0, color: "#F97316" },
  ].filter(x => x.value > 0);

  const operationData = [
    { name: "YC thuê", value: summary?.tongYeuCauThue || 0 },
    { name: "Chờ duyệt", value: summary?.yeuCauPending || 0 },
    { name: "Hợp đồng", value: summary?.tongHopDong || 0 },
    { name: "Đang thuê", value: summary?.hopDongInProgress || 0 },
    { name: "Hoàn tất", value: summary?.hopDongCompleted || 0 },
  ];

  const kpis = [
    { label: "Tổng người dùng", value: summary?.tongNguoiDung ?? "—", icon: Users, sub: `${summary?.tongKhachHang ?? 0} khách hàng`, cls: "text-blue-600 bg-blue-50" },
    { label: "Tổng khách hàng", value: summary?.tongKhachHang ?? "—", icon: UserCircle, sub: "Hồ sơ trong hệ thống", cls: "text-violet-600 bg-violet-50" },
    { label: "Tổng số xe", value: summary?.tongXe ?? "—", icon: Car, sub: `${summary?.xeAvailable ?? 0} xe sẵn sàng`, cls: "text-cyan-600 bg-cyan-50" },
    { label: "Xe đang thuê", value: summary?.xeRenting ?? "—", icon: Car, sub: `${summary?.xeReserved ?? 0} xe giữ chỗ`, cls: "text-orange-600 bg-orange-50" },
    { label: "YC chờ xử lý", value: summary?.yeuCauPending ?? "—", icon: Clock3, sub: `${summary?.tongYeuCauThue ?? 0} tổng yêu cầu`, cls: "text-amber-600 bg-amber-50" },
    { label: "Tổng hợp đồng", value: summary?.tongHopDong ?? "—", icon: FileText, sub: `${summary?.hopDongInProgress ?? 0} đang thực hiện`, cls: "text-indigo-600 bg-indigo-50" },
    { label: "Đã thanh toán", value: fmtMoney(summary?.tongTienDaThanhToan), icon: CreditCard, sub: "Giao dịch PAID", cls: "text-emerald-600 bg-emerald-50" },
    { label: "Đã hoàn tiền", value: fmtMoney(summary?.tongTienDaHoan), icon: Activity, sub: "Giao dịch REFUNDED", cls: "text-rose-600 bg-rose-50" },
  ];

  const logout = () => {
    onLogout?.();
  };

  return (
    <div className="cp-admin flex h-screen overflow-hidden">
      <aside className={`${sidebarOpen ? "w-60" : "w-16"} bg-[#040d1e] flex flex-col transition-all duration-300 shrink-0 overflow-y-auto`}>
        <div className="h-16 flex items-center px-4 border-b border-white/10 gap-3 shrink-0">
          {sidebarOpen && <div className="flex items-center gap-2 flex-1 min-w-0"><div className="w-7 h-7 bg-[#00B4D8] rounded-lg flex items-center justify-center"><Car size={14} className="text-white"/></div><div><div className="text-white font-bold text-sm">CARRENT PRO</div><div className="text-white/30 text-[10px]">Admin Panel</div></div></div>}
          <button onClick={()=>setSidebarOpen(!sidebarOpen)} className="text-white/40 hover:text-white">{sidebarOpen?<X size={18}/>:<Menu size={18}/>}</button>
        </div>
        <nav className="flex-1 px-2 py-4 space-y-5">
          {NAV_GROUPS.map(g => <div key={g.label}>{sidebarOpen&&<div className="text-white/25 text-[10px] font-bold px-3 mb-1.5 tracking-wider">{g.label}</div>}<div className="space-y-0.5">{g.items.map(i=>{const Icon=i.icon,active=i.id===activeNav;return <button key={i.id} onClick={()=>{setActiveNav(i.id);setSearch("")}} className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium ${active?"bg-white/15 text-white":"text-white/45 hover:bg-white/10 hover:text-white"}`}><Icon size={16}/>{sidebarOpen&&<span>{i.label}</span>}</button>})}</div></div>)}
        </nav>
        <button onClick={logout} className="m-2 mb-4 flex items-center gap-3 px-3 py-2.5 rounded-xl text-white/45 hover:bg-white/10 hover:text-white text-sm"><LogOut size={16}/>{sidebarOpen&&"Đăng xuất"}</button>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-gray-100 flex items-center px-6 gap-4 shrink-0">
          <div className="relative flex-1 max-w-md"><Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"/><input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Tìm kiếm trong màn hiện tại..." className="w-full pl-9 pr-4 py-2.5 bg-[#F6F8FB] border border-gray-200 rounded-xl text-sm outline-none focus:border-[#00B4D8]"/></div>
          <button onClick={()=>load(true)} className="ml-auto w-9 h-9 rounded-xl border flex items-center justify-center text-gray-500 hover:text-[#00B4D8]" title="Làm mới dữ liệu"><RefreshCw size={16} className={refreshing?"animate-spin":""}/></button><LiveNotificationBell
            storageKey="admin-all-flow"
            intervalMs={5000}
            poll={pollAdminFlow}
            onData={data=>{if(!data)return; setUsers(data.users||[]);setReviews(data.reviews||[]);setAudits(data.audits||[]);}}
            onOpenNotification={n=>{setActiveNav(n.target||"dashboard");setSearch("");}}
          /><div className="relative"><button onClick={()=>setProfileMenu(v=>!v)} className="flex items-center gap-2 border rounded-xl px-3 py-2 hover:bg-slate-50"><div className="w-7 h-7 rounded-full bg-[#0D2148] text-white grid place-items-center text-xs font-bold">{(adminName||"A")[0]}</div><span className="text-sm font-semibold">{adminName}</span><ChevronDown size={14}/></button>{profileMenu&&<div className="absolute right-0 top-12 w-52 bg-white border rounded-xl shadow-xl p-2 z-50"><div className="px-3 py-2 text-xs text-gray-400">Quản trị viên</div><button onClick={()=>{setProfileMenu(false);setActiveNav("dashboard")}} className="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-50 text-sm">Bảng điều khiển</button><button onClick={logout} className="w-full text-left px-3 py-2 rounded-lg hover:bg-red-50 text-red-600 text-sm">Đăng xuất</button></div>}</div>
        </header>

        <main className="cp-admin-main flex-1 overflow-y-auto p-6">
          {message&&<div className="mb-4 bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl px-4 py-3 text-sm">{message}</div>}
          {error&&<div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm flex justify-between gap-3"><span>{error}</span><button onClick={()=>setError("")}><X size={16}/></button></div>}
          {loading ? <div className="bg-white border rounded-2xl p-10 text-center text-gray-400">Đang tải dữ liệu quản trị từ backend...</div> : <>
            {activeNav==="dashboard"&&<DashboardView adminName={adminName} kpis={kpis} vehiclePie={vehiclePie} operationData={operationData} audits={audits} reviews={reviews}/>} 
            {activeNav==="users"&&<UsersView rows={filteredUsers} onStatus={updateUserStatus} onRole={setSelectedUser}/>} 
            {activeNav==="customers"&&<CustomersView rows={filteredCustomers} onReview={setReviewCustomer}/>} 
            {activeNav==="vehicles"&&<VehiclesView rows={filteredVehicles} onChangeStatus={setSelectedVehicle} onAdd={()=>setVehicleEditor({})} onEdit={v=>setVehicleEditor(v)}/>} 
            {activeNav==="reviews"&&<ReviewsView rows={reviews} customers={customers} vehicles={vehicles} onVisibility={setReviewVisibility}/>} 
            {activeNav==="stats"&&<StatsView summary={summary} vehiclePie={vehiclePie} operationData={operationData} finance={finance}/>} 
            {activeNav==="audit"&&<AuditView rows={audits}/>} 
          </>}
        </main>
      </div>

      {selectedUser&&<RoleModal user={selectedUser} onClose={()=>setSelectedUser(null)} onSave={role=>updateUserRole(selectedUser,role)}/>} 
      {selectedVehicle&&<VehicleStatusModal vehicle={selectedVehicle} onClose={()=>setSelectedVehicle(null)} onSave={status=>updateVehicleStatus(selectedVehicle,status)}/>} 
      {vehicleEditor&&<VehicleEditorModal vehicle={vehicleEditor} brands={brands} types={types} onClose={()=>setVehicleEditor(null)} onSave={saveVehicle}/>}
        {reviewCustomer&&<AdminDocumentReview customer={reviewCustomer} onClose={()=>setReviewCustomer(null)} onVerify={async c=>{await verifyCustomer(c);setReviewCustomer(null)}}/>}
    </div>
  );
}

function DashboardView({kpis,vehiclePie,operationData,audits,reviews}){
  return <div><PageHead title="Admin Dashboard" subtitle={`Tổng quan hệ thống từ backend · ${new Date().toLocaleDateString("vi-VN",{weekday:"long",day:"2-digit",month:"2-digit",year:"numeric"})}`}/>
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">{kpis.map(k=>{const Icon=k.icon;return <div key={k.label} className="bg-white rounded-2xl border border-gray-100 p-4 hover:shadow-md transition-all"><div className={`w-9 h-9 rounded-xl flex items-center justify-center ${k.cls} mb-3`}><Icon size={17}/></div><div className="text-2xl font-bold text-[#0D1B3E]">{k.value}</div><div className="text-xs text-gray-500 mt-1">{k.label}</div><div className="text-[11px] text-gray-400 mt-1">{k.sub}</div></div>})}</div>
    <div className="grid lg:grid-cols-3 gap-5 mb-5"><div className="lg:col-span-2 bg-white rounded-2xl border p-5"><h3 className="font-bold text-[#0D1B3E] mb-4 flex items-center gap-2"><BarChart3 size={16} className="text-[#00B4D8]"/>Tình hình vận hành</h3><ResponsiveContainer width="100%" height={220}><BarChart data={operationData}><CartesianGrid strokeDasharray="3 3" stroke="#EEF2F7"/><XAxis dataKey="name" tick={{fontSize:11,fill:"#9CA3AF"}}/><YAxis allowDecimals={false} tick={{fontSize:11,fill:"#9CA3AF"}}/><Tooltip contentStyle={{borderRadius:12,border:"1px solid #E5E7EB"}}/><Bar dataKey="value" fill="#0D1B3E" radius={[7,7,0,0]}/></BarChart></ResponsiveContainer></div><div className="bg-white rounded-2xl border p-5"><h3 className="font-bold text-[#0D1B3E] mb-3">Trạng thái xe</h3>{vehiclePie.length?<><ResponsiveContainer width="100%" height={160}><PieChart><Pie data={vehiclePie} dataKey="value" innerRadius={48} outerRadius={70}>{vehiclePie.map((x,i)=><Cell key={i} fill={x.color}/>)}</Pie><Tooltip/></PieChart></ResponsiveContainer><div className="space-y-2">{vehiclePie.map(x=><div key={x.name} className="flex justify-between text-xs"><span className="flex items-center gap-2 text-gray-500"><i className="w-2.5 h-2.5 rounded-full" style={{background:x.color}}/>{x.name}</span><b>{x.value}</b></div>)}</div></>:<div className="text-sm text-gray-400 py-16 text-center">Chưa có dữ liệu xe</div>}</div></div>
    <div className="grid lg:grid-cols-2 gap-5"><div className="bg-white rounded-2xl border p-5"><h3 className="font-bold text-[#0D1B3E] mb-4">Audit gần đây</h3>{audits.slice(0,6).map((a,i)=><div key={a.id||i} className="py-3 border-b last:border-0"><div className="flex justify-between gap-3"><b className="text-sm text-[#0D1B3E]">{a.hanhDong}</b><span className="text-[11px] text-gray-400">{fmtDate(a.thoiGian)}</span></div><div className="text-xs text-gray-500 mt-1">{a.loaiDoiTuong} #{a.idDoiTuong} · {a.moTa}</div></div>)}</div><div className="bg-white rounded-2xl border p-5"><h3 className="font-bold text-[#0D1B3E] mb-4">Đánh giá gần đây</h3>{reviews.slice(0,6).map(r=><div key={r.id} className="py-3 border-b last:border-0 flex items-center gap-3"><div className="w-9 h-9 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center font-bold">{r.diemDanhGia}★</div><div className="min-w-0"><div className="text-sm font-semibold">Hợp đồng #{r.idHopDong} · Xe #{r.idXe||"—"}</div><div className="text-xs text-gray-500 truncate">{r.nhanXet||"Không có nhận xét"}</div></div></div>)}</div></div>
  </div>
}

function UsersView({rows,onStatus,onRole}){return <section><PageHead title="Quản lý người dùng" subtitle={`${rows.length} người dùng · dữ liệu trực tiếp từ backend`}/><Table headers={["ID","Người dùng","Email","SĐT","Vai trò","Trạng thái","Thao tác"]}>{rows.map(u=><tr key={u.id} className="border-b hover:bg-gray-50/70"><Td bold>#{u.id}</Td><Td bold>{u.hoTen}</Td><Td>{u.email}</Td><Td>{u.soDienThoai||"—"}</Td><Td><div className="flex flex-wrap gap-1">{(u.roles||[]).map(r=><span key={r} className="px-2 py-1 rounded-full bg-violet-50 text-violet-700 border border-violet-100 text-xs font-semibold">{r}</span>)}</div></Td><Td>{u.dangHoatDong?<span className="text-emerald-600 font-semibold text-xs">● Đang hoạt động</span>:<span className="text-red-500 font-semibold text-xs">● Đã khóa</span>}</Td><Td><div className="flex gap-2"><button title="Đổi vai trò" onClick={()=>onRole(u)} className="w-8 h-8 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center"><Shield size={14}/></button><button title={u.dangHoatDong?"Khóa":"Mở khóa"} onClick={()=>onStatus(u)} className={`w-8 h-8 rounded-lg flex items-center justify-center ${u.dangHoatDong?"bg-red-50 text-red-500":"bg-emerald-50 text-emerald-600"}`}>{u.dangHoatDong?<Lock size={14}/>:<Unlock size={14}/>}</button></div></Td></tr>)}</Table>{!rows.length&&<Empty text="Không tìm thấy người dùng."/>}</section>}

function CustomersView({rows,onReview}){return <section><PageHead title="Quản lý khách hàng" subtitle="Kiểm tra CCCD mặt trước, mặt sau và GPLX trước khi xác minh"/><Table headers={["ID","Khách hàng","Liên hệ","CCCD / GPLX","Giấy tờ","Trạng thái","Thao tác"]}>{rows.map(c=>{const enough=!!(c.anhCccdMatTruoc&&c.anhCccdMatSau&&c.anhGplx);const ok=c.cccdDaXacMinh&&c.gplxDaXacMinh;return <tr key={c.idKhachHang} className="border-b hover:bg-gray-50/70"><Td bold>#{c.idKhachHang}</Td><Td><div className="font-semibold text-[#0D1B3E]">{c.hoTen}</div><div className="text-xs text-gray-400">User #{c.idNguoiDung}</div></Td><Td><div>{c.email}</div><div className="text-xs text-gray-400">{c.soDienThoai||"—"}</div></Td><Td><div>{c.soCccd||"—"}</div><div className="text-xs text-gray-400">GPLX: {c.soGplx||"—"}</div></Td><Td>{enough?<span className="text-emerald-600 text-xs font-bold">✓ Đủ 3 ảnh</span>:<span className="text-amber-600 text-xs font-bold">Thiếu giấy tờ</span>}</Td><Td>{ok?<span className="text-emerald-600 text-xs font-semibold">✓ Đã xác minh</span>:<span className="text-amber-600 text-xs font-semibold">● Chờ xác minh</span>}</Td><Td><button disabled={!enough} onClick={()=>onReview(c)} className="px-3 py-2 rounded-lg text-xs font-semibold bg-[#0D1B3E] disabled:bg-gray-200 disabled:text-gray-400 text-white flex items-center gap-1"><Eye size={14}/> {enough?"Xem hồ sơ":"Thiếu ảnh"}</button></Td></tr>})}</Table>{!rows.length&&<Empty text="Không tìm thấy khách hàng."/>}</section>}

function AdminDocumentReview({customer,onClose,onVerify}){const[urls,setUrls]=useState({}),[loading,setLoading]=useState(true),[error,setError]=useState("");useEffect(()=>{let alive=true;const made=[];(async()=>{try{const defs=[["front","cccd-front"],["back","cccd-back"],["gplx","gplx"]];const out={};for(const [k,t] of defs){const b=await customersApi.documentBlob(customer.idKhachHang,t);const u=URL.createObjectURL(b);made.push(u);out[k]=u}if(alive)setUrls(out)}catch(e){if(alive)setError(e.message||"Không tải được ảnh giấy tờ") }finally{if(alive)setLoading(false)}})();return()=>{alive=false;made.forEach(URL.revokeObjectURL)}},[customer.idKhachHang]);const ok=customer.cccdDaXacMinh&&customer.gplxDaXacMinh;return <div className="fixed inset-0 z-[100] bg-slate-950/60 backdrop-blur-sm p-4 overflow-y-auto"><div className="max-w-6xl mx-auto bg-white rounded-3xl shadow-2xl overflow-hidden"><div className="p-6 border-b flex justify-between gap-4"><div><div className="text-xs font-bold tracking-widest text-cyan-600">KIỂM DUYỆT GIẤY TỜ</div><h2 className="text-xl font-extrabold text-[#0D1B3E] mt-1">{customer.hoTen}</h2><p className="text-sm text-gray-500">CCCD {customer.soCccd||"—"} · GPLX {customer.soGplx||"—"}</p></div><button onClick={onClose} className="w-10 h-10 border rounded-xl">×</button></div><div className="p-6">{error&&<div className="bg-red-50 text-red-700 p-3 rounded-xl mb-4">{error}</div>}{loading?<div className="py-20 text-center text-gray-400">Đang tải 3 ảnh giấy tờ...</div>:<div className="grid lg:grid-cols-3 gap-5">{[["CCCD mặt trước","front"],["CCCD mặt sau","back"],["GPLX ô tô","gplx"]].map(([l,k])=><div key={k} className="border rounded-2xl overflow-hidden bg-slate-50"><div className="px-4 py-3 bg-white border-b font-bold">{l}</div><a href={urls[k]} target="_blank" rel="noreferrer" className="block aspect-[1.55/1] p-3"><img src={urls[k]} alt={l} className="w-full h-full object-contain bg-white rounded-xl"/></a><div className="px-4 pb-3 text-xs text-gray-400">Bấm ảnh để xem lớn</div></div>)}</div>}</div><div className="p-5 bg-slate-50 border-t flex flex-wrap justify-between gap-3 items-center"><p className="text-sm text-gray-500">Chỉ xác minh sau khi đã đối chiếu đủ cả 3 ảnh.</p><div className="flex gap-2"><button onClick={onClose} className="px-4 py-2.5 border bg-white rounded-xl font-semibold">Đóng</button><button disabled={loading||error||!urls.front||!urls.back||!urls.gplx} onClick={()=>onVerify(customer)} className={`${ok?"bg-red-600":"bg-emerald-600"} disabled:bg-gray-300 text-white px-5 py-2.5 rounded-xl font-bold`}>{ok?"Hủy xác minh":"Xác minh CCCD + GPLX"}</button></div></div></div></div>}
function VehiclesView({rows,onChangeStatus,onAdd,onEdit}){return <section><div className="flex items-start justify-between gap-3"><PageHead title="Quản lý xe" subtitle={`${rows.length} xe · thêm, sửa, ảnh và trạng thái`}/><button onClick={onAdd} className="bg-[#0D1B3E] text-white px-4 py-2.5 rounded-xl font-bold text-sm flex items-center gap-2"><Plus size={16}/> Thêm xe mới</button></div><div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">{rows.map(v=><div key={v.id} className="bg-white rounded-2xl border overflow-hidden hover:shadow-lg transition"><img src={v.image} alt={v.name} className="w-full h-40 object-cover bg-slate-100"/><div className="p-5"><div className="flex justify-between gap-3"><div><div className="text-xs text-gray-400">Xe #{v.id}</div><div className="font-bold text-[#0D1B3E] text-lg mt-1">{v.name}</div><div className="text-sm text-gray-500">{v.plate||"Chưa có biển số"}</div></div><span className={`h-fit px-2.5 py-1 rounded-full border text-xs font-semibold ${vehicleStatusClass(v.status)}`}>{vehicleStatusLabel[v.status]||v.status}</span></div><div className="grid grid-cols-2 gap-3 mt-5 text-sm"><InfoBox label="Hãng / loại" value={`${v.brand} · ${v.type}`}/><InfoBox label="Số chỗ" value={`${v.seats} chỗ`}/><InfoBox label="Giá/ngày" value={fmtMoney(v.pricePerDay)}/><InfoBox label="Năm / màu" value={`${v.namSanXuat||"—"} · ${v.mauXe||"—"}`}/></div><div className="grid grid-cols-2 gap-2 mt-4"><button onClick={()=>onEdit(v)} className="rounded-xl bg-cyan-50 text-cyan-700 py-2.5 text-sm font-bold flex justify-center items-center gap-2"><Pencil size={15}/> Sửa thông tin</button><button onClick={()=>onChangeStatus(v)} className="rounded-xl border border-[#0D1B3E] text-[#0D1B3E] py-2.5 text-sm font-bold">Trạng thái</button></div></div></div>)}</div>{!rows.length&&<Empty text="Không tìm thấy xe."/>}</section>}

function VehicleEditorModal({vehicle,brands,types,onClose,onSave}){const [form,setForm]=useState({id:vehicle.id||null,idHangXe:vehicle.idHangXe||brands[0]?.id||1,idLoaiXe:vehicle.idLoaiXe||types[0]?.id||1,bienSoXe:vehicle.plate||"",mauXe:vehicle.mauXe||"",namSanXuat:vehicle.namSanXuat||new Date().getFullYear(),donGiaNgay:vehicle.pricePerDay||0,moTa:vehicle.moTa||""});const [image,setImage]=useState(null);const [preview,setPreview]=useState(vehicle.image||"");const change=e=>setForm({...form,[e.target.name]:e.target.value});return <Modal onClose={onClose}><div className="flex justify-between"><div><h2 className="text-xl font-bold text-[#0D1B3E]">{form.id?"Cập nhật xe":"Thêm xe mới"}</h2><p className="text-sm text-gray-500 mt-1">Thông tin xe và ảnh đại diện</p></div><button onClick={onClose}><X size={18}/></button></div><div className="mt-5"><label className="block border-2 border-dashed rounded-2xl overflow-hidden cursor-pointer bg-slate-50">{preview?<img src={preview} className="w-full h-48 object-cover"/>:<div className="h-40 grid place-items-center text-gray-400"><div className="text-center"><Upload className="mx-auto mb-2"/><b>Chọn ảnh xe</b><div className="text-xs mt-1">JPG/PNG/WEBP · tối đa 5 MB</div></div></div>}<input type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={e=>{const f=e.target.files?.[0];if(f){setImage(f);setPreview(URL.createObjectURL(f))}}}/></label></div><div className="grid sm:grid-cols-2 gap-4 mt-5"><Field label="Biển số"><input name="bienSoXe" value={form.bienSoXe} onChange={change} className="input-admin"/></Field><Field label="Giá thuê/ngày"><input name="donGiaNgay" type="number" value={form.donGiaNgay} onChange={change} className="input-admin"/></Field><Field label="Hãng xe"><select name="idHangXe" value={form.idHangXe} onChange={change} className="input-admin">{brands.map(b=><option key={b.id} value={b.id}>{b.ten||b.tenHang}</option>)}</select></Field><Field label="Loại xe"><select name="idLoaiXe" value={form.idLoaiXe} onChange={change} className="input-admin">{types.map(t=><option key={t.id} value={t.id}>{t.ten||t.tenLoai} {t.soCho?`· ${t.soCho} chỗ`:""}</option>)}</select></Field><Field label="Màu xe"><input name="mauXe" value={form.mauXe} onChange={change} className="input-admin"/></Field><Field label="Năm sản xuất"><input name="namSanXuat" type="number" value={form.namSanXuat} onChange={change} className="input-admin"/></Field></div><Field label="Mô tả"><textarea name="moTa" value={form.moTa} onChange={change} rows="3" className="input-admin mt-1"/></Field><div className="flex gap-3 mt-5"><button onClick={onClose} className="flex-1 border rounded-xl py-3 font-semibold">Hủy</button><button onClick={()=>onSave({...form,idHangXe:Number(form.idHangXe),idLoaiXe:Number(form.idLoaiXe),namSanXuat:Number(form.namSanXuat),donGiaNgay:Number(form.donGiaNgay)},image)} className="flex-1 bg-[#0D1B3E] text-white rounded-xl py-3 font-bold flex justify-center gap-2"><Save size={16}/> Lưu xe</button></div></Modal>}
function Field({label,children}){return <label className="block text-sm font-semibold text-[#0D1B3E] mb-4"><span className="block mb-2">{label}</span>{children}</label>}
function ReviewsView({rows,customers,vehicles,onVisibility}){
  const [star,setStar]=useState("ALL");
  const [visibility,setVisibility]=useState("ALL");
  const [q,setQ]=useState("");
  const [selected,setSelected]=useState(null);
  const visibleRows=rows.filter(x=>x.hienThi!==false);
  const avg=visibleRows.length?visibleRows.reduce((a,x)=>a+Number(x.diemDanhGia||0),0)/visibleRows.length:0;
  const dist=[5,4,3,2,1].map(st=>({star:st,count:visibleRows.filter(x=>Number(x.diemDanhGia)===st).length}));
  const filtered=rows.filter(r=>{
    const c=customers.find(x=>Number(x.idKhachHang)===Number(r.idKhachHang));
    const v=vehicles.find(x=>Number(x.id)===Number(r.idXe));
    const hitStar=star==="ALL"||Number(r.diemDanhGia)===Number(star);
    const hitVisibility=visibility==="ALL"||(visibility==="VISIBLE"?r.hienThi!==false:r.hienThi===false);
    const hay=`${r.id} ${r.idHopDong} ${r.nhanXet||""} ${r.tenKhachHang||c?.hoTen||""} ${v?.name||""}`.toLowerCase();
    return hitStar&&hitVisibility&&hay.includes(q.trim().toLowerCase());
  });
  const toggle=async r=>{const updated=await onVisibility(r,r.hienThi===false);setSelected(p=>p&&Number(p.id)===Number(r.id)?{...p,...updated}:p)};
  return <section><PageHead title="Quản lý đánh giá" subtitle="Đánh giá xác thực từ hợp đồng hoàn tất · Admin có thể ẩn/hiện trên trang chi tiết xe"/>
    <div className="grid md:grid-cols-4 gap-4 mb-5"><div className="bg-white border rounded-2xl p-5"><div className="text-sm text-gray-500">Tổng đánh giá</div><div className="text-3xl font-extrabold mt-2">{rows.length}</div></div><div className="bg-white border rounded-2xl p-5"><div className="text-sm text-gray-500">Đang hiển thị</div><div className="text-3xl font-extrabold text-emerald-600 mt-2">{visibleRows.length}</div></div><div className="bg-white border rounded-2xl p-5"><div className="text-sm text-gray-500">Điểm trung bình công khai</div><div className="text-3xl font-extrabold text-amber-500 mt-2">{avg.toFixed(1)} ★</div></div><div className="bg-white border rounded-2xl p-5">{dist.map(x=><button onClick={()=>setStar(String(x.star))} key={x.star} className="w-full flex justify-between text-sm py-1 hover:text-amber-600"><span>{x.star} sao</span><b>{x.count}</b></button>)}</div></div>
    <div className="bg-white border rounded-2xl p-4 mb-4 flex flex-col lg:flex-row gap-3"><div className="relative flex-1"><Search size={16} className="absolute left-3 top-3.5 text-gray-400"/><input value={q} onChange={e=>setQ(e.target.value)} placeholder="Tìm khách hàng, xe, hợp đồng, nội dung..." className="input-admin pl-9"/></div><select value={star} onChange={e=>setStar(e.target.value)} className="input-admin lg:w-44"><option value="ALL">Tất cả số sao</option>{[5,4,3,2,1].map(x=><option key={x} value={x}>{x} sao</option>)}</select><select value={visibility} onChange={e=>setVisibility(e.target.value)} className="input-admin lg:w-48"><option value="ALL">Tất cả trạng thái</option><option value="VISIBLE">Đang hiển thị</option><option value="HIDDEN">Đã ẩn</option></select></div>
    <Table headers={["ID","Khách hàng","Xe","Hợp đồng","Điểm","Nhận xét","Trạng thái","Thao tác"]}>{filtered.map(r=>{const c=customers.find(x=>Number(x.idKhachHang)===Number(r.idKhachHang));const v=vehicles.find(x=>Number(x.id)===Number(r.idXe));const name=r.tenKhachHang||c?.hoTen||`KH #${r.idKhachHang}`;return <tr key={r.id} className={`border-b ${r.hienThi===false?"bg-slate-50 opacity-75":""}`}><Td bold>#{r.id}</Td><Td>{name}</Td><Td>{v?.name||`Xe #${r.idXe||"—"}`}</Td><Td>#{r.idHopDong}</Td><Td><span className="text-amber-500 font-bold">{r.diemDanhGia} ★</span></Td><Td><div className="max-w-[230px] truncate">{r.nhanXet||"Không có nhận xét"}</div></Td><Td><span className={`px-2.5 py-1 rounded-full text-xs font-bold ${r.hienThi===false?"bg-slate-200 text-slate-600":"bg-emerald-50 text-emerald-700"}`}>{r.hienThi===false?"Đã ẩn":"Đang hiển thị"}</span></Td><Td><div className="flex gap-2"><button onClick={()=>setSelected({...r,customer:c,vehicle:v})} className="px-3 py-2 border border-cyan-200 text-cyan-700 rounded-lg text-xs font-bold">Chi tiết</button><button onClick={()=>toggle(r)} className={`px-3 py-2 border rounded-lg text-xs font-bold flex items-center gap-1 ${r.hienThi===false?"border-emerald-200 text-emerald-700":"border-red-200 text-red-600"}`}>{r.hienThi===false?<><Eye size={14}/>Hiện lại</>:<><EyeOffIcon/>Ẩn</>}</button></div></Td></tr>})}</Table>{!filtered.length&&<Empty text="Không có đánh giá phù hợp bộ lọc."/>}
    {selected&&<Modal onClose={()=>setSelected(null)}><div className="flex justify-between gap-4"><div><div className="text-xs font-bold tracking-widest text-amber-500">ĐÁNH GIÁ ĐÃ XÁC THỰC</div><h2 className="text-xl font-extrabold text-[#0D1B3E] mt-1">Hợp đồng #{selected.idHopDong}</h2></div><button onClick={()=>setSelected(null)}><X size={18}/></button></div><div className="flex items-center justify-between mt-5"><div className="flex gap-1">{[1,2,3,4,5].map(x=><Star key={x} size={25} className={x<=Number(selected.diemDanhGia)?"fill-amber-400 text-amber-400":"text-gray-200"}/>)}</div><span className={`px-3 py-1.5 rounded-full text-xs font-bold ${selected.hienThi===false?"bg-slate-100 text-slate-600":"bg-emerald-50 text-emerald-700"}`}>{selected.hienThi===false?"Đã ẩn khỏi trang xe":"Đang hiển thị công khai"}</span></div><div className="grid sm:grid-cols-2 gap-3 mt-5"><div className="bg-slate-50 p-3 rounded-xl"><div className="text-xs text-gray-400">Khách hàng</div><b>{selected.tenKhachHang||selected.customer?.hoTen||`KH #${selected.idKhachHang}`}</b></div><div className="bg-slate-50 p-3 rounded-xl"><div className="text-xs text-gray-400">Xe</div><b>{selected.vehicle?.name||`Xe #${selected.idXe}`}</b></div></div><div className="mt-4 bg-slate-50 rounded-2xl p-4"><div className="text-xs text-gray-400 mb-2">Nội dung phản hồi</div><div className="whitespace-pre-wrap text-gray-700">{selected.nhanXet||"Không có nhận xét."}</div></div><div className="text-xs text-gray-400 mt-4">Gửi lúc {fmtDate(selected.thoiGianTao)} · Đánh giá #{selected.id}</div><button onClick={()=>toggle(selected)} className={`w-full mt-5 py-3 rounded-xl font-bold ${selected.hienThi===false?"bg-emerald-600 text-white":"bg-red-50 text-red-600 border border-red-200"}`}>{selected.hienThi===false?"Hiển thị lại đánh giá":"Ẩn đánh giá khỏi trang chi tiết xe"}</button></Modal>}
  </section>;
}
function EyeOffIcon(){return <span className="text-sm leading-none">⊘</span>}

function StatsView({summary,vehiclePie,operationData,finance}){
  const net=Number(summary?.tongTienDaThanhToan||0)-Number(summary?.tongTienDaHoan||0);
  const currentYear=String(new Date().getFullYear());
  const yearNow=finance.yearly.find(x=>x.year===currentYear);
  const monthNow=finance.monthly.at(-1);
  const moneyTip=v=>fmtMoney(v);
  return <section>
    <PageHead title="Thống kê & báo cáo" subtitle="Doanh thu, giao dịch, hợp đồng và vận hành được tổng hợp trực tiếp từ dữ liệu backend"/>
    <div className="grid sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-5">
      <MetricCard label="Doanh thu thuần toàn hệ thống" value={fmtMoney(net)} sub={`${finance.paidCount} giao dịch đã thanh toán`} tone="cyan"/>
      <MetricCard label="Doanh thu tháng gần nhất" value={fmtMoney(monthNow?.net||0)} sub={monthNow?.month||"Chưa có giao dịch"} tone="emerald"/>
      <MetricCard label={`Doanh thu năm ${currentYear}`} value={fmtMoney(yearNow?.net||0)} sub={`Hoàn tiền ${fmtMoney(yearNow?.refunded||0)}`} tone="violet"/>
      <MetricCard label="Hợp đồng hoàn tất" value={summary?.hopDongCompleted||0} sub={`${summary?.hopDongInProgress||0} hợp đồng đang thực hiện`} tone="amber"/>
    </div>

    <div className="grid xl:grid-cols-3 gap-5 mb-5">
      <div className="xl:col-span-2 bg-white rounded-2xl border border-slate-100 p-5 shadow-sm">
        <div className="flex items-start justify-between gap-3 mb-5"><div><h3 className="font-extrabold text-[#0D1B3E]">Doanh thu theo tháng</h3><p className="text-xs text-slate-400 mt-1">Tối đa 12 tháng gần nhất · PAID trừ REFUNDED</p></div><span className="text-xs px-3 py-1.5 rounded-full bg-cyan-50 text-cyan-700 font-bold">Dữ liệu thật</span></div>
        {finance.monthly.length?<ResponsiveContainer width="100%" height={310}><LineChart data={finance.monthly}><CartesianGrid strokeDasharray="3 3" stroke="#E8EEF6"/><XAxis dataKey="month" tick={{fontSize:11,fill:"#64748B"}}/><YAxis tick={{fontSize:11,fill:"#64748B"}} tickFormatter={v=>`${Math.round(v/1000000)}tr`}/><Tooltip formatter={(v,n)=>[moneyTip(v),n==="net"?"Doanh thu thuần":n==="paid"?"Đã thanh toán":"Đã hoàn"]}/><Legend/><Line type="monotone" dataKey="paid" name="Đã thanh toán" stroke="#06B6D4" strokeWidth={3} dot={{r:4}}/><Line type="monotone" dataKey="refunded" name="Hoàn tiền" stroke="#F43F5E" strokeWidth={3} dot={{r:4}}/><Line type="monotone" dataKey="net" name="Doanh thu thuần" stroke="#10B981" strokeWidth={3} dot={{r:4}}/></LineChart></ResponsiveContainer>:<Empty text="Chưa có giao dịch có thời gian thanh toán để lập biểu đồ tháng."/>}
      </div>
      <div className="bg-white rounded-2xl border border-slate-100 p-5 shadow-sm"><h3 className="font-extrabold text-[#0D1B3E] mb-1">Tài chính tổng quan</h3><p className="text-xs text-slate-400 mb-5">Đối soát dòng tiền</p><div className="space-y-4"><StatLine label="Tổng tiền đã thanh toán" value={fmtMoney(summary?.tongTienDaThanhToan)}/><StatLine label="Tổng tiền đã hoàn" value={fmtMoney(summary?.tongTienDaHoan)}/><StatLine label="Doanh thu thuần" value={fmtMoney(net)}/><StatLine label="GD thanh toán" value={finance.paidCount}/><StatLine label="GD hoàn tiền" value={finance.refundCount}/></div></div>
    </div>

    <div className="grid lg:grid-cols-2 gap-5 mb-5">
      <div className="bg-white rounded-2xl border border-slate-100 p-5 shadow-sm"><h3 className="font-extrabold text-[#0D1B3E] mb-1">Doanh thu theo năm</h3><p className="text-xs text-slate-400 mb-4">So sánh doanh thu thuần từng năm</p>{finance.yearly.length?<ResponsiveContainer width="100%" height={280}><BarChart data={finance.yearly}><CartesianGrid strokeDasharray="3 3" stroke="#E8EEF6"/><XAxis dataKey="year"/><YAxis tickFormatter={v=>`${Math.round(v/1000000)}tr`}/><Tooltip formatter={v=>moneyTip(v)}/><Bar dataKey="net" name="Doanh thu thuần" fill="#8B5CF6" radius={[8,8,0,0]}/></BarChart></ResponsiveContainer>:<Empty text="Chưa có dữ liệu theo năm."/>}</div>
      <div className="bg-white rounded-2xl border border-slate-100 p-5 shadow-sm"><h3 className="font-extrabold text-[#0D1B3E] mb-1">Vận hành thuê xe</h3><p className="text-xs text-slate-400 mb-4">Khối lượng nghiệp vụ hiện tại</p><ResponsiveContainer width="100%" height={280}><BarChart data={operationData}><CartesianGrid strokeDasharray="3 3" stroke="#E8EEF6"/><XAxis dataKey="name" tick={{fontSize:11}}/><YAxis allowDecimals={false}/><Tooltip/><Bar dataKey="value" name="Số lượng" fill="#06B6D4" radius={[8,8,0,0]}/></BarChart></ResponsiveContainer></div>
    </div>

    <div className="grid lg:grid-cols-2 gap-5">
      <div className="bg-white rounded-2xl border border-slate-100 p-5 shadow-sm"><h3 className="font-extrabold text-[#0D1B3E] mb-4">Tình trạng đội xe</h3>{vehiclePie.map(x=><div key={x.name} className="flex justify-between py-3 border-b text-sm"><span className="flex gap-2 items-center"><i className="w-2.5 h-2.5 rounded-full" style={{background:x.color}}/>{x.name}</span><b>{x.value}</b></div>)}<div className="flex justify-between py-3 text-sm"><span>Tổng xe</span><b>{summary?.tongXe||0}</b></div></div>
      <div className="bg-white rounded-2xl border border-slate-100 p-5 shadow-sm"><h3 className="font-extrabold text-[#0D1B3E] mb-4">Chỉ số hiệu quả</h3><StatLine label="Hoàn tất / Tổng HĐ" value={`${summary?.tongHopDong?Math.round((summary.hopDongCompleted||0)*100/summary.tongHopDong):0}%`}/><StatLine label="Xe đang khai thác" value={`${(summary?.xeRenting||0)+(summary?.xeReserved||0)} / ${summary?.tongXe||0}`}/><StatLine label="Xe bảo trì" value={summary?.xeMaintenance||0}/><StatLine label="Khách hàng" value={summary?.tongKhachHang||0}/><StatLine label="Yêu cầu chờ duyệt" value={summary?.yeuCauPending||0}/></div>
    </div>
  </section>
}

function MetricCard({label,value,sub,tone}){const map={cyan:"from-cyan-500 to-blue-600",emerald:"from-emerald-500 to-teal-600",violet:"from-violet-500 to-indigo-600",amber:"from-amber-500 to-orange-600"};return <div className={`rounded-2xl p-5 text-white bg-gradient-to-br ${map[tone]||map.cyan} shadow-lg shadow-slate-900/5`}><div className="text-xs font-semibold text-white/75">{label}</div><div className="text-2xl font-extrabold mt-2 tracking-tight">{value}</div><div className="text-[11px] text-white/70 mt-2">{sub}</div></div>}

function AuditView({rows}){return <section><PageHead title="Audit Log" subtitle={`${rows.length} bản ghi · chỉ đọc`}/><Table headers={["ID","Thời gian","Người dùng","Hành động","Đối tượng","Mô tả"]}>{rows.map(a=><tr key={a.id} className="border-b"><Td bold>#{a.id}</Td><Td>{fmtDate(a.thoiGian)}</Td><Td>{a.idNguoiDung?`#${a.idNguoiDung}`:"Hệ thống"}</Td><Td><span className="font-semibold text-[#0D1B3E]">{a.hanhDong}</span></Td><Td>{a.loaiDoiTuong} #{a.idDoiTuong||"—"}</Td><Td>{a.moTa||"—"}</Td></tr>)}</Table>{!rows.length&&<Empty text="Chưa có audit log."/>}</section>}

function RoleModal({user,onClose,onSave}){const [role,setRole]=useState(user.roles?.[0]||"KHACH_HANG");return <Modal onClose={onClose}><h2 className="text-lg font-bold text-[#0D1B3E]">Cập nhật vai trò</h2><p className="text-sm text-gray-500 mt-1">{user.hoTen} · {user.email}</p><label className="text-sm font-semibold block mt-5 mb-2">Vai trò</label><select value={role} onChange={e=>setRole(e.target.value)} className="w-full border rounded-xl px-3 py-3 outline-none"><option value="KHACH_HANG">KHACH_HANG</option><option value="NHAN_VIEN">NHAN_VIEN</option><option value="ADMIN">ADMIN</option></select><div className="flex gap-3 mt-5"><button onClick={onClose} className="flex-1 border rounded-xl py-2.5">Hủy</button><button onClick={()=>onSave(role)} className="flex-1 bg-[#0D1B3E] text-white rounded-xl py-2.5 font-semibold">Lưu thay đổi</button></div></Modal>}
function VehicleStatusModal({vehicle,onClose,onSave}){const [status,setStatus]=useState(vehicle.status||"AVAILABLE");return <Modal onClose={onClose}><h2 className="text-lg font-bold text-[#0D1B3E]">Trạng thái xe #{vehicle.id}</h2><p className="text-sm text-gray-500 mt-1">{vehicle.name} · {vehicle.plate}</p><select value={status} onChange={e=>setStatus(e.target.value)} className="w-full border rounded-xl px-3 py-3 mt-5 outline-none">{["AVAILABLE","RESERVED","RENTING","MAINTENANCE","INACTIVE"].map(s=><option key={s} value={s}>{vehicleStatusLabel[s]} ({s})</option>)}</select><p className="text-xs text-amber-600 mt-3">Chỉ nên đổi trạng thái thủ công khi đúng nghiệp vụ. RESERVED/RENTING thường được cập nhật bởi luồng hợp đồng.</p><div className="flex gap-3 mt-5"><button onClick={onClose} className="flex-1 border rounded-xl py-2.5">Hủy</button><button onClick={()=>onSave(status)} className="flex-1 bg-[#0D1B3E] text-white rounded-xl py-2.5 font-semibold">Cập nhật</button></div></Modal>}

function PageHead({title,subtitle}){return <div className="mb-6"><h1 className="text-xl font-bold text-[#0D1B3E]">{title}</h1><p className="text-gray-500 text-sm mt-1">{subtitle}</p></div>}
function Table({headers,children}){return <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden"><div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="bg-[#F6F8FB] border-b">{headers.map(h=><th key={h} className="text-left text-xs font-semibold text-gray-400 px-4 py-3 whitespace-nowrap">{h}</th>)}</tr></thead><tbody>{children}</tbody></table></div></div>}
function Td({children,bold=false}){return <td className={`px-4 py-3 ${bold?"font-semibold text-[#0D1B3E]":"text-gray-600"}`}>{children}</td>}
function Empty({text}){return <div className="bg-white border rounded-2xl p-8 mt-4 text-center text-gray-400 text-sm">{text}</div>}
function Modal({children,onClose}){return <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" onMouseDown={e=>{if(e.target===e.currentTarget)onClose()}}><div className="bg-white rounded-2xl max-w-lg w-full shadow-2xl p-6">{children}</div></div>}
function InfoBox({label,value}){return <div className="bg-[#F6F8FB] rounded-xl p-3"><div className="text-[11px] text-gray-400">{label}</div><div className="font-semibold text-[#0D1B3E] text-sm mt-1">{value}</div></div>}
function StatLine({label,value}){return <div className="flex justify-between items-center border-b pb-3"><span className="text-sm text-gray-500">{label}</span><b className="text-[#0D1B3E]">{value}</b></div>}
