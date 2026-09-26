import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, CheckCircle2, Clock3, Copy, CreditCard, RefreshCw, ShieldCheck } from "lucide-react";
import { paymentsApi } from "../../services/api";

const money = n => `${Number(n || 0).toLocaleString("vi-VN")} ₫`;
const BANK = { name: "MB Bank (DEMO)", account: "0123456789", holder: "CONG TY THUE XE DEMO" };

export default function PaymentPage({ onNavigate, initialData }) {
  const contract = initialData?.contract || initialData || null;
  const paymentType = initialData?.paymentType || "TIEN_COC";
  const requestedAmount = Number(initialData?.amount || 0);
  const [payment, setPayment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState("");
  const [msg, setMsg] = useState("");

  const amount = paymentType === "PHI_PHAT_SINH"
    ? requestedAmount
    : paymentType === "TIEN_COC"
      ? Number(contract?.tienCoc || 0)
      : Number(contract?.tienThue || contract?.tongTien || 0);
  const transferText = payment?.maGiaoDich || "";
  const qrPayload = useMemo(() => JSON.stringify({ bank:BANK.name, account:BANK.account, holder:BANK.holder, amount:payment?.soTien || amount, content:transferText }), [payment, amount, transferText]);
  const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=260x260&margin=8&data=${encodeURIComponent(qrPayload)}`;

  const load = async () => {
    if (!contract?.id) { setErr("Không tìm thấy hợp đồng cần thanh toán."); setLoading(false); return; }
    try {
      setErr("");
      const rows = await paymentsApi.byContract(contract.id);
      const list = Array.isArray(rows) ? rows : [];
      const current = list.find(x => x.loaiThanhToan === paymentType && !["REFUNDED"].includes(x.trangThai));
      setPayment(current || null);
    } catch (e) { setErr(`${e.status || ""} ${e.message}`.trim()); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [contract?.id]);
  useEffect(() => { const f=()=>load(); window.addEventListener("carrent:remote-update",f); return()=>window.removeEventListener("carrent:remote-update",f); }, [contract?.id]);
  useEffect(() => {
    if (payment?.trangThai !== "WAITING_CONFIRMATION") return;
    const timer = setInterval(load, 3000);
    return () => clearInterval(timer);
  }, [payment?.trangThai, contract?.id]);

  const createPayment = async () => {
    try {
      setBusy(true); setErr(""); setMsg("");
      const r = await paymentsApi.create({ idHopDong:contract.id, loaiThanhToan:paymentType, soTien:amount, phuongThuc:"CHUYEN_KHOAN", ghiChu:paymentType === "PHI_PHAT_SINH" ? "Quyết toán hợp đồng sau khi trả xe" : (paymentType === "TIEN_COC" ? "Thanh toán tiền cọc giữ xe" : "Thanh toán tiền thuê xe") });
      setPayment(r); setMsg("Đã tạo mã thanh toán. Vui lòng chuyển khoản theo đúng nội dung bên dưới.");
    } catch(e) { setErr(`${e.status || ""} ${e.message}`.trim()); }
    finally { setBusy(false); }
  };

  const markPaid = async () => {
    try {
      setBusy(true); setErr(""); setMsg("");
      const r = await paymentsApi.customerPaid(payment.id);
      setPayment(r); setMsg("Đã gửi thông báo. Nhân viên đang đối soát giao dịch của bạn.");
    } catch(e) { setErr(`${e.status || ""} ${e.message}`.trim()); }
    finally { setBusy(false); }
  };

  const copy = async value => { try { await navigator.clipboard.writeText(value); setMsg("Đã sao chép."); } catch {} };
  const status = payment?.trangThai;

  return <div className="min-h-screen bg-[#F6F8FB]">
    <div className="max-w-5xl mx-auto p-5 md:p-7">
      <button onClick={()=>onNavigate("tracking", { idYeuCau: contract?.idYeuCauThue })} className="flex items-center gap-2 text-gray-500 mb-5"><ArrowLeft size={17}/> Theo dõi thuê xe</button>
      <div className="bg-white rounded-3xl border overflow-hidden">
        <div className="p-6 border-b flex flex-wrap justify-between gap-4">
          <div className="flex gap-3"><div className="w-11 h-11 rounded-xl bg-cyan-50 text-cyan-600 flex items-center justify-center"><CreditCard/></div><div><h1 className="text-2xl font-extrabold text-[#0D1B3E]">{paymentType === "PHI_PHAT_SINH" ? "Quyết toán hợp đồng" : (paymentType === "TIEN_COC" ? "Thanh toán tiền cọc" : "Thanh toán hợp đồng")}</h1><p className="text-sm text-gray-500">Chuyển khoản và chờ nhân viên xác nhận thanh toán.</p></div></div>
          <div className="text-right"><div className="text-xs text-gray-400">SỐ TIỀN CẦN THANH TOÁN</div><div className="text-2xl font-extrabold text-[#0D1B3E]">{money(payment?.soTien || amount)}</div></div>
        </div>
        <div className="p-6">
          <div className="grid sm:grid-cols-3 gap-3 mb-5"><Mini l="Hợp đồng" v={contract?.soHopDong || `#${contract?.id || "—"}`}/><Mini l="Trạng thái hợp đồng" v={contract?.trangThai || "—"}/><Mini l="Hình thức" v="Chuyển khoản"/></div>
          {err&&<Notice type="error">{err}</Notice>}{msg&&<Notice>{msg}</Notice>}
          {loading ? <div className="py-14 text-center text-gray-400">Đang tải thanh toán...</div> : !payment ? <div className="text-center py-10"><ShieldCheck className="mx-auto text-cyan-500 mb-3" size={38}/><h2 className="font-bold text-xl text-[#0D1B3E]">Sẵn sàng thanh toán</h2><p className="text-sm text-gray-500 mt-2 mb-5">Hệ thống sẽ tự tạo mã giao dịch riêng cho hợp đồng này.</p><button disabled={busy} onClick={createPayment} className="bg-[#0D1B3E] text-white px-7 py-3 rounded-xl font-bold disabled:opacity-50">{busy?"Đang tạo...":"Tạo mã & QR thanh toán"}</button></div> : status === "PAID" ? <Success onNavigate={onNavigate} contract={contract} extraFee={paymentType === "PHI_PHAT_SINH"}/> : <div className="grid lg:grid-cols-[300px_1fr] gap-7">
            <div className="text-center"><div className="border rounded-2xl p-4 inline-block bg-white"><img src={qrUrl} alt="QR thanh toán demo" className="w-[260px] h-[260px]"/></div><div className="text-xs text-gray-400 mt-3">QR DEMO · chứa thông tin chuyển khoản</div></div>
            <div className="space-y-3"><Row l="Ngân hàng" v={BANK.name}/><Row l="Số tài khoản" v={BANK.account} copy={()=>copy(BANK.account)}/><Row l="Chủ tài khoản" v={BANK.holder}/><Row l="Số tiền" v={money(payment.soTien)} copy={()=>copy(String(payment.soTien))}/><Row l="Nội dung chuyển khoản" v={payment.maGiaoDich} copy={()=>copy(payment.maGiaoDich)}/>
              {status === "PENDING" && <><div className="bg-amber-50 border border-amber-200 text-amber-800 rounded-xl p-4 text-sm">Chuyển đúng <b>số tiền</b> và <b>nội dung {payment.maGiaoDich}</b>. Sau khi chuyển khoản, bấm nút bên dưới.</div><button disabled={busy} onClick={markPaid} className="w-full bg-emerald-600 text-white py-3.5 rounded-xl font-bold disabled:opacity-50">{busy?"Đang gửi...":"Tôi đã thanh toán"}</button></>}
              {status === "WAITING_CONFIRMATION" && <div className="bg-blue-50 border border-blue-200 text-blue-800 rounded-2xl p-5"><div className="flex gap-3"><Clock3 className="shrink-0"/><div><b>Đang chờ nhân viên đối soát</b><p className="text-sm mt-1">Hệ thống tự kiểm tra trạng thái khoảng 3 giây/lần. Bạn không cần tạo giao dịch mới.</p></div></div><button onClick={load} className="mt-4 flex items-center gap-2 text-sm font-bold"><RefreshCw size={15}/> Kiểm tra ngay</button></div>}
              {status === "FAILED" && <div className="bg-red-50 border border-red-200 text-red-700 rounded-2xl p-5"><b>Chưa xác nhận được thanh toán</b><p className="text-sm mt-1">Nhân viên chưa tìm thấy khoản tiền khớp. Vui lòng kiểm tra lại giao dịch.</p></div>}
            </div>
          </div>}
        </div>
      </div>
    </div>
  </div>;
}
function Mini({l,v}) { return <div className="bg-[#F6F8FB] rounded-xl p-3"><div className="text-xs text-gray-400">{l}</div><div className="font-semibold text-[#0D1B3E] mt-1">{v}</div></div>; }
function Row({l,v,copy}) { return <div className="border rounded-xl p-3 flex items-center justify-between gap-4"><div><div className="text-xs text-gray-400">{l}</div><div className="font-bold text-[#0D1B3E] mt-1 break-all">{v}</div></div>{copy&&<button onClick={copy} className="text-cyan-600"><Copy size={17}/></button>}</div>; }
function Notice({children,type}) { return <div className={`${type==="error"?"bg-red-50 border-red-200 text-red-700":"bg-emerald-50 border-emerald-200 text-emerald-700"} border p-3 rounded-xl mb-4 flex gap-2`}><CheckCircle2 size={17}/>{children}</div>; }
function Success({onNavigate,contract,extraFee}) { return <div className="py-12 text-center"><div className="w-16 h-16 mx-auto rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center"><CheckCircle2 size={34}/></div><h2 className="text-2xl font-extrabold text-[#0D1B3E] mt-4">Thanh toán thành công</h2><p className="text-gray-500 mt-2">{extraFee ? "Khoản quyết toán cuối đã được xác nhận. Hợp đồng đã hoàn tất." : "Hợp đồng đã được thanh toán và xe đang được chuẩn bị để bàn giao."}</p><button onClick={()=>onNavigate("tracking",{idYeuCau:contract?.idYeuCauThue})} className="mt-6 bg-[#0D1B3E] text-white px-6 py-3 rounded-xl font-bold">Xem tiến trình thuê xe</button></div>; }
