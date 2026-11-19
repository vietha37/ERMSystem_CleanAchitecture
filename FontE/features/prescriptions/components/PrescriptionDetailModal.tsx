"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { HospitalPrescriptionDetail } from "../types";
import { formatDateTime } from "../utils/prescriptionFormatters";

interface PrescriptionDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedPrescription: HospitalPrescriptionDetail | null;
}

function InfoLine({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-slate-500">{label}</p>
      <p className="mt-1 font-semibold text-slate-900">{value}</p>
    </div>
  );
}

export function PrescriptionDetailModal({
  isOpen,
  onClose,
  selectedPrescription,
}: PrescriptionDetailModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Chi tiết đơn thuốc">
      {selectedPrescription ? (
        <div className="space-y-5">
          <div className="grid gap-3 rounded-2xl border border-cyan-100 bg-cyan-50/70 p-4 text-sm md:grid-cols-2">
            <InfoLine label="Đơn thuốc" value={selectedPrescription.prescriptionNumber} />
            <InfoLine label="Hồ sơ khám" value={selectedPrescription.encounterNumber} />
            <InfoLine label="Bệnh nhân" value={selectedPrescription.patientName} />
            <InfoLine label="Bác sĩ" value={selectedPrescription.doctorName} />
            <InfoLine
              label="Chẩn đoán"
              value={selectedPrescription.primaryDiagnosisName || "--"}
            />
            <InfoLine
              label="Ngày tạo"
              value={formatDateTime(selectedPrescription.createdAtLocal)}
            />
          </div>

          {(selectedPrescription.warningDetails.length > 0 ||
            selectedPrescription.warnings.length > 0) && (
            <div className="rounded-2xl border border-amber-200 bg-amber-50/80 p-4">
              <p className="text-sm font-bold text-amber-900">Cảnh báo lâm sàng</p>
              <div className="mt-3 space-y-2">
                {selectedPrescription.warningDetails.length > 0
                  ? selectedPrescription.warningDetails.map((warning) => (
                      <div
                        key={warning.code}
                        className="rounded-2xl border border-amber-100 bg-white/80 px-4 py-3 text-sm leading-6 text-amber-900"
                      >
                        <div className="flex flex-wrap items-center gap-2">
                          <span
                            className={`rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-[0.16em] ${
                              warning.severity === "critical"
                                ? "bg-red-100 text-red-700"
                                : "bg-amber-100 text-amber-700"
                            }`}
                          >
                            {warning.severity === "critical" ? "Nghiêm trọng" : "Cảnh báo"}
                          </span>
                          <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600">
                            {warning.category}
                          </span>
                        </div>
                        <p className="mt-3">{warning.message}</p>
                        {warning.relatedMedicines.length > 0 && (
                          <p className="mt-2 text-xs text-amber-800">
                            Thuốc liên quan: {warning.relatedMedicines.join(", ")}
                          </p>
                        )}
                        {warning.recommendation && (
                          <p className="mt-2 text-xs text-slate-600">
                            Khuyến nghị: {warning.recommendation}
                          </p>
                        )}
                      </div>
                    ))
                  : selectedPrescription.warnings.map((warning, index) => (
                      <div
                        key={`${index}-${warning}`}
                        className="rounded-2xl border border-amber-100 bg-white/80 px-4 py-3 text-sm leading-6 text-amber-900"
                      >
                        {warning}
                      </div>
                    ))}
              </div>
            </div>
          )}

          <div className="overflow-hidden rounded-2xl border border-slate-100">
            <table className="w-full border-collapse text-left text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 font-semibold text-slate-700">Thuốc</th>
                  <th className="px-4 py-3 font-semibold text-slate-700">Liều dùng</th>
                  <th className="px-4 py-3 font-semibold text-slate-700">Tần suất</th>
                  <th className="px-4 py-3 font-semibold text-slate-700">Số lượng</th>
                </tr>
              </thead>
              <tbody>
                {selectedPrescription.items.map((item) => (
                  <tr key={item.prescriptionItemId} className="border-t border-slate-100">
                    <td className="px-4 py-3">
                      <div className="font-semibold text-slate-900">{item.medicineName}</div>
                      <div className="mt-1 text-xs text-slate-500">
                        {item.drugCode}
                        {item.strength ? ` / ${item.strength}` : ""}
                        {item.dosageForm ? ` / ${item.dosageForm}` : ""}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-600">{item.doseInstruction}</td>
                    <td className="px-4 py-3 text-slate-600">
                      {[
                        item.route,
                        item.frequency,
                        item.durationDays ? `${item.durationDays} ngày` : null,
                      ]
                        .filter(Boolean)
                        .join(" / ") || "--"}
                    </td>
                    <td className="px-4 py-3 text-slate-600">
                      {item.quantity} {item.unit || ""}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex justify-end">
            <Button
              variant="secondary"
              onClick={onClose}
            >
              Đóng
            </Button>
          </div>
        </div>
      ) : null}
    </Modal>
  );
}
