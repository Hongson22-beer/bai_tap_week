import { useState } from "react";
import { authApi } from "./services/api";
import LandingPage from "./pages/customer/LandingPage";
import BrowseCarsPage from "./pages/customer/BrowseCarsPage";
import CarDetailPage from "./pages/customer/CarDetailPage";
import CheckoutPage from "./pages/customer/CheckoutPage";
import TrackingPage from "./pages/customer/TrackingPage";
import PaymentPage from "./pages/customer/PaymentPage";
import RentalHistoryPage from "./pages/customer/RentalHistoryPage";
import LoginPage from "./pages/auth/LoginPage";
import StaffDashboard from "./pages/staff/StaffDashboard";
import AdminDashboard from "./pages/admin/AdminDashboard";
import Toast from "./components/shared/Toast";
import ContractPage from "./pages/customer/ContractPage";
import RegisterPage from "./pages/auth/RegisterPage";
import ExtendRentalPage from "./pages/customer/ExtendRentalPage";
import CancelContractPage from "./pages/customer/CancelContractPage";
import HandoverTrackingPage from "./pages/customer/HandoverTrackingPage";
import ReviewPage from "./pages/customer/ReviewPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import ProfilePage from "./pages/customer/ProfilePage";
import LiveNotificationBell from "./components/shared/LiveNotificationBell";
import { pollCustomerFlow } from "./services/liveNotifications";

export default function App() {
  const [currentUser, setCurrentUser] = useState(() => authApi.currentUser());
  const [page, setPage] = useState(() => {
    const user = authApi.currentUser();
    const saved = sessionStorage.getItem("carrent_current_page");
    if (user?.role === "admin") return saved === "admin" ? "admin" : "admin";
    if (user?.role === "staff") return saved === "staff" ? "staff" : "staff";
    const customerSafePages = ["landing", "browse", "tracking", "history", "contract", "cancel", "handover-tracking", "profile"];
    return user?.role === "customer" && customerSafePages.includes(saved) ? saved : "landing";
  });
  const [pageData, setPageData] = useState(null);
  const [toast, setToast] = useState(null);

  const showToast = (message, type = "success") => {
    setToast({ message, type });
  };

  const navigate = (target, data) => {
    setPageData(data || null);
    setPage(target);
    const safePages = ["landing", "browse", "tracking", "history", "contract", "cancel", "handover-tracking", "profile", "staff", "admin", "login"];
    if (safePages.includes(target)) sessionStorage.setItem("carrent_current_page", target);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const handleLogin = (user) => {
    setCurrentUser(user);
    showToast(`Đăng nhập thành công! Chào ${user.name || user.email}`, "success");
    if (user.role === "admin") navigate("admin");
    else if (user.role === "staff") navigate("staff");
    else navigate("landing");
  };

  const handleLogout = () => {
    const role = currentUser?.role;
    authApi.logout();
    sessionStorage.removeItem("carrent_current_page");
    setCurrentUser(null);
    showToast("Đã đăng xuất thành công", "success");
    // Customer returns to the public homepage; staff/admin return to Login.
    navigate(role === "staff" || role === "admin" ? "login" : "landing");
  };

  // Register page (inline)
  if (page === "register") {
    return <RegisterPage onLogin={handleLogin} onNavigate={navigate} toast={toast} onCloseToast={() => setToast(null)} showToast={showToast} />;
  }

  if (page === "forgot-password") {
    return <ForgotPasswordPage onNavigate={navigate} />;
  }

  if (page === "profile") {
    return <ProfilePage onNavigate={navigate} currentUser={currentUser} onLogout={handleLogout} />;
  }

  return (
    <>
      {page === "login" && (
        <LoginPage onLogin={handleLogin} onNavigate={navigate} />
      )}

      {page === "landing" && (
        <LandingPage onNavigate={navigate} currentUser={currentUser} onLogout={handleLogout} />
      )}

      {page === "browse" && (
        <BrowseCarsPage onNavigate={navigate} />
      )}

      {page === "car-detail" && pageData && (
        <CarDetailPage car={pageData} onNavigate={navigate} />
      )}

      {page === "checkout" && pageData && currentUser?.role === "customer" && (
        <CheckoutPage car={pageData} onNavigate={navigate} />
      )}
      {page === "checkout" && (!pageData || currentUser?.role !== "customer") && (
        <LandingPage onNavigate={navigate} currentUser={currentUser} onLogout={handleLogout} />
      )}

      {page === "tracking" && (
        <TrackingPage onNavigate={navigate} initialData={pageData} />
      )}

      {page === "payment" && (
        <PaymentPage onNavigate={navigate} initialData={pageData} />
      )}

      {page === "history" && (
        <RentalHistoryPage onNavigate={navigate} />
      )}

      {page === "contract" && (
        <ContractPage onNavigate={navigate} />
      )}

      {page === "extend" && (
        <ExtendRentalPage onNavigate={navigate} initialData={pageData} />
      )}

      {page === "cancel" && (
        <CancelContractPage onNavigate={navigate} />
      )}

      {page === "handover-tracking" && (
        <HandoverTrackingPage onNavigate={navigate} />
      )}

      {page === "review" && (
        <ReviewPage onNavigate={navigate} initialData={pageData} />
      )}

      {page === "staff" && currentUser?.role === "staff" && (
        <StaffDashboard staffName={currentUser?.name || "Lê Minh Tuấn"} onNavigate={navigate} onLogout={handleLogout} />
      )}

      {page === "admin" && currentUser?.role === "admin" && (
        <AdminDashboard adminName={currentUser?.name || "Trần Thanh Admin"} onNavigate={navigate} onLogout={handleLogout} />
      )}

      {currentUser?.role === "customer" && !["login","register","forgot-password"].includes(page) && <div className="fixed top-4 right-5 z-[65]"><LiveNotificationBell
        storageKey={`customer-all-flow-${currentUser?.id || currentUser?.idNguoiDung || currentUser?.email || "me"}`}
        intervalMs={3000}
        poll={pollCustomerFlow}
        onData={data=>window.dispatchEvent(new CustomEvent("carrent:remote-update",{detail:data}))}
        onOpenNotification={n=>navigate(n.target||"tracking")}
      /></div>}

      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}
    </>
  );
}
