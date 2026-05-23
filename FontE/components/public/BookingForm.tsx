"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import {
  hospitalAppointmentService,
  type PublicHospitalAppointmentBookingPayload,
} from "@/services/hospitalAppointmentService";
import { type HospitalDoctor } from "@/services/hospitalDoctorService";

export function BookingForm({
  serviceOptions,
  specialtyOptions,
  doctors,
}: {
  serviceOptions: string[];
  specialtyOptions: Array<{ id: string; name: string }>;
  doctors: HospitalDoctor[];
}) {
  const [submitting, setSubmitting] = useState(false);
  const [selectedSpecialtyId, setSelectedSpecialtyId] = useState(specialtyOptions[0]?.id ?? "");
  const [selectedDoctorId, setSelectedDoctorId] = useState("");

  const filteredDoctors = useMemo(() => {
    if (!selectedSpecialtyId) {
      return doctors;
    }

    return doctors.filter((doctor) => doctor.specialtyId === selectedSpecialtyId);
  }, [doctors, selectedSpecialtyId]);

  const selectedDoctor = filteredDoctors.find((doctor) => doctor.doctorProfileId === selectedDoctorId) ?? filteredDoctors[0];

  useEffect(() => {
    if (!filteredDoctors.length) {
      setSelectedDoctorId("");
      return;
    }

    if (!filteredDoctors.some((doctor) => doctor.doctorProfileId === selectedDoctorId)) {
      setSelectedDoctorId(filteredDoctors[0].doctorProfileId);
    }
  }, [filteredDoctors, selectedDoctorId]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedDoctor) {
      toast.error("Hệ thống chưa có bác sĩ phù hợp để đặt lịch.");
      return;
    }

    setSubmitting(true);

    const form = event.currentTarget;
    const formData = new FormData(form);

    const payload: PublicHospitalAppointmentBookingPayload = {
      fullName: String(formData.get("fullName") ?? "").trim(),
      phone: String(formData.get("phone") ?? "").trim(),
      email: String(formData.get("email") ?? "").trim() || undefined,
      dateOfBirth: String(formData.get("dateOfBirth") ?? ""),
      gender: String(formData.get("gender") ?? ""),
      doctorProfileId: selectedDoctor.doctorProfileId,
      specialtyId: selectedSpecialtyId || undefined,
      serviceCode: String(formData.get("serviceCode") ?? "").trim() || undefined,
      preferredDate: String(formData.get("preferredDate") ?? ""),
      preferredTime: String(formData.get("preferredTime") ?? ""),
      chiefComplaint: String(formData.get("chiefComplaint") ?? "").trim() || undefined,
      notes: String(formData.get("notes") ?? "").trim() || undefined,
    };

    try {
      const result = await hospitalAppointmentService.bookPublicAppointment(payload);
      toast.success(`Đặt lịch thành công. Mã lịch hẹn: ${result.appointmentNumber}`);
      form.reset();
      setSelectedSpecialtyId(specialtyOptions[0]?.id ?? "");
      setSelectedDoctorId("");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Không thể đặt lịch lúc này.";
      toast.error(message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-4 rounded-[2rem] border border-slate-200 bg-white/92 p-6 shadow-[0_30px_90px_rgba(15,23,42,0.08)] backdrop-blur md:grid-cols-2 md:p-8"
    >
      <Field name="fullName" label="Họ và tên" placeholder="Nguyễn Văn A" required />
      <Field name="phone" label="Số điện thoại" placeholder="09xx xxx xxx" required />
      <Field name="email" label="Email" placeholder="tenban@email.com" />
      <Field name="dateOfBirth" label="Ngày sinh" type="date" required />
      <SelectField
        label="Giới tính"
        name="gender"
        options={[
          { value: "Nam", label: "Nam" },
          { value: "Nu", label: "Nữ" },
          { value: "Khac", label: "Khác" },
        ]}
      />
      <SelectField
        label="Chuyên khoa"
        name="specialtyId"
        value={selectedSpecialtyId}
        onChange={setSelectedSpecialtyId}
        options={specialtyOptions.map((specialty) => ({
          value: specialty.id,
          label: specialty.name,
        }))}
      />
      <SelectField
        label="Bác sĩ"
        name="doctorProfileId"
        value={selectedDoctorId}
        onChange={setSelectedDoctorId}
        disabled={!filteredDoctors.length}
        options={filteredDoctors.map((doctor) => ({
          value: doctor.doctorProfileId,
          label: `${doctor.fullName} - ${doctor.specialtyName}`,
        }))}
      />
      <label className="grid gap-2 text-sm font-medium text-slate-700">
        Dịch vụ quan tâm
        <select
          name="serviceCode"
          className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
        >
          {serviceOptions.map((service) => (
            <option key={service}>{service}</option>
          ))}
        </select>
      </label>
      <Field name="preferredDate" label="Ngày mong muốn" type="date" required />
      <Field name="preferredTime" label="Giờ bắt đầu" type="time" required />
      <label className="md:col-span-2 grid gap-2 text-sm font-medium text-slate-700">
        Triệu chứng / nhu cầu chính
        <textarea
          name="chiefComplaint"
          rows={4}
          placeholder="Ví dụ: đau ngực nhẹ, cần tư vấn tim mạch, muốn đặt lịch buổi sáng."
          className="rounded-3xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
        />
      </label>
      <label className="md:col-span-2 grid gap-2 text-sm font-medium text-slate-700">
        Ghi chú bổ sung
        <textarea
          name="notes"
          rows={3}
          placeholder="Thông tin thêm cho điều phối viên nếu cần."
          className="rounded-3xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
        />
      </label>

      {selectedDoctor ? (
        <div className="md:col-span-2 rounded-[1.6rem] border border-cyan-100 bg-cyan-50/60 p-4 text-sm text-slate-700">
          <p className="font-semibold text-slate-900">Lịch làm việc hiện có của bác sĩ {selectedDoctor.fullName}</p>
          <div className="mt-3 flex flex-wrap gap-2">
            {selectedDoctor.schedules.map((schedule) => (
              <span key={schedule.scheduleId} className="rounded-full border border-cyan-200 bg-white px-3 py-2 text-xs text-slate-700">
                {formatDayOfWeek(schedule.dayOfWeek)} {schedule.startTime.slice(0, 5)}-{schedule.endTime.slice(0, 5)} | {schedule.clinicName}
              </span>
            ))}
          </div>
        </div>
      ) : null}

      <div className="md:col-span-2 flex flex-col gap-4 pt-2 md:flex-row md:items-center md:justify-between">
        <p className="max-w-2xl text-sm leading-6 text-slate-500">
          Sau khi gửi, hệ thống sẽ tạo lịch hẹn và đưa sự kiện vào outbox để dịch vụ thông báo có thể nhắc lịch ở các bước sau.
        </p>
        <button
          type="submit"
          disabled={submitting || !selectedDoctor}
          className="inline-flex h-12 items-center justify-center rounded-full bg-slate-950 px-6 text-sm font-semibold text-white transition hover:bg-cyan-700 disabled:cursor-not-allowed disabled:opacity-70"
        >
          {submitting ? "Đang gửi yêu cầu..." : "Gửi yêu cầu đặt lịch"}
        </button>
      </div>
    </form>
  );
}

function Field({
  name,
  label,
  placeholder,
  type = "text",
  required = false,
}: {
  name: string;
  label: string;
  placeholder?: string;
  type?: string;
  required?: boolean;
}) {
  return (
    <label className="grid gap-2 text-sm font-medium text-slate-700">
      {label}
      <input
        name={name}
        type={type}
        placeholder={placeholder}
        required={required}
        className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
      />
    </label>
  );
}

function SelectField({
  label,
  name,
  options,
  value,
  onChange,
  disabled = false,
}: {
  label: string;
  name: string;
  options: Array<{ value: string; label: string }>;
  value?: string;
  onChange?: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <label className="grid gap-2 text-sm font-medium text-slate-700">
      {label}
      <select
        name={name}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange?.(event.target.value)}
        className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white disabled:cursor-not-allowed disabled:opacity-70"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
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
