import { UserRole } from "@/services/types";

export type NavigationIcon =
  | "dashboard"
  | "doctorWorklist"
  | "staff"
  | "security"
  | "patients"
  | "appointments"
  | "medicalRecords"
  | "prescriptions"
  | "clinicalOrders"
  | "billing"
  | "notifications"
  | "portal";

export type NavigationItem = {
  id: string;
  name: string;
  path: string;
  icon: NavigationIcon;
  i18nKey: string;
  shortLabel: string;
};

export const menuItemsByRole: Record<UserRole, NavigationItem[]> = {
  Admin: [
    { id: "dashboard", name: "Tổng quan", path: "/dashboard", icon: "dashboard", i18nKey: "nav.dashboard", shortLabel: "HQ" },
    { id: "doctorWorklist", name: "Công việc bác sĩ", path: "/doctor-worklist", icon: "doctorWorklist", i18nKey: "nav.doctorWorklist", shortLabel: "BS" },
    { id: "staff", name: "Nhân sự", path: "/staff", icon: "staff", i18nKey: "nav.staff", shortLabel: "NS" },
    { id: "security", name: "Bảo mật", path: "/security", icon: "security", i18nKey: "nav.security", shortLabel: "BM" },
    { id: "patients", name: "Bệnh nhân", path: "/patients", icon: "patients", i18nKey: "nav.patients", shortLabel: "BN" },
    { id: "appointments", name: "Lịch hẹn", path: "/appointments", icon: "appointments", i18nKey: "nav.appointments", shortLabel: "LH" },
    { id: "medicalRecords", name: "Hồ sơ bệnh án", path: "/medical-records", icon: "medicalRecords", i18nKey: "nav.medicalRecords", shortLabel: "BA" },
    { id: "prescriptions", name: "Đơn thuốc", path: "/prescriptions", icon: "prescriptions", i18nKey: "nav.prescriptions", shortLabel: "DT" },
    { id: "clinicalOrders", name: "Cận lâm sàng", path: "/clinical-orders", icon: "clinicalOrders", i18nKey: "nav.clinicalOrders", shortLabel: "CLS" },
    { id: "billing", name: "Hóa đơn & Viện phí", path: "/billing", icon: "billing", i18nKey: "nav.billing", shortLabel: "HD" },
    { id: "notifications", name: "Thông báo", path: "/notifications", icon: "notifications", i18nKey: "nav.notifications", shortLabel: "TB" },
  ],
  Doctor: [
    { id: "dashboard", name: "Tổng quan", path: "/dashboard", icon: "dashboard", i18nKey: "nav.dashboard", shortLabel: "HQ" },
    { id: "doctorWorklist", name: "Công việc bác sĩ", path: "/doctor-worklist", icon: "doctorWorklist", i18nKey: "nav.doctorWorklist", shortLabel: "BS" },
    { id: "patients", name: "Bệnh nhân", path: "/patients", icon: "patients", i18nKey: "nav.patients", shortLabel: "BN" },
    { id: "appointments", name: "Lịch hẹn", path: "/appointments", icon: "appointments", i18nKey: "nav.appointments", shortLabel: "LH" },
    { id: "medicalRecords", name: "Hồ sơ bệnh án", path: "/medical-records", icon: "medicalRecords", i18nKey: "nav.medicalRecords", shortLabel: "BA" },
    { id: "prescriptions", name: "Đơn thuốc", path: "/prescriptions", icon: "prescriptions", i18nKey: "nav.prescriptions", shortLabel: "DT" },
    { id: "clinicalOrders", name: "Cận lâm sàng", path: "/clinical-orders", icon: "clinicalOrders", i18nKey: "nav.clinicalOrders", shortLabel: "CLS" },
    { id: "security", name: "Bảo mật", path: "/security", icon: "security", i18nKey: "nav.security", shortLabel: "BM" },
  ],
  Cashier: [
    { id: "dashboard", name: "Tổng quan", path: "/dashboard", icon: "dashboard", i18nKey: "nav.dashboard", shortLabel: "HQ" },
    { id: "patients", name: "Bệnh nhân", path: "/patients", icon: "patients", i18nKey: "nav.patients", shortLabel: "BN" },
    { id: "appointments", name: "Lịch hẹn", path: "/appointments", icon: "appointments", i18nKey: "nav.appointments", shortLabel: "LH" },
    { id: "billing", name: "Hóa đơn & Viện phí", path: "/billing", icon: "billing", i18nKey: "nav.billing", shortLabel: "HD" },
    { id: "notifications", name: "Thông báo", path: "/notifications", icon: "notifications", i18nKey: "nav.notifications", shortLabel: "TB" },
    { id: "security", name: "Bảo mật", path: "/security", icon: "security", i18nKey: "nav.security", shortLabel: "BM" },
  ],
  Patient: [
    { id: "portal", name: "Cổng thông tin bệnh nhân", path: "/portal", icon: "portal", i18nKey: "nav.portal", shortLabel: "PT" },
  ],
};

export function resolvePageTitle(pathname: string): { title: string; subtitle: string } {
  const map: Array<{ match: string; title: string; subtitle: string }> = [
    {
      match: "/dashboard",
      title: "Trung tâm điều hành",
      subtitle: "Theo dõi luồng vận hành trong ngày theo vai trò.",
    },
    {
      match: "/doctor-worklist",
      title: "Công việc bác sĩ",
      subtitle: "Theo dõi lịch khám, check-in và hồ sơ khám cần xử lý.",
    },
    {
      match: "/appointments",
      title: "Điều phối lịch hẹn",
      subtitle: "Xử lý check-in, đổi lịch và hủy lịch theo chính sách.",
    },
    {
      match: "/patients",
      title: "Đăng bộ bệnh nhân",
      subtitle: "Quản lý hồ sơ hành chính và điểm tiếp nhận bệnh nhân.",
    },
    {
      match: "/medical-records",
      title: "Hồ sơ lâm sàng",
      subtitle: "Mở hồ sơ khám, cập nhật diễn biến và chốt hồ sơ.",
    },
    {
      match: "/prescriptions",
      title: "Bàn kê thuốc",
      subtitle: "Phát hành và theo dõi đơn thuốc theo hồ sơ khám.",
    },
    {
      match: "/clinical-orders",
      title: "Cận lâm sàng",
      subtitle: "Quản lý chỉ định xét nghiệm và chẩn đoán hình ảnh.",
    },
    {
      match: "/billing",
      title: "Luồng hóa đơn & Viện phí",
      subtitle: "Theo dõi phát hành hóa đơn, thu tiền và công nợ.",
    },
    {
      match: "/notifications",
      title: "Trung tâm thông báo",
      subtitle: "Giám sát luồng thông báo và kết quả gửi ra ngoài.",
    },
    {
      match: "/staff",
      title: "Vận hành nhân sự",
      subtitle: "Điều phối tài khoản, quyền hạn và đồng bộ danh bạ bệnh viện.",
    },
    {
      match: "/security",
      title: "An toàn & Bảo mật",
      subtitle: "Cấu hình xác thực hai yếu tố và quản lý an ninh hệ thống.",
    },
    {
      match: "/portal",
      title: "Cổng bệnh nhân",
      subtitle: "Theo dõi lịch sử khám, đơn thuốc và hóa đơn dành cho bệnh nhân.",
    },
  ];

  return (
    map.find((item) => pathname === item.match || pathname.startsWith(`${item.match}/`)) ?? {
      title: "ERM Hospital",
      subtitle: "Hệ thống Quản lý Bệnh viện",
    }
  );
}
