import { useEffect, useRef, useState } from "react";
import { Bell, Trash2, X, CheckCircle2 } from "lucide-react";

const safeParse = (raw, fallback) => {
  try { return raw ? JSON.parse(raw) : fallback; } catch { return fallback; }
};

export default function LiveNotificationBell({
  storageKey,
  poll,
  intervalMs = 4000,
  onOpenNotification,
  onData,
  className = "",
}) {
  const itemsKey = `carrent.notifications.${storageKey}`;
  const dismissedKey = `carrent.notifications.dismissed.${storageKey}`;
  const seenKey = `carrent.notifications.seen.${storageKey}`;
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState(() => safeParse(sessionStorage.getItem(itemsKey), []));
  const [toast, setToast] = useState(null);
  const running = useRef(false);
  const initialized = useRef(false);
  const lastDataSignature = useRef(null);

  const persist = next => {
    setItems(next);
    sessionStorage.setItem(itemsKey, JSON.stringify(next));
  };

  const runPoll = async () => {
    if (running.current) return;
    running.current = true;
    try {
      const result = await poll();
      if (!result) return;
      if (result.data !== undefined) {
        let signature = null;
        try { signature = JSON.stringify(result.data); } catch {}
        if (signature === null || signature !== lastDataSignature.current) {
          lastDataSignature.current = signature;
          onData?.(result.data);
        }
      }
      const incoming = Array.isArray(result.notifications) ? result.notifications : [];
      const dismissed = new Set(safeParse(sessionStorage.getItem(dismissedKey), []));
      const current = safeParse(sessionStorage.getItem(itemsKey), []);
      const seen = new Set(safeParse(sessionStorage.getItem(seenKey), []));
      // First successful poll is the baseline: do not spam old historical events.
      if (!initialized.current && seen.size === 0 && current.length === 0) {
        incoming.forEach(x => seen.add(String(x.id)));
        sessionStorage.setItem(seenKey, JSON.stringify([...seen].slice(-1000)));
        initialized.current = true;
        return;
      }
      const fresh = incoming.filter(x => !seen.has(String(x.id)) && !dismissed.has(String(x.id)));
      incoming.forEach(x => seen.add(String(x.id)));
      sessionStorage.setItem(seenKey, JSON.stringify([...seen].slice(-1000)));
      if (fresh.length) {
        const next = [...fresh, ...current].slice(0, 50);
        persist(next);
        setToast(fresh[0]);
        window.setTimeout(() => setToast(null), 4200);
      }
      initialized.current = true;
    } catch (e) {
      console.warn("[LiveNotification] poll failed", e);
      initialized.current = true;
    } finally { running.current = false; }
  };

  useEffect(() => {
    runPoll();
    const id = window.setInterval(runPoll, intervalMs);
    const onFocus = () => runPoll();
    window.addEventListener("focus", onFocus);
    return () => { window.clearInterval(id); window.removeEventListener("focus", onFocus); };
  }, []);

  const removeOne = (id, e) => {
    e?.stopPropagation();
    const dismissed = new Set(safeParse(sessionStorage.getItem(dismissedKey), []));
    dismissed.add(String(id));
    sessionStorage.setItem(dismissedKey, JSON.stringify([...dismissed].slice(-300)));
    persist(items.filter(x => String(x.id) !== String(id)));
  };

  const clearAll = () => {
    const dismissed = new Set(safeParse(sessionStorage.getItem(dismissedKey), []));
    items.forEach(x => dismissed.add(String(x.id)));
    sessionStorage.setItem(dismissedKey, JSON.stringify([...dismissed].slice(-300)));
    persist([]);
  };

  return <div className={`relative ${className}`}>
    <button onClick={()=>setOpen(v=>!v)} className="relative w-10 h-10 border border-slate-200 bg-white rounded-xl grid place-items-center hover:bg-slate-50 hover:border-cyan-300 transition" title="Thông báo">
      <Bell size={18} className="text-gray-500"/>
      {items.length>0 && <span className="absolute -right-1.5 -top-1.5 min-w-[20px] h-5 px-1 rounded-full bg-red-500 text-white text-[10px] font-bold grid place-items-center ring-2 ring-white">{items.length>99?"99+":items.length}</span>}
    </button>

    {open && <>
      <button aria-label="Đóng thông báo" onClick={()=>setOpen(false)} className="fixed inset-0 z-[70] cursor-default" />
      <div className="absolute right-0 top-12 z-[80] w-[390px] max-w-[90vw] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl shadow-slate-900/15">
        <div className="flex items-center gap-3 px-4 py-3.5 border-b bg-gradient-to-r from-slate-50 to-cyan-50/60">
          <div className="w-9 h-9 rounded-xl bg-[#0D2148] text-white grid place-items-center"><Bell size={17}/></div>
          <div><div className="font-bold text-[#0D1B3E]">Thông báo</div><div className="text-[11px] text-slate-500">Tự động cập nhật dữ liệu mới</div></div>
          {items.length>0 && <button onClick={clearAll} className="ml-auto text-xs font-semibold text-red-500 hover:text-red-700">Xóa tất cả</button>}
        </div>
        <div className="max-h-[420px] overflow-y-auto">
          {items.length===0 ? <div className="px-5 py-10 text-center"><CheckCircle2 size={34} className="mx-auto mb-3 text-emerald-400"/><div className="font-semibold text-slate-700">Không có thông báo mới</div><div className="text-xs text-slate-400 mt-1">Hệ thống sẽ tự kiểm tra, không cần F5.</div></div> : items.map(n => <button key={n.id} onClick={()=>{ removeOne(n.id); setOpen(false); onOpenNotification?.(n); }} className="w-full text-left px-4 py-3.5 border-b last:border-b-0 hover:bg-cyan-50/50 transition flex gap-3 group">
            <div className={`mt-0.5 w-9 h-9 shrink-0 rounded-xl grid place-items-center ${n.tone==="warning"?"bg-amber-50 text-amber-600":n.tone==="success"?"bg-emerald-50 text-emerald-600":"bg-cyan-50 text-cyan-600"}`}><Bell size={16}/></div>
            <div className="min-w-0 flex-1"><div className="font-semibold text-sm text-[#0D1B3E]">{n.title}</div><div className="text-xs text-slate-500 mt-1 leading-5">{n.message}</div><div className="text-[10px] text-slate-400 mt-1.5">{n.timeLabel || "Vừa cập nhật"}</div></div>
            <span onClick={e=>removeOne(n.id,e)} title="Xóa thông báo" className="w-8 h-8 rounded-lg grid place-items-center text-slate-300 hover:text-red-500 hover:bg-red-50 opacity-60 group-hover:opacity-100"><Trash2 size={14}/></span>
          </button>)}
        </div>
      </div>
    </>}

    {toast && <div className="fixed right-6 top-20 z-[100] w-[360px] max-w-[90vw] rounded-2xl border border-cyan-200 bg-white shadow-2xl p-4 animate-[fadeIn_.2s_ease-out]">
      <div className="flex gap-3"><div className="w-10 h-10 rounded-xl bg-cyan-500 text-white grid place-items-center shrink-0"><Bell size={18}/></div><div className="flex-1"><div className="text-[10px] font-bold tracking-wider text-cyan-600">THÔNG BÁO MỚI</div><div className="font-bold text-sm text-[#0D1B3E] mt-0.5">{toast.title}</div><div className="text-xs text-slate-500 mt-1">{toast.message}</div></div><button onClick={()=>setToast(null)} className="text-slate-400 hover:text-slate-700 self-start"><X size={15}/></button></div>
    </div>}
  </div>;
}
