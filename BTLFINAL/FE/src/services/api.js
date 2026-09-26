const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:5101/api";

// Authentication is TAB-SCOPED on purpose.
// sessionStorage gives each browser tab its own JWT/user session, which makes
// CUSTOMER / NHAN_VIEN / ADMIN usable side-by-side during demo and testing.
const AUTH_KEYS = ["access_token", "current_user"];

// Remove legacy shared auth once this build is loaded. Other localStorage data
// is intentionally untouched.
AUTH_KEYS.forEach(k => localStorage.removeItem(k));

const getStoredUser = () => {
  try {
    return JSON.parse(sessionStorage.getItem("current_user") || "null");
  } catch {
    return null;
  }
};

const getToken = () =>
  sessionStorage.getItem("access_token") ||
  getStoredUser()?.token ||
  null;

async function request(path, options = {}) {
  const token = getToken();
  const headers = {
    ...(options.body ? { "Content-Type": "application/json" } : {}),
    ...(options.headers || {}),
  };
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }

  if (!response.ok) {
    const error = new Error(data?.message || data?.error || (typeof data === "string" ? data : null) || `HTTP ${response.status}`);
    error.status = response.status;
    error.data = data;
    throw error;
  }
  return data;
}

async function upload(path, file) {
  const token = getToken();
  const form = new FormData(); form.append("file", file);
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  const response = await fetch(`${API_BASE}${path}`, { method: "POST", headers, body: form });
  const text = await response.text(); let data = null; try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!response.ok) { const e = new Error(data?.message || `HTTP ${response.status}`); e.status=response.status; throw e; }
  return data;
}

function decodeJwt(token) {
  try {
    return JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
  } catch {
    return {};
  }
}

function pickToken(data) {
  return data?.token || data?.accessToken || data?.access_token || data?.jwtToken || data?.jwt;
}

function rolesFrom(payload, data) {
  const value = payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || data?.role || data?.roles || [];
  return (Array.isArray(value) ? value : [value]).filter(Boolean).map(String);
}

function appRole(roles) {
  return roles.includes("ADMIN") ? "admin" : roles.includes("NHAN_VIEN") ? "staff" : "customer";
}

export const authApi = {
  async login(email, matKhau) {
    const data = await request("/Auth/login", { method: "POST", body: JSON.stringify({ email, matKhau }) });
    const token = pickToken(data);
    if (!token) throw new Error("Backend không trả JWT token.");

    AUTH_KEYS.forEach(k => sessionStorage.removeItem(k));

    const payload = decodeJwt(token);
    const roles = rolesFrom(payload, data);
    const user = {
      id: Number(payload.sub || payload.nameid || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || data?.id || 0),
      name: data?.hoTen || data?.name || payload.unique_name || payload.name || email,
      email,
      roles,
      role: appRole(roles),
      token,
    };

    sessionStorage.setItem("access_token", token);
    sessionStorage.setItem("current_user", JSON.stringify(user));
    return user;
  },
  logout() {
    AUTH_KEYS.forEach(k => sessionStorage.removeItem(k));
  },
  currentUser: getStoredUser,
};

export const arr = value => Array.isArray(value) ? value : (value?.items || value?.data || value?.results || []);

export const mapVehicle = v => ({
  ...v,
  id: v.id ?? v.idXe,
  idHangXe: v.idHangXe ?? v.hangXeId,
  idLoaiXe: v.idLoaiXe ?? v.loaiXeId,
  name: v.tenXe || v.ten || v.name || `${v.hangXe || v.tenHang || "Xe"} ${v.loaiXe || v.dongXe || ""}`.trim(),
  brand: v.hangXe?.tenHang || v.tenHang || v.hangXe || v.brand || "Khác",
  type: v.loaiXe?.tenLoai || v.tenLoai || v.loaiXe || v.type || "Ô tô",
  seats: v.soChoNgoi ?? v.soCho ?? v.seats ?? 5,
  transmission: v.hopSo || v.truyenDong || v.transmission || "Tự động",
  fuel: v.nhienLieu || v.loaiNhienLieu || v.fuel || "Xăng",
  pricePerDay: Number(v.giaThueNgay ?? v.donGiaNgay ?? v.giaThue ?? v.pricePerDay ?? 0),
  status: v.trangThai || v.status || "AVAILABLE",
  plate: v.bienSoXe || v.plate || "",
  image: (() => { const raw=v.hinhAnh || v.anhXe || v.image; return raw ? (String(raw).startsWith("http") ? raw : `${API_BASE.replace(/\/api$/, "")}${raw}`) : "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&h=500&fit=crop&auto=format"; })(),
  namSanXuat: v.namSanXuat ?? v.year ?? null,
  mauXe: v.mauXe || v.color || "",
});

export const vehiclesApi = {
  async list() { return arr(await request("/Vehicles")).map(mapVehicle); },
  async available(q = "") { return arr(await request(`/Vehicles/available${q ? `?${q}` : ""}`)).map(mapVehicle); },
  get: id => request(`/Vehicles/${id}`),
  history: id => request(`/Vehicles/${id}/history`),
  setStatus: (id, trangThai, ghiChu = "") => request(`/Vehicles/${id}/status`, { method: "PATCH", body: JSON.stringify({ trangThai, ghiChu }) }),
  create: body => request("/Vehicles", { method: "POST", body: JSON.stringify(body) }),
  update: (id, body) => request(`/Vehicles/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  uploadImage: (id, file) => upload(`/Vehicles/${id}/image`, file),
};

export const categoriesApi = {
  brands: () => request("/Categories/brands"),
  types: () => request("/Categories/types"),
};

export const customersApi = {
  me: () => request("/Customers/me"),
  updateMe: body => request("/Customers/me", { method: "PUT", body: JSON.stringify(body) }),
  uploadDocument: (type, file) => upload(`/Customers/me/documents/${type}`, file),
  list: () => request("/Customers"),
  documentBlob: async (id, type) => {
    const token = getToken();
    const headers = token ? { Authorization: `Bearer ${token}` } : {};
    const response = await fetch(`${API_BASE}/Customers/${id}/documents/${type}`, { headers });
    if (!response.ok) {
      let message = `HTTP ${response.status}`;
      try { const d = await response.json(); message = d?.message || message; } catch {}
      const e = new Error(message); e.status = response.status; throw e;
    }
    return response.blob();
  },
  verify: (id, daXacMinh) => request(`/Customers/${id}/verify-cccd`, { method: "PATCH", body: JSON.stringify({ daXacMinh }) }),
  verifyDocuments: (id, cccdDaXacMinh, gplxDaXacMinh) => request(`/Customers/${id}/verify-documents`, { method: "PATCH", body: JSON.stringify({ cccdDaXacMinh, gplxDaXacMinh }) }),
};

export const rentalsApi = {
  createOnline: body => request("/rentals/ONLINE", { method: "POST", body: JSON.stringify(body) }),
  createOffline: body => request("/rentals/offline", { method: "POST", body: JSON.stringify(body) }),
  checkAvailability: (idXe, thoiGianNhan, thoiGianTraDuKien) => request(`/rentals/availability?idXe=${encodeURIComponent(idXe)}&thoiGianNhan=${encodeURIComponent(thoiGianNhan)}&thoiGianTraDuKien=${encodeURIComponent(thoiGianTraDuKien)}`),
  bookedPeriods: (idXe, from = null, to = null) => {
    const q = new URLSearchParams();
    if (from) q.set("from", from);
    if (to) q.set("to", to);
    return request(`/rentals/vehicle/${encodeURIComponent(idXe)}/booked-periods${q.toString() ? `?${q.toString()}` : ""}`);
  },
  list: (q = "") => request(`/rentals${q ? `?${q}` : ""}`),
  get: id => request(`/rentals/${id}`),
  approve: id => request(`/rentals/${id}/approve`, { method: "PUT" }),
  reject: (id, lyDoTuChoi) => request(`/rentals/${id}/reject`, { method: "PUT", body: JSON.stringify({ lyDoTuChoi }) }),
  cancel: (id, lyDoHuy) => request(`/rentals/${id}/cancel`, { method: "PUT", body: JSON.stringify({ lyDoHuy }) }),
  async discoverMine() {
    return arr(await request("/rentals/my"));
  },
};

export const contractsApi = {
  list: () => request("/contracts"),
  create: body => request("/contracts", { method: "POST", body: JSON.stringify(body) }),
  get: id => request(`/contracts/${id}`),
  send: id => request(`/contracts/${id}/send`, { method: "PUT" }),
  confirm: id => request(`/contracts/${id}/confirm`, { method: "PUT" }),
  reject: (id, lyDo) => request(`/contracts/${id}/reject`, { method: "PUT", body: JSON.stringify({ lyDo }) }),
  history: id => request(`/contracts/${id}/history`),
  async discover() {
    return arr(await request("/contracts"));
  },
};

export const paymentsApi = {
  create: body => request("/Payments", { method: "POST", body: JSON.stringify(body) }),
  get: id => request(`/Payments/${id}`),
  byContract: id => request(`/Payments/contract/${id}`),
  refund: id => request(`/Payments/${id}/refund`, { method: "POST" }),
  status: id => request(`/Payments/${id}/status`),
  customerPaid: id => request(`/Payments/${id}/customer-paid`, { method: "POST" }),
  simulateBankReceived: id => request(`/Payments/${id}/simulate-bank-received`, { method: "POST" }),
  verify: (id, confirmed) => request(`/Payments/${id}/verify`, { method: "POST", body: JSON.stringify({ confirmed }) }),
};

export const extensionsApi = {
  create: body => request("/Extensions", { method: "POST", body: JSON.stringify(body) }),
  get: id => request(`/Extensions/${id}`),
  byContract: id => request(`/Extensions/contract/${id}`),
  myByContract: id => request(`/Extensions/my/contract/${id}`),
  process: (id, body) => request(`/Extensions/${id}/process`, { method: "PUT", body: JSON.stringify(body) }),
};

export const cancellationsApi = {
  create: body => request("/Cancellations", { method: "POST", body: JSON.stringify(body) }),
  get: id => request(`/Cancellations/${id}`),
  byContract: id => request(`/Cancellations/contract/${id}`),
  myByContract: id => request(`/Cancellations/my/contract/${id}`),
  process: (id, body) => request(`/Cancellations/${id}/process`, { method: "PUT", body: JSON.stringify(body) }),
};

export const handoversApi = {
  create: body => request("/Handovers", { method: "POST", body: JSON.stringify(body) }),
  get: id => request(`/Handovers/${id}`),
  byContract: id => request(`/Handovers/contract/${id}`),
  confirmReceived: id => request(`/Handovers/contract/${id}/confirm-received`, { method: "POST" }),
};

export const returnsApi = {
  create: body => request("/Returns", { method: "POST", body: JSON.stringify(body) }),
  get: id => request(`/Returns/${id}`),
  byContract: id => request(`/Returns/contract/${id}`),
  requestReturn: (idHopDong, ghiChu = "") => request(`/Returns/contract/${idHopDong}/request`, { method: "POST", body: JSON.stringify({ ghiChu }) }),
  requestByContract: idHopDong => request(`/Returns/contract/${idHopDong}/request`),
};

export const evaluationsApi = {
  create: body => request("/Evaluations", { method: "POST", body: JSON.stringify(body) }),
  list: () => request("/Evaluations"),
  get: id => request(`/Evaluations/${id}`),
  byContract: id => request(`/Evaluations/contract/${id}`),
  myByContract: id => request(`/Evaluations/my/contract/${id}`),
  byVehicle: id => request(`/Evaluations/vehicle/${id}`),
  setVisibility: (id, hienThi) => request(`/Evaluations/${id}/visibility`, { method: "PATCH", body: JSON.stringify({ hienThi }) }),
};

export const adminApi = {
  reports: () => request("/Reports/summary"),
  users: () => request("/admin/users"),
  audits: () => request("/AuditLogs"),
  setStatus: (id, dangHoatDong) => request(`/admin/users/${id}/status`, { method: "PATCH", body: JSON.stringify({ dangHoatDong }) }),
  setRoles: (id, roles) => request(`/admin/users/${id}/roles`, { method: "PUT", body: JSON.stringify({ roles }) }),
};

export { request, API_BASE };
