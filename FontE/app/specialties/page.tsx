import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { specialties } from "@/content/hospitalContent";
import { hospitalCatalogService } from "@/services/hospitalCatalogService";

export default async function SpecialtiesPage() {
  let specialtiesFromApi = [] as Awaited<ReturnType<typeof hospitalCatalogService.getSpecialties>>;

  try {
    specialtiesFromApi = await hospitalCatalogService.getSpecialties();
  } catch {
    specialtiesFromApi = [];
  }

  const hasApiData = specialtiesFromApi.length > 0;

  return (
    <PublicPageShell>
      <section className="mx-auto max-w-7xl px-4 py-16 md:px-6 md:py-22">
        <SectionHeading
          eyebrow="Hệ chuyên khoa"
          title="Chuyên khoa được đồng bộ theo danh mục nghiệp vụ thay vì chỉ dùng nội dung giới thiệu."
          description="Đây là điểm neo để đặt lịch, điều phối và phân quyền vận hành nội bộ bám cùng một cấu trúc dữ liệu."
        />

        {hasApiData ? (
          <div className="mt-12 grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
            {specialtiesFromApi.map((specialty) => (
              <article key={specialty.id} className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-[0_20px_55px_rgba(15,23,42,0.05)]">
                <p className="text-sm uppercase tracking-[0.22em] text-cyan-700">
                  {specialty.departmentName ?? "Hệ chuyên khoa"}
                </p>
                <h2 className="mt-4 text-3xl font-semibold tracking-tight text-slate-950">{specialty.name}</h2>
                <p className="mt-4 text-sm font-medium leading-7 text-slate-700">Mã chuyên khoa: {specialty.specialtyCode}</p>
                <p className="mt-4 text-sm leading-7 text-slate-600">
                  Chuyên khoa này đang được đọc từ database đích để frontend, đặt lịch và dashboard nội bộ sử dụng cùng một danh mục.
                </p>
              </article>
            ))}
          </div>
        ) : (
          <div className="mt-12 grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
            {specialties.map((specialty) => (
              <article key={specialty.title} className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-[0_20px_55px_rgba(15,23,42,0.05)]">
                <p className="text-sm uppercase tracking-[0.22em] text-cyan-700">Chuyên khoa</p>
                <h2 className="mt-4 text-3xl font-semibold tracking-tight text-slate-950">{specialty.title}</h2>
                <p className="mt-4 text-sm font-medium leading-7 text-slate-700">{specialty.lead}</p>
                <p className="mt-4 text-sm leading-7 text-slate-600">{specialty.description}</p>
              </article>
            ))}
          </div>
        )}
      </section>
    </PublicPageShell>
  );
}
