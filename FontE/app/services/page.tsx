import Link from "next/link";
import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { serviceCategories } from "@/content/hospitalContent";

export default function ServicesPage() {
  return (
    <PublicPageShell>
      <section className="bg-white">
        <div className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
          <SectionHeading
            eyebrow="Dịch vụ khám bệnh"
            title="Chọn dịch vụ theo nhu cầu, thời gian và mức chi phí dự kiến."
            description="Trang này dùng dữ liệu tĩnh để người bệnh dễ hiểu trước khi đặt lịch, không phụ thuộc database vận hành."
          />
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
        <div className="grid gap-5 lg:grid-cols-2">
          {serviceCategories.map((service) => (
            <article key={service.title} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex flex-col gap-5 md:flex-row md:justify-between">
                <div>
                  <p className="text-sm font-semibold uppercase tracking-[0.18em] text-cyan-700">Dịch vụ</p>
                  <h2 className="mt-3 text-3xl font-bold tracking-tight text-slate-950">{service.title}</h2>
                  <p className="mt-4 text-sm leading-7 text-slate-600">{service.summary}</p>
                </div>
                <div className="grid min-w-48 gap-3 text-sm">
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="font-semibold text-slate-950">Thời gian</p>
                    <p className="mt-1 text-slate-600">{service.duration}</p>
                  </div>
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="font-semibold text-slate-950">Chi phí dự kiến</p>
                    <p className="mt-1 text-slate-600">{service.priceRange}</p>
                  </div>
                </div>
              </div>

              <div className="mt-5 rounded-lg border border-slate-200 bg-slate-50 p-4">
                <p className="text-sm font-semibold text-slate-950">Phù hợp với</p>
                <p className="mt-2 text-sm leading-6 text-slate-600">{service.idealFor}</p>
              </div>

              <ul className="mt-5 grid gap-3">
                {service.items.map((item) => (
                  <li key={item} className="flex gap-3 text-sm leading-6 text-slate-700">
                    <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-cyan-600" />
                    <span>{item}</span>
                  </li>
                ))}
              </ul>

              <Link
                href="/booking"
                className="mt-6 inline-flex h-11 items-center justify-center rounded-full bg-slate-950 px-5 text-sm font-semibold text-white transition hover:bg-cyan-700"
              >
                Đặt lịch dịch vụ này
              </Link>
            </article>
          ))}
        </div>
      </section>
    </PublicPageShell>
  );
}
