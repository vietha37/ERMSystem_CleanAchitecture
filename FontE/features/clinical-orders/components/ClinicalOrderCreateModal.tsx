"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import {
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderEligibleEncounter,
} from "../types";

interface ClinicalOrderCreateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  selectedEncounterId: string;
  onEncounterChange: (val: string) => void;
  eligibleEncounters: HospitalClinicalOrderEligibleEncounter[];
  createCategory: HospitalClinicalOrderCategory;
  onCategoryChange: (val: HospitalClinicalOrderCategory) => void;
  priorityCode: string;
  onPriorityChange: (val: string) => void;
  selectedServiceId: string;
  onServiceChange: (val: string) => void;
  availableServices: HospitalClinicalOrderCatalogItem[];
}

export function ClinicalOrderCreateModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  selectedEncounterId,
  onEncounterChange,
  eligibleEncounters,
  createCategory,
  onCategoryChange,
  priorityCode,
  onPriorityChange,
  selectedServiceId,
  onServiceChange,
  availableServices,
}: ClinicalOrderCreateModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Tạo chỉ định cận lâm sàng">
      <form className="space-y-4" onSubmit={onSubmit}>
        <div>
          <label className="mb-2 block text-sm font-semibold text-slate-700">
            Hồ sơ khám
          </label>
          <select
            value={selectedEncounterId}
            onChange={(event) => onEncounterChange(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          >
            <option value="">Chọn hồ sơ khám</option>
            {eligibleEncounters.map((encounter) => (
              <option key={encounter.encounterId} value={encounter.encounterId}>
                {encounter.encounterNumber} - {encounter.patientName} -{" "}
                {encounter.primaryDiagnosisName || encounter.specialtyName}
              </option>
            ))}
          </select>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Loại chỉ định
            </label>
            <select
              value={createCategory}
              onChange={(event) =>
                onCategoryChange(event.target.value as HospitalClinicalOrderCategory)
              }
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            >
              <option value="Lab">Xét nghiệm</option>
              <option value="Imaging">Chẩn đoán hình ảnh</option>
            </select>
          </div>

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Mức ưu tiên
            </label>
            <input
              type="text"
              value={priorityCode}
              onChange={(event) => onPriorityChange(event.target.value)}
              disabled={createCategory !== "Lab"}
              placeholder="Routine"
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100 disabled:bg-slate-100"
            />
          </div>
        </div>

        <div>
          <label className="mb-2 block text-sm font-semibold text-slate-700">Dịch vụ</label>
          <select
            value={selectedServiceId}
            onChange={(event) => onServiceChange(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          >
            <option value="">Chọn dịch vụ</option>
            {availableServices.map((service) => (
              <option key={service.serviceId} value={service.serviceId}>
                {service.serviceCode} - {service.serviceName}
                {service.extraLabel ? ` (${service.extraLabel})` : ""}
              </option>
            ))}
          </select>
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang tạo..." : "Lưu chỉ định"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
