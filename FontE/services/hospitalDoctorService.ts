export type HospitalDoctorSchedule = {
  scheduleId: string;
  clinicId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotMinutes: number;
  validFrom: string;
  validTo?: string | null;
  clinicName: string;
  floorLabel?: string | null;
  roomLabel?: string | null;
};

export type HospitalDoctor = {
  doctorProfileId: string;
  staffProfileId: string;
  specialtyId: string;
  fullName: string;
  specialtyName: string;
  departmentName: string;
  photoUrl?: string | null;
  licenseNumber?: string | null;
  biography?: string | null;
  yearsOfExperience?: number | null;
  consultationFee?: number | null;
  isBookable: boolean;
  schedules: HospitalDoctorSchedule[];
};

function getApiBaseUrl() {
  return process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5219/api";
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Khong the tai du lieu tu ${path}`);
  }

  return response.json() as Promise<T>;
}

export const hospitalDoctorService = {
  getAll: (specialtyId?: string) =>
    getJson<HospitalDoctor[]>(
      specialtyId ? `/hospital-doctors?specialtyId=${encodeURIComponent(specialtyId)}` : "/hospital-doctors"
    ),
  getById: (doctorProfileId: string) => getJson<HospitalDoctor>(`/hospital-doctors/${doctorProfileId}`),
};
