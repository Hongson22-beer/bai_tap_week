import { useEffect } from "react";
import { CheckCircle, XCircle, AlertCircle, X } from "lucide-react";

export default function Toast({ message, type, onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000);
    return () => clearTimeout(t);
  }, [onClose]);

  const styles = {
    success: { bg: "bg-emerald-600", icon: CheckCircle },
    error: { bg: "bg-red-600", icon: XCircle },
    warning: { bg: "bg-amber-500", icon: AlertCircle },
  }[type];
  const Icon = styles.icon;

  return (
    <div className={`fixed bottom-6 right-6 z-[9999] flex items-center gap-3 ${styles.bg} text-white px-4 py-3 rounded-xl shadow-2xl max-w-sm animate-in slide-in-from-bottom-2`}>
      <Icon size={18} className="shrink-0" />
      <p className="text-sm font-medium flex-1">{message}</p>
      <button onClick={onClose} className="opacity-70 hover:opacity-100 transition-opacity">
        <X size={16} />
      </button>
    </div>
  );
}
