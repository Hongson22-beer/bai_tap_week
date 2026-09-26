import { useEffect, useMemo, useState } from "react";
import {
  LayoutDashboard, FileText, Users, Car, CreditCard, Package, RotateCcw,
  Clock, XCircle, Bell, Search, ChevronDown, LogOut, Menu, X, Check,
  BarChart3, Eye, Plus, RefreshCw, Send, ShieldCheck, AlertCircle, CheckCircle2, BadgeCheck, FileCheck2, Pencil, Upload, Save
} from "lucide-react";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from "recharts";
import {
  authApi, rentalsApi, vehiclesApi, customersApi, contractsApi, paymentsApi,
  handoversApi, returnsApi, extensionsApi, cancellationsApi, categoriesApi
} from "../../services/api";
import StaffContractCreate from "./StaffContractCreate";
import StaffHandoverReturn from "./StaffHandoverReturn";
import StaffOfflineCreate from "./StaffOfflineCreate";
import LiveNotificationBell from "../../components/shared/LiveNotificationBell";
import { pollStaffFlow } from "../../services/liveNotifications";

const navItems = [
  { id: "dashboard", label: "Bảng điều khiển", icon: LayoutDashboard },
  { id: "requests", label: "Yêu cầu thuê", icon: FileText },
  { id: "customers", label: "Khách hàng", icon: Users },
  { id: "vehicles", label: "Xe", icon: Car },
  { id: "contracts", label: "Hợp đồng", icon: FileText },
  { id: "payments", label: "Thanh toán", icon: CreditCard },
  { id: "handover", label: "Giao xe", icon: Package },
  { id: "return", label: "Trả xe", icon: RotateCcw },
  { id: "renewal", label: "Gia hạn", icon: Clock },
  { id: "cancel", label: "Hủy hợp đồng", icon: XCircle },
];

const VN_STATUS = {
  PENDING: "Chờ duyệt", APPROVED: "Đã duyệt", REJECTED: "Từ chối", CANCELLED: "Đã hủy",
  DRAFT: "Nháp", SENT: "Đã gửi", CUSTOMER_CONFIRMED: "Khách đã xác nhận", PAID: "Đã thanh toán",
  READY_FOR_PICKUP: "Sẵn sàng giao xe", IN_PROGRESS: "Đang thuê", RETURNED: "Đã trả xe", COMPLETED: "Hoàn tất",
  AVAILABLE: "Sẵn sàng", RESERVED: "Đã giữ chỗ", RENTING: "Đang thuê", MAINTENANCE: "Bảo trì", INACTIVE: "Ngừng hoạt động",
  WAITING_CONFIRMATION: "Chờ đối soát", FAILED: "Thất bại", REFUND_PENDING: "Chờ hoàn tiền", REFUNDED: "Đã hoàn tiền",
};

const statusClass = s => {
  if (["APPROVED","PAID","COMPLETED","AVAILABLE","REFUNDED"].includes(s)) return "bg-emerald-50 text-emerald-700 border-emerald-200";
  if (["PENDING","WAITING_CONFIRMATION","DRAFT","SENT","CUSTOMER_CONFIRMED","READY_FOR_PICKUP","REFUND_PENDING"].includes(s)) return "bg-amber-50 text-amber-700 border-amber-200";
  if (["REJECTED","CANCELLED","FAILED","INACTIVE"].includes(s)) return "bg-red-50 text-red-700 border-red-200";
  if (["IN_PROGRESS","RENTING","RESERVED"].includes(s)) return "bg-blue-50 text-blue-700 border-blue-200";
  return "bg-gray-50 text-gray-600 border-gray-200";
};

const Status = ({ value }) => <span className={`inline-flex px-2.5 py-1 rounded-full border text-xs font-semibold ${statusClass(value)}`}>{VN_STATUS[value] || value || "—"}</span>;
const fmtDate = v => v ? new Date(v).toLocaleString("vi-VN", { day:"2-digit", month:"2-digit", year:"numeric", hour:"2-digit", minute:"2-digit" }) : "—";
const fmtMoney = n => `${Number(n || 0).toLocaleString("vi-VN")} ₫`;
const arr = x => Array.isArray(x) ? x : (x?.items || []);

export default function StaffDashboard({ staffName, onNavigate, onLogout }) {
  const [activeNav, setActiveNav] = useState("dashboard");
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [selectedReq, setSelectedReq] = useState(null);
  const [selectedContract, setSelectedContract] = useState(null);
  const [subScreen, setSubScreen] = useState(null);
  const [rentals, setRentals] = useState([]);
  const [vehicles, setVehicles] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [contracts, setContracts] = useState([]);
  const [payments, setPayments] = useState([]);
  const [extensions, setExtensions] = useState([]);
  const [cancellations, setCancellations] = useState([]);
  const [handovers, setHandovers] = useState({});
  const [returns, setReturns] = useState({});
  const [returnRequests, setReturnRequests] = useState({});
  const [loading, setLoading] = useState(true);
  const [secondaryLoading, setSecondaryLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [handoverContractId, setHandoverContractId] = useState(null);
  const [contractRequestId, setContractRequestId] = useState(null);
  const [returnContractId, setReturnContractId] = useState(null);
  const [detail, setDetail] = useState(null);
  const [profileMenu, setProfileMenu] = useState(false);
  const [vehicleEditor, setVehicleEditor] = useState(null);
  const [brands, setBrands] = useState([]);
  const [types, setTypes] = useState([]);

  const loadPrimary = async () => {
    setLoading(true);
    setError("");

    // Dashboard must remain usable when one independent API fails.
    // Do not let /Customers (or another secondary data source) blank every KPI.
    const [rResult, vResult, cResult, hResult] = await Promise.allSettled([
      rentalsApi.list("page=1&pageSize=100"),
      vehiclesApi.list(),
      customersApi.list(),
      contractsApi.list(),
    ]);

    if (rResult.status === "fulfilled") setRentals(arr(rResult.value));
    else { setRentals([]); console.error("[Dashboard] rentals API failed:", rResult.reason); }

    if (vResult.status === "fulfilled") setVehicles(vResult.value || []);
    else { setVehicles([]); console.error("[Dashboard] vehicles API failed:", vResult.reason); }

    if (cResult.status === "fulfilled") setCustomers(arr(cResult.value));
    else { setCustomers([]); console.error("[Dashboard] customers API failed:", cResult.reason); }

    if (hResult.status === "fulfilled") setContracts(arr(hResult.value));
    else { setContracts([]); console.error("[Dashboard] contracts API failed:", hResult.reason); }
    setLoading(false);
  };

  const loadSecondary = async (contractList = contracts) => {
    if (!contractList.length) { setPayments([]); setExtensions([]); setCancellations([]); return; }
    try {
      setSecondaryLoading(true);
      const paymentRows = [], extensionRows = [], cancelRows = [], handoverMap = {}, returnMap = {}, returnRequestMap = {};
      await Promise.all(contractList.map(async c => {
        const id = c.id;
        const [p, e, cn, h, r, rr] = await Promise.all([
          paymentsApi.byContract(id).catch(()=>[]), extensionsApi.byContract(id).catch(()=>[]), cancellationsApi.byContract(id).catch(()=>[]),
          handoversApi.byContract(id).catch(()=>null), returnsApi.byContract(id).catch(()=>null), returnsApi.requestByContract(id).catch(()=>null),
        ]);
        arr(p).forEach(x => paymentRows.push({...x, contract:c}));
        arr(e).forEach(x => extensionRows.push({...x, contract:c}));
        arr(cn).forEach(x => cancelRows.push({...x, contract:c}));
        if (h) handoverMap[id] = h;
        if (r) returnMap[id] = r;
        if (rr) returnRequestMap[id] = rr;
      }));
      setPayments(paymentRows.sort((a,b)=>Number(b.id)-Number(a.id)));
      setExtensions(extensionRows.sort((a,b)=>Number(b.id)-Number(a.id)));
      setCancellations(cancelRows.sort((a,b)=>Number(b.id)-Number(a.id)));
      setHandovers(handoverMap); setReturns(returnMap); setReturnRequests(returnRequestMap);
    } finally { setSecondaryLoading(false); }
  };

  useEffect(() => { loadPrimary(); }, []);
  useEffect(() => { Promise.all([categoriesApi.brands().catch(()=>[]), categoriesApi.types().catch(()=>[])]).then(([b,t])=>{setBrands(arr(b));setTypes(arr(t));}); }, []);
  useEffect(() => { if (contracts.length) loadSecondary(contracts); }, [contracts.length]);

  const customerById = id => customers.find(x => Number(x.idKhachHang) === Number(id));
  const vehicleById = id => vehicles.find(x => Number(x.id) === Number(id));
  const rentalById = id => rentals.find(x => Number(x.id) === Number(id));
  const rentalForContract = c => rentalById(c?.idYeuCauThue);
  const customerForContract = c => customerById(rentalForContract(c)?.idKhachHang);
  const vehicleForContract = c => vehicleById(rentalForContract(c)?.idXe);

  const pendingCount = rentals.filter(r=>r.trangThai === "PENDING").length;
  const approvedCount = rentals.filter(r=>r.trangThai === "APPROVED").length;
  const cancelledCount = rentals.filter(r=>r.trangThai === "CANCELLED").length;
  const availableCars = vehicles.filter(v=>v.status === "AVAILABLE").length;
  const rentingCars = vehicles.filter(v=>v.status === "RENTING").length;

  const kpis = [
    ["Yêu cầu chờ xử lý", pendingCount, Clock, "bg-amber-50 text-amber-600", "Dữ liệu backend"],
    ["Tổng yêu cầu", rentals.length, FileText, "bg-blue-50 text-blue-600", "API thật"],
    ["Đã duyệt", approvedCount, Check, "bg-emerald-50 text-emerald-600", "Đang hiệu lực"],
    ["Đã hủy", cancelledCount, XCircle, "bg-violet-50 text-violet-600", "Theo backend"],
    ["Xe khả dụng", availableCars, Car, "bg-orange-50 text-orange-600", `${vehicles.length} xe tổng cộng`],
    ["Xe đang thuê", rentingCars, CreditCard, "bg-cyan-50 text-cyan-600", "Trạng thái xe"],
  ];

  const monthData = useMemo(() => {
    const map = {};
    rentals.forEach(r => { const d = new Date(r.thoiGianTao || r.thoiGianNhan); if (!Number.isNaN(d)) { const k=`T${d.getMonth()+1}`; map[k]=(map[k]||0)+1; } });
    return Object.entries(map).map(([name,value])=>({name,value}));
  }, [rentals]);

  const vehiclePie = [
    { name:"Sẵn sàng", value:vehicles.filter(v=>v.status==="AVAILABLE").length, fill:"#10b981" },
    { name:"Đang thuê", value:vehicles.filter(v=>v.status==="RENTING").length, fill:"#3b82f6" },
    { name:"Giữ chỗ", value:vehicles.filter(v=>v.status==="RESERVED").length, fill:"#f59e0b" },
    { name:"Khác", value:vehicles.filter(v=>!["AVAILABLE","RENTING","RESERVED"].includes(v.status)).length, fill:"#94a3b8" },
  ].filter(x=>x.value>0);

  const approveRental = async id => {
    try { setError(""); await rentalsApi.approve(id); setMessage(`Đã chấp nhận yêu cầu #${id}. Sang mục Hợp đồng để lập hợp đồng.`); setSelectedReq(null); await loadPrimary(); }
    catch(e){ setError(`${e.status||""} ${e.message}`.trim()); }
  };
  const rejectRental = async id => {
    const reason = window.prompt("Nhập lý do từ chối:"); if (!reason) return;
    try { await rentalsApi.reject(id, reason); setMessage(`Đã từ chối yêu cầu #${id}.`); setSelectedReq(null); await loadPrimary(); }
    catch(e){ setError(`${e.status||""} ${e.message}`.trim()); }
  };
  const sendContract = async id => {
    try { await contractsApi.send(id); setMessage(`Đã gửi hợp đồng #${id} cho khách hàng.`); await loadPrimary(); }
    catch(e){ setError(`${e.status||""} ${e.message}`.trim()); }
  };
  const processExtension = async (id,status) => {
    try { await extensionsApi.process(id,{idNguoiXuLy:authApi.currentUser()?.id||0,trangThai:status}); setMessage(`Đã ${status === "APPROVED" ? "duyệt" : "từ chối"} gia hạn #${id}.`); await loadSecondary(); }
    catch(e){ setError(`${e.status||""} ${e.message}`.trim()); }
  };
  const processCancellation = async (id,status) => {
    try { await cancellationsApi.process(id,{idNguoiXuLy:authApi.currentUser()?.id||0,trangThai:status}); setMessage(`Đã ${status === "APPROVED" ? "duyệt" : "từ chối"} yêu cầu hủy #${id}.`); await loadPrimary(); await loadSecondary(); }
    catch(e){ setError(`${e.status||""} ${e.message}`.trim()); }
  };
  const verifyCustomer = async (c,value) => {
    try {
      await customersApi.verifyDocuments(c.idKhachHang, value, value);
      setMessage(value
        ? `Đã xác minh CCCD + GPLX cho khách hàng #${c.idKhachHang}.`
        : `Đã hủy xác minh hồ sơ khách hàng #${c.idKhachHang}.`);
      await loadPrimary();
    } catch(e){ setError(`${e.status||""} ${e.message}`.trim()); }
  };

  const saveVehicle = async (form, imageFile) => {
    try { setError(""); const saved = form.id ? await vehiclesApi.update(form.id, form) : await vehiclesApi.create(form); const id=saved?.id||form.id; if(imageFile&&id) await vehiclesApi.uploadImage(id,imageFile); setMessage(form.id?`Đã cập nhật xe #${id}.`:`Đã thêm xe #${id}.`); setVehicleEditor(null); await loadPrimary(); } catch(e){setError(`${e.status||""} ${e.message||"Không lưu được xe"}`.trim());}
  };
  const changeVehicleStatus = async v => { const next=window.prompt("Nhập trạng thái: AVAILABLE / MAINTENANCE / INACTIVE",v.status||"AVAILABLE"); if(!next)return; try{await vehiclesApi.setStatus(v.id,next.trim().toUpperCase(),"Nhân viên cập nhật trạng thái xe");setMessage(`Đã cập nhật trạng thái xe #${v.id}.`);await loadPrimary();}catch(e){setError(`${e.status||""} ${e.message||"Không cập nhật được trạng thái"}`.trim())}};

  if (subScreen === "offline-create") return <StaffShell staffName={staffName} onLogout={onLogout} sidebarOpen={sidebarOpen} setSidebarOpen={setSidebarOpen}><StaffOfflineCreate onBack={()=>{setSubScreen(null);loadPrimary();}} onCreated={()=>loadPrimary()}/></StaffShell>;
  if (subScreen === "create-contract") return <StaffShell staffName={staffName} onLogout={onLogout} sidebarOpen={sidebarOpen} setSidebarOpen={setSidebarOpen}><StaffContractCreate requestId={contractRequestId} onBack={()=>{setSubScreen(null);setContractRequestId(null);loadPrimary();}} onCreated={async(result)=>{await loadPrimary(); setContractRequestId(null); setSubScreen(null); setActiveNav("contracts"); setSelectedContract(result||null);}}/></StaffShell>;
  if (subScreen === "handover-form") return <StaffShell staffName={staffName} onLogout={onLogout} sidebarOpen={sidebarOpen} setSidebarOpen={setSidebarOpen}><StaffHandoverReturn mode="handover" contractId={handoverContractId} onBack={()=>{setSubScreen(null);loadPrimary();loadSecondary();}} onCompleted={(h)=>{setHandovers(prev=>({...prev,[handoverContractId]:h}));setSubScreen(null);setActiveNav("handover");loadPrimary();}}/></StaffShell>;
  if (subScreen === "return-form") return <StaffShell staffName={staffName} onLogout={onLogout} sidebarOpen={sidebarOpen} setSidebarOpen={setSidebarOpen}><StaffHandoverReturn mode="return" contractId={returnContractId} onBack={()=>{setSubScreen(null);loadPrimary();}}/></StaffShell>;

  return <div className="cp-staff min-h-screen bg-[#F6F8FB] flex">
    <aside className={`${sidebarOpen ? "w-[258px]" : "w-[76px]"} bg-gradient-to-b from-[#071A3A] via-[#0D2148] to-[#102B59] text-white fixed inset-y-0 left-0 z-40 transition-all duration-300 flex flex-col shadow-2xl shadow-blue-950/20`}>
      <div className="h-[70px] flex items-center px-4 border-b border-white/10 gap-3"><div className="w-9 h-9 bg-cyan-500 rounded-xl flex items-center justify-center"><Car size={18}/></div>{sidebarOpen&&<div className="font-bold tracking-tight">CARRENT PRO</div>}<button onClick={()=>setSidebarOpen(!sidebarOpen)} className="ml-auto text-white/50 hover:text-white">{sidebarOpen?<X size={18}/>:<Menu size={18}/>}</button></div>
      <nav className="p-2 flex-1 space-y-1 mt-3">{navItems.map(item=>{const Icon=item.icon;const active=activeNav===item.id;return <button key={item.id} onClick={()=>{setActiveNav(item.id);setMessage("");setError("");}} className={`w-full flex items-center gap-3 px-3 py-3 rounded-xl text-sm transition ${active?"bg-white/15 text-white":"text-blue-100/70 hover:bg-white/10 hover:text-white"}`}><Icon size={18}/>{sidebarOpen&&<><span className="flex-1 text-left">{item.label}</span>{item.id==="requests"&&pendingCount>0&&<span className="bg-cyan-500 text-white text-xs rounded-full min-w-6 h-6 px-1.5 flex items-center justify-center">{pendingCount}</span>}</>}</button>})}</nav>
      <button onClick={onLogout} className="m-3 p-3 rounded-xl flex items-center gap-3 text-blue-100/70 hover:bg-white/10"><LogOut size={18}/>{sidebarOpen&&"Đăng xuất"}</button>
    </aside>

    <main className={`flex-1 ${sidebarOpen?"ml-[258px]":"ml-[76px]"} transition-all duration-300`}>
      <header className="h-[74px] bg-white/95 backdrop-blur border-b border-slate-200 flex items-center px-7 sticky top-0 z-30 shadow-sm"><div className="relative max-w-md w-full"><Search size={17} className="absolute left-4 top-3.5 text-gray-400"/><input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Tìm kiếm..." className="w-full bg-[#F6F8FB] border border-gray-200 rounded-xl pl-11 pr-4 py-3 text-sm outline-none focus:border-cyan-400"/></div><div className="ml-auto flex items-center gap-3"><LiveNotificationBell
              storageKey="staff-all-flow"
              intervalMs={3000}
              poll={pollStaffFlow}
              onData={data=>{
                if(!data) return;
                setRentals(data.rentals||[]); setContracts(data.contracts||[]);
                setPayments((data.payments||[]).sort((a,b)=>Number(b.id)-Number(a.id)));
                setExtensions((data.extensions||[]).sort((a,b)=>Number(b.id)-Number(a.id)));
                setCancellations((data.cancellations||[]).sort((a,b)=>Number(b.id)-Number(a.id)));
                const hm={},rm={},rrm={};
                (data.handovers||[]).forEach(x=>hm[x.idHopDong]=x);
                (data.returns||[]).forEach(x=>{ if(x._request) rrm[x.idHopDong]=x; else rm[x.idHopDong]=x; });
                setHandovers(hm); setReturns(rm); setReturnRequests(rrm);
              }}
              onOpenNotification={n=>{setActiveNav(n.target||"dashboard");setSearch("");}}
            /><div className="relative"><button onClick={()=>setProfileMenu(v=>!v)} className="flex items-center gap-3 border rounded-xl px-3 py-2 hover:bg-slate-50"><div className="w-8 h-8 rounded-full bg-[#0D2148] text-white flex items-center justify-center font-bold">{(staffName||"N")[0]}</div><div className="text-left hidden sm:block"><div className="font-semibold text-sm text-[#0D1B3E]">{staffName}</div><div className="text-[11px] text-gray-400">Nhân viên</div></div><ChevronDown size={15}/></button>{profileMenu&&<div className="absolute right-0 top-12 w-52 bg-white border rounded-xl shadow-xl p-2 z-50"><button onClick={()=>{setProfileMenu(false);setActiveNav("dashboard")}} className="w-full text-left px-3 py-2 rounded-lg hover:bg-slate-50 text-sm">Bảng điều khiển</button><button onClick={onLogout} className="w-full text-left px-3 py-2 rounded-lg hover:bg-red-50 text-red-600 text-sm">Đăng xuất</button></div>}</div></div></header>

      <div className="cp-staff-main p-7 xl:p-8">
        {error&&<div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-xl p-3 text-sm flex items-center gap-2"><AlertCircle size={16}/>{error}<button className="ml-auto" onClick={()=>setError("")}><X size={15}/></button></div>}
        {message&&<div className="mb-4 bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl p-3 text-sm flex items-center gap-2"><Check size={16}/>{message}<button className="ml-auto" onClick={()=>setMessage("")}><X size={15}/></button></div>}

        {loading ? <div className="bg-white rounded-2xl border p-8 text-gray-500 flex items-center gap-2"><RefreshCw className="animate-spin" size={18}/> Đang tải dữ liệu backend...</div> : <>
          {activeNav === "dashboard" && <DashboardView staffName={staffName} kpis={kpis} monthData={monthData} vehiclePie={vehiclePie}/>} 
          {activeNav === "requests" && <RequestsView rentals={rentals} customers={customers} vehicles={vehicles} onView={setSelectedReq} onCreateOffline={()=>setSubScreen("offline-create")} search={search}/>} 
          {activeNav === "customers" && <CustomersView customers={customers} onVerify={verifyCustomer} search={search}/>} 
          {activeNav === "vehicles" && <VehiclesView vehicles={vehicles} search={search} onAdd={()=>setVehicleEditor({})} onEdit={v=>setVehicleEditor(v)} onStatus={changeVehicleStatus}/>} 
          {activeNav === "contracts" && <ContractsView contracts={contracts} rentals={rentals} customers={customers} vehicles={vehicles} rentalForContract={rentalForContract} customerForContract={customerForContract} vehicleForContract={vehicleForContract} onCreate={(requestId=null)=>{setContractRequestId(requestId);setSubScreen("create-contract")}} onView={setSelectedContract} onSend={sendContract} search={search}/>} 
          {activeNav === "payments" && <PaymentsView payments={payments} contracts={contracts} rentalForContract={rentalForContract} customerForContract={customerForContract} loading={secondaryLoading} onReload={()=>loadSecondary(contracts)} setMessage={setMessage} setError={setError} onView={p=>setDetail({type:"payment",data:p})}/>} 
          {activeNav === "handover" && <HandoverView contracts={contracts} rentalForContract={rentalForContract} customerForContract={customerForContract} vehicleForContract={vehicleForContract} handovers={handovers} onView={(c,h)=>setDetail({type:"handover",data:{contract:c,handover:h,rental:rentalForContract(c),customer:customerForContract(c),vehicle:vehicleForContract(c)}})} onStart={id=>{setHandoverContractId(id);setSubScreen("handover-form")}}/>} 
          {activeNav === "return" && <ReturnView contracts={contracts} rentalForContract={rentalForContract} vehicleForContract={vehicleForContract} returns={returns} returnRequests={returnRequests} payments={payments} onReload={async()=>{await loadPrimary(); await loadSecondary(contracts)}} setMessage={setMessage} setError={setError} onView={(c,r,req)=>setDetail({type:"return",data:{contract:c,returnData:r,request:req,rental:rentalForContract(c),vehicle:vehicleForContract(c)}})} onStart={id=>{setReturnContractId(id);setSubScreen("return-form")}}/>} 
          {activeNav === "renewal" && <ExtensionView rows={extensions} payments={payments} onProcess={processExtension} onReload={()=>loadSecondary(contracts)} setMessage={setMessage} setError={setError} loading={secondaryLoading} onView={x=>setDetail({type:"extension",data:x})}/>} 
          {activeNav === "cancel" && <CancellationView rows={cancellations} onProcess={processCancellation} loading={secondaryLoading} onView={x=>setDetail({type:"cancellation",data:x})}/>} 
        </>}
      </div>
    </main>

    {selectedReq&&<RequestModal request={selectedReq} customer={customerById(selectedReq.idKhachHang)} vehicle={vehicleById(selectedReq.idXe)} onClose={()=>setSelectedReq(null)} onApprove={approveRental} onReject={rejectRental}/>} 
    {selectedContract&&<ContractModal contract={selectedContract} rental={rentalForContract(selectedContract)} customer={customerForContract(selectedContract)} vehicle={vehicleForContract(selectedContract)} onClose={()=>setSelectedContract(null)} onSend={sendContract}/>} 
    {detail&&<OperationalDetail detail={detail} onClose={()=>setDetail(null)}/>}
    {vehicleEditor&&<StaffVehicleEditor vehicle={vehicleEditor} brands={brands} types={types} onClose={()=>setVehicleEditor(null)} onSave={saveVehicle}/>}
  </div>;
}

function StaffShell({children,staffName,onLogout,sidebarOpen,setSidebarOpen}) { return <div className="min-h-screen bg-[#F6F8FB]"><header className="h-[70px] bg-white border-b px-6 flex items-center"><button onClick={()=>setSidebarOpen(!sidebarOpen)} className="mr-4"><Menu/></button><div className="font-bold text-[#0D1B3E]">CARRENT PRO · Nhân viên</div><div className="ml-auto text-sm font-semibold">{staffName}</div></header><div className="p-6">{children}</div></div>; }

function DashboardView({staffName,kpis,monthData,vehiclePie}) { return <><div className="mb-6"><h1 className="text-2xl font-bold text-[#0D1B3E]">Xin chào, {staffName}!</h1><p className="text-gray-500 text-sm mt-1">{new Date().toLocaleDateString("vi-VN",{weekday:"long",day:"2-digit",month:"2-digit",year:"numeric"})}</p></div><div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4 mb-6">{kpis.map(([l,v,I,c,t])=><div key={l} className="bg-white rounded-2xl border border-gray-100 p-5"><div className={`w-11 h-11 rounded-xl flex items-center justify-center ${c}`}><I size={20}/></div><div className="text-2xl font-bold text-[#0D1B3E] mt-3">{v}</div><div className="font-medium text-gray-600 text-sm">{l}</div><div className="text-xs text-gray-400 mt-1">{t}</div></div>)}</div><div className="grid xl:grid-cols-3 gap-5"><div className="xl:col-span-2 bg-white rounded-2xl border p-5"><h2 className="font-bold text-[#0D1B3E] mb-4 flex items-center gap-2"><BarChart3 size={17} className="text-cyan-500"/> Lượt thuê theo tháng</h2><div className="h-56"><ResponsiveContainer width="100%" height="100%"><BarChart data={monthData}><CartesianGrid strokeDasharray="3 3" stroke="#eef2f7"/><XAxis dataKey="name"/><YAxis allowDecimals={false}/><Tooltip/><Bar dataKey="value" fill="#0D2148" radius={[6,6,0,0]}/></BarChart></ResponsiveContainer></div></div><div className="bg-white rounded-2xl border p-5"><h2 className="font-bold text-[#0D1B3E] mb-4">Trạng thái xe</h2><div className="h-44"><ResponsiveContainer><PieChart><Pie data={vehiclePie} dataKey="value" innerRadius={48} outerRadius={70} paddingAngle={2}>{vehiclePie.map((x,i)=><Cell key={i} fill={x.fill}/>)}</Pie></PieChart></ResponsiveContainer></div>{vehiclePie.map(x=><div key={x.name} className="flex justify-between text-sm py-1"><span className="text-gray-500">{x.name}</span><b>{x.value}</b></div>)}</div></div></>; }

function RequestsView({rentals,customers,vehicles,onView,onCreateOffline,search}) { const q=search.toLowerCase(); const rows=rentals.filter(r=>!q||String(r.id).includes(q)||String(r.trangThai).toLowerCase().includes(q)); return <section><div className="flex items-center justify-between mb-5"><PageHead title="Quản lý yêu cầu thuê" subtitle={`${rows.length} yêu cầu · dữ liệu backend`}/><button onClick={onCreateOffline} className="bg-[#0D1B3E] text-white px-4 py-2.5 rounded-xl font-semibold text-sm flex items-center gap-2"><Plus size={16}/> Tạo yêu cầu (Offline)</button></div><Table headers={["Mã YC","Khách hàng","Xe","Nguồn","Nhận","Trả","Trạng thái","Thao tác"]}>{rows.map(r=>{const c=customers.find(x=>Number(x.idKhachHang)===Number(r.idKhachHang));const v=vehicles.find(x=>Number(x.id)===Number(r.idXe));return <tr key={r.id} className="border-b border-gray-50 hover:bg-gray-50"><Td bold>#{r.id}</Td><Td>{c?.hoTen||`Khách hàng #${r.idKhachHang}`}</Td><Td>{v?.name||`Xe #${r.idXe}`}</Td><Td>{r.nguon==="ONLINE"?"TRỰC TUYẾN":"TẠI QUẦY"}</Td><Td>{fmtDate(r.thoiGianNhan)}</Td><Td>{fmtDate(r.thoiGianTraDuKien)}</Td><Td><Status value={r.trangThai}/></Td><Td><button onClick={()=>onView(r)} className="text-cyan-600"><Eye size={16}/></button></Td></tr>})}</Table></section>; }
function CustomersView({customers,onVerify,search}) {
  const q=search.toLowerCase();
  const rows=customers.filter(c=>!q||(c.hoTen||"").toLowerCase().includes(q)||(c.email||"").toLowerCase().includes(q)||(c.soCccd||"").includes(q));
  const verified=customers.filter(c=>c.cccdDaXacMinh&&c.gplxDaXacMinh).length;
  const waiting=customers.filter(c=>c.anhCccdMatTruoc&&c.anhCccdMatSau&&c.anhGplx&&!(c.cccdDaXacMinh&&c.gplxDaXacMinh)).length;
  const [review,setReview]=useState(null);
  const [docUrls,setDocUrls]=useState({});
  const [docLoading,setDocLoading]=useState(false);
  const [docError,setDocError]=useState("");

  const closeReview=()=>{
    Object.values(docUrls).forEach(u=>u&&URL.revokeObjectURL(u));
    setDocUrls({}); setReview(null); setDocError("");
  };
  const openReview=async(c)=>{
    Object.values(docUrls).forEach(u=>u&&URL.revokeObjectURL(u));
    setReview(c); setDocUrls({}); setDocError(""); setDocLoading(true);
    const defs=[["front","cccd-front",c.anhCccdMatTruoc],["back","cccd-back",c.anhCccdMatSau],["gplx","gplx",c.anhGplx]];
    const out={};
    const failed=[];
    await Promise.all(defs.map(async([key,type,exists])=>{
      if(!exists) return;
      try { const blob=await customersApi.documentBlob(c.idKhachHang,type); out[key]=URL.createObjectURL(blob); }
      catch(e){ failed.push(type); }
    }));
    setDocUrls(out);
    if(failed.length) setDocError("Không tải được một số ảnh giấy tờ. Hãy kiểm tra backend hoặc file ảnh đã lưu.");
    setDocLoading(false);
  };
  return <section>
    <div className="flex flex-col lg:flex-row lg:items-end justify-between gap-4 mb-6">
      <PageHead title="Quản lý khách hàng" subtitle="Nhân viên/Admin phải xem đủ CCCD mặt trước, mặt sau và GPLX trước khi xác minh hồ sơ"/>
      <div className="flex gap-3">
        <div className="bg-white border border-slate-200 rounded-2xl px-4 py-3 min-w-32 shadow-sm"><div className="text-xs text-slate-400 font-semibold uppercase">Đã xác minh</div><div className="text-xl font-extrabold text-emerald-600 mt-1">{verified}</div></div>
        <div className="bg-white border border-slate-200 rounded-2xl px-4 py-3 min-w-32 shadow-sm"><div className="text-xs text-slate-400 font-semibold uppercase">Chờ xử lý</div><div className="text-xl font-extrabold text-amber-600 mt-1">{waiting}</div></div>
      </div>
    </div>
    <div className="bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-slate-100 flex items-center gap-3"><div className="w-10 h-10 rounded-xl bg-cyan-50 text-cyan-600 flex items-center justify-center"><ShieldCheck size={20}/></div><div><div className="font-bold text-[#0D1B3E]">Hồ sơ xác minh</div><div className="text-xs text-slate-400">{rows.length} khách hàng phù hợp</div></div></div>
      <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="bg-slate-50/80 text-slate-500 text-xs uppercase tracking-wide">{["Khách hàng","Liên hệ","CCCD","GPLX","Giấy tờ","Trạng thái","Thao tác"].map(h=><th key={h} className="px-5 py-3.5 text-left font-semibold whitespace-nowrap">{h}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">
      {rows.map(c=>{const ok=c.cccdDaXacMinh&&c.gplxDaXacMinh;const imgs=!!(c.anhCccdMatTruoc&&c.anhCccdMatSau&&c.anhGplx);return <tr key={c.idKhachHang} className="hover:bg-slate-50/70 transition-colors">
        <td className="px-5 py-4"><div className="flex items-center gap-3"><div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[#0D2148] to-[#1A4B86] text-white flex items-center justify-center font-bold">{(c.hoTen||"K").charAt(0).toUpperCase()}</div><div><div className="font-bold text-slate-800">{c.hoTen||`Khách hàng #${c.idKhachHang}`}</div><div className="text-xs text-slate-400">ID #{c.idKhachHang}</div></div></div></td>
        <td className="px-5 py-4"><div className="text-slate-700">{c.soDienThoai||"—"}</div><div className="text-xs text-slate-400 mt-0.5">{c.email||"—"}</div></td>
        <td className="px-5 py-4 font-medium text-slate-700">{c.soCccd||"—"}</td><td className="px-5 py-4 font-medium text-slate-700">{c.soGplx||"—"}</td>
        <td className="px-5 py-4">{imgs?<span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 text-emerald-700 px-2.5 py-1 font-semibold text-xs"><FileCheck2 size={14}/>Đủ 3 ảnh</span>:<span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 text-amber-700 px-2.5 py-1 font-semibold text-xs"><AlertCircle size={14}/>Chưa đủ ảnh</span>}</td>
        <td className="px-5 py-4">{ok?<span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 text-emerald-700 px-2.5 py-1 font-semibold text-xs"><BadgeCheck size={14}/>Đã xác minh</span>:<span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 text-amber-700 px-2.5 py-1 font-semibold text-xs"><Clock size={14}/>Chờ xác minh</span>}</td>
        <td className="px-5 py-4"><button disabled={!imgs} onClick={()=>openReview(c)} className="inline-flex items-center justify-center gap-2 px-4 py-2 rounded-xl bg-[#0D2148] text-white font-semibold text-xs shadow-sm hover:bg-[#16386D] disabled:bg-slate-200 disabled:text-slate-400 disabled:shadow-none transition"><Eye size={15}/>{imgs?"Xem hồ sơ":"Thiếu giấy tờ"}</button></td>
      </tr>})}
      </tbody></table></div>
      {!rows.length&&<div className="p-10 text-center text-slate-400">Không tìm thấy khách hàng phù hợp.</div>}
    </div>

    {review&&<div className="fixed inset-0 z-[100] bg-slate-950/55 backdrop-blur-sm p-4 md:p-8 overflow-y-auto">
      <div className="max-w-6xl mx-auto bg-white rounded-3xl shadow-2xl overflow-hidden">
        <div className="px-6 py-5 border-b border-slate-100 flex items-center justify-between bg-gradient-to-r from-slate-50 to-white">
          <div><div className="text-xs uppercase tracking-widest font-bold text-cyan-600">Kiểm duyệt giấy tờ</div><h2 className="text-xl font-extrabold text-[#0D1B3E] mt-1">{review.hoTen}</h2><div className="text-sm text-slate-500 mt-1">Đối chiếu thông tin và 3 ảnh gốc trước khi xác minh.</div></div>
          <button onClick={closeReview} className="w-10 h-10 rounded-xl border border-slate-200 flex items-center justify-center hover:bg-slate-100"><X size={19}/></button>
        </div>
        <div className="p-6">
          <div className="grid md:grid-cols-4 gap-3 mb-6">
            {[["Mã khách hàng",`#${review.idKhachHang}`],["Số CCCD",review.soCccd||"—"],["Số GPLX",review.soGplx||"—"],["Số điện thoại",review.soDienThoai||"—"]].map(([k,v])=><div key={k} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3"><div className="text-[11px] uppercase font-bold tracking-wide text-slate-400">{k}</div><div className="font-bold text-slate-800 mt-1">{v}</div></div>)}
          </div>
          {docError&&<div className="mb-4 rounded-xl bg-red-50 border border-red-100 text-red-700 px-4 py-3 text-sm">{docError}</div>}
          {docLoading?<div className="py-24 text-center text-slate-500"><RefreshCw className="animate-spin inline mr-2" size={18}/>Đang tải ảnh giấy tờ...</div>:<div className="grid lg:grid-cols-3 gap-5">
            {[["CCCD mặt trước","front"],["CCCD mặt sau","back"],["Giấy phép lái xe","gplx"]].map(([label,key])=><div key={key} className="rounded-2xl border border-slate-200 overflow-hidden bg-slate-50"><div className="px-4 py-3 bg-white border-b font-bold text-slate-700 flex items-center gap-2"><FileCheck2 size={16} className="text-cyan-600"/>{label}</div><div className="aspect-[1.55/1] flex items-center justify-center p-3">{docUrls[key]?<a href={docUrls[key]} target="_blank" rel="noreferrer" className="w-full h-full"><img src={docUrls[key]} alt={label} className="w-full h-full object-contain rounded-xl bg-white"/></a>:<div className="text-sm text-slate-400 text-center">Không có ảnh</div>}</div><div className="px-4 pb-3 text-xs text-slate-400">Bấm vào ảnh để xem kích thước lớn.</div></div>)}
          </div>}
        </div>
        <div className="px-6 py-5 bg-slate-50 border-t border-slate-100 flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div className="text-sm text-slate-500">{review.cccdDaXacMinh&&review.gplxDaXacMinh?"Hồ sơ hiện đang ở trạng thái đã xác minh.":"Chỉ xác minh khi thông tin trên 3 ảnh khớp với hồ sơ khách hàng."}</div>
          <div className="flex gap-2 justify-end"><button onClick={closeReview} className="px-4 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-sm text-slate-600">Đóng</button>{review.cccdDaXacMinh&&review.gplxDaXacMinh?<button onClick={async()=>{await onVerify(review,false);closeReview();}} className="px-4 py-2.5 rounded-xl border border-red-200 bg-white text-red-600 font-bold text-sm flex items-center gap-2"><XCircle size={16}/>Hủy xác minh</button>:<button disabled={docLoading||!docUrls.front||!docUrls.back||!docUrls.gplx} onClick={async()=>{await onVerify(review,true);closeReview();}} className="px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 disabled:bg-slate-300 text-white font-bold text-sm flex items-center gap-2"><ShieldCheck size={17}/>Xác minh CCCD + GPLX</button>}</div>
        </div>
      </div>
    </div>}
  </section>;
}
function VehiclesView({vehicles,search,onAdd,onEdit,onStatus}) { const q=search.toLowerCase(); const rows=vehicles.filter(v=>!q||(v.name||"").toLowerCase().includes(q)||(v.plate||"").toLowerCase().includes(q)); return <section><div className="flex items-start justify-between gap-3"><PageHead title="Quản lý xe" subtitle={`${rows.length} xe · Nhân viên và Admin cùng quản lý theo BA`}/><button onClick={onAdd} className="bg-[#0D1B3E] text-white px-4 py-2.5 rounded-xl font-bold text-sm flex gap-2 items-center"><Plus size={16}/> Thêm xe mới</button></div><div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-4">{rows.map(v=><div key={v.id} className="bg-white rounded-2xl border p-4"><img src={v.image} className="w-full h-36 object-cover rounded-xl mb-3"/><div className="flex justify-between gap-3"><div><div className="font-bold text-[#0D1B3E]">{v.name}</div><div className="text-sm text-gray-400">{v.plate||`Xe #${v.id}`}</div></div><Status value={v.status}/></div><div className="mt-3 text-sm font-semibold text-cyan-700">{fmtMoney(v.pricePerDay)}/ngày</div><div className="grid grid-cols-2 gap-2 mt-4"><button onClick={()=>onEdit(v)} className="bg-cyan-50 text-cyan-700 rounded-xl py-2.5 font-bold text-sm flex justify-center gap-2"><Pencil size={15}/> Sửa / Ảnh</button><button onClick={()=>onStatus(v)} className="border rounded-xl py-2.5 font-bold text-sm">Trạng thái</button></div></div>)}</div>{!rows.length&&<Empty text="Không tìm thấy xe."/>}</section>; }
function StaffVehicleEditor({vehicle,brands,types,onClose,onSave}){const [form,setForm]=useState({id:vehicle.id||null,idHangXe:vehicle.idHangXe||brands[0]?.id||1,idLoaiXe:vehicle.idLoaiXe||types[0]?.id||1,bienSoXe:vehicle.plate||"",mauXe:vehicle.mauXe||"",namSanXuat:vehicle.namSanXuat||new Date().getFullYear(),donGiaNgay:vehicle.pricePerDay||0,moTa:vehicle.moTa||""});const [image,setImage]=useState(null);const [preview,setPreview]=useState(vehicle.image||"");const change=e=>setForm({...form,[e.target.name]:e.target.value});return <Modal onClose={onClose}><div className="flex justify-between"><div><h2 className="text-xl font-bold">{form.id?"Sửa thông tin xe":"Thêm xe mới"}</h2><p className="text-sm text-gray-500">Quản lý xe theo BA</p></div><button onClick={onClose}><X size={18}/></button></div><label className="block mt-5 border-2 border-dashed rounded-xl overflow-hidden cursor-pointer">{preview?<img src={preview} className="w-full h-44 object-cover"/>:<div className="h-36 grid place-items-center text-gray-400"><div className="text-center"><Upload className="mx-auto"/>Chọn ảnh xe</div></div>}<input type="file" accept="image/*" className="hidden" onChange={e=>{const f=e.target.files?.[0];if(f){setImage(f);setPreview(URL.createObjectURL(f))}}}/></label><div className="grid sm:grid-cols-2 gap-3 mt-4"><input name="bienSoXe" placeholder="Biển số" value={form.bienSoXe} onChange={change} className="border rounded-xl p-3"/><input name="donGiaNgay" type="number" placeholder="Giá/ngày" value={form.donGiaNgay} onChange={change} className="border rounded-xl p-3"/><select name="idHangXe" value={form.idHangXe} onChange={change} className="border rounded-xl p-3">{brands.map(b=><option key={b.id} value={b.id}>{b.ten||b.tenHang}</option>)}</select><select name="idLoaiXe" value={form.idLoaiXe} onChange={change} className="border rounded-xl p-3">{types.map(t=><option key={t.id} value={t.id}>{t.ten||t.tenLoai}</option>)}</select><input name="mauXe" placeholder="Màu xe" value={form.mauXe} onChange={change} className="border rounded-xl p-3"/><input name="namSanXuat" type="number" placeholder="Năm SX" value={form.namSanXuat} onChange={change} className="border rounded-xl p-3"/></div><textarea name="moTa" placeholder="Mô tả" value={form.moTa} onChange={change} className="border rounded-xl p-3 w-full mt-3"/><button onClick={()=>onSave({...form,idHangXe:Number(form.idHangXe),idLoaiXe:Number(form.idLoaiXe),namSanXuat:Number(form.namSanXuat),donGiaNgay:Number(form.donGiaNgay)},image)} className="w-full mt-4 bg-[#0D1B3E] text-white rounded-xl py-3 font-bold flex justify-center gap-2"><Save size={16}/> Lưu xe</button></Modal>}

function ContractsView({contracts,rentals,customers,vehicles,rentalForContract,customerForContract,vehicleForContract,onCreate,onView,onSend,search}) {
  const q=search.toLowerCase();
  const contractRentalIds=new Set(contracts.map(c=>Number(c.idYeuCauThue)));
  const waiting=rentals.filter(r=>r.trangThai==="APPROVED"&&!contractRentalIds.has(Number(r.id))).filter(r=>!q||String(r.id).includes(q)||String(r.idXe).includes(q));
  const rows=contracts.filter(c=>!q||(c.soHopDong||"").toLowerCase().includes(q)||String(c.id).includes(q)||String(c.idYeuCauThue).includes(q));
  return <section>
    <div className="flex items-center justify-between mb-5"><PageHead title="Hợp đồng" subtitle={`${rows.length} hợp đồng · ${waiting.length} yêu cầu đã duyệt chờ lập hợp đồng`}/><button onClick={()=>onCreate(null)} className="bg-[#0D1B3E] text-white px-4 py-2.5 rounded-xl font-semibold text-sm flex items-center gap-2"><Plus size={16}/> Lập hợp đồng mới</button></div>
    {waiting.length>0&&<div className="bg-white rounded-2xl border border-amber-200 overflow-hidden mb-5"><div className="px-5 py-4 bg-amber-50 border-b border-amber-100"><div className="font-bold text-[#0D1B3E]">Yêu cầu đã duyệt chờ lập hợp đồng</div><div className="text-xs text-gray-500 mt-1">Sau khi duyệt yêu cầu thuê, yêu cầu sẽ xuất hiện tại đây cho đến khi nhân viên lập hợp đồng.</div></div><div className="divide-y">{waiting.map(r=>{const cu=customers.find(x=>Number(x.idKhachHang)===Number(r.idKhachHang));const v=vehicles.find(x=>Number(x.id)===Number(r.idXe));return <div key={r.id} className="p-4 flex flex-col md:flex-row md:items-center gap-3 justify-between"><div className="grid sm:grid-cols-4 gap-4 flex-1"><div><div className="text-xs text-gray-400">Yêu cầu</div><b>#{r.id}</b></div><div><div className="text-xs text-gray-400">Khách hàng</div><b>{cu?.hoTen||`KH #${r.idKhachHang}`}</b></div><div><div className="text-xs text-gray-400">Xe</div><b>{v?.name||`Xe #${r.idXe}`}</b></div><div><div className="text-xs text-gray-400">Thời gian</div><span className="text-sm">{fmtDate(r.thoiGianNhan)} → {fmtDate(r.thoiGianTraDuKien)}</span></div></div><button onClick={()=>onCreate(r.id)} className="bg-[#0D1B3E] text-white px-4 py-2.5 rounded-xl font-semibold text-sm whitespace-nowrap">Lập hợp đồng</button></div>})}</div></div>}
    <Table headers={["Số HĐ","Khách hàng","Xe","Nhận xe","Trả xe","Tổng tiền","Trạng thái","Thao tác"]}>{rows.map(c=>{const r=rentalForContract(c),cu=customerForContract(c),v=vehicleForContract(c);return <tr key={c.id} className="border-b"><Td bold>{c.soHopDong}<div className="text-xs text-gray-400">ID #{c.id} · YC #{c.idYeuCauThue}</div></Td><Td>{cu?.hoTen||`KH #${r?.idKhachHang||"—"}`}</Td><Td>{v?.name||`Xe #${r?.idXe||"—"}`}</Td><Td>{fmtDate(c.thoiGianNhanDuKien)}</Td><Td>{fmtDate(c.thoiGianTraDuKien)}</Td><Td bold>{fmtMoney(c.tongTien)}</Td><Td><Status value={c.trangThai}/></Td><Td><div className="flex gap-2"><button onClick={()=>onView(c)} className="text-cyan-600" title="Xem chi tiết"><Eye size={16}/></button>{c.trangThai==="DRAFT"&&<button onClick={()=>onSend(c.id)} className="text-blue-600" title="Gửi cho khách"><Send size={16}/></button>}</div></Td></tr>})}</Table>
    {rows.length===0&&waiting.length===0&&<Empty text="Chưa có hợp đồng hoặc yêu cầu APPROVED chờ lập hợp đồng."/>}
  </section>;
}

function PaymentsView({payments,contracts,rentalForContract,customerForContract,loading,onReload,setMessage,setError,onView}) {
  const [busy,setBusy]=useState(null);
  const [refund,setRefund]=useState(null);
  const contractForPayment=p=>contracts.find(c=>Number(c.id)===Number(p.idHopDong));
  const customerForPayment=p=>{const c=contractForPayment(p);return c?customerForContract(c):null};
  const maskAccount=v=>{const x=String(v||"").replace(/\s/g,"");return x?`${"•".repeat(Math.max(0,x.length-4))}${x.slice(-4)}`:"Chưa cập nhật"};
  const confirmPayment=async(p)=>{
    const label=p.contract?.soHopDong||contractForPayment(p)?.soHopDong||`#${p.idHopDong}`;
    if(!window.confirm(`Xác nhận đã nhận ${fmtMoney(p.soTien)} cho hợp đồng ${label}?`)) return;
    try{setBusy(p.id);setError("");await paymentsApi.verify(p.id,true);setMessage(`Đã xác nhận thanh toán #${p.id}.`);await onReload();}
    catch(e){setError(`${e.status||""} ${e.message}`.trim());}finally{setBusy(null)}
  };
  const finishRefund=async()=>{
    const p=refund?.payment;if(!p)return;
    try{setBusy(p.id);setError("");await paymentsApi.refund(p.id);setRefund(null);setMessage(`Đã xác nhận chuyển ${fmtMoney(p.soTien)} hoàn tiền cho khách hàng.`);await onReload();}
    catch(e){setError(`${e.status||""} ${e.message||"Không xác nhận được hoàn tiền"}`.trim());}finally{setBusy(null)}
  };
  return <section>
    <div className="flex justify-between gap-3 items-start"><PageHead title="Thanh toán & hoàn tiền" subtitle="Đối soát tiền cọc, quyết toán và xác nhận chuyển khoản hoàn tiền cho khách"/><button onClick={onReload} className="border rounded-xl px-3 py-2 text-sm flex items-center gap-2"><RefreshCw size={15}/> Làm mới</button></div>
    {loading?<Empty text="Đang tải thanh toán..."/>:<Table headers={["ID","Hợp đồng","Loại","Số tiền","Mã giao dịch","Trạng thái","Xử lý"]}>{payments.map(p=>{const isRefund=p.loaiThanhToan==="HOAN_TIEN";return <tr key={p.id} className="border-b"><Td bold>#{p.id}</Td><Td>{p.contract?.soHopDong||contractForPayment(p)?.soHopDong||`#${p.idHopDong}`}</Td><Td>{p.loaiThanhToan==="PHI_GIA_HAN"?"Phí gia hạn (lịch sử)":p.loaiThanhToan==="PHI_PHAT_SINH"?"Quyết toán hợp đồng":p.loaiThanhToan==="TIEN_COC"?"Tiền cọc":isRefund?"Hoàn tiền cọc":p.loaiThanhToan}</Td><Td bold>{fmtMoney(p.soTien)}</Td><Td>{p.maGiaoDich||"—"}</Td><Td>{["PENDING","WAITING_CONFIRMATION"].includes(p.trangThai)?<span className="inline-flex px-3 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">Chờ xác nhận</span>:<Status value={p.trangThai}/>}</Td><Td><div className="flex items-center gap-2 flex-wrap"><button onClick={()=>onView(p)} className="border border-cyan-200 text-cyan-700 px-3 py-2 rounded-lg font-bold text-xs flex items-center gap-1"><Eye size={14}/> Chi tiết</button>{isRefund&&p.trangThai==="REFUND_PENDING"&&<button disabled={busy===p.id} onClick={()=>setRefund({payment:p,customer:customerForPayment(p),contract:contractForPayment(p)})} className="bg-rose-600 disabled:bg-gray-300 text-white px-3 py-2 rounded-lg font-semibold text-xs">Hoàn tiền</button>}{!isRefund&&["PENDING","WAITING_CONFIRMATION"].includes(p.trangThai)&&<button disabled={busy===p.id} onClick={()=>confirmPayment(p)} className="bg-emerald-600 disabled:bg-gray-300 text-white px-3 py-2 rounded-lg font-semibold text-xs">{busy===p.id?"Đang xác nhận...":"Xác nhận"}</button>}</div></Td></tr>})}</Table>}
    {!loading&&!payments.length&&<Empty text="Chưa có thanh toán."/>}
    {refund&&<div className="fixed inset-0 bg-slate-950/55 z-[80] grid place-items-center p-4"><div className="bg-white rounded-2xl shadow-2xl border w-full max-w-xl overflow-hidden"><div className="p-5 border-b flex items-start justify-between"><div><div className="text-xs font-bold text-rose-600 uppercase tracking-wider">Xử lý hoàn tiền</div><h3 className="text-xl font-bold text-[#0D1B3E] mt-1">{refund.contract?.soHopDong||`Hợp đồng #${refund.payment.idHopDong}`}</h3></div><button onClick={()=>setRefund(null)} className="w-9 h-9 border rounded-xl grid place-items-center"><X size={17}/></button></div><div className="p-5 space-y-4"><div className="bg-rose-50 border border-rose-100 rounded-xl p-4"><div className="text-sm text-slate-500">Số tiền cần hoàn</div><div className="text-3xl font-extrabold text-rose-600 mt-1">{fmtMoney(refund.payment.soTien)}</div></div><div className="grid sm:grid-cols-2 gap-3 text-sm"><div className="bg-slate-50 rounded-xl p-3"><div className="text-slate-400">Khách hàng</div><b>{refund.customer?.hoTen||"—"}</b></div><div className="bg-slate-50 rounded-xl p-3"><div className="text-slate-400">Ngân hàng</div><b>{refund.customer?.nganHang||"Chưa cập nhật"}</b></div><div className="bg-slate-50 rounded-xl p-3"><div className="text-slate-400">Số tài khoản</div><b>{maskAccount(refund.customer?.soTaiKhoan)}</b></div><div className="bg-slate-50 rounded-xl p-3"><div className="text-slate-400">Chủ tài khoản</div><b>{refund.customer?.chuTaiKhoan||"Chưa cập nhật"}</b></div></div>{(!refund.customer?.nganHang||!refund.customer?.soTaiKhoan||!refund.customer?.chuTaiKhoan)&&<div className="bg-amber-50 border border-amber-200 text-amber-800 rounded-xl p-3 text-sm">Khách hàng chưa cập nhật đủ thông tin nhận hoàn tiền. Không thể xác nhận đã chuyển.</div>}<div className="text-xs text-slate-500">Giao dịch tiền cọc ban đầu vẫn được giữ nguyên trong lịch sử. Nút dưới đây chỉ xác nhận giao dịch HOAN_TIEN hiện tại đã được chuyển cho khách.</div></div><div className="p-5 border-t flex gap-3 justify-end"><button onClick={()=>setRefund(null)} className="border rounded-xl px-4 py-2.5 font-semibold text-sm">Đóng</button><button disabled={busy===refund.payment.id||!refund.customer?.nganHang||!refund.customer?.soTaiKhoan||!refund.customer?.chuTaiKhoan} onClick={finishRefund} className="bg-rose-600 disabled:bg-gray-300 text-white rounded-xl px-4 py-2.5 font-bold text-sm">{busy===refund.payment.id?"Đang xử lý...":`Xác nhận đã chuyển ${fmtMoney(refund.payment.soTien)}`}</button></div></div></div>}
  </section>;
}

function HandoverView({contracts,rentalForContract,customerForContract,vehicleForContract,handovers,onStart,onView}) { const rows=contracts.filter(c=>["READY_FOR_PICKUP","WAITING_CUSTOMER_RECEIVE"].includes(c.trangThai)||handovers[c.id]); return <section><PageHead title="Giao xe" subtitle="Xe sẵn sàng giao, đang chờ khách xác nhận nhận xe và lịch sử bàn giao"/><div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">{rows.map(c=>{const r=rentalForContract(c),cu=customerForContract(c),v=vehicleForContract(c),h=handovers[c.id];return <div className="bg-white rounded-2xl border p-5" key={c.id}><div className="flex justify-between"><b>{c.soHopDong}</b><Status value={c.trangThai}/></div><div className="mt-3 font-semibold">{v?.name||`Xe #${r?.idXe}`}</div><div className="text-sm text-gray-500">{cu?.hoTen||`Khách hàng #${r?.idKhachHang}`}</div><div className="text-sm text-gray-400 mt-1">Nhận: {fmtDate(c.thoiGianNhanDuKien)}</div><button onClick={()=>onView(c,h)} className="mt-4 w-full border border-cyan-200 text-cyan-700 rounded-xl py-2.5 font-bold text-sm flex items-center justify-center gap-2"><Eye size={15}/> Xem chi tiết bàn giao</button>{h?<div className="mt-3 bg-emerald-50 text-emerald-700 rounded-xl p-3 text-sm">{c.trangThai==="WAITING_CUSTOMER_RECEIVE"?"Đã giao · Chờ khách xác nhận nhận xe":"Đã bàn giao lúc "+fmtDate(h.thoiGianGiao)}</div>:<button onClick={()=>onStart(c.id)} className="mt-4 w-full bg-[#0D1B3E] text-white rounded-xl py-2.5 font-semibold text-sm">Bắt đầu giao xe</button>}</div>})}</div>{!rows.length&&<Empty text="Hiện chưa có hợp đồng sẵn sàng bàn giao."/>}</section>; }
function ReturnView({contracts,rentalForContract,vehicleForContract,returns,returnRequests,payments,onReload,setMessage,setError,onStart,onView}) {
  const [busy,setBusy]=useState(null);
  const rows=contracts.filter(c=>c.trangThai==="IN_PROGRESS"||returns[c.id]);
  const extraPaymentFor = id => payments
    .filter(p=>Number(p.idHopDong)===Number(id) && p.loaiThanhToan==="PHI_PHAT_SINH")
    .sort((a,b)=>Number(b.id)-Number(a.id))[0];
  const confirmExtraPayment=async(p,c)=>{
    if(!p || p.trangThai!=="WAITING_CONFIRMATION") return;
    if(!window.confirm(`Xác nhận đã nhận ${fmtMoney(p.soTien)} phí phát sinh của hợp đồng ${c.soHopDong}?`)) return;
    try{
      setBusy(p.id); setError("");
      await paymentsApi.verify(p.id,true);
      setMessage(`Đã xác nhận phí phát sinh #${p.id}. Thanh toán đã được cập nhật và hợp đồng đã hoàn tất.`);
      await onReload();
    }catch(e){setError(`${e.status||""} ${e.message||""}`.trim());}
    finally{setBusy(null)}
  };
  return <section><PageHead title="Trả xe" subtitle="Khách gửi yêu cầu → nhân viên kiểm tra xe → khách thanh toán phí → nhân viên xác nhận → hoàn tất"/><div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">{rows.map(c=>{
    const r=rentalForContract(c),v=vehicleForContract(c),ret=returns[c.id],req=returnRequests[c.id],pay=extraPaymentFor(c.id);
    const paid=pay?.trangThai==="PAID";
    const customerReportedPaid=pay?.trangThai==="WAITING_CONFIRMATION";
    let label=ret?(c.trangThai==="RETURNED"?(customerReportedPaid?"Chờ NV xác nhận":"Chờ khách quyết toán"):"Hoàn tất"):req?.trangThai==="PENDING"?"Chờ kiểm tra xe":"Đang thuê";
    return <div className="bg-white rounded-2xl border p-5" key={c.id}>
      <div className="flex justify-between gap-2"><b>{c.soHopDong}</b><span className={`px-2.5 py-1 rounded-full text-xs font-bold ${label==="Chờ kiểm tra xe"?"bg-amber-50 text-amber-700":label==="Chờ khách quyết toán"?"bg-orange-50 text-orange-700":label==="Chờ NV xác nhận"?"bg-cyan-50 text-cyan-700":label==="Hoàn tất"?"bg-emerald-50 text-emerald-700":"bg-blue-50 text-blue-700"}`}>{label}</span></div>
      <div className="mt-3 font-semibold">{v?.name||`Xe #${r?.idXe}`}</div><div className="text-sm text-gray-400 mt-1">Trả dự kiến: {fmtDate(c.thoiGianTraDuKien)}</div>
      <button onClick={()=>onView(c,ret,req)} className="mt-3 w-full border border-cyan-200 text-cyan-700 rounded-xl py-2.5 font-bold text-sm flex items-center justify-center gap-2"><Eye size={15}/> Xem chi tiết trả xe</button>{ret?<>
        <div className={`mt-4 rounded-xl p-3 text-sm ${c.trangThai==="RETURNED"?"bg-orange-50 text-orange-700":"bg-emerald-50 text-emerald-700"}`}>
          {c.trangThai==="RETURNED"?(customerReportedPaid?`Khách đã báo thanh toán ${fmtMoney(ret.tongPhiPhatSinh)} · Chờ nhân viên xác nhận`:`Đã kiểm tra · Chờ khách quyết toán ${fmtMoney(ret.tongPhiPhatSinh)}`):`Đã trả xe · Phí ${fmtMoney(ret.tongPhiPhatSinh)} · ${paid||Number(ret.tongPhiPhatSinh)===0?"Đã thanh toán · ":""}Đã hoàn tất`}
        </div>
        {customerReportedPaid&&<button disabled={busy===pay.id} onClick={()=>confirmExtraPayment(pay,c)} className="mt-3 w-full bg-emerald-600 disabled:bg-gray-300 text-white rounded-xl py-2.5 font-semibold text-sm">{busy===pay.id?"Đang xác nhận...":"✓ Xác nhận đã thanh toán"}</button>}
        {paid&&<div className="mt-3 bg-emerald-50 text-emerald-700 border border-emerald-100 rounded-xl p-3 text-sm font-semibold">✓ Phí phát sinh đã thanh toán</div>}
      </>:req?.trangThai==="PENDING"?<><div className="mt-4 bg-amber-50 text-amber-700 rounded-xl p-3 text-sm">Khách đã gửi yêu cầu trả xe{req.ghiChu?` · ${req.ghiChu}`:""}</div><button onClick={()=>onStart(c.id)} className="mt-3 w-full bg-emerald-600 text-white rounded-xl py-2.5 font-semibold text-sm">Kiểm tra / Nhận xe trả</button></>:<div className="mt-4 bg-blue-50 text-blue-700 rounded-xl p-3 text-sm">Chưa có yêu cầu trả xe từ khách hàng.</div>}
    </div>
  })}</div>{!rows.length&&<Empty text="Hiện chưa có hợp đồng đang thuê hoặc lịch sử trả xe."/>}</section>;
}
function ExtensionView({rows,payments,onProcess,onReload,setMessage,setError,loading,onView}) {
  const [busy,setBusy]=useState(null);
  const payFor=x=>payments.filter(p=>Number(p.idHopDong)===Number(x.idHopDong)&&p.loaiThanhToan==="PHI_GIA_HAN"&&(p.ghiChu||"").includes(`EXTENSION:${x.id}`)).sort((a,b)=>Number(b.id)-Number(a.id))[0];
  const confirmPay=async(p,x)=>{if(!p)return;try{setBusy(p.id);setError("");await paymentsApi.verify(p.id,true);setMessage(`Đã xác nhận phí gia hạn #${p.id} của ${x.contract?.soHopDong||`hợp đồng #${x.idHopDong}`}.`);await onReload();}catch(e){setError(`${e.status||""} ${e.message}`.trim())}finally{setBusy(null)}};
  return <section><PageHead title="Gia hạn hợp đồng" subtitle="Kiểm tra lịch xe → duyệt/từ chối → phí gia hạn được cộng vào quyết toán cuối hợp đồng"/>{loading?<Empty text="Đang tải..."/>:<Table headers={["ID","Hợp đồng","Trả cũ → Trả mới","Gia hạn","Phí","Trạng thái","Thanh toán / Xử lý"]}>{rows.map(x=>{const p=payFor(x);return <tr key={x.id} className="border-b"><Td bold><button onClick={()=>onView(x)} className="flex items-center gap-1 hover:text-cyan-600"><Eye size={14}/> #{x.id}</button></Td><Td>{x.contract?.soHopDong||`#${x.idHopDong}`}</Td><Td><div>{fmtDate(x.thoiGianTraCu)}</div><div className="text-cyan-600 font-semibold">→ {fmtDate(x.thoiGianTraMoi)}</div></Td><Td>+{x.soNgayGiaHan} ngày</Td><Td bold>{fmtMoney(x.tienPhatSinh)}</Td><Td><Status value={x.trangThai}/></Td><Td>{x.trangThai==="PENDING"?<div className="flex gap-2"><button onClick={()=>onProcess(x.id,"APPROVED")} className="bg-emerald-600 text-white px-3 py-2 rounded-lg font-semibold text-xs">Duyệt</button><button onClick={()=>onProcess(x.id,"REJECTED")} className="bg-red-50 text-red-600 px-3 py-2 rounded-lg font-semibold text-xs">Từ chối</button></div>:x.trangThai==="REJECTED"?<span className="text-red-600 text-xs font-semibold">Đã từ chối</span>:<span className="text-emerald-600 text-xs font-bold">✓ Đã cộng vào quyết toán cuối</span>}</Td></tr>})}</Table>}{!loading&&!rows.length&&<Empty text="Chưa có yêu cầu gia hạn."/>}</section>; }
function CancellationView({rows,onProcess,loading,onView}) { return <section><PageHead title="Duyệt hủy hợp đồng" subtitle="Yêu cầu hủy đọc trực tiếp từ backend"/>{loading?<Empty text="Đang tải..."/>:<Table headers={["ID","Hợp đồng","Lý do","Hoàn dự kiến","Tiền phạt","Trạng thái","Xử lý"]}>{rows.map(x=><tr key={x.id} className="border-b"><Td bold><button onClick={()=>onView(x)} className="flex items-center gap-1 hover:text-cyan-600"><Eye size={14}/> #{x.id}</button></Td><Td>{x.contract?.soHopDong||`#${x.idHopDong}`}</Td><Td>{x.lyDo||"—"}</Td><Td>{fmtMoney(x.soTienHoanDuKien)}</Td><Td>{fmtMoney(x.soTienPhat)}</Td><Td><Status value={x.trangThai}/></Td><Td>{x.trangThai==="PENDING"?<div className="flex gap-2"><button onClick={()=>onProcess(x.id,"APPROVED")} className="text-emerald-600 font-semibold text-xs">Duyệt</button><button onClick={()=>onProcess(x.id,"REJECTED")} className="text-red-600 font-semibold text-xs">Từ chối</button></div>:"—"}</Td></tr>)}</Table>}{!loading&&!rows.length&&<Empty text="Chưa có yêu cầu hủy."/>}</section>; }

function RequestModal({request,customer,vehicle,onClose,onApprove,onReject}) { return <Modal onClose={onClose}><div className="flex items-start justify-between"><div><h2 className="text-lg font-bold text-[#0D1B3E]">Chi tiết yêu cầu</h2><div className="text-gray-400 text-sm">#{request.id}</div></div><div className="flex items-center gap-3"><Status value={request.trangThai}/><button onClick={onClose}><X size={18}/></button></div></div><div className="grid md:grid-cols-2 gap-5 mt-6"><Card title="Thông tin khách hàng"><Info l="Họ tên" v={customer?.hoTen||`Khách hàng #${request.idKhachHang}`}/><Info l="E-mail" v={customer?.email||"—"}/><Info l="SĐT" v={customer?.soDienThoai||"—"}/><Info l="CCCD" v={customer?.soCccd||"—"}/><Info l="Xác minh CCCD" v={customer?.cccdDaXacMinh?"✓ Đã xác minh":"Chưa xác minh"}/></Card><Card title="Thông tin thuê"><Info l="Xe" v={vehicle?.name||`Xe #${request.idXe}`}/><Info l="Ngày nhận" v={fmtDate(request.thoiGianNhan)}/><Info l="Ngày trả" v={fmtDate(request.thoiGianTraDuKien)}/><Info l="Tiền dự kiến" v={fmtMoney(request.tienUocTinh)}/><Info l="Nguồn" v={request.nguon}/></Card></div>{request.trangThai==="PENDING"&&<div className="grid grid-cols-2 gap-3 mt-5"><button onClick={()=>onApprove(request.id)} className="bg-emerald-600 text-white py-3 rounded-xl font-bold">✓ Chấp nhận</button><button onClick={()=>onReject(request.id)} className="border border-red-200 text-red-600 py-3 rounded-xl font-bold">⊗ Từ chối</button></div>}<p className="text-xs text-gray-400 mt-4">Sau khi chấp nhận, vào mục <b>Hợp đồng</b> để lập hợp đồng. Hệ thống không tự lập hợp đồng.</p></Modal>; }
function ContractModal({contract,rental,customer,vehicle,onClose,onSend}) { return <Modal onClose={onClose}><div className="flex justify-between"><div><h2 className="text-lg font-bold">{contract.soHopDong}</h2><p className="text-gray-400 text-sm">Hợp đồng #{contract.id}</p></div><div className="flex gap-2"><Status value={contract.trangThai}/><button onClick={onClose}><X size={18}/></button></div></div><div className="grid md:grid-cols-2 gap-4 mt-5"><Card title="Thông tin"><Info l="Yêu cầu thuê" v={`#${contract.idYeuCauThue}`}/><Info l="Khách hàng" v={customer?.hoTen||`#${rental?.idKhachHang||"—"}`}/><Info l="Xe" v={vehicle?.name||`#${rental?.idXe||"—"}`}/><Info l="Đơn giá" v={fmtMoney(contract.donGiaNgay)}/></Card><Card title="Thời gian & tiền"><Info l="Nhận" v={fmtDate(contract.thoiGianNhanDuKien)}/><Info l="Trả" v={fmtDate(contract.thoiGianTraDuKien)}/><Info l="Số ngày" v={contract.soNgayThue}/><Info l="Tổng tiền" v={fmtMoney(contract.tongTien)}/></Card></div>{contract.dieuKhoan&&<div className="mt-4 bg-gray-50 p-4 rounded-xl whitespace-pre-wrap text-sm text-gray-600">{contract.dieuKhoan}</div>}{contract.trangThai==="DRAFT"&&<button onClick={()=>{onSend(contract.id);onClose();}} className="mt-5 w-full bg-[#0D1B3E] text-white py-3 rounded-xl font-bold flex items-center justify-center gap-2"><Send size={16}/> Gửi cho khách hàng</button>}</Modal>; }

function OperationalDetail({detail,onClose}){const d=detail.data||{};const rows=detail.type==="payment"?[["Loại","Thanh toán"],["Hợp đồng",d.contract?.soHopDong||`#${d.idHopDong}`],["Loại thanh toán",d.loaiThanhToan],["Số tiền",fmtMoney(d.soTien)],["Phương thức",d.phuongThuc],["Mã giao dịch",d.maGiaoDich||"—"],["Trạng thái",VN_STATUS[d.trangThai]||d.trangThai]]:detail.type==="handover"?[["Hợp đồng",d.contract?.soHopDong],["Khách hàng",d.customer?.hoTen],["Xe",d.vehicle?.name],["Thời gian giao",fmtDate(d.handover?.thoiGianGiao)],["Số KM",`${d.handover?.soKm??"—"} km`],["Nhiên liệu",`${d.handover?.mucNhienLieu??"—"}%`],["Tình trạng",d.handover?.tinhTrangXe||"—"],["Ghi chú",d.handover?.ghiChu||"—"]]:detail.type==="return"?[["Hợp đồng",d.contract?.soHopDong],["Xe",d.vehicle?.name],["Trả dự kiến",fmtDate(d.contract?.thoiGianTraDuKien)],["Trả thực tế",fmtDate(d.returnData?.thoiGianTraThucTe)],["KM trả",`${d.returnData?.soKm??"—"} km`],["Nhiên liệu",`${d.returnData?.mucNhienLieu??"—"}%`],["Tình trạng",d.returnData?.tinhTrangXe||"—"],["Phí trả muộn",fmtMoney(d.returnData?.phiTraMuon)],["Phí khác",fmtMoney(d.returnData?.phiPhatSinh)],["Quyết toán",fmtMoney(d.returnData?.tongPhiPhatSinh)],["Ghi chú yêu cầu",d.request?.ghiChu||"—"]]:detail.type==="extension"?[["Hợp đồng",d.contract?.soHopDong||`#${d.idHopDong}`],["Trả cũ",fmtDate(d.thoiGianTraCu)],["Trả mới",fmtDate(d.thoiGianTraMoi)],["Số ngày",d.soNgayGiaHan],["Phí cộng quyết toán",fmtMoney(d.tienPhatSinh)],["Lý do",d.lyDo||"—"],["Trạng thái",VN_STATUS[d.trangThai]||d.trangThai]]:[["Hợp đồng",d.contract?.soHopDong||`#${d.idHopDong}`],["Lý do hủy",d.lyDo||"—"],["Hoàn dự kiến",fmtMoney(d.soTienHoanDuKien)],["Tiền phạt",fmtMoney(d.soTienPhat)],["Trạng thái",VN_STATUS[d.trangThai]||d.trangThai]];return <Modal onClose={onClose}><div className="flex justify-between items-start"><div><div className="text-xs font-bold tracking-wider text-cyan-600">CHI TIẾT NGHIỆP VỤ</div><h2 className="text-xl font-bold text-[#0D1B3E] mt-1">{detail.type==="payment"?"Thanh toán":detail.type==="handover"?"Bàn giao xe":detail.type==="return"?"Trả xe":detail.type==="extension"?"Gia hạn hợp đồng":"Hủy hợp đồng"}</h2></div><button onClick={onClose}><X size={18}/></button></div><div className="grid sm:grid-cols-2 gap-3 mt-6">{rows.map(([l,v])=><div key={l} className="bg-slate-50 rounded-xl p-4"><div className="text-xs text-gray-400">{l}</div><div className="font-semibold text-[#0D1B3E] mt-1 break-words">{v??"—"}</div></div>)}</div><button onClick={onClose} className="mt-5 w-full border rounded-xl py-3 font-bold">Đóng</button></Modal>}

function PageHead({title,subtitle}) { return <div className="mb-5"><h1 className="text-xl font-bold text-[#0D1B3E]">{title}</h1><p className="text-gray-500 text-sm mt-1">{subtitle}</p></div>; }
function Table({headers,children}) { return <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden"><div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="bg-[#F6F8FB] border-b">{headers.map(h=><th key={h} className="text-left text-xs font-semibold text-gray-400 px-4 py-3 whitespace-nowrap">{h}</th>)}</tr></thead><tbody>{children}</tbody></table></div></div>; }
function Td({children,bold=false}) { return <td className={`px-4 py-3 whitespace-nowrap ${bold?"font-semibold text-[#0D1B3E]":"text-gray-600"}`}>{children}</td>; }
function Empty({text}) { return <div className="bg-white border rounded-2xl p-8 mt-4 text-center text-gray-400 text-sm">{text}</div>; }
function Modal({children,onClose}) { return <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" onMouseDown={e=>{if(e.target===e.currentTarget)onClose()}}><div className="bg-white rounded-2xl max-w-3xl w-full shadow-2xl p-6 max-h-[90vh] overflow-y-auto">{children}</div></div>; }
function Card({title,children}) { return <div><h3 className="font-bold text-[#0D1B3E] mb-3">{title}</h3><div className="bg-[#F6F8FB] rounded-xl p-4 space-y-2">{children}</div></div>; }
function Info({l,v}) { return <div className="flex justify-between gap-4 text-sm"><span className="text-gray-400">{l}</span><span className="font-semibold text-[#0D1B3E] text-right">{v}</span></div>; }
