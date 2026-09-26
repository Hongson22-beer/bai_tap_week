import { useEffect, useMemo, useState } from "react";
import {
  ArrowLeft, Search, FileText, CreditCard, Car, CheckCircle2, Clock3,
  XCircle, RefreshCw, ChevronRight, ShieldCheck, KeyRound, Star
} from "lucide-react";
import {
  rentalsApi, contractsApi, paymentsApi, handoversApi, returnsApi, vehiclesApi, cancellationsApi
} from "../../services/api";

const VN = {
  PENDING: "Chờ duyệt", APPROVED: "Đã duyệt", REJECTED: "Đã từ chối", CANCELLED: "Đã hủy",
  DRAFT: "Đang lập hợp đồng", SENT: "Chờ bạn xác nhận", CUSTOMER_CONFIRMED: "Bạn đã xác nhận",
  PAID: "Đã thanh toán", WAITING_CONFIRMATION: "Chờ đối soát", READY_FOR_PICKUP: "Sẵn sàng nhận xe", WAITING_CUSTOMER_RECEIVE: "Chờ bạn xác nhận nhận xe", IN_PROGRESS: "Đang thuê",
  RETURNED: "Đã trả xe", COMPLETED: "Hoàn tất", FAILED: "Thất bại", REFUND_PENDING: "Chờ hoàn tiền",
  REFUNDED: "Đã hoàn tiền"
};
const fmtDate = v => v ? new Date(v).toLocaleString("vi-VN", { day:"2-digit", month:"2-digit", year:"numeric", hour:"2-digit", minute:"2-digit" }) : "—";
const money = n => `${Number(n || 0).toLocaleString("vi-VN")} ₫`;
const arr = x => Array.isArray(x) ? x : (x?.items || []);

function Pill({value}) {
  const good=["APPROVED","CUSTOMER_CONFIRMED","PAID","READY_FOR_PICKUP","IN_PROGRESS","RETURNED","COMPLETED","REFUNDED"].includes(value);
  const bad=["REJECTED","CANCELLED","FAILED"].includes(value);
  return <span className={`inline-flex px-2.5 py-1 rounded-full text-xs font-bold border ${good?"bg-emerald-50 text-emerald-700 border-emerald-200":bad?"bg-red-50 text-red-700 border-red-200":"bg-amber-50 text-amber-700 border-amber-200"}`}>{VN[value]||value||"—"}</span>;
}

export default function TrackingPage({ onNavigate, initialData }) {
  const [id, setId] = useState(String(initialData?.id || initialData?.idYeuCau || ""));
  const [rental, setRental] = useState(null);
  const [contract, setContract] = useState(null);
  const [payments, setPayments] = useState([]);
  const [handover, setHandover] = useState(null);
  const [returnData, setReturnData] = useState(null);
  const [returnRequest, setReturnRequest] = useState(null);
  const [vehicle, setVehicle] = useState(null);
  const [cancellations, setCancellations] = useState([]);
  const [mine, setMine] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingMine, setLoadingMine] = useState(true);
  const [err, setErr] = useState("");
  const [msg, setMsg] = useState("");

  const loadWorkflow = async (rentalId) => {
    if (!rentalId) return setErr("Nhập mã yêu cầu thuê.");
    setLoading(true); setErr(""); setMsg("");
    setRental(null); setContract(null); setPayments([]); setHandover(null); setReturnData(null); setReturnRequest(null); setVehicle(null); setCancellations([]);
    try {
      const r = await rentalsApi.get(Number(rentalId));
      setRental(r);
      vehiclesApi.get(r.idXe).then(setVehicle).catch(()=>{});
      const contracts = await contractsApi.discover({ max: 120 });
      const c = contracts.find(x => Number(x.idYeuCauThue) === Number(r.id));
      setContract(c || null);
      if (c) {
        const [p,h,ret,retReq,cancelRows] = await Promise.all([
          paymentsApi.byContract(c.id).catch(()=>[]),
          handoversApi.byContract(c.id).catch(()=>null),
          returnsApi.byContract(c.id).catch(()=>null),
          returnsApi.requestByContract(c.id).catch(()=>null),
          cancellationsApi.myByContract(c.id).catch(()=>[]),
        ]);
        setPayments(arr(p)); setHandover(h); setReturnData(ret); setReturnRequest(retReq); setCancellations(arr(cancelRows));
      }
    } catch (e) {
      setErr(`${e.status || ""} ${e.message || "Không thể tải hành trình thuê xe"}`.trim());
    } finally { setLoading(false); }
  };

  const loadMine = async () => {
    setLoadingMine(true);
    try { setMine(await rentalsApi.discoverMine({ max: 120 })); }
    catch (e) { setMine([]); setErr(e?.status===401 ? "Vui lòng đăng nhập tài khoản khách hàng để xem danh sách đơn thuê của bạn." : (e?.message||"Không tải được danh sách đơn thuê.")); }
    finally { setLoadingMine(false); }
  };

  useEffect(() => { loadMine(); }, []);
  useEffect(() => { if (initialData?.id || initialData?.idYeuCau) loadWorkflow(initialData.id || initialData.idYeuCau); }, []);
  useEffect(() => {
    const refresh = () => { loadMine(); if (id) loadWorkflow(id); };
    window.addEventListener("carrent:remote-update", refresh);
    return () => window.removeEventListener("carrent:remote-update", refresh);
  }, [id]);

  const paid = payments.some(p => p.trangThai === "PAID" && p.loaiThanhToan !== "HOAN_TIEN");
  const waitingPayment = payments.some(p => p.trangThai === "WAITING_CONFIRMATION");
  const stage = useMemo(() => {
    if (!rental) return 0;
    if (["REJECTED","CANCELLED"].includes(rental.trangThai)) return -1;
    if (returnData || contract?.trangThai === "COMPLETED") return 6;
    if (handover || contract?.trangThai === "IN_PROGRESS") return 5;
    if (["READY_FOR_PICKUP","PAID"].includes(contract?.trangThai) || paid) return 4;
    if (contract?.trangThai === "CUSTOMER_CONFIRMED") return 3;
    if (contract?.trangThai === "SENT") return 2;
    if (rental.trangThai === "APPROVED" || contract?.trangThai === "DRAFT") return 1;
    return 0;
  }, [rental, contract, paid, handover, returnData]);

  const confirmContract = async () => {
    try {
      setErr("");
      const updated = await contractsApi.confirm(contract.id);
      setContract(updated); setMsg("Bạn đã xác nhận hợp đồng thành công. Bước tiếp theo là thanh toán tiền cọc để giữ xe.");
    } catch(e) { setErr(`${e.status||""} ${e.message}`.trim()); }
  };
  const rejectContract = async () => {
    const reason = window.prompt("Nhập lý do từ chối hợp đồng:");
    if (!reason?.trim()) return;
    try {
      setErr("");
      const updated = await contractsApi.reject(contract.id, reason.trim());
      setContract(updated); setMsg("Đã gửi yêu cầu từ chối hợp đồng.");
    } catch(e) { setErr(`${e.status||""} ${e.message}`.trim()); }
  };

  const confirmReceived = async () => {
    if (!contract?.id) return;
    if (!window.confirm("Bạn xác nhận đã nhận xe và đã kiểm tra thông tin bàn giao?")) return;
    try {
      setErr("");
      await handoversApi.confirmReceived(contract.id);
      setMsg("Xác nhận nhận xe thành công. Hợp đồng đã bắt đầu thời gian thuê.");
      await loadWorkflow(rental.id);
    } catch(e) { setErr(`${e.status||""} ${e.message}`.trim()); }
  };

  const requestReturn = async () => {
    if (!contract?.id) return;
    const note = window.prompt("Ghi chú cho nhân viên khi trả xe (có thể để trống):", "") ?? null;
    if (note === null) return;
    if (!window.confirm("Gửi yêu cầu trả xe? Nhân viên sẽ kiểm tra KM, nhiên liệu và tình trạng xe thực tế.")) return;
    try {
      setErr("");
      const req = await returnsApi.requestReturn(contract.id, note.trim());
      setReturnRequest(req);
      setMsg("Đã gửi yêu cầu trả xe. Vui lòng mang xe đến/đợi nhân viên tiếp nhận và kiểm tra.");
    } catch(e) { setErr(`${e.status||""} ${e.message}`.trim()); }
  };

  const requestCancellation = async () => {
    if (!contract?.id) return;
    const reason = window.prompt("Nhập lý do bạn muốn hủy hợp đồng:", "");
    if (!reason?.trim()) return;
    if (!window.confirm("Gửi yêu cầu hủy hợp đồng? Nhân viên sẽ xem xét và hệ thống tự tính số tiền hoàn cọc theo chính sách.")) return;
    try {
      setErr("");
      const result = await cancellationsApi.create({ idHopDong: Number(contract.id), idNguoiYeuCau: 0, lyDo: reason.trim(), soTienHoanDuKien: 0, soTienPhat: 0 });
      setCancellations(prev => [result, ...prev]);
      setMsg(`Đã gửi yêu cầu hủy #${result.id}. Hoàn dự kiến: ${money(result.soTienHoanDuKien)}.`);
    } catch(e) { setErr(`${e.status||""} ${e.message||"Không thể gửi yêu cầu hủy"}`.trim()); }
  };

  const pendingCancellation = cancellations.find(x => x.trangThai === "PENDING");
  const canCancelContract = contract && !["IN_PROGRESS","RETURNED","COMPLETED","CANCELLED"].includes(contract.trangThai);

  const steps = [
    ["Yêu cầu thuê", "Nhân viên tiếp nhận và duyệt", FileText],
    ["Lập hợp đồng", "Nhân viên lập hợp đồng", ShieldCheck],
    ["Xác nhận hợp đồng", "Khách hàng đọc và xác nhận", CheckCircle2],
    ["Thanh toán cọc", "Thanh toán tiền cọc để giữ xe", CreditCard],
    ["Sẵn sàng nhận xe", "Xe được giữ và chuẩn bị bàn giao", Car],
    ["Nhận xe", "Bàn giao và bắt đầu thuê", KeyRound],
    ["Hoàn tất", "Trả xe và kết thúc hành trình", CheckCircle2],
  ];

  return <div className="min-h-screen bg-[#F6F8FB]">
    <header className="bg-white border-b"><div className="max-w-6xl mx-auto px-5 h-16 flex items-center justify-between"><button onClick={()=>onNavigate("landing")} className="flex items-center gap-2 text-sm text-gray-500"><ArrowLeft size={17}/> Trang chủ</button><div className="font-extrabold text-[#0D1B3E]">CARRENT <span className="text-[#00B4D8]">PRO</span></div></div></header>
    <main className="max-w-6xl mx-auto p-5 sm:p-7">
      <div className="mb-6"><h1 className="text-2xl sm:text-3xl font-extrabold text-[#0D1B3E]">Theo dõi thuê xe</h1><p className="text-gray-500 mt-1">Theo dõi toàn bộ hành trình từ yêu cầu thuê → hợp đồng → thanh toán → nhận xe → trả xe.</p></div>

      {!loadingMine&&mine.length>0&&<div className="mb-5 bg-white border rounded-2xl p-5"><div className="flex items-center justify-between gap-3 mb-4"><div><h2 className="font-bold text-[#0D1B3E]">Danh sách đơn thuê của tôi</h2><p className="text-sm text-gray-500">Chọn trực tiếp một đơn bên dưới, không cần nhớ mã yêu cầu.</p></div><span className="bg-cyan-50 text-cyan-700 px-3 py-1.5 rounded-full text-xs font-bold">{mine.length} đơn</span></div><div className="grid md:grid-cols-2 xl:grid-cols-3 gap-3">{mine.map(x=><button key={x.id} onClick={()=>{setId(String(x.id));loadWorkflow(x.id)}} className={`text-left border rounded-xl p-4 hover:border-cyan-400 hover:shadow-sm transition ${Number(rental?.id)===Number(x.id)?"border-cyan-400 bg-cyan-50/50":"border-slate-200"}`}><div className="flex justify-between gap-2"><b className="text-[#0D1B3E]">Yêu cầu #{x.id}</b><Pill value={x.trangThai}/></div><div className="text-xs text-gray-500 mt-3">Nhận: {fmtDate(x.thoiGianNhan)}</div><div className="text-xs text-gray-500 mt-1">Trả: {fmtDate(x.thoiGianTraDuKien)}</div><div className="text-sm font-bold text-cyan-700 mt-3">Xem hành trình →</div></button>)}</div></div>}

      <div className="grid lg:grid-cols-[330px_1fr] gap-5">
        <aside className="space-y-4">
          <div className="bg-white border rounded-2xl p-5"><div className="font-bold text-[#0D1B3E] mb-3">Tra cứu nhanh</div><div className="flex gap-2"><input value={id} onChange={e=>setId(e.target.value)} className="border rounded-xl p-3 min-w-0 flex-1" placeholder="Mã yêu cầu"/><button onClick={()=>loadWorkflow(id)} className="bg-[#0D1B3E] text-white w-12 rounded-xl flex items-center justify-center"><Search size={18}/></button></div></div>
          <div className="bg-white border rounded-2xl p-5"><div className="flex justify-between items-center mb-3"><div className="font-bold text-[#0D1B3E]">Yêu cầu của tôi</div><button onClick={loadMine} className="text-gray-400 hover:text-[#0D1B3E]"><RefreshCw size={16}/></button></div>{loadingMine?<div className="text-sm text-gray-400">Đang tải...</div>:mine.length?<div className="space-y-2 max-h-[430px] overflow-auto">{mine.map(x=><button key={x.id} onClick={()=>{setId(String(x.id));loadWorkflow(x.id)}} className={`w-full text-left border rounded-xl p-3 hover:border-[#00B4D8] ${Number(rental?.id)===Number(x.id)?"border-[#00B4D8] bg-cyan-50/50":"border-gray-100"}`}><div className="flex justify-between gap-2"><b className="text-sm text-[#0D1B3E]">Yêu cầu #{x.id}</b><Pill value={x.trangThai}/></div><div className="text-xs text-gray-400 mt-2">{fmtDate(x.thoiGianNhan)} → {fmtDate(x.thoiGianTraDuKien)}</div></button>)}</div>:<div className="text-sm text-gray-400">Chưa tìm thấy yêu cầu của bạn.</div>}</div>
        </aside>

        <section className="space-y-5">
          {err&&<div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded-2xl">{err}</div>}
          {msg&&<div className="bg-emerald-50 border border-emerald-200 text-emerald-700 p-4 rounded-2xl">{msg}</div>}
          {loading&&<div className="bg-white border rounded-2xl p-10 text-center text-gray-400">Đang tải hành trình thuê xe...</div>}
          {!loading&&!rental&&<div className="bg-white border rounded-2xl p-10 text-center"><Clock3 className="mx-auto text-gray-300 mb-3" size={34}/><div className="font-bold text-[#0D1B3E]">Chọn một yêu cầu để theo dõi</div><div className="text-sm text-gray-400 mt-1">Bạn sẽ thấy toàn bộ tiến trình tại một nơi.</div></div>}
          {!loading&&rental&&<>
            <div className="bg-white border rounded-2xl p-5"><div className="flex flex-wrap items-start justify-between gap-3"><div><div className="text-xs text-gray-400">YÊU CẦU THUÊ #{rental.id}</div><h2 className="text-xl font-extrabold text-[#0D1B3E] mt-1">{vehicle?.tenXe || vehicle?.name || `Xe #${rental.idXe}`}</h2><div className="text-sm text-gray-500 mt-1">{fmtDate(rental.thoiGianNhan)} → {fmtDate(rental.thoiGianTraDuKien)}</div></div><Pill value={contract?.trangThai || rental.trangThai}/></div><div className="grid sm:grid-cols-3 gap-3 mt-5"><Mini l="Nguồn" v={rental.nguon==="ONLINE"?"Trực tuyến":"Tại quầy"}/><Mini l="Tiền dự kiến" v={money(rental.tienUocTinh)}/><Mini l="Nhân viên xử lý" v={rental.idNhanVienXuLy?`NV #${rental.idNhanVienXuLy}`:"Chưa phân công"}/></div></div>

            {stage===-1?<div className="bg-white border rounded-2xl p-6 flex gap-3"><XCircle className="text-red-500"/><div><b className="text-[#0D1B3E]">Yêu cầu đã kết thúc</b><p className="text-sm text-gray-500 mt-1">Trạng thái: {VN[rental.trangThai]||rental.trangThai}</p></div></div>:<div className="bg-white border rounded-2xl p-5"><h3 className="font-bold text-[#0D1B3E] mb-5">Tiến trình thuê xe</h3><div className="space-y-0">{steps.map(([title,desc,Icon],i)=>{const done=i<stage;const current=i===stage;return <div key={title} className="flex gap-4 min-h-[72px]"><div className="flex flex-col items-center"><div className={`w-9 h-9 rounded-full flex items-center justify-center border-2 ${done?"bg-emerald-500 border-emerald-500 text-white":current?"bg-[#0D1B3E] border-[#0D1B3E] text-white":"bg-white border-gray-200 text-gray-300"}`}>{done?<CheckCircle2 size={18}/>:<Icon size={17}/>}</div>{i<steps.length-1&&<div className={`w-0.5 flex-1 ${done?"bg-emerald-300":"bg-gray-200"}`}/>}</div><div className="pt-1"><div className={`font-bold text-sm ${done||current?"text-[#0D1B3E]":"text-gray-400"}`}>{title}</div><div className="text-xs text-gray-400 mt-1">{desc}</div>{current&&<div className="text-xs font-semibold text-[#00B4D8] mt-1">Đang ở bước này</div>}</div></div>})}</div></div>}

            {rental.trangThai==="PENDING"&&<ActionBox title="Đang chờ nhân viên duyệt" text="Yêu cầu của bạn đã được gửi. Khi nhân viên chấp nhận, bước lập hợp đồng sẽ bắt đầu."/>}
            {rental.trangThai==="APPROVED"&&!contract&&<ActionBox title="Yêu cầu đã được duyệt" text="Nhân viên đang lập hợp đồng. Khi hợp đồng được gửi, bạn sẽ xem và xác nhận ngay tại trang này."/>}
            {contract&&<div className="bg-white border rounded-2xl p-5"><div className="flex flex-wrap justify-between gap-3"><div><div className="text-xs text-gray-400">HỢP ĐỒNG</div><div className="font-extrabold text-lg text-[#0D1B3E] mt-1">{contract.soHopDong || `Hợp đồng #${contract.id}`}</div></div><Pill value={contract.trangThai}/></div><div className="grid sm:grid-cols-2 gap-3 mt-4"><Mini l="Nhận xe dự kiến" v={fmtDate(contract.thoiGianNhanDuKien)}/><Mini l="Trả xe dự kiến" v={fmtDate(contract.thoiGianTraDuKien)}/><Mini l="Đơn giá/ngày" v={money(contract.donGiaNgay)}/><Mini l="Tổng tiền" v={money(contract.tongTien || contract.tienThue)}/></div>{contract.dieuKhoan&&<div className="mt-4 bg-gray-50 rounded-xl p-4 text-sm text-gray-600 whitespace-pre-wrap"><div className="font-bold text-[#0D1B3E] mb-2">Điều khoản</div>{contract.dieuKhoan}</div>}{contract.trangThai==="DRAFT"&&<div className="mt-4 text-sm bg-amber-50 text-amber-700 p-3 rounded-xl">Hợp đồng đang được nhân viên hoàn thiện và chưa gửi cho bạn.</div>}{contract.trangThai==="SENT"&&<div className="grid sm:grid-cols-2 gap-3 mt-5"><button onClick={confirmContract} className="bg-emerald-600 text-white py-3 rounded-xl font-bold">✓ Xác nhận hợp đồng</button><button onClick={rejectContract} className="border border-red-200 text-red-600 py-3 rounded-xl font-bold">Từ chối hợp đồng</button></div>}{contract.trangThai==="CUSTOMER_CONFIRMED"&&<button onClick={()=>onNavigate("payment", { contract })} className="mt-5 w-full bg-[#0D1B3E] text-white py-3 rounded-xl font-bold flex justify-center items-center gap-2"><CreditCard size={17}/> Thanh toán tiền cọc <ChevronRight size={17}/></button>}</div>}

            {contract&&cancellations.length>0&&<div className="bg-white border rounded-2xl p-5"><h3 className="font-bold text-[#0D1B3E] mb-3">Yêu cầu hủy hợp đồng</h3><div className="space-y-2">{cancellations.map(x=><div key={x.id} className="bg-slate-50 rounded-xl p-3 flex flex-wrap justify-between gap-3"><div><b className="text-sm">Yêu cầu hủy #{x.id}</b><div className="text-xs text-gray-500 mt-1">{x.lyDo||"Không có lý do"}</div><div className="text-xs text-gray-400 mt-1">Hoàn dự kiến: {money(x.soTienHoanDuKien)} · Không hoàn: {money(x.soTienPhat)}</div></div><Pill value={x.trangThai}/></div>)}</div></div>}
            {canCancelContract&&!pendingCancellation&&<button onClick={requestCancellation} className="w-full bg-white border border-red-200 text-red-600 py-3 rounded-xl font-bold hover:bg-red-50">Yêu cầu hủy hợp đồng</button>}
            {pendingCancellation&&<ActionBox title="Yêu cầu hủy đang chờ xử lý" text="Nhân viên sẽ duyệt hoặc từ chối. Nếu được duyệt, hợp đồng sẽ hủy và khoản hoàn cọc được xử lý theo chính sách."/>}

            {waitingPayment&&<ActionBox title="Đang chờ nhân viên xác nhận thanh toán" text="Bạn đã báo thanh toán thành công. Vui lòng chờ nhân viên xác nhận đã nhận tiền."/>}
            {contract&&payments.length>0&&<div className="bg-white border rounded-2xl p-5"><h3 className="font-bold text-[#0D1B3E] mb-3">Thanh toán</h3><div className="space-y-2">{payments.map(p=><div key={p.id} className="flex flex-wrap items-center justify-between gap-3 bg-gray-50 rounded-xl p-3"><div><b className="text-sm">{p.loaiThanhToan}</b><div className="text-xs text-gray-400">#{p.id} · {p.phuongThuc}</div></div><div className="flex items-center gap-3"><b>{money(p.soTien)}</b><Pill value={p.trangThai}/></div></div>)}</div></div>}
            {handover&&contract?.trangThai==="WAITING_CUSTOMER_RECEIVE"&&<div className="bg-white border border-cyan-200 rounded-2xl p-5"><div className="flex gap-3"><KeyRound className="text-[#00B4D8] shrink-0"/><div className="flex-1"><div className="font-bold text-[#0D1B3E]">Xe đã được nhân viên bàn giao</div><p className="text-sm text-gray-500 mt-1">Vui lòng kiểm tra xe và thông tin bàn giao trước khi xác nhận nhận xe.</p><div className="grid sm:grid-cols-2 gap-3 mt-4"><Mini l="Thời gian giao" v={fmtDate(handover.thoiGianGiao || handover.thoiGianBanGiao || handover.thoiGianTao)}/><Mini l="Số KM" v={`${handover.soKm ?? 0} km`}/><Mini l="Mức nhiên liệu" v={`${handover.mucNhienLieu ?? 0}%`}/><Mini l="Tình trạng xe" v={handover.tinhTrangXe || "Không ghi nhận bất thường"}/></div>{handover.ghiChu&&<div className="mt-3 bg-gray-50 rounded-xl p-3 text-sm text-gray-600"><b>Ghi chú:</b> {handover.ghiChu}</div>}<button onClick={confirmReceived} className="mt-4 w-full bg-emerald-600 text-white py-3 rounded-xl font-bold">✓ Xác nhận đã nhận xe</button></div></div></div>}
            {handover&&contract?.trangThai!=="WAITING_CUSTOMER_RECEIVE"&&<ActionBox title="Bạn đã nhận xe" text={`Bàn giao lúc: ${fmtDate(handover.thoiGianGiao || handover.thoiGianBanGiao || handover.thoiGianTao)}. Hợp đồng đang trong thời gian thuê.`}/>} 
            {contract?.trangThai==="IN_PROGRESS"&&!returnData&&<div className="bg-white border border-cyan-200 rounded-2xl p-5"><div className="flex gap-3"><Clock3 className="text-[#00B4D8] shrink-0"/><div className="flex-1"><div className="font-bold text-[#0D1B3E]">Gia hạn hợp đồng</div><p className="text-sm text-gray-500 mt-1">Bạn cần sử dụng xe lâu hơn? Gửi yêu cầu gia hạn để hệ thống kiểm tra lịch xe và nhân viên duyệt.</p><div className="grid sm:grid-cols-2 gap-3 mt-4"><Mini l="Trả hiện tại" v={fmtDate(contract.thoiGianTraDuKien)}/><Mini l="Đơn giá/ngày" v={money(contract.donGiaNgay)}/></div><button onClick={()=>onNavigate("extend", { contract })} className="mt-4 w-full bg-cyan-600 hover:bg-cyan-700 text-white py-3 rounded-xl font-bold flex items-center justify-center gap-2"><Clock3 size={17}/> Yêu cầu gia hạn hợp đồng <ChevronRight size={17}/></button></div></div></div>}
            {contract?.trangThai==="IN_PROGRESS"&&!returnData&&<div className="bg-white border rounded-2xl p-5"><div className="flex gap-3"><Car className="text-[#00B4D8] shrink-0"/><div className="flex-1"><div className="font-bold text-[#0D1B3E]">Trả xe</div>{returnRequest?<><p className="text-sm text-gray-500 mt-1">Yêu cầu trả xe đã được gửi. Nhân viên sẽ kiểm tra xe thực tế và đối chiếu với biên bản bàn giao.</p><div className="mt-3 bg-amber-50 border border-amber-100 text-amber-700 rounded-xl p-3 text-sm font-semibold">⏳ Chờ nhân viên kiểm tra xe</div>{returnRequest.ghiChu&&<div className="text-sm text-gray-500 mt-2"><b>Ghi chú:</b> {returnRequest.ghiChu}</div>}</>:<><p className="text-sm text-gray-500 mt-1">Khi muốn kết thúc chuyến thuê, bạn chỉ cần gửi yêu cầu. KM, nhiên liệu, tình trạng xe và phí phát sinh sẽ do nhân viên kiểm tra.</p><button onClick={requestReturn} className="mt-4 w-full bg-[#0D1B3E] text-white py-3 rounded-xl font-bold">Yêu cầu trả xe</button></>}</div></div></div>}
            {returnData&&<div className="bg-white border rounded-2xl p-5"><div className="flex flex-wrap justify-between gap-3"><div><div className="font-bold text-[#0D1B3E]">Đã tiếp nhận xe trả</div><div className="text-sm text-gray-500 mt-1">Nhân viên đã kiểm tra và ghi nhận tình trạng xe.</div></div><Pill value={contract?.trangThai}/></div><div className="grid sm:grid-cols-2 gap-3 mt-4"><Mini l="Thời gian trả thực tế" v={fmtDate(returnData.thoiGianTraThucTe)}/><Mini l="Số KM khi trả" v={`${returnData.soKm ?? 0} km`}/><Mini l="Nhiên liệu khi trả" v={`${returnData.mucNhienLieu ?? 0}%`}/><Mini l="Tình trạng xe" v={returnData.tinhTrangXe || "—"}/><Mini l="Phí trả muộn" v={money(returnData.phiTraMuon)}/><Mini l="Phí phát sinh khác" v={money(returnData.phiPhatSinh)}/></div><div className="mt-4 rounded-xl bg-gray-50 p-4 flex items-center justify-between"><span className="font-semibold text-gray-600">Số tiền còn phải quyết toán</span><b className="text-lg text-[#0D1B3E]">{money(returnData.tongPhiPhatSinh)}</b></div>{Number(returnData.tongPhiPhatSinh||0)>0&&contract?.trangThai==="RETURNED"&&<button onClick={()=>onNavigate("payment",{contract,paymentType:"PHI_PHAT_SINH",amount:Number(returnData.tongPhiPhatSinh)})} className="mt-4 w-full bg-[#0D1B3E] text-white py-3 rounded-xl font-bold flex justify-center items-center gap-2"><CreditCard size={17}/> Quyết toán hợp đồng <ChevronRight size={17}/></button>}{contract?.trangThai==="COMPLETED"&&<><div className="mt-4 bg-emerald-50 border border-emerald-100 text-emerald-700 rounded-xl p-3 text-sm font-semibold">✓ Hợp đồng đã hoàn tất.</div><button onClick={()=>onNavigate("review",{contract,rental,vehicle})} className="mt-3 w-full bg-amber-500 hover:bg-amber-600 text-white py-3 rounded-xl font-bold flex items-center justify-center gap-2"><Star size={18} className="fill-white"/> Đánh giá trải nghiệm chuyến thuê</button></>}</div>} 
          </>}
        </section>
      </div>
    </main>
  </div>;
}

function Mini({l,v}) { return <div className="bg-[#F6F8FB] rounded-xl p-3"><div className="text-xs text-gray-400">{l}</div><div className="font-semibold text-[#0D1B3E] mt-1">{v}</div></div>; }
function ActionBox({title,text}) { return <div className="bg-white border rounded-2xl p-5 flex gap-3"><CheckCircle2 className="text-[#00B4D8] shrink-0"/><div><div className="font-bold text-[#0D1B3E]">{title}</div><p className="text-sm text-gray-500 mt-1">{text}</p></div></div>; }
