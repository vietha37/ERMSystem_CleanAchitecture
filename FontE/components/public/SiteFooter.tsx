import Link from "next/link";
import { footerLinks } from "@/content/hospitalContent";

export function SiteFooter() {
  return (
    <footer className="mt-24 bg-slate-950 text-slate-200">
      <div className="mx-auto grid max-w-7xl gap-12 px-4 py-16 md:grid-cols-[1.2fr_repeat(3,0.8fr)] md:px-6">
        <div className="max-w-md">
          <p className="font-serif text-3xl text-white">Bệnh viện tư ERM</p>
          <p className="mt-4 text-sm leading-7 text-slate-400">
            Mô hình bệnh viện tư định hướng gia đình, kết nối đa chuyên khoa, cận lâm sàng và chăm sóc sau khám
            trên cùng một hành trình số hóa.
          </p>
          <div className="mt-6 space-y-2 text-sm text-slate-300">
            <p>Địa chỉ: 42 Nguyễn Văn Huyên, Cầu Giấy, Hà Nội</p>
            <p>Tổng đài: 1900 565 656</p>
            <p>Email: concierge@ermhospital.vn</p>
          </div>
        </div>

        <FooterColumn title="Dịch vụ" items={footerLinks.services} />
        <FooterColumn title="Hỗ trợ" items={footerLinks.support} />
        <FooterColumn title="Hệ thống" items={footerLinks.company} />
      </div>

      <div className="border-t border-white/10">
        <div className="mx-auto flex max-w-7xl flex-col gap-3 px-4 py-5 text-sm text-slate-400 md:flex-row md:items-center md:justify-between md:px-6">
          <p>© 2026 Bệnh viện tư ERM. Thiết kế cho vận hành bệnh viện tư hiện đại.</p>
          <div className="flex flex-wrap gap-4">
            <Link href="/booking" className="transition hover:text-cyan-200">
              Đặt lịch trực tuyến
            </Link>
            <Link href="/services" className="transition hover:text-cyan-200">
              Danh mục dịch vụ
            </Link>
            <Link href="/login" className="transition hover:text-cyan-200">
              Cổng nội bộ
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}

function FooterColumn({ title, items }: { title: string; items: string[] }) {
  return (
    <div>
      <h3 className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-200">{title}</h3>
      <ul className="mt-5 space-y-3 text-sm text-slate-400">
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </div>
  );
}
