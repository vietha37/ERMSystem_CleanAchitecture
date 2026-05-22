import { UserRole } from "@/services/types";

export type NavigationItem = {
  name: string;
  path: string;
  shortLabel: string;
};

export const menuItemsByRole: Record<UserRole, NavigationItem[]> = {
  Admin: [
    { name: "Tổng quan", path: "/dashboard", shortLabel: "HQ" },
    { name: "Công việc bác sĩ", path: "/doctor-worklist", shortLabel: "BS" },
    { name: "Nhân sự", path: "/staff", shortLabel: "NS" },
    { name: "Bệnh nhân", path: "/patients", shortLabel: "BN" },
    { name: "Lịch hẹn", path: "/appointments", shortLabel: "LH" },
    { name: "Hồ sơ bệnh án", path: "/medical-records", shortLabel: "BA" },
    { name: "Đơn thuốc", path: "/prescriptions", shortLabel: "DT" },
    { name: "Cận lâm sàng", path: "/clinical-orders", shortLabel: "CLS" },
    { name: "Hóa đơn", path: "/billing", shortLabel: "HD" },
    { name: "Thông báo", path: "/notifications", shortLabel: "TB" },
  ],
  Doctor: [
    { name: "Tổng quan", path: "/dashboard", shortLabel: "HQ" },
    { name: "Công việc bác sĩ", path: "/doctor-worklist", shortLabel: "BS" },
    { name: "Bệnh nhân", path: "/patients", shortLabel: "BN" },
    { name: "Lịch hẹn", path: "/appointments", shortLabel: "LH" },
    { name: "Hồ sơ bệnh án", path: "/medical-records", shortLabel: "BA" },
    { name: "Đơn thuốc", path: "/prescriptions", shortLabel: "DT" },
    { name: "Cận lâm sàng", path: "/clinical-orders", shortLabel: "CLS" },
  ],
  Receptionist: [
    { name: "Tổng quan", path: "/dashboard", shortLabel: "HQ" },
    { name: "Công việc bác sĩ", path: "/doctor-worklist", shortLabel: "BS" },
    { name: "Bệnh nhân", path: "/patients", shortLabel: "BN" },
    { name: "Lịch hẹn", path: "/appointments", shortLabel: "LH" },
    { name: "Cận lâm sàng", path: "/clinical-orders", shortLabel: "CLS" },
    { name: "Hóa đơn", path: "/billing", shortLabel: "HD" },
    { name: "Thông báo", path: "/notifications", shortLabel: "TB" },
  ],
  Patient: [{ name: "Cổng thông tin bệnh nhân", path: "/portal", shortLabel: "PT" }],
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
      title: "Luồng hóa đơn",
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
      subtitle: "Điều phối tài khoản và vai trò vận hành nội bộ.",
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
      subtitle: "Không gian vận hành",
    }
  );
}
