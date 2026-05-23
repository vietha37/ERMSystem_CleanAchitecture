import Link from "next/link";

const navItems = [
  { label: "Trang chủ", href: "/" },
  { label: "Dịch vụ", href: "/services" },
  { label: "Chuyên khoa", href: "/specialties" },
  { label: "Bác sĩ", href: "/doctors" },
  { label: "Đặt lịch", href: "/booking" },
  { label: "Tin tức", href: "/news" },
];

export function SiteHeader() {
  return (
    <header className="border-b border-white/10 bg-slate-950 text-white">
      <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-3 px-4 py-3 text-sm md:px-6">
        <div className="flex flex-wrap items-center gap-3 text-slate-300">
          <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-3 py-1 text-cyan-100">
            Tổng đài ưu tiên 1900 565 656
          </span>
          <span>Phục vụ 24/7 cho đặt lịch, xét nghiệm tại nhà và hỗ trợ sau khám.</span>
        </div>
        <Link
          href="/login"
          className="rounded-full border border-white/15 px-4 py-2 font-medium text-white transition hover:border-cyan-300 hover:text-cyan-200"
        >
          Đăng nhập cổng nội bộ
        </Link>
      </div>

      <div className="mx-auto flex max-w-7xl flex-col gap-5 px-4 py-5 md:px-6 lg:flex-row lg:items-center lg:justify-between">
        <Link href="/" className="flex items-center gap-4">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-cyan-400 to-emerald-400 text-lg font-bold text-slate-950 shadow-[0_18px_45px_rgba(6,182,212,0.28)]">
            EH
          </div>
          <div>
            <p className="font-serif text-3xl leading-none tracking-tight text-white">Bệnh viện tư ERM</p>
            <p className="mt-1 text-sm uppercase tracking-[0.24em] text-cyan-200">
              Chăm sóc dự phòng, chẩn đoán và y học gia đình
            </p>
          </div>
        </Link>

        <nav className="flex flex-wrap gap-2 text-sm font-medium text-slate-200">
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="rounded-full px-4 py-2 transition hover:bg-white/8 hover:text-white"
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </div>
    </header>
  );
}
