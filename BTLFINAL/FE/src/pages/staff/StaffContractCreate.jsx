import { useEffect, useMemo, useState } from "react";
import { ChevronLeft, FileText, CheckCircle, Send, Save, Car, User } from "lucide-react";
import { contractsApi, customersApi, rentalsApi, vehiclesApi } from "../../services/api";

const money = n => `${Number(n || 0).toLocaleString("vi-VN")} ₫`;
const localInput = d => {
  if (!d) return "";
  const x = new Date(d);
  if (Number.isNaN(x.getTime())) return "";
  const pad = v => String(v).padStart(2, "0");
  return `${x.getFullYear()}-${pad(x.getMonth() + 1)}-${pad(x.getDate())}T${pad(x.getHours())}:${pad(x.getMinutes())}`;
};

export default function StaffContractCreate({ onBack, requestId = null, onCreated }) {
  const [rentals, setRentals] = useState([]);
  const [vehicles, setVehicles] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [selectedRentalId, setSelectedRentalId] = useState(requestId ? String(requestId) : "");
  const [soHopDong, setSoHopDong] = useState(`HD-${new Date().toISOString().slice(0,10).replaceAll("-", "")}-${String(Date.now()).slice(-4)}`);
  const [donGiaNgay, setDonGiaNgay] = useState(0);
  const [pickup, setPickup] = useState("");
  const [returnAt, setReturnAt] = useState("");
  const [terms, setTerms] = useState(`1. Người thuê xuất trình giấy tờ hợp lệ khi nhận xe.\n2. Xe phải được trả đúng thời gian và tình trạng đã thỏa thuận.\n3. Các khoản phát sinh được xử lý theo biên bản trả xe.\n4. Hai bên chịu trách nhiệm thực hiện đúng các điều khoản của hợp đồng.`);
  const [created, setCreated] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  useEffect(() => {
    (async () => {
      try {
        const [r, v, c, contracts] = await Promise.all([
          rentalsApi.list("page=1&pageSize=100"),
          vehiclesApi.list(),
          customersApi.list(),
          contractsApi.discover({ start: 1, max: 80, missLimit: 12 }),
        ]);

        // Chỉ cho phép lập hợp đồng từ yêu cầu APPROVED CHƯA có hợp đồng.
        // Một yêu cầu thuê chỉ được gắn với tối đa một hợp đồng, kể cả hợp đồng cũ
        // đã COMPLETED/CANCELLED. Backend vẫn là lớp bảo vệ cuối cùng bằng 409.
        const contractRentalIds = new Set(
          (contracts || [])
            .map(x => Number(x?.idYeuCauThue))
            .filter(Number.isFinite)
        );

        const availableRentals = (r?.items || r || []).filter(x =>
          x.trangThai === "APPROVED" && !contractRentalIds.has(Number(x.id))
        );

        setRentals(availableRentals);
        setVehicles(v || []);
        setCustomers(c || []);

        // Nếu màn hình được mở từ một yêu cầu đã có hợp đồng thì bỏ lựa chọn đó.
        if (requestId && !availableRentals.some(x => String(x.id) === String(requestId))) {
          setSelectedRentalId("");
          setMessage(`Yêu cầu thuê #${requestId} đã có hợp đồng hoặc không còn đủ điều kiện để lập hợp đồng mới.`);
        }
      } catch (e) {
        setError(`${e.status || ""} ${e.message || "Không tải được dữ liệu"}`.trim());
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const rental = useMemo(() => rentals.find(x => String(x.id) === String(selectedRentalId)), [rentals, selectedRentalId]);
  const vehicle = useMemo(() => rental ? vehicles.find(x => Number(x.id) === Number(rental.idXe)) : null, [rental, vehicles]);
  const customer = useMemo(() => rental ? customers.find(x => Number(x.idKhachHang) === Number(rental.idKhachHang)) : null, [rental, customers]);

  useEffect(() => {
    if (!rental) return;
    setPickup(localInput(rental.thoiGianNhan));
    setReturnAt(localInput(rental.thoiGianTraDuKien));
    setDonGiaNgay(Number(vehicle?.pricePerDay || vehicle?.donGiaNgay || 0));
  }, [rental, vehicle]);

  const days = useMemo(() => {
    if (!pickup || !returnAt) return 0;
    const h = (new Date(returnAt) - new Date(pickup)) / 3600000;
    return Math.max(1, Math.ceil(h / 24));
  }, [pickup, returnAt]);

  const rentAmount = days * Number(donGiaNgay || 0);

  const createContract = async () => {
    if (!rental) return setError("Chọn một yêu cầu thuê đã được duyệt.");
    if (!soHopDong.trim()) return setError("Nhập số hợp đồng.");
    if (!pickup || !returnAt) return setError("Chọn thời gian nhận/trả xe.");
    try {
      setSaving(true); setError(""); setMessage("");
      const result = await contractsApi.create({
        idYeuCauThue: Number(rental.id),
        soHopDong: soHopDong.trim(),
        donGiaNgay: Number(donGiaNgay || 0),
        dieuKhoan: terms,
        thoiGianNhanDuKien: new Date(pickup).toISOString(),
        thoiGianTraDuKien: new Date(returnAt).toISOString(),
      });
      setCreated(result);
      setMessage(`Đã lập hợp đồng ${result.soHopDong} ở trạng thái ${result.trangThai}.`);
      onCreated?.(result);
    } catch (e) {
      setError(`${e.status || ""} ${e.message || "Không thể lập hợp đồng"}`.trim());
    } finally { setSaving(false); }
  };

  const sendContract = async () => {
    if (!created?.id) return;
    try {
      setSending(true); setError("");
      const result = await contractsApi.send(created.id);
      setCreated(result);
      setMessage(`Đã gửi hợp đồng ${result.soHopDong} cho khách hàng.`);
      onCreated?.(result);
    } catch (e) {
      setError(`${e.status || ""} ${e.message || "Không thể gửi hợp đồng"}`.trim());
    } finally { setSending(false); }
  };

  const inputCls = "w-full px-3.5 py-3 rounded-xl border border-gray-200 text-sm focus:outline-none focus:border-[#00B4D8] transition-all bg-white";
  const roCls = "w-full px-3.5 py-3 rounded-xl border border-gray-100 text-sm bg-gray-50 text-gray-600";

  return <div className="h-full flex flex-col">
    <div className="flex items-center gap-3 mb-6">
      <button onClick={onBack} className="flex items-center gap-1.5 text-gray-500 hover:text-[#0D1B3E] text-sm font-medium"><ChevronLeft size={18}/> Hợp đồng</button>
      <span className="text-gray-300">/</span>
      <h1 className="text-xl font-bold text-[#0D1B3E]">Lập hợp đồng mới</h1>
      <span className={`ml-2 text-xs font-semibold px-2.5 py-1 rounded-full ${created?.trangThai === "SENT" ? "bg-blue-50 text-blue-700" : "bg-gray-100 text-gray-500"}`}>{created?.trangThai || "DRAFT"}</span>
    </div>

    {error && <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl p-4 mb-4 text-sm">{error}</div>}
    {message && <div className="bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl p-4 mb-4 text-sm flex gap-2"><CheckCircle size={16}/>{message}</div>}

    <div className="grid lg:grid-cols-3 gap-5">
      <div className="lg:col-span-2 space-y-5">
        <div className="bg-white rounded-2xl border border-gray-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4 flex items-center gap-2"><FileText size={16} className="text-[#00B4D8]"/> Yêu cầu thuê đã duyệt</h3>
          {loading ? <div className="text-gray-400 text-sm">Đang tải dữ liệu backend...</div> : <select value={selectedRentalId} onChange={e=>setSelectedRentalId(e.target.value)} className={inputCls} disabled={!!created}>
            <option value="">-- Chọn yêu cầu APPROVED --</option>
            {rentals.map(r => <option key={r.id} value={r.id}>#{r.id} · Xe #{r.idXe} · {new Date(r.thoiGianNhan).toLocaleString("vi-VN")} → {new Date(r.thoiGianTraDuKien).toLocaleString("vi-VN")}</option>)}
          </select>}
          {!loading && rentals.length === 0 && <p className="mt-3 text-sm text-amber-600">Hiện chưa có yêu cầu APPROVED chưa xử lý.</p>}
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4">Thông tin hợp đồng</h3>
          <div className="grid sm:grid-cols-2 gap-3">
            <div><label className="block text-xs font-semibold text-gray-400 mb-1.5">SỐ HỢP ĐỒNG</label><input value={soHopDong} onChange={e=>setSoHopDong(e.target.value)} className={inputCls} disabled={!!created}/></div>
            <div><label className="block text-xs font-semibold text-gray-400 mb-1.5">MÃ YÊU CẦU</label><input value={rental ? `#${rental.id}` : "—"} readOnly className={roCls}/></div>
          </div>
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4">Khách hàng & Xe</h3>
          <div className="grid sm:grid-cols-2 gap-4">
            <div className="bg-gray-50 rounded-xl p-4"><div className="flex items-center gap-2 text-xs font-semibold text-gray-400 uppercase mb-3"><User size={14}/> Bên thuê</div><div className="font-semibold text-[#0D1B3E]">{customer?.hoTen || (rental ? `Khách hàng #${rental.idKhachHang}` : "—")}</div><div className="text-sm text-gray-500 mt-1">{customer?.email || "—"}</div><div className="text-sm text-gray-500">CCCD: {customer?.soCccd || "—"}</div></div>
            <div className="bg-gray-50 rounded-xl p-4"><div className="flex items-center gap-2 text-xs font-semibold text-gray-400 uppercase mb-3"><Car size={14}/> Xe thuê</div><div className="font-semibold text-[#0D1B3E]">{vehicle?.name || (rental ? `Xe #${rental.idXe}` : "—")}</div><div className="text-sm text-gray-500 mt-1">{vehicle?.plate || "—"}</div><div className="text-sm text-gray-500">{money(vehicle?.pricePerDay || donGiaNgay)}/ngày</div></div>
          </div>
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 p-5">
          <h3 className="font-bold text-[#0D1B3E] mb-4">Thời gian & Tiền thuê</h3>
          <div className="grid sm:grid-cols-2 gap-3 mb-4">
            <div><label className="block text-xs font-semibold text-gray-400 mb-1.5">NHẬN XE DỰ KIẾN</label><input type="datetime-local" value={pickup} onChange={e=>setPickup(e.target.value)} className={inputCls} disabled={!!created}/></div>
            <div><label className="block text-xs font-semibold text-gray-400 mb-1.5">TRẢ XE DỰ KIẾN</label><input type="datetime-local" value={returnAt} onChange={e=>setReturnAt(e.target.value)} className={inputCls} disabled={!!created}/></div>
            <div><label className="block text-xs font-semibold text-gray-400 mb-1.5">SỐ NGÀY THUÊ</label><input value={days || "—"} readOnly className={roCls}/></div>
            <div><label className="block text-xs font-semibold text-gray-400 mb-1.5">ĐƠN GIÁ/NGÀY</label><input type="number" value={donGiaNgay} onChange={e=>setDonGiaNgay(e.target.value)} className={inputCls} disabled={!!created}/></div>
          </div>
          <div className="bg-[#F6F8FB] rounded-xl p-4 flex justify-between font-bold"><span>Tạm tính tiền thuê</span><span className="text-[#00B4D8]">{money(created?.tienThue ?? rentAmount)}</span></div>
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 p-5"><h3 className="font-bold text-[#0D1B3E] mb-3">Điều khoản hợp đồng</h3><textarea value={terms} onChange={e=>setTerms(e.target.value)} rows={7} disabled={!!created} className={`${inputCls} resize-none font-mono`}/></div>
      </div>

      <div className="space-y-4">
        <div className="bg-white rounded-2xl border border-gray-100 p-5 sticky top-0">
          <h3 className="font-bold text-[#0D1B3E] mb-4">Thao tác</h3>
          {!created ? <button disabled={saving || !rental} onClick={createContract} className="w-full flex items-center justify-center gap-2 py-3 bg-[#0D1B3E] disabled:bg-gray-300 text-white rounded-xl font-bold text-sm hover:bg-[#1A3A6E] transition-colors"><Save size={16}/>{saving ? "Đang lập..." : "Lập hợp đồng"}</button> : <>
            <div className="bg-gray-50 rounded-xl p-4 text-sm mb-3"><div className="text-gray-400 text-xs">Hợp đồng vừa tạo</div><div className="font-bold text-[#0D1B3E] mt-1">{created.soHopDong}</div><div className="text-gray-500">ID #{created.id} · {created.trangThai}</div></div>
            {created.trangThai === "DRAFT" && <button disabled={sending} onClick={sendContract} className="w-full flex items-center justify-center gap-2 py-3 bg-[#0D1B3E] text-white rounded-xl font-bold text-sm hover:bg-[#1A3A6E]"><Send size={16}/>{sending ? "Đang gửi..." : "Gửi cho khách hàng"}</button>}
          </>}
          <div className="mt-5 pt-5 border-t border-gray-100 space-y-2 text-xs text-gray-400"><p>• Chỉ yêu cầu thuê APPROVED mới được lập hợp đồng.</p><p>• Hợp đồng mới ở trạng thái DRAFT.</p><p>• Sau khi gửi sẽ chuyển sang SENT để khách xác nhận.</p></div>
        </div>
      </div>
    </div>
  </div>;
}
