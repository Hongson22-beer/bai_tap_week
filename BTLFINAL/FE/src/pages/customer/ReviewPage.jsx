import { useEffect, useState } from "react";
import { ArrowLeft, CheckCircle2, MessageSquareText, Star } from "lucide-react";
import { evaluationsApi } from "../../services/api";

export default function ReviewPage({ onNavigate, initialData }) {
  const contract = initialData?.contract || null;
  const rental = initialData?.rental || null;
  const vehicle = initialData?.vehicle || null;
  const [rating, setRating] = useState(5);
  const [text, setText] = useState("");
  const [existing, setExisting] = useState(initialData?.evaluation || null);
  const [loading, setLoading] = useState(!!contract?.id && !initialData?.evaluation);
  const [submitting, setSubmitting] = useState(false);
  const [err, setErr] = useState("");
  const [msg, setMsg] = useState("");

  useEffect(() => {
    if (!contract?.id || existing) { setLoading(false); return; }
    let alive = true;
    evaluationsApi.myByContract(contract.id)
      .then(r => { if (alive) setExisting(r); })
      .catch(e => { if (alive && e.status !== 404) setErr(e.message || "Không kiểm tra được đánh giá"); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, [contract?.id]);

  const submit = async () => {
    if (!contract?.id) { setErr("Vui lòng mở đánh giá từ một hợp đồng đã hoàn tất."); return; }
    try {
      setSubmitting(true); setErr(""); setMsg("");
      const r = await evaluationsApi.create({ idHopDong: Number(contract.id), idKhachHang: 0, idXe: rental?.idXe ? Number(rental.idXe) : null, diemDanhGia: Number(rating), nhanXet: text.trim() });
      setExisting(r); setMsg("Cảm ơn bạn. Đánh giá đã được ghi nhận cho chuyến thuê này.");
    } catch (e) { setErr(`${e.status || ""} ${e.message || "Không gửi được đánh giá"}`.trim()); }
    finally { setSubmitting(false); }
  };

  const stars = existing?.diemDanhGia || rating;
  return <div className="min-h-screen bg-[#F5F7FB] p-5 sm:p-8">
    <div className="max-w-2xl mx-auto">
      <button onClick={() => onNavigate("tracking", initialData?.rental)} className="flex items-center gap-2 text-sm text-slate-500 mb-5"><ArrowLeft size={17}/> Quay lại hành trình</button>
      <div className="bg-white rounded-3xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-6 sm:p-8 bg-gradient-to-br from-[#0D1B3E] to-[#18346c] text-white">
          <div className="text-xs font-bold tracking-[.2em] text-cyan-300">TRẢI NGHIỆM SAU CHUYẾN ĐI</div>
          <h1 className="text-2xl sm:text-3xl font-extrabold mt-2">Đánh giá chuyến thuê của bạn</h1>
          <p className="text-slate-300 mt-2">Chỉ hợp đồng đã hoàn tất mới có thể đánh giá. Mỗi hợp đồng được ghi nhận một đánh giá.</p>
        </div>
        <div className="p-6 sm:p-8">
          {!contract?.id && <div className="bg-amber-50 border border-amber-200 text-amber-700 rounded-2xl p-4">Hãy mở trang Theo dõi thuê xe và chọn <b>Đánh giá trải nghiệm</b> ở hợp đồng đã hoàn tất.</div>}
          {contract?.id && <div className="grid sm:grid-cols-3 gap-3 mb-6 text-sm"><Mini l="Hợp đồng" v={contract.soHopDong || `#${contract.id}`}/><Mini l="Xe" v={vehicle?.name || vehicle?.tenXe || `#${rental?.idXe || "—"}`}/><Mini l="Trạng thái" v={contract.trangThai}/></div>}
          {err && <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded-2xl mb-4">{err}</div>}
          {msg && <div className="bg-emerald-50 border border-emerald-200 text-emerald-700 p-4 rounded-2xl mb-4 flex gap-2"><CheckCircle2 size={18}/>{msg}</div>}
          {loading ? <div className="py-10 text-center text-slate-400">Đang kiểm tra đánh giá...</div> : existing ? <div className="rounded-2xl border border-emerald-200 bg-emerald-50/50 p-6 text-center"><CheckCircle2 className="mx-auto text-emerald-600" size={34}/><h2 className="font-extrabold text-[#0D1B3E] mt-3">Bạn đã đánh giá chuyến thuê này</h2><div className="flex justify-center gap-1 mt-4">{[1,2,3,4,5].map(x=><Star key={x} size={28} className={x<=stars?"fill-amber-400 text-amber-400":"text-slate-200"}/>)}</div><div className="text-2xl font-extrabold text-amber-500 mt-2">{stars}/5</div><p className="mt-4 text-slate-600 whitespace-pre-wrap">{existing.nhanXet || "Không có nhận xét."}</p></div> : contract?.trangThai === "COMPLETED" ? <>
            <div className="text-center"><div className="font-bold text-[#0D1B3E]">Bạn hài lòng với trải nghiệm đến mức nào?</div><div className="flex justify-center gap-2 mt-4">{[1,2,3,4,5].map(x=><button key={x} onClick={()=>setRating(x)} className="p-1 transition hover:scale-110" aria-label={`${x} sao`}><Star size={36} className={x<=rating?"fill-amber-400 text-amber-400":"text-slate-200"}/></button>)}</div><div className="text-sm font-bold text-amber-600 mt-2">{rating} / 5 sao</div></div>
            <label className="block mt-7"><span className="text-sm font-bold text-[#0D1B3E] flex items-center gap-2"><MessageSquareText size={17}/> Chia sẻ trải nghiệm</span><textarea value={text} maxLength={2000} onChange={e=>setText(e.target.value)} rows="6" placeholder="Ví dụ: tình trạng xe, độ sạch sẽ, quá trình nhận/trả xe, chất lượng phục vụ..." className="mt-2 w-full border border-slate-200 rounded-2xl p-4 outline-none focus:border-cyan-400 resize-none"/><div className="text-right text-xs text-slate-400 mt-1">{text.length}/2000</div></label>
            <button disabled={submitting} onClick={submit} className="mt-5 w-full bg-[#0D1B3E] disabled:bg-slate-300 text-white py-3.5 rounded-2xl font-bold">{submitting?"Đang gửi...":"Gửi đánh giá"}</button>
          </> : contract?.id ? <div className="bg-amber-50 border border-amber-200 text-amber-700 rounded-2xl p-4">Hợp đồng hiện là <b>{contract.trangThai}</b>. Theo BA, chỉ hợp đồng <b>COMPLETED</b> mới được đánh giá.</div> : null}
        </div>
      </div>
    </div>
  </div>;
}
function Mini({l,v}){return <div className="bg-slate-50 rounded-xl p-3"><div className="text-xs text-slate-400">{l}</div><div className="font-bold text-[#0D1B3E] mt-1">{v||"—"}</div></div>}
