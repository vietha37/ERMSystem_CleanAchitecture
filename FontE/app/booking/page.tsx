import { BookingForm, type BookingServiceOption } from "@/components/public/BookingForm";
import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { hospitalCatalogService } from "@/services/hospitalCatalogService";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";

const fallbackServices: BookingServiceOption[] = [
  { value: "GENERAL-CHECKUP", label: "Khám tổng quát cao cấp" },
  { value: "HOME-LAB", label: "Lấy mẫu xét nghiệm tại nhà" },
  { value: "CARDIOLOGY", label: "Khám chuyên khoa tim mạch" },
  { value: "OBGYN", label: "Khám sản phụ khoa" },
  { value: "CORPORATE-SCREENING", label: "Tầm soát doanh nghiệp" },
];

const serviceLabelOverrides: Record<string, string> = {
  "IMG-US-ABD": "Siêu âm ổ bụng tổng quát",
};

export default async function BookingPage() {
  let serviceOptions = fallbackServices;
  let specialties = [] as Awaited<ReturnType<typeof hospitalCatalogService.getSpecialties>>;
  let doctors = [] as Awaited<ReturnType<typeof hospitalDoctorService.getAll>>;

  try {
    const services = await hospitalCatalogService.getServices();
    if (services.length > 0) {
      serviceOptions = services.map((service) => ({
        value: service.serviceCode,
        label: serviceLabelOverrides[service.serviceCode] ?? service.name,
      }));
    }
  } catch {
    serviceOptions = fallbackServices;
  }

  try {
    specialties = await hospitalCatalogService.getSpecialties();
  } catch {
    specialties = [];
  }

  try {
    doctors = await hospitalDoctorService.getAll();
  } catch {
    doctors = [];
  }

  return (
    <PublicPageShell>
      <section className="mx-auto max-w-7xl px-4 py-16 md:px-6 md:py-22">
        <div className="grid gap-10 lg:grid-cols-[0.92fr_1.08fr] lg:items-start">
          <div>
            <SectionHeading
              eyebrow="Đặt lịch khám"
              title="Gửi yêu cầu trước, ERM xác nhận khung giờ phù hợp."
              description="Bạn có thể chọn chuyên khoa, bác sĩ, ngày giờ mong muốn và để lại thông tin liên hệ. Bộ phận điều phối sẽ xác nhận lịch hẹn trước khi bạn đến viện."
            />

            <div className="mt-8 grid gap-4">
              {[
                "Nên đặt trước ít nhất 2 giờ để được điều phối bác sĩ và phòng khám phù hợp.",
                "Nếu chưa biết nên chọn chuyên khoa nào, hãy mô tả triệu chứng trong phần ghi chú.",
                "Mang theo giấy tờ tùy thân, kết quả xét nghiệm cũ và đơn thuốc đang sử dụng nếu có.",
              ].map((item) => (
                <div
                  key={item}
                  className="rounded-xl border border-slate-200 bg-white p-5 text-sm leading-7 text-slate-700 shadow-sm"
                >
                  {item}
                </div>
              ))}
            </div>
          </div>

          <BookingForm
            serviceOptions={serviceOptions}
            specialtyOptions={specialties
              .filter((specialty) => specialty.isActive)
              .map((specialty) => ({
                id: specialty.id,
                name: specialty.name,
              }))}
            doctors={doctors}
          />
        </div>
      </section>
    </PublicPageShell>
  );
}
