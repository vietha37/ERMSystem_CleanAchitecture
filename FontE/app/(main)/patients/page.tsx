"use client";

import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { ErrorState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { formatDateTimeValue, formatDateValue } from "@/lib/dateFormatting";
import { patientService } from "@/services/patientService";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import { getApiErrorMessage } from "@/services/error";
import { useAuth } from "@/hooks/useAuth";
import {
  HospitalAppointmentWorklistItem,
  HospitalEncounterSummary,
  Patient,
} from "@/services/types";
import toast from "react-hot-toast";

type PatientViewModel = Patient & {
  status?: string;
  medicalRecordNumber?: string;
  latestDiagnosis?: string;
  latestVisitDate?: string;
  doctorName?: string;
};

type DrawerData = {
  appointments: HospitalAppointmentWorklistItem[];
  encounters: HospitalEncounterSummary[];
  isLoading: boolean;
};

type PatientFormState = {
  dateOfBirth: string;
  gender: string;
  phone: string;
  address: string;
};

const DEFAULT_FORM_STATE: PatientFormState = {
  dateOfBirth: "",
  gender: "Male",
  phone: "",
  address: "",
};

export default function PatientsPage() {
  const { role } = useAuth();
  const isDoctor = role === "Doctor";

  const [patients, setPatients] = useState<PatientViewModel[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<"create" | "edit">("create");
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(null);
  const firstInputRef = useRef<HTMLInputElement>(null);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [selectedPatient, setSelectedPatient] = useState<PatientViewModel | null>(null);
  const [drawerData, setDrawerData] = useState<DrawerData>({
    appointments: [],
    encounters: [],
    isLoading: false,
  });

  const [fullName, setFullName] = useState("");
  const [formData, setFormData] = useState<PatientFormState>(DEFAULT_FORM_STATE);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setCurrentPage(1);
    }, 400);

    return () => clearTimeout(handler);
  }, [searchQuery]);

  useEffect(() => {
    if (isModalOpen && firstInputRef.current) {
      setTimeout(() => firstInputRef.current?.focus(), 100);
    }
  }, [isModalOpen]);

  const filterDoctorPatients = useCallback(
    (items: PatientViewModel[]) => {
      const query = debouncedSearch.trim().toLowerCase();
      if (!query) {
        return items;
      }

      return items.filter((patient) =>
        [
          patient.fullName,
          patient.phone,
          patient.address,
          patient.medicalRecordNumber,
          patient.latestDiagnosis,
          patient.doctorName,
        ]
          .filter(Boolean)
          .some((value) => value!.toLowerCase().includes(query))
      );
    },
    [debouncedSearch]
  );

  const mapDoctorPatients = useCallback(
    (
      appointments: HospitalAppointmentWorklistItem[],
      encounters: HospitalEncounterSummary[]
    ): PatientViewModel[] => {
      const patientMap = new Map<string, PatientViewModel>();
      const latestEncounterByPatient = new Map<string, HospitalEncounterSummary>();

      encounters.forEach((encounter) => {
        const existing = latestEncounterByPatient.get(encounter.patientId);
        if (
          !existing ||
          new Date(encounter.updatedAtLocal).getTime() >
            new Date(existing.updatedAtLocal).getTime()
        ) {
          latestEncounterByPatient.set(encounter.patientId, encounter);
        }
      });

      appointments.forEach((appointment) => {
        const linkedEncounter = latestEncounterByPatient.get(appointment.patientId);
        const existing = patientMap.get(appointment.patientId);
        const candidateDate =
          linkedEncounter?.updatedAtLocal ?? appointment.appointmentStartLocal;

        if (
          !existing ||
          new Date(candidateDate).getTime() >
            new Date(existing.latestVisitDate ?? "1970-01-01T00:00:00Z").getTime()
        ) {
          patientMap.set(appointment.patientId, {
            id: appointment.patientId,
            fullName: appointment.patientName,
            dateOfBirth: "",
            gender: "",
            phone: appointment.patientPhone ?? "",
            address: "",
            createdAt: appointment.appointmentStartLocal,
            medicalRecordNumber: appointment.medicalRecordNumber,
            latestDiagnosis: linkedEncounter?.primaryDiagnosisName ?? "",
            latestVisitDate: candidateDate,
            doctorName: appointment.doctorName,
            status:
              appointment.status === "Cancelled" ? "Tạm dừng" : "Đang theo dõi",
          });
        }
      });

      return Array.from(patientMap.values()).sort((a, b) => {
        return (
          new Date(b.latestVisitDate ?? b.createdAt).getTime() -
          new Date(a.latestVisitDate ?? a.createdAt).getTime()
        );
      });
    },
    []
  );

  const fetchPatients = useCallback(async () => {
    setIsLoading(true);

    try {
      if (isDoctor) {
        const [appointmentData, encounterData] = await Promise.all([
          hospitalAppointmentWorklistService.getAll({
            pageNumber: 1,
            pageSize: 100,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalEncounterService.getAll({
            pageNumber: 1,
            pageSize: 100,
            textSearch: debouncedSearch || undefined,
          }),
        ]);

        const allPatients = mapDoctorPatients(
          appointmentData.items,
          encounterData.items
        );
        const filteredPatients = filterDoctorPatients(allPatients);
        const startIndex = (currentPage - 1) * pageSize;
        const pagedPatients = filteredPatients.slice(startIndex, startIndex + pageSize);

        setPatients(pagedPatients);
        setTotalItems(filteredPatients.length);
        setTotalPages(Math.max(1, Math.ceil(filteredPatients.length / pageSize)));
        setListError(null);
        return;
      }

      const data = await patientService.getAll(currentPage, pageSize, debouncedSearch);
      const mapped: PatientViewModel[] = data.items.map((patient) => ({
        ...patient,
        status: "Đang theo dõi",
      }));

      setPatients(mapped);
      setTotalPages(Math.max(1, Math.ceil((data.totalCount || 0) / pageSize)));
      setTotalItems(data.totalCount || 0);
      setListError(null);
    } catch (error: unknown) {
      setListError(getApiErrorMessage(error, "Không thể tải danh sách bệnh nhân."));
      toast.error(getApiErrorMessage(error, "Không thể tải danh sách bệnh nhân."));
      setPatients([]);
      setTotalPages(1);
      setTotalItems(0);
    } finally {
      setIsLoading(false);
    }
  }, [
    currentPage,
    debouncedSearch,
    filterDoctorPatients,
    isDoctor,
    mapDoctorPatients,
    pageSize,
  ]);

  useEffect(() => {
    fetchPatients();
  }, [fetchPatients]);

  const openCreateModal = () => {
    if (isDoctor) {
      toast.error("Bác sĩ không có quyền tạo hồ sơ bệnh nhân từ màn này.");
      return;
    }

    setModalMode("create");
    setSelectedPatientId(null);
    setFullName("");
    setFormData(DEFAULT_FORM_STATE);
    setIsModalOpen(true);
  };

  const openEditModal = (patient: PatientViewModel, event: React.MouseEvent) => {
    event.stopPropagation();

    if (isDoctor) {
      toast.error("Bác sĩ chỉ được xem danh sách bệnh nhân liên quan.");
      return;
    }

    setModalMode("edit");
    setSelectedPatientId(patient.id);
    setFullName(patient.fullName || "");
    setFormData({
      dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.split("T")[0] : "",
      gender: patient.gender || "Male",
      phone: patient.phone || "",
      address: patient.address || "",
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!fullName.trim() || !formData.dateOfBirth) {
      toast.error("Họ tên và ngày sinh là bắt buộc.");
      return;
    }

    setIsSubmitting(true);
    const payload = {
      fullName: fullName.trim(),
      ...formData,
    };

    try {
      if (modalMode === "create") {
        await patientService.create(payload);
        toast.success("Đã tạo hồ sơ bệnh nhân.");
      } else if (selectedPatientId) {
        await patientService.update(selectedPatientId, payload);
        toast.success("Đã cập nhật hồ sơ bệnh nhân.");
      }

      setIsModalOpen(false);
      await fetchPatients();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xử lý yêu cầu."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: string, event: React.MouseEvent) => {
    event.stopPropagation();

    if (isDoctor) {
      toast.error("Bác sĩ không có quyền xóa hồ sơ bệnh nhân.");
      return;
    }

    if (
      !window.confirm(
        "Bạn có chắc muốn xóa hồ sơ bệnh nhân này không? Thao tác này không thể hoàn tác."
      )
    ) {
      return;
    }

    try {
      await patientService.delete(id);
      toast.success("Đã xóa hồ sơ bệnh nhân.");

      if (patients.length === 1 && currentPage > 1) {
        setCurrentPage((value) => value - 1);
      } else {
        await fetchPatients();
      }

      if (selectedPatient?.id === id) {
        setIsDrawerOpen(false);
      }
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xóa hồ sơ bệnh nhân."));
    }
  };

  const openDrawer = async (patient: PatientViewModel) => {
    setSelectedPatient(patient);
    setIsDrawerOpen(true);
    setDrawerData({
      appointments: [],
      encounters: [],
      isLoading: true,
    });

    try {
      const [appointmentData, encounterData] = await Promise.all([
        hospitalAppointmentWorklistService.getAll({
          pageNumber: 1,
          pageSize: 100,
          textSearch: patient.medicalRecordNumber || patient.fullName,
        }),
        hospitalEncounterService.getAll({
          pageNumber: 1,
          pageSize: 100,
          textSearch: patient.medicalRecordNumber || patient.fullName,
        }),
      ]);

      const patientAppointments = appointmentData.items
        .filter((item) => item.patientId === patient.id)
        .sort(
          (left, right) =>
            new Date(right.appointmentStartLocal).getTime() -
            new Date(left.appointmentStartLocal).getTime()
        );

      const patientEncounters = encounterData.items
        .filter((item) => item.patientId === patient.id)
        .sort(
          (left, right) =>
            new Date(right.updatedAtLocal).getTime() -
            new Date(left.updatedAtLocal).getTime()
        );

      setDrawerData({
        appointments: patientAppointments,
        encounters: patientEncounters,
        isLoading: false,
      });
    } catch {
      setDrawerData({
        appointments: [],
        encounters: [],
        isLoading: false,
      });
    }
  };

  const patientMetrics = useMemo(() => {
    const activePatients = patients.filter(
      (patient) => patient.status !== "Tạm dừng"
    ).length;
    const withRecentVisit = patients.filter((patient) => patient.latestVisitDate).length;

    return {
      activePatients,
      withRecentVisit,
    };
  }, [patients]);

  const getInitials = (fullNameValue?: string) => {
    if (!fullNameValue) {
      return "?";
    }

    const parts = fullNameValue.trim().split(" ").filter(Boolean);
    if (parts.length === 0) {
      return "?";
    }

    if (parts.length === 1) {
      return parts[0].charAt(0).toUpperCase();
    }

    return `${parts[0].charAt(0)}${parts[parts.length - 1].charAt(0)}`.toUpperCase();
  };

  const latestDiagnosis =
    drawerData.encounters.find((encounter) => encounter.primaryDiagnosisName)
      ?.primaryDiagnosisName ?? "Chưa có";

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-gray-900">
              {isDoctor ? "Bệnh nhân liên quan" : "Bệnh nhân"}
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              {isDoctor
                ? ""
                : "Danh sách bệnh nhân và hồ sơ khám."}
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <div className="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-800">
              <div className="text-xs font-semibold uppercase tracking-wide text-blue-500">
                Hồ sơ đang theo dõi
              </div>
              <div className="mt-1 text-xl font-bold">{patientMetrics.activePatients}</div>
            </div>
            <div className="rounded-xl border border-emerald-100 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
              <div className="text-xs font-semibold uppercase tracking-wide text-emerald-500">
                Có lần khám gần đây
              </div>
              <div className="mt-1 text-xl font-bold">{patientMetrics.withRecentVisit}</div>
            </div>
            {!isDoctor && (
              <Button
                onClick={openCreateModal}
                className="shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg"
              >
                + Thêm bệnh nhân
              </Button>
            )}
          </div>
        </div>
      </div>

      <Card className="rounded-2xl border-none bg-white p-6 shadow-sm">
        <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div className="relative w-full md:w-[420px]">
            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-gray-400">
              Tim
            </span>
            <input
              type="text"
              placeholder="Tìm theo tên, số điện thoại, mã bệnh nhân..."
              className="w-full rounded-xl border border-gray-200 py-2.5 pl-10 pr-4 text-sm shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
            />
          </div>

          <div className="flex items-center gap-3 rounded-xl border border-gray-100 bg-gray-50 px-4 py-2">
            <span className="text-sm font-medium text-gray-500">Hiển thị</span>
            <select
              className="cursor-pointer bg-transparent text-sm font-bold text-gray-700 outline-none"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setCurrentPage(1);
              }}
            >
              <option value={5}>5 / trang</option>
              <option value={10}>10 / trang</option>
              <option value={20}>20 / trang</option>
            </select>
          </div>
        </div>

        <div className="relative h-[600px] overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div className="h-full overflow-y-auto">
            <table className="min-w-max w-full border-collapse text-left">
              <thead className="sticky top-0 z-10 border-b border-gray-200 bg-white/95 shadow-sm backdrop-blur-md">
                <tr>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    <span className="sr-only">Ảnh</span>
                  </th>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    Họ và tên
                  </th>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    Mã bệnh nhân
                  </th>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    Điện thoại
                  </th>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    Bác sĩ phụ trách
                  </th>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    Lần khám gần nhất
                  </th>
                  <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                    Trạng thái
                  </th>
                  <th className="p-4 text-right text-xs font-bold uppercase tracking-wider text-gray-500">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {isLoading ? (
                  Array.from({ length: 5 }).map((_, index) => (
                    <tr key={index} className="animate-pulse border-b border-gray-50">
                      <td className="p-4">
                        <div className="h-10 w-10 rounded-full bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="h-4 w-40 rounded bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="h-4 w-24 rounded bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="h-4 w-28 rounded bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="h-4 w-32 rounded bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="h-4 w-28 rounded bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="h-6 w-24 rounded-full bg-gray-200" />
                      </td>
                      <td className="p-4">
                        <div className="ml-auto h-8 w-20 rounded-lg bg-gray-200" />
                      </td>
                    </tr>
                  ))
                ) : listError ? (
                  <tr>
                    <td colSpan={8}>
                      <ErrorState
                        title="Không thể tải danh sách bệnh nhân"
                        description={listError}
                        onAction={() => void fetchPatients()}
                      />
                    </td>
                  </tr>
                ) : patients.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="p-16 text-center text-gray-500">
                      <div className="mb-3 font-semibold text-gray-400">Hồ sơ trống</div>
                      <p className="font-medium">
                        {debouncedSearch
                          ? "Không có bệnh nhân nào khớp điều kiện tìm kiếm."
                          : "Chưa có hồ sơ bệnh nhân phù hợp."}
                      </p>
                    </td>
                  </tr>
                ) : (
                  patients.map((patient) => (
                    <tr
                      key={patient.id}
                      onClick={() => openDrawer(patient)}
                      className="cursor-pointer bg-white transition-all duration-200 hover:-translate-y-0.5 hover:bg-blue-50/40 hover:shadow-md"
                    >
                      <td className="p-4">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full border border-blue-200 bg-gradient-to-br from-blue-100 to-blue-200 text-sm font-bold text-blue-700 shadow-sm">
                          {getInitials(patient.fullName)}
                        </div>
                      </td>
                      <td className="p-4">
                        <div className="font-semibold text-gray-800">{patient.fullName}</div>
                        <div className="mt-0.5 text-xs text-gray-500">
                          {patient.dateOfBirth
                            ? formatDateValue(patient.dateOfBirth, "Chưa có ngày sinh")
                            : "Chưa có ngày sinh"}
                        </div>
                      </td>
                      <td className="p-4 text-sm font-semibold text-blue-700">
                        {patient.medicalRecordNumber || "Chưa có"}
                      </td>
                      <td className="p-4 text-sm font-medium text-gray-600">
                        {patient.phone || "Chưa có"}
                      </td>
                      <td className="p-4 text-sm text-gray-600">
                        {patient.doctorName || "Chưa phân công"}
                      </td>
                      <td className="p-4 text-sm text-gray-600">
                        {patient.latestVisitDate
                          ? formatDateTimeValue(patient.latestVisitDate, "Chưa từng khám")
                          : "Chưa từng khám"}
                      </td>
                      <td className="p-4">
                        <span className="inline-flex w-max items-center gap-1.5 rounded-full border border-green-100 bg-green-50 px-2.5 py-1 text-xs font-bold text-green-700">
                          <span className="h-2 w-2 animate-pulse rounded-full bg-green-500" />
                          {patient.status || "Đang theo dõi"}
                        </span>
                      </td>
                      <td className="p-4">
                        <div className="flex justify-end gap-2">
                          {!isDoctor && (
                            <button
                              onClick={(event) => openEditModal(patient, event)}
                              className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-semibold text-gray-600 shadow-sm transition-colors hover:border-blue-300 hover:bg-blue-50 hover:text-blue-600"
                              title="Sửa hồ sơ bệnh nhân"
                            >
                              Sửa
                            </button>
                          )}
                          {!isDoctor && (
                            <button
                              onClick={(event) => handleDelete(patient.id, event)}
                              className="rounded-lg border border-red-100 bg-white px-3 py-1.5 text-sm font-semibold text-red-500 shadow-sm transition-colors hover:border-red-600 hover:bg-red-500 hover:text-white"
                              title="Xóa hồ sơ bệnh nhân"
                            >
                              Xóa
                            </button>
                          )}
                          {isDoctor && (
                            <span className="rounded-lg border border-blue-100 bg-blue-50 px-3 py-1.5 text-sm font-semibold text-blue-700">
                              Chỉ xem
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {!isLoading && totalPages > 0 && (
          <div className="mt-5 flex items-center justify-between text-sm">
            <div className="font-medium text-gray-500">
              Hiển thị{" "}
              <span className="font-bold text-gray-900">
                {totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1}
              </span>{" "}
              đến{" "}
              <span className="font-bold text-gray-900">
                {Math.min(currentPage * pageSize, totalItems)}
              </span>{" "}
              trong tổng số{" "}
              <span className="font-bold text-blue-600">{totalItems}</span> bệnh nhân
            </div>

            <div className="flex items-center gap-1 rounded-xl border border-gray-200 bg-gray-50 p-1 shadow-sm">
              <button
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(1)}
                className="rounded-lg px-3 py-1.5 font-bold text-gray-600 transition-all hover:bg-white hover:shadow-sm disabled:opacity-40"
              >
                «
              </button>
              <button
                disabled={currentPage === 1}
                onClick={() => setCurrentPage((value) => value - 1)}
                className="rounded-lg px-3 py-1.5 font-bold text-gray-600 transition-all hover:bg-white hover:shadow-sm disabled:opacity-40"
              >
                Trước
              </button>
              <span className="rounded-lg bg-blue-100/50 px-4 py-1.5 font-bold text-blue-700 shadow-inner">
                {currentPage} / {totalPages}
              </span>
              <button
                disabled={currentPage === totalPages}
                onClick={() => setCurrentPage((value) => value + 1)}
                className="rounded-lg px-3 py-1.5 font-bold text-gray-600 transition-all hover:bg-white hover:shadow-sm disabled:opacity-40"
              >
                Tiếp
              </button>
            </div>
          </div>
        )}
      </Card>

      {isDrawerOpen && (
        <div
          className="fixed inset-0 z-40 bg-gray-900/30 backdrop-blur-sm transition-opacity duration-300"
          onClick={() => setIsDrawerOpen(false)}
        />
      )}

      <div
        className={`fixed right-0 top-0 z-50 flex h-full w-full max-w-md transform flex-col bg-white shadow-2xl transition-transform duration-300 ease-in-out ${
          isDrawerOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        {selectedPatient && (
          <>
            <div className="sticky top-0 z-10 flex items-center justify-between border-b border-gray-100 bg-blue-50/30 px-6 py-5 backdrop-blur-sm">
              <h2 className="text-xl font-bold text-gray-800">Chi tiết bệnh nhân</h2>
              <button
                onClick={() => setIsDrawerOpen(false)}
                className="flex h-8 w-8 items-center justify-center rounded-full bg-gray-100 font-bold text-gray-500 transition-colors hover:bg-red-100 hover:text-red-500"
              >
                ×
              </button>
            </div>

            <div className="flex-1 space-y-8 overflow-y-auto p-6">
              <div className="flex flex-col items-center text-center">
                <div className="mb-4 flex h-24 w-24 items-center justify-center rounded-full border-4 border-blue-50 bg-gradient-to-br from-blue-500 to-blue-600 text-3xl font-bold text-white shadow-lg">
                  {getInitials(selectedPatient.fullName)}
                </div>
                <h3 className="text-2xl font-bold text-gray-900">
                  {selectedPatient.fullName}
                </h3>
                <div className="mt-2 flex flex-wrap items-center justify-center gap-2">
                  <span className="rounded-full bg-green-100 px-3 py-1 text-xs font-bold uppercase tracking-wide text-green-700">
                    {selectedPatient.status || "Đang theo dõi"}
                  </span>
                  <span className="rounded-full bg-gray-100 px-3 py-1 text-xs font-bold text-gray-600">
                    Mã: {selectedPatient.medicalRecordNumber || "Chưa có"}
                  </span>
                </div>
              </div>

              <div className="space-y-3 rounded-2xl border border-gray-100 bg-gray-50 p-4 shadow-inner">
                <div className="flex items-start gap-3">
                  <div className="mt-0.5 text-blue-500">DT</div>
                  <div>
                    <div className="text-xs font-bold uppercase text-gray-400">
                      Số điện thoại
                    </div>
                    <div className="font-semibold text-gray-800">
                      {selectedPatient.phone || "Chưa có số điện thoại"}
                    </div>
                  </div>
                </div>
                <div className="flex items-start gap-3">
                  <div className="mt-0.5 text-blue-500">BS</div>
                  <div>
                    <div className="text-xs font-bold uppercase text-gray-400">
                      Bác sĩ phụ trách gần nhất
                    </div>
                    <div className="font-semibold text-gray-800">
                      {selectedPatient.doctorName || "Chưa phân công"}
                    </div>
                  </div>
                </div>
                <div className="flex items-start gap-3">
                  <div className="mt-0.5 text-blue-500">DC</div>
                  <div>
                    <div className="text-xs font-bold uppercase text-gray-400">
                      Địa chỉ
                    </div>
                    <div className="font-semibold text-gray-800">
                      {selectedPatient.address || "Chưa có địa chỉ"}
                    </div>
                  </div>
                </div>
              </div>

              <div className="relative overflow-hidden rounded-2xl border border-blue-100 bg-gradient-to-br from-blue-50 to-blue-100/50 p-5 shadow-sm">
                <h4 className="mb-4 flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-blue-800">
                  <span className="h-2 w-2 rounded-full bg-blue-500" />
                  Tóm tắt hồ sơ
                </h4>

                {drawerData.isLoading ? (
                  <div className="space-y-3 animate-pulse">
                    <div className="h-4 w-full rounded bg-blue-200/50" />
                    <div className="h-4 w-2/3 rounded bg-blue-200/50" />
                    <div className="h-4 w-3/4 rounded bg-blue-200/50" />
                  </div>
                ) : (
                  <div className="space-y-4">
                    <div className="flex items-center justify-between rounded-xl border border-white bg-white/60 p-3">
                      <span className="text-sm font-medium text-gray-600">
                        Tổng lịch hẹn
                      </span>
                      <span className="text-lg font-bold text-blue-700">
                        {drawerData.appointments.length}
                      </span>
                    </div>
                    <div className="flex items-center justify-between rounded-xl border border-white bg-white/60 p-3">
                      <span className="text-sm font-medium text-gray-600">
                        Lần khám gần nhất
                      </span>
                      <span className="text-sm font-bold text-gray-800">
                        {drawerData.appointments[0]
                          ? formatDateTimeValue(
                              drawerData.appointments[0].appointmentStartLocal,
                              "Chưa từng"
                            )
                          : "Chưa từng"}
                      </span>
                    </div>
                    <div className="flex items-center justify-between rounded-xl border border-white bg-white/60 p-3">
                      <span className="text-sm font-medium text-gray-600">
                        Chẩn đoán gần nhất
                      </span>
                      <span className="text-sm font-bold text-blue-700">
                        {latestDiagnosis}
                      </span>
                    </div>
                  </div>
                )}
              </div>

              <div>
                <h4 className="mb-3 px-1 text-sm font-bold uppercase tracking-wide text-gray-800">
                  Lịch hẹn gần đây
                </h4>
                <div className="overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
                  <table className="w-full text-left text-sm">
                    <thead className="border-b border-gray-100 bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 font-semibold text-gray-500">Ngày</th>
                        <th className="px-4 py-3 font-semibold text-gray-500">
                          Bác sĩ
                        </th>
                        <th className="px-4 py-3 font-semibold text-gray-500">
                          Trạng thái
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-50">
                      {drawerData.isLoading ? (
                        <tr>
                          <td colSpan={3} className="px-4 py-6 text-center text-gray-400">
                            Đang tải lịch sử...
                          </td>
                        </tr>
                      ) : drawerData.appointments.length === 0 ? (
                        <tr>
                          <td colSpan={3} className="px-4 py-6 text-center text-gray-400">
                            Chưa có lịch hẹn nào.
                          </td>
                        </tr>
                      ) : (
                        drawerData.appointments.slice(0, 5).map((appointment) => (
                          <tr
                            key={appointment.appointmentId}
                            className="transition-colors hover:bg-blue-50/30"
                          >
                            <td className="px-4 py-3 text-gray-600">
                              {formatDateTimeValue(appointment.appointmentStartLocal)}
                            </td>
                            <td className="px-4 py-3 font-semibold text-gray-800">
                              {appointment.doctorName}
                            </td>
                            <td className="px-4 py-3">
                              <span className="rounded-md bg-blue-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider text-blue-700">
                                {appointment.status}
                              </span>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>

              <div>
                <h4 className="mb-3 px-1 text-sm font-bold uppercase tracking-wide text-gray-800">
                  Hồ sơ khám gần đây
                </h4>
                <div className="space-y-3">
                  {drawerData.isLoading ? (
                    <div className="rounded-2xl border border-gray-100 bg-white p-4 text-sm text-gray-400">
                      Đang tải hồ sơ khám...
                    </div>
                  ) : drawerData.encounters.length === 0 ? (
                    <div className="rounded-2xl border border-gray-100 bg-white p-4 text-sm text-gray-400">
                      Chưa có hồ sơ khám nào.
                    </div>
                  ) : (
                    drawerData.encounters.slice(0, 4).map((encounter) => (
                      <div
                        key={encounter.encounterId}
                        className="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <div className="text-sm font-bold text-gray-900">
                              {encounter.encounterNumber}
                            </div>
                            <div className="mt-1 text-xs text-gray-500">
                              {formatDateTimeValue(encounter.startedAtLocal)}
                            </div>
                          </div>
                          <span className="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-semibold text-gray-700">
                            {encounter.encounterStatus}
                          </span>
                        </div>
                        <div className="mt-3 text-sm text-gray-700">
                          <div>
                            <span className="font-semibold">Chẩn đoán:</span>{" "}
                            {encounter.primaryDiagnosisName || "Chưa có"}
                          </div>
                          <div className="mt-1">
                            <span className="font-semibold">Tóm tắt:</span>{" "}
                            {encounter.summary || "Chưa có ghi chú tóm tắt"}
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>

            <div className="shrink-0 border-t border-gray-100 bg-gray-50 p-6">
              {isDoctor ? (
                <div className="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-800">
                  Bác sĩ chỉ được xem bệnh nhân thuộc lịch khám và hồ sơ khám liên quan.
                </div>
              ) : (
                <div className="flex flex-col gap-3">
                  <button
                    onClick={() => {
                      toast("Chuyển sang màn tạo lịch hẹn...");
                      setIsDrawerOpen(false);
                    }}
                    className="w-full rounded-xl bg-blue-600 py-3.5 font-bold text-white shadow-md transition-all hover:bg-blue-700 hover:shadow-lg"
                  >
                    + Tạo lịch hẹn
                  </button>
                  <button
                    onClick={() => {
                      toast("Chuyển sang màn hồ sơ khám...");
                      setIsDrawerOpen(false);
                    }}
                    className="w-full rounded-xl border border-blue-200 bg-white py-3.5 font-bold text-blue-700 shadow-sm transition-all hover:bg-gray-50"
                  >
                    Xem toàn bộ hồ sơ khám
                  </button>
                </div>
              )}
            </div>
          </>
        )}
      </div>

      {!isDoctor && (
        <Modal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          title={
            modalMode === "create"
              ? "Thêm bệnh nhân mới"
              : "Cập nhật hồ sơ bệnh nhân"
          }
        >
          <form onSubmit={handleSubmit} className="mt-2 space-y-4 px-1 pb-2">
            <div>
              <label className="mb-1.5 block text-sm font-bold text-gray-700">
                Họ và tên <span className="text-red-500">*</span>
              </label>
              <input
                ref={firstInputRef}
                type="text"
                className="w-full rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all placeholder:text-gray-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
                placeholder="Ví dụ: Nguyễn Văn A"
                value={fullName}
                onChange={(event) => setFullName(event.target.value)}
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="mb-1.5 block text-sm font-bold text-gray-700">
                  Ngày sinh <span className="text-red-500">*</span>
                </label>
                <input
                  type="date"
                  className="w-full rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
                  value={formData.dateOfBirth}
                  onChange={(event) =>
                    setFormData((value) => ({
                      ...value,
                      dateOfBirth: event.target.value,
                    }))
                  }
                  required
                />
              </div>

              <div>
                <label className="mb-1.5 block text-sm font-bold text-gray-700">
                  Giới tính
                </label>
                <select
                  className="w-full cursor-pointer rounded-xl border border-gray-300 bg-white px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
                  value={formData.gender}
                  onChange={(event) =>
                    setFormData((value) => ({ ...value, gender: event.target.value }))
                  }
                >
                  <option value="Male">Nam</option>
                  <option value="Female">Nữ</option>
                  <option value="Other">Khác</option>
                </select>
              </div>
            </div>

            <div>
              <label className="mb-1.5 block text-sm font-bold text-gray-700">
                Số điện thoại
              </label>
              <input
                type="tel"
                className="w-full rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all placeholder:text-gray-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
                placeholder="0901234567"
                value={formData.phone}
                onChange={(event) =>
                  setFormData((value) => ({ ...value, phone: event.target.value }))
                }
              />
            </div>

            <div>
              <label className="mb-1.5 block text-sm font-bold text-gray-700">
                Địa chỉ thường trú
              </label>
              <textarea
                rows={3}
                className="w-full resize-y rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all placeholder:text-gray-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
                placeholder="Nhập địa chỉ..."
                value={formData.address}
                onChange={(event) =>
                  setFormData((value) => ({ ...value, address: event.target.value }))
                }
              />
            </div>

            <div className="flex justify-end gap-3 border-t border-gray-100 pt-6">
              <button
                type="button"
                onClick={() => setIsModalOpen(false)}
                className="rounded-xl bg-gray-100 px-5 py-2.5 font-bold text-gray-600 transition-colors hover:bg-gray-200"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="flex items-center gap-2 rounded-xl bg-blue-600 px-6 py-2.5 font-bold text-white shadow-md transition-colors hover:bg-blue-700 hover:shadow-lg disabled:opacity-50"
              >
                {isSubmitting ? (
                  <>
                    <div className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
                    {modalMode === "create" ? "Đang tạo..." : "Đang cập nhật..."}
                  </>
                ) : modalMode === "create" ? (
                  "Lưu bệnh nhân"
                ) : (
                  "Cập nhật thay đổi"
                )}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
