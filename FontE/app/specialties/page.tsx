import Link from "next/link";
import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { specialties } from "@/content/hospitalContent";

export default function SpecialtiesPage() {
  return (
    <PublicPageShell>
      <section className="bg-white">
        <div className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
          <SectionHeading
            eyebrow="Hệ chuyên khoa"
            title="Chọn chuyên khoa theo triệu chứng và mục tiêu khám."
            description="Nội dung tĩnh giúp người bệnh tự định hướng trước khi đặt lịch, không phụ thuộc dữ liệu database."
          />
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
        <div className="grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
          {specialties.map((specialty) => (
            <article key={specialty.title} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
              <p className="text-sm font-semibold uppercase tracking-[0.18em] text-cyan-700">Chuyên khoa</p>
              <h2 className="mt-3 text-3xl font-bold tracking-tight text-slate-950">{specialty.title}</h2>
              <p className="mt-4 text-sm font-semibold leading-7 text-slate-800">{specialty.lead}</p>
              <p className="mt-3 text-sm leading-7 text-slate-600">{specialty.description}</p>

              <div className="mt-5">
                <p className="text-sm font-semibold text-slate-950">Nên đặt lịch khi có</p>
                <div className="mt-3 flex flex-wrap gap-2">
                  {specialty.symptoms.map((symptom) => (
                    <span key={symptom} className="rounded-full border border-slate-200 px-3 py-1.5 text-sm text-slate-700">
                      {symptom}
                    </span>
                  ))}
                </div>
              </div>

              <Link
                href="/booking"
                className="mt-6 inline-flex h-11 items-center justify-center rounded-full bg-slate-950 px-5 text-sm font-semibold text-white transition hover:bg-cyan-700"
              >
                Đặt lịch chuyên khoa
              </Link>
            </article>
          ))}
        </div>
      </section>
    </PublicPageShell>
  );
}
