import { useEffect, useMemo, useState } from "react";
import { CalendarDays, ChevronLeft, ChevronRight, RefreshCw } from "lucide-react";
import { rentalsApi } from "../../services/api";

const pad = n => String(n).padStart(2, "0");
const localKey = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const fmt = value => new Date(value).toLocaleString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });

export default function VehicleAvailabilityCalendar({ vehicleId }) {
  const [month, setMonth] = useState(() => { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1); });
  const [periods, setPeriods] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const monthStart = new Date(month.getFullYear(), month.getMonth(), 1);
  const monthEnd = new Date(month.getFullYear(), month.getMonth() + 1, 1);

  const load = async () => {
    if (!vehicleId) return;
    setLoading(true); setError("");
    try {
      const data = await rentalsApi.bookedPeriods(Number(vehicleId), monthStart.toISOString(), monthEnd.toISOString());
      setPeriods(Array.isArray(data) ? data : []);
    } catch (e) {
      setPeriods([]);
      setError(e.status === 401 ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại để xem lịch xe." : (e.message || "Không tải được lịch xe."));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [vehicleId, month.getFullYear(), month.getMonth()]);

  const busyDays = useMemo(() => {
    const set = new Set();
    periods.forEach(p => {
      const start = new Date(p.start);
      const end = new Date(p.end);
      let d = new Date(start.getFullYear(), start.getMonth(), start.getDate());
      const last = new Date(end.getFullYear(), end.getMonth(), end.getDate());
      while (d <= last) { set.add(localKey(d)); d.setDate(d.getDate() + 1); }
    });
    return set;
  }, [periods]);

  const cells = useMemo(() => {
    const first = new Date(month.getFullYear(), month.getMonth(), 1);
    const offset = (first.getDay() + 6) % 7;
    const count = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
    return [...Array(offset).fill(null), ...Array.from({ length: count }, (_, i) => new Date(month.getFullYear(), month.getMonth(), i + 1))];
  }, [month]);

  const today = new Date();
  const today0 = new Date(today.getFullYear(), today.getMonth(), today.getDate());

  return (
    <div className="mb-6 rounded-2xl border border-gray-200 bg-[#F8FAFC] p-4 sm:p-5">
      <div className="flex items-center justify-between gap-3 mb-4">
        <div>
          <h3 className="font-bold text-[#0D1B3E] flex items-center gap-2"><CalendarDays size={18} className="text-[#00B4D8]" /> Lịch khả dụng của xe</h3>
          <p className="text-xs text-gray-500 mt-1">Xem trước các ngày đã có lịch thuê rồi chọn ngày/giờ bên dưới.</p>
        </div>
        <button onClick={load} type="button" className="p-2 rounded-lg border bg-white hover:bg-gray-50" title="Tải lại lịch"><RefreshCw size={15} className={loading ? "animate-spin" : ""} /></button>
      </div>

      <div className="flex items-center justify-between mb-3">
        <button type="button" onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))} className="p-2 rounded-lg hover:bg-white"><ChevronLeft size={18} /></button>
        <div className="font-bold text-sm text-[#0D1B3E]">Tháng {pad(month.getMonth() + 1)}/{month.getFullYear()}</div>
        <button type="button" onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))} className="p-2 rounded-lg hover:bg-white"><ChevronRight size={18} /></button>
      </div>

      <div className="grid grid-cols-7 gap-1 text-center text-[11px] font-bold text-gray-400 mb-1">
        {["T2","T3","T4","T5","T6","T7","CN"].map(x => <div key={x} className="py-1">{x}</div>)}
      </div>
      <div className="grid grid-cols-7 gap-1">
        {cells.map((d, i) => {
          if (!d) return <div key={`e-${i}`} />;
          const busy = busyDays.has(localKey(d));
          const past = d < today0;
          return <div key={localKey(d)} title={busy ? "Ngày này có lịch thuê - xem khoảng giờ bên dưới" : "Chưa có lịch thuê"} className={`h-10 rounded-lg flex items-center justify-center text-sm font-semibold border ${past ? "bg-gray-100 text-gray-300 border-gray-100" : busy ? "bg-red-50 text-red-700 border-red-200" : "bg-emerald-50 text-emerald-700 border-emerald-100"}`}>{d.getDate()}</div>;
        })}
      </div>

      <div className="flex flex-wrap gap-4 mt-4 text-xs text-gray-600">
        <span className="flex items-center gap-1.5"><i className="w-3 h-3 rounded bg-emerald-100 border border-emerald-200" /> Chưa có lịch</span>
        <span className="flex items-center gap-1.5"><i className="w-3 h-3 rounded bg-red-100 border border-red-200" /> Có lịch thuê</span>
        <span className="flex items-center gap-1.5"><i className="w-3 h-3 rounded bg-gray-200" /> Ngày đã qua</span>
      </div>

      {error && <div className="mt-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl px-3 py-2">{error}</div>}
      {!error && !loading && periods.length === 0 && <div className="mt-4 text-sm text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-xl px-3 py-2">✓ Tháng này chưa có lịch thuê.</div>}
      {periods.length > 0 && <div className="mt-4 space-y-2">
        <p className="text-xs font-bold text-gray-500 uppercase tracking-wide">Khoảng thời gian đã có lịch</p>
        {periods.map(p => <div key={p.rentalId} className="text-sm bg-white border border-red-100 rounded-xl px-3 py-2.5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1">
          <span className="font-semibold text-red-700">{fmt(p.start)} → {fmt(p.end)}</span>
          <span className="text-[11px] font-bold text-amber-700 bg-amber-50 px-2 py-1 rounded-full self-start sm:self-auto">{p.trangThai}</span>
        </div>)}
      </div>}
      <p className="text-[11px] text-gray-500 mt-3">Lưu ý: màu đỏ nghĩa là ngày đó có ít nhất một khoảng giờ đã được đặt. Bạn vẫn có thể chọn giờ khác trong ngày; hệ thống sẽ kiểm tra chính xác trước khi tiếp tục.</p>
    </div>
  );
}
