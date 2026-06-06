import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { doctors } from "@/content/hospitalContent";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";
import Image from "next/image";

function formatCurrency(amount?: number | null) {
  if (amount == null) {
    return null;
  }

  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(amount);
}

function formatDayOfWeek(dayOfWeek: number) {
  switch (dayOfWeek) {
    case 1:
      return "Thứ 2";
    case 2:
      return "Thứ 3";
    case 3:
      return "Thứ 4";
    case 4:
      return "Thứ 5";
    case 5:
      return "Thứ 6";
    case 6:
      return "Thứ 7";
    case 0:
      return "Chủ nhật";
    default:
      return "Khác";
  }
}

function buildDoctorPlaceholder(name: string, specialty: string) {
  const parts = name.split(" ").filter(Boolean);
  const initials = `${parts.at(-2)?.[0] ?? "B"}${parts.at(-1)?.[0] ?? "S"}`.toUpperCase();
  const hue = Array.from(name).reduce((sum, char) => sum + char.charCodeAt(0), 0) % 360;
  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" width="640" height="760" viewBox="0 0 640 760">
      <rect width="640" height="760" fill="hsl(${hue}, 72%, 90%)"/>
      <circle cx="320" cy="238" r="112" fill="#fff"/>
      <circle cx="320" cy="226" r="68" fill="hsl(${hue}, 70%, 42%)"/>
      <path d="M168 650c18-142 84-220 152-220s134 78 152 220" fill="hsl(${hue}, 70%, 42%)"/>
      <rect x="118" y="610" width="404" height="150" rx="44" fill="#0f172a"/>
      <text x="320" y="244" text-anchor="middle" dominant-baseline="middle" font-family="Segoe UI, Arial" font-size="54" font-weight="700" fill="#fff">${initials}</text>
      <text x="320" y="700" text-anchor="middle" font-family="Segoe UI, Arial" font-size="28" font-weight="700" fill="#fff">${specialty}</text>
    </svg>`;

  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

export default async function DoctorsPage() {
  let hospitalDoctors = [] as Awaited<ReturnType<typeof hospitalDoctorService.getAll>>;

  try {
    hospitalDoctors = await hospitalDoctorService.getAll();
  } catch {
    hospitalDoctors = [];
  }

  const hasApiData = hospitalDoctors.length > 0;

  return (
    <PublicPageShell>
      <section className="mx-auto max-w-7xl px-4 py-16 md:px-6 md:py-22">
        <SectionHeading
          eyebrow="Đội ngũ bác sĩ"
          title="Hồ sơ bác sĩ cần đủ chiều sâu để tạo niềm tin ngay từ lần xem đầu tiên."
          description="Một website bệnh viện tư mạnh không giấu chuyên môn. Nó cho thấy rõ bác sĩ điều trị nhóm bệnh nào, có bao nhiêu kinh nghiệm và người bệnh nên đặt lịch trong bối cảnh nào."
        />

        {hasApiData ? (
          <div className="mt-12 grid gap-5 lg:grid-cols-2">
            {hospitalDoctors.map((doctor) => (
              <article key={doctor.doctorProfileId} className="rounded-[2rem] border border-slate-200 bg-white p-7 shadow-[0_20px_55px_rgba(15,23,42,0.05)]">
                <div className="flex flex-col gap-5 md:flex-row md:items-start">
                  <Image
                    src={doctor.photoUrl ?? buildDoctorPlaceholder(doctor.fullName, doctor.specialtyName)}
                    alt={`Ảnh bác sĩ ${doctor.fullName}`}
                    width={360}
                    height={440}
                    unoptimized
                    className="h-48 w-full rounded-[1.4rem] object-cover object-top md:h-44 md:w-36"
                  />
                  <div className="flex min-w-0 flex-1 flex-wrap items-start justify-between gap-4">
                    <div>
                    <p className="text-sm uppercase tracking-[0.22em] text-cyan-700">{doctor.specialtyName}</p>
                    <h2 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">{doctor.fullName}</h2>
                    <p className="mt-2 text-sm text-slate-500">{doctor.departmentName}</p>
                    </div>
                    <div className="rounded-full bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700">
                    {doctor.yearsOfExperience ? `${doctor.yearsOfExperience} năm kinh nghiệm` : "Bác sĩ chuyên khoa"}
                    </div>
                  </div>
                </div>

                <p className="mt-6 text-sm leading-7 text-slate-600">
                  {doctor.biography ?? "Thông tin đang được cập nhật."}
                </p>

                <div className="mt-6 flex flex-wrap gap-2">
                  {doctor.schedules.map((schedule) => (
                    <span key={schedule.scheduleId} className="rounded-full border border-slate-200 px-3 py-2 text-sm text-slate-700">
                      {formatDayOfWeek(schedule.dayOfWeek)} {schedule.startTime.slice(0, 5)}-{schedule.endTime.slice(0, 5)} | {schedule.clinicName}
                    </span>
                  ))}
                </div>

                <div className="mt-6 flex flex-wrap gap-4 text-sm text-slate-600">
                  {doctor.licenseNumber ? <span>Chứng chỉ: {doctor.licenseNumber}</span> : null}
                  {formatCurrency(doctor.consultationFee) ? <span>Phí khám: {formatCurrency(doctor.consultationFee)}</span> : null}
                </div>
              </article>
            ))}
          </div>
        ) : (
          <div className="mt-12 grid gap-5 lg:grid-cols-2">
            {doctors.map((doctor) => (
              <article key={doctor.name} className="rounded-[2rem] border border-slate-200 bg-white p-7 shadow-[0_20px_55px_rgba(15,23,42,0.05)]">
                <div className="flex flex-col gap-5 md:flex-row md:items-start">
                  <Image
                    src={buildDoctorPlaceholder(doctor.name, doctor.specialty)}
                    alt={`Ảnh bác sĩ ${doctor.name}`}
                    width={360}
                    height={440}
                    unoptimized
                    className="h-48 w-full rounded-[1.4rem] object-cover object-top md:h-44 md:w-36"
                  />
                  <div className="flex min-w-0 flex-1 flex-wrap items-start justify-between gap-4">
                    <div>
                    <p className="text-sm uppercase tracking-[0.22em] text-cyan-700">{doctor.specialty}</p>
                    <h2 className="mt-2 text-3xl font-semibold tracking-tight text-slate-950">{doctor.name}</h2>
                    <p className="mt-2 text-sm text-slate-500">{doctor.title}</p>
                    </div>
                    <div className="rounded-full bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700">{doctor.experience}</div>
                  </div>
                </div>

                <div className="mt-6 flex flex-wrap gap-2">
                  {doctor.focus.map((item) => (
                    <span key={item} className="rounded-full border border-slate-200 px-3 py-2 text-sm text-slate-700">
                      {item}
                    </span>
                  ))}
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </PublicPageShell>
  );
}
