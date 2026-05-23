import { BookingForm } from "@/components/public/BookingForm";
import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { hospitalCatalogService } from "@/services/hospitalCatalogService";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";

const fallbackServices = [
  "Khám tổng quát cao cấp",
  "Lấy mẫu xét nghiệm tại nhà",
  "Khám chuyên khoa tim mạch",
  "Khám sản phụ khoa",
  "Tầm soát doanh nghiệp",
];

export default async function BookingPage() {
  let serviceOptions = fallbackServices;
  let specialties = [] as Awaited<ReturnType<typeof hospitalCatalogService.getSpecialties>>;
  let doctors = [] as Awaited<ReturnType<typeof hospitalDoctorService.getAll>>;

  try {
    const services = await hospitalCatalogService.getServices();
    if (services.length > 0) {
      serviceOptions = services.map((service) => `${service.serviceCode} - ${service.name}`);
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
              eyebrow="Đặt lịch thông minh"
              title="Trang đặt lịch đã bắt đầu đi vào luồng đặt lịch thật trên cơ sở dữ liệu đích."
              description="Người bệnh chọn chuyên khoa, bác sĩ, ngày giờ và gửi yêu cầu. Hệ thống kiểm tra lịch làm việc và tạo lịch hẹn nếu hợp lệ."
            />

            <div className="mt-8 grid gap-4">
              {[
                "Kiểm tra khung giờ dựa trên lịch làm việc của bác sĩ.",
                "Tự động tạo hồ sơ bệnh nhân mới nếu chưa tồn tại trong hệ thống đích.",
                "Đẩy sự kiện AppointmentCreated.v1 vào notification outbox để xử lý nhắc lịch ở giai đoạn sau.",
              ].map((item) => (
                <div key={item} className="rounded-[1.6rem] border border-slate-200 bg-white p-5 text-sm leading-7 text-slate-700 shadow-[0_20px_55px_rgba(15,23,42,0.05)]">
                  {item}
                </div>
              ))}
            </div>
          </div>

          <BookingForm
            serviceOptions={serviceOptions}
            specialtyOptions={specialties.map((specialty) => ({
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
