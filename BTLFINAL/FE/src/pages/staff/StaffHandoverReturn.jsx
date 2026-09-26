import { useEffect, useState } from "react";
import { ChevronLeft, Package, CheckCircle, RotateCcw, AlertTriangle, MapPin, Phone, User } from "lucide-react";
import { contractsApi, handoversApi, rentalsApi, returnsApi } from "../../services/api";

const fmt = v => v ? new Date(v).toLocaleString("vi-VN") : "—";

export default function StaffHandoverReturn({ mode, onBack, onCompleted, contractId }) {
  const [contract, setContract] = useState(null);
  const [rental, setRental] = useState(null);
  const [handover, setHandover] = useState(null);
  const [returnRequest, setReturnRequest] = useState(null);
  const [fuel, setFuel] = useState(100);
  const [km, setKm] = useState(0);
  const [condition, setCondition] = useState("Tốt");
  const [notes, setNotes] = useState("");
  const [lateFee, setLateFee] = useState(0);
  const [extraFee, setExtraFee] = useState(0);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const c = await contractsApi.get(contractId);
        setContract(c);
        const r = await rentalsApi.get(c.idYeuCauThue);
        setRental(r);
        if (mode === "return") {
          const [h, rr] = await Promise.all([handoversApi.byContract(c.id).catch(() => null), returnsApi.requestByContract(c.id).catch(() => null)]);
          setHandover(h); setReturnRequest(rr);
          if (h) {
            setKm(Number(h.soKm || 0));
            setFuel(Number(h.mucNhienLieu ?? 100));
            setCondition(h.tinhTrangXe || "Tốt");
          }
        }
      } catch (e) {
        setError(`${e.status || ""} ${e.message || "Không tải được hợp đồng"}`.trim());
      } finally { setLoading(false); }
    })();
  }, [contractId, mode]);

  const submit = async () => {
    if (!contract || !rental) return;
    try {
      setSubmitting(true); setError(""); setMessage("");
      if (mode === "return" && !returnRequest) {
        setError("Khách hàng chưa gửi yêu cầu trả xe. Chưa thể lập biên bản kiểm tra.");
        return;
      }
      if (mode === "return" && handover && Number(km || 0) < Number(handover.soKm || 0)) {
        setError(`Số KM khi trả không được nhỏ hơn KM lúc giao (${handover.soKm} km).`);
        return;
      }
      if (Number(lateFee || 0) < 0 || Number(extraFee || 0) < 0) {
        setError("Phí trả muộn và phí phát sinh không được nhỏ hơn 0.");
        return;
      }
      if (mode === "handover") {
        const r = await handoversApi.create({
          idHopDong: Number(contract.id), idXe: Number(rental.idXe), soKm: Number(km || 0), mucNhienLieu: Number(fuel), tinhTrangXe: condition, ghiChu: notes, idNhanVien: null,
        });
        setHandover(r);
        setMessage(`Đã ghi nhận bàn giao #${r.id}. Đang chờ khách hàng xác nhận đã nhận xe.`);
        if (onCompleted) onCompleted(r);
      } else {
        const r = await returnsApi.create({
          idHopDong: Number(contract.id), idXe: Number(rental.idXe), thoiGianTraDuKien: contract.thoiGianTraDuKien,
          thoiGianTraThucTe: new Date().toISOString(), soKm: Number(km || 0), mucNhienLieu: Number(fuel), tinhTrangXe: condition,
          ghiChu: notes, phiTraMuon: Number(lateFee || 0), phiPhatSinh: Number(extraFee || 0), canBaoTri: condition === "Hư hỏng", idNhanVien: null,
        });
        setMessage(`Trả xe thành công #${r.id}. Tổng phí phát sinh: ${Number(r.tongPhiPhatSinh || 0).toLocaleString("vi-VN")} ₫.`);
      }
    } catch (e) {
      setError(`${e.status || ""} ${e.message || "Không thể xử lý"}`.trim());
    } finally { setSubmitting(false); }
  };

  const inputCls = "w-full px-3.5 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-all";
  if (loading) return <div className="p-8 text-gray-500">Đang tải dữ liệu backend...</div>;

  return <div className="h-full flex flex-col">
    <div className="flex items-center gap-3 mb-6"><button onClick={onBack} className="flex items-center gap-1.5 text-gray-500 hover:text-[#0D1B3E] text-sm font-medium"><ChevronLeft size={18}/> Quay lại</button><span className="text-gray-300">/</span><h1 className="text-xl font-bold text-[#0D1B3E]">{mode === "handover" ? "Giao xe cho khách" : "Nhận xe trả"}</h1></div>
    {error && <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl p-4 mb-4 text-sm">{error}</div>}
    {message && <div className="bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl p-4 mb-4 text-sm flex gap-2"><CheckCircle size={16}/>{message}</div>}

    <div className="grid lg:grid-cols-3 gap-5">
      <div className="lg:col-span-2 space-y-5">
        <div className="bg-white rounded-2xl border border-gray-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4">Thông tin giao nhận</h3>
          <div className="grid sm:grid-cols-2 gap-3 text-sm">
            <Info l="Hợp đồng" v={`${contract?.soHopDong || "—"} (#${contract?.id || "—"})`}/><Info l="Yêu cầu thuê" v={`#${contract?.idYeuCauThue || "—"}`}/><Info l="Xe" v={`#${rental?.idXe || "—"}`}/><Info l="Nhận dự kiến" v={fmt(contract?.thoiGianNhanDuKien)}/><Info l="Trả dự kiến" v={fmt(contract?.thoiGianTraDuKien)}/><Info l="Hình thức nhận" v={rental?.hinhThucNhanXe === "GIAO_TAN_NOI" ? "Giao xe tận nơi" : "Nhận tại cửa hàng"}/>
          </div>
          {rental?.hinhThucNhanXe === "GIAO_TAN_NOI" && <div className="mt-4 rounded-xl bg-cyan-50 border border-cyan-100 p-4 space-y-2 text-sm">
            <div className="font-bold text-[#0D1B3E]">Thông tin người nhận</div>
            <div className="flex gap-2 text-gray-700"><User size={16}/><span>{rental?.tenNguoiNhan || "—"}</span></div>
            <div className="flex gap-2 text-gray-700"><Phone size={16}/><span>{rental?.soDienThoaiNhan || "—"}</span></div>
            <div className="flex gap-2 text-gray-700"><MapPin size={16}/><span>{rental?.diaChiGiaoXe || "—"}</span></div>
            {rental?.ghiChuGiaoXe && <div className="text-gray-500 pt-1">Ghi chú: {rental.ghiChuGiaoXe}</div>}
          </div>}
        </div>

        {mode === "return" && returnRequest && <div className="bg-amber-50 rounded-2xl border border-amber-200 p-5"><h3 className="font-bold text-amber-800">Yêu cầu trả xe từ khách hàng</h3><div className="text-sm text-amber-700 mt-2">Gửi lúc: {fmt(returnRequest.thoiGianYeuCau)}</div><div className="text-sm text-amber-700 mt-1">Ghi chú: {returnRequest.ghiChu || "Không có"}</div><div className="text-xs text-amber-600 mt-3">Nhân viên chịu trách nhiệm kiểm tra KM, nhiên liệu, tình trạng xe và xác định phí phát sinh.</div></div>}
        {mode === "return" && handover && <div className="bg-white rounded-2xl border border-cyan-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4">Đối chiếu với lúc bàn giao</h3>
          <div className="grid sm:grid-cols-3 gap-3">
            <Info l="KM lúc giao" v={`${handover.soKm ?? 0} km`}/><Info l="Nhiên liệu lúc giao" v={`${handover.mucNhienLieu ?? 0}%`}/><Info l="Tình trạng lúc giao" v={handover.tinhTrangXe || "—"}/>
          </div>
          <div className="mt-3 text-xs text-gray-500">Nhân viên nhập thông tin thực tế khi nhận lại xe. KM trả không được nhỏ hơn KM lúc giao.</div>
        </div>}

        <div className="bg-white rounded-2xl border border-gray-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4 flex items-center gap-2">{mode === "handover" ? <><Package size={16} className="text-[#00B4D8]"/> Thông tin bàn giao</> : <><RotateCcw size={16} className="text-[#00B4D8]"/> Thông tin trả xe</>}</h3>
          <div className="grid sm:grid-cols-2 gap-4"><label><span className="block text-xs font-semibold text-gray-500 mb-1.5">SỐ KM</span><input type="number" min="0" value={km} onChange={e=>setKm(e.target.value)} className={inputCls}/></label><label><span className="block text-xs font-semibold text-gray-500 mb-1.5">MỨC NHIÊN LIỆU: {fuel}%</span><input type="range" min="0" max="100" step="5" value={fuel} onChange={e=>setFuel(Number(e.target.value))} className="w-full accent-[#00B4D8] mt-3"/></label></div>
          <div className="mt-4"><label className="block text-xs font-semibold text-gray-500 mb-1.5">TÌNH TRẠNG XE</label><div className="grid grid-cols-3 gap-2">{["Tốt","Trầy xước nhẹ","Hư hỏng"].map(v=><button key={v} onClick={()=>setCondition(v)} className={`py-2.5 rounded-xl text-xs font-semibold border-2 ${condition===v ? "border-[#00B4D8] bg-cyan-50 text-cyan-700":"border-gray-200 text-gray-500"}`}>{v}</button>)}</div></div>
          {mode === "return" && <div className="grid sm:grid-cols-2 gap-3 mt-4"><label><span className="block text-xs font-semibold text-gray-500 mb-1.5">PHÍ TRẢ MUỘN</span><input type="number" value={lateFee} onChange={e=>setLateFee(e.target.value)} className={inputCls}/></label><label><span className="block text-xs font-semibold text-gray-500 mb-1.5">PHÍ PHÁT SINH</span><input type="number" value={extraFee} onChange={e=>setExtraFee(e.target.value)} className={inputCls}/></label></div>}
          <div className="mt-4"><label className="block text-xs font-semibold text-gray-500 mb-1.5">GHI CHÚ</label><textarea value={notes} onChange={e=>setNotes(e.target.value)} rows="3" className={`${inputCls} resize-none`}/></div>
        </div>

        {condition === "Hư hỏng" && <div className="bg-red-50 border border-red-200 rounded-xl p-4 flex gap-3 text-sm text-red-700"><AlertTriangle size={16}/><div><b>Xe sẽ tự chuyển sang BẢO TRÌ (MAINTENANCE).</b><div className="mt-1">Xe sẽ không xuất hiện là sẵn sàng cho thuê cho đến khi nhân viên/Admin cập nhật lại trạng thái sau bảo trì. Hãy ghi rõ hư hỏng và phí phát sinh trước khi xác nhận.</div></div></div>}
        <button disabled={submitting || !!message || (mode === "return" && !returnRequest)} onClick={submit} className={`w-full py-4 rounded-2xl font-bold text-sm text-white disabled:bg-gray-300 flex items-center justify-center gap-2 ${mode === "handover" ? "bg-[#0D1B3E]":"bg-emerald-600"}`}>{mode === "handover" ? <Package size={18}/> : <CheckCircle size={18}/>} {submitting ? "Đang xử lý..." : mode === "handover" ? "Xác nhận đã giao xe" : "Xác nhận kết quả kiểm tra"}</button>
      </div>
      <div><div className="bg-white rounded-2xl border border-gray-100 p-5 text-sm text-gray-600"><h4 className="font-bold text-[#0D1B3E] mb-3">Lưu ý {mode === "handover" ? "bàn giao" : "nhận xe"}</h4>{mode === "handover" ? <><p>• Đối chiếu đúng khách hàng và xe trước khi giao.</p><p>• Ghi số KM, nhiên liệu và tình trạng xe thực tế.</p><p>• Sau khi bạn xác nhận giao, khách hàng sẽ nhận thông báo và bấm xác nhận đã nhận xe.</p></> : <><p>• Đối chiếu tình trạng xe với lúc bàn giao.</p><p>• Ghi rõ hư hỏng hoặc chi phí phát sinh nếu có.</p><p>• Xác nhận sau khi đã nhận lại xe.</p></>}</div></div>
    </div>
  </div>;
}

function Info({l,v}) { return <div className="bg-gray-50 rounded-xl p-3"><div className="text-gray-400 text-xs">{l}</div><div className="font-semibold text-[#0D1B3E] mt-1">{v}</div></div>; }
