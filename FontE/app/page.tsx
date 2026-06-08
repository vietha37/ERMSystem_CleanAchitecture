import Link from "next/link";
import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import {
  hospitalStats,
  newsArticles,
  patientJourney,
  quickActions,
  serviceCategories,
  specialties,
  trustPoints,
} from "@/content/hospitalContent";

export default function HomePage() {
  const featuredServices = serviceCategories.slice(0, 3);
  const featuredSpecialties = specialties.slice(0, 4);

  return (
    <PublicPageShell>
      <section className="bg-white">
        <div className="mx-auto grid max-w-7xl gap-10 px-4 py-10 md:px-6 lg:grid-cols-[0.95fr_1.05fr] lg:items-center lg:py-16">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.24em] text-cyan-700">
              Bệnh viện tư đa chuyên khoa
            </p>
            <h1 className="mt-5 max-w-3xl text-4xl font-bold leading-tight tracking-tight text-slate-950 md:text-6xl">
              Đặt lịch khám, theo dõi hồ sơ và nhận kết quả trên một hành trình rõ ràng.
            </h1>
            <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-600">
              ERM Hospital tập trung vào khám đúng chuyên khoa, giảm thời gian chờ và lưu toàn bộ lịch khám,
              kết quả cận lâm sàng, đơn thuốc, hóa đơn trong cổng bệnh nhân.
            </p>

            <div className="mt-8 flex flex-wrap gap-3">
              <Link
                href="/booking"
                className="inline-flex h-12 items-center justify-center rounded-full bg-cyan-700 px-6 text-sm font-semibold text-white transition hover:bg-cyan-800"
              >
                Đặt lịch khám
              </Link>
              <Link
                href="/specialties"
                className="inline-flex h-12 items-center justify-center rounded-full border border-slate-300 bg-white px-6 text-sm font-semibold text-slate-900 transition hover:border-cyan-600 hover:text-cyan-700"
              >
                Tìm chuyên khoa phù hợp
              </Link>
            </div>

            <div className="mt-10 grid gap-3 sm:grid-cols-2">
              {trustPoints.map((item) => (
                <div key={item} className="flex gap-3 rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm leading-6 text-slate-700">
                  <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-cyan-600" />
                  <span>{item}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-slate-100 shadow-sm">
            <div
              role="img"
              aria-label="Khu vực tiếp đón bệnh viện hiện đại"
              className="h-[360px] w-full bg-cover bg-center md:h-[520px]"
              style={{
                backgroundImage:
                  "url('https://images.unsplash.com/photo-1631815588090-d4bfec5b1ccb?auto=format&fit=crop&w=1200&q=80')",
              }}
            />
            <div className="grid gap-4 bg-white p-5 sm:grid-cols-2">
              <div>
                <p className="text-sm font-semibold text-slate-950">Hotline đặt lịch</p>
                <p className="mt-1 text-2xl font-bold text-cyan-700">1900 565 656</p>
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-950">Giờ tiếp nhận</p>
                <p className="mt-1 text-sm leading-6 text-slate-600">Thứ 2 - Chủ nhật, 7:00 - 20:00</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="border-y border-slate-200 bg-white">
        <div className="mx-auto grid max-w-7xl gap-4 px-4 py-6 md:grid-cols-4 md:px-6">
          {hospitalStats.map((item) => (
            <div key={item.label} className="border-l border-slate-200 pl-4 first:border-l-0">
              <p className="text-3xl font-bold tracking-tight text-slate-950">{item.value}</p>
              <p className="mt-1 text-sm leading-6 text-slate-600">{item.label}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
        <SectionHeading
          eyebrow="Cần làm gì hôm nay?"
          title="Các thao tác quan trọng được đưa lên đầu trang."
          description="Người bệnh có thể bắt đầu bằng đặt lịch, tìm chuyên khoa, xem dịch vụ hoặc mở cổng bệnh nhân."
        />
        <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {quickActions.map((action) => (
            <Link
              key={action.title}
              href={action.href}
              className="group rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-cyan-200 hover:shadow-md"
            >
              <div className={`h-1.5 w-16 rounded-full ${action.accent}`} />
              <h3 className="mt-5 text-xl font-bold tracking-tight text-slate-950">{action.title}</h3>
              <p className="mt-3 text-sm leading-7 text-slate-600">{action.description}</p>
              <span className="mt-5 inline-flex text-sm font-semibold text-cyan-700 transition group-hover:translate-x-1">
                Xem chi tiết
              </span>
            </Link>
          ))}
        </div>
      </section>

      <section className="bg-white">
        <div className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
          <SectionHeading
            eyebrow="Dịch vụ nổi bật"
            title="Thông tin dịch vụ được viết theo nhu cầu khám thật."
            description="Mỗi dịch vụ có đối tượng phù hợp, thời gian dự kiến và khoảng chi phí để người bệnh dễ ra quyết định."
          />
          <div className="mt-8 grid gap-5 lg:grid-cols-3">
            {featuredServices.map((service) => (
              <article key={service.title} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
                <h3 className="text-2xl font-bold tracking-tight text-slate-950">{service.title}</h3>
                <p className="mt-3 text-sm leading-7 text-slate-600">{service.summary}</p>
                <dl className="mt-5 grid gap-3 text-sm">
                  <div>
                    <dt className="font-semibold text-slate-950">Phù hợp với</dt>
                    <dd className="mt-1 leading-6 text-slate-600">{service.idealFor}</dd>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="rounded-lg bg-slate-50 p-3">
                      <dt className="font-semibold text-slate-950">Thời gian</dt>
                      <dd className="mt-1 text-slate-600">{service.duration}</dd>
                    </div>
                    <div className="rounded-lg bg-slate-50 p-3">
                      <dt className="font-semibold text-slate-950">Chi phí</dt>
                      <dd className="mt-1 text-slate-600">{service.priceRange}</dd>
                    </div>
                  </div>
                </dl>
              </article>
            ))}
          </div>
          <div className="mt-8">
            <Link href="/services" className="text-sm font-semibold text-cyan-700 hover:text-cyan-900">
              Xem toàn bộ dịch vụ
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
        <SectionHeading
          eyebrow="Chọn đúng chuyên khoa"
          title="Bắt đầu từ triệu chứng thường gặp."
          description="Nếu chưa biết nên khám ở đâu, người bệnh có thể xem nhóm triệu chứng và chọn chuyên khoa phù hợp."
        />
        <div className="mt-8 grid gap-5 md:grid-cols-2">
          {featuredSpecialties.map((specialty) => (
            <article key={specialty.title} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
              <p className="text-sm font-semibold uppercase tracking-[0.18em] text-cyan-700">Chuyên khoa</p>
              <h3 className="mt-3 text-2xl font-bold tracking-tight text-slate-950">{specialty.title}</h3>
              <p className="mt-3 text-sm leading-7 text-slate-600">{specialty.lead}</p>
              <div className="mt-5 flex flex-wrap gap-2">
                {specialty.symptoms.map((symptom) => (
                  <span key={symptom} className="rounded-full border border-slate-200 px-3 py-1.5 text-sm text-slate-700">
                    {symptom}
                  </span>
                ))}
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="bg-slate-950 text-white">
        <div className="mx-auto grid max-w-7xl gap-8 px-4 py-12 md:px-6 md:py-16 lg:grid-cols-[0.85fr_1.15fr]">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.24em] text-cyan-200">Quy trình đi khám</p>
            <h2 className="mt-4 text-3xl font-bold tracking-tight md:text-5xl">
              Từ đặt lịch đến nhận kết quả, mỗi bước đều có người điều phối.
            </h2>
            <p className="mt-4 text-base leading-7 text-slate-300">
              Quy trình này giúp giảm thời gian chờ, hạn chế nhập lại thông tin và giữ dữ liệu khám bệnh liền mạch.
            </p>
          </div>
          <div className="grid gap-3">
            {patientJourney.map((step, index) => (
              <div key={step} className="rounded-xl border border-white/10 bg-white/5 p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-200">Bước {index + 1}</p>
                <p className="mt-3 text-sm leading-7 text-slate-100">{step}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 py-12 md:px-6 md:py-16">
        <SectionHeading
          eyebrow="Kiến thức sức khỏe"
          title="Nội dung giúp người bệnh chuẩn bị trước khi đi khám."
          description="Các bài viết tập trung vào dấu hiệu cần đi khám, cách chuẩn bị và các mốc theo dõi sau điều trị."
        />
        <div className="mt-8 grid gap-5 lg:grid-cols-3">
          {newsArticles.map((article) => (
            <article key={article.title} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
              <p className="text-sm font-semibold uppercase tracking-[0.18em] text-cyan-700">{article.category}</p>
              <h3 className="mt-3 text-xl font-bold tracking-tight text-slate-950">{article.title}</h3>
              <p className="mt-3 text-sm leading-7 text-slate-600">{article.summary}</p>
              <p className="mt-5 text-sm font-medium text-slate-500">{article.readTime}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 pb-14 md:px-6">
        <div className="rounded-2xl border border-cyan-200 bg-cyan-50 p-8 md:p-10">
          <div className="grid gap-6 lg:grid-cols-[1fr_auto] lg:items-center">
            <div>
              <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-700">Sẵn sàng đi khám?</p>
              <h2 className="mt-3 text-3xl font-bold tracking-tight text-slate-950 md:text-4xl">
                Gửi yêu cầu đặt lịch, ERM sẽ xác nhận khung giờ phù hợp.
              </h2>
              <p className="mt-4 text-sm leading-7 text-slate-600">
                Bạn có thể chọn chuyên khoa trước, hoặc để bộ phận điều phối gọi lại tư vấn cửa vào phù hợp.
              </p>
            </div>
            <Link
              href="/booking"
              className="inline-flex h-12 items-center justify-center rounded-full bg-slate-950 px-6 text-sm font-semibold text-white transition hover:bg-cyan-700"
            >
              Đặt lịch ngay
            </Link>
          </div>
        </div>
      </section>
    </PublicPageShell>
  );
}
