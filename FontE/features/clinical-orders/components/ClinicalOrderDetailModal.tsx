"use client";

import { Modal } from "@/components/ui/Modal";
import { HospitalClinicalOrderDetail } from "../types";
import {
  formatDateTime,
  getCategoryLabel,
  getStatusLabel,
} from "../utils/clinicalOrderFormatters";

interface ClinicalOrderDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedDetail: HospitalClinicalOrderDetail | null;
}

function InfoTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-400">
        {label}
      </div>
      <div className="mt-2 text-sm font-medium text-slate-800">{value}</div>
    </div>
  );
}

export function ClinicalOrderDetailModal({
  isOpen,
  onClose,
  selectedDetail,
}: ClinicalOrderDetailModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Chi tiết chỉ định">
      {!selectedDetail ? (
        <div className="py-8 text-sm text-slate-500">Đang tải chi tiết...</div>
      ) : (
        <div className="space-y-4">
          <div className="grid gap-3 md:grid-cols-2">
            <InfoTile label="Mã chỉ định" value={selectedDetail.orderNumber} />
            <InfoTile label="Loại" value={getCategoryLabel(selectedDetail.category)} />
            <InfoTile label="Bệnh nhân" value={selectedDetail.patientName} />
            <InfoTile label="Mã bệnh án" value={selectedDetail.medicalRecordNumber} />
            <InfoTile label="Hồ sơ khám" value={selectedDetail.encounterNumber} />
            <InfoTile label="Dịch vụ" value={selectedDetail.serviceName} />
            <InfoTile label="Trạng thái" value={getStatusLabel(selectedDetail.status)} />
            <InfoTile
              label="Hoàn thành"
              value={formatDateTime(selectedDetail.completedAtLocal)}
            />
          </div>

          {selectedDetail.category === "Lab" ? (
            <div className="space-y-3">
              <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                <div className="text-sm font-semibold text-slate-800">Thông tin mẫu</div>
                <div className="mt-2 grid gap-2 text-sm text-slate-600 md:grid-cols-2">
                  <div>Mã mẫu: {selectedDetail.specimenCode || "--"}</div>
                  <div>Trạng thái: {selectedDetail.specimenStatus || "--"}</div>
                  <div>Lấy mẫu: {formatDateTime(selectedDetail.collectedAtLocal)}</div>
                  <div>Tiếp nhận: {formatDateTime(selectedDetail.receivedAtLocal)}</div>
                </div>
              </div>

              <div>
                <div className="mb-2 text-sm font-semibold text-slate-800">
                  Kết quả xét nghiệm
                </div>
                {selectedDetail.resultItems.length === 0 ? (
                  <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">
                    Chưa có dòng kết quả nào.
                  </div>
                ) : (
                  <div className="space-y-2">
                    {selectedDetail.resultItems.map((item) => (
                      <div
                        key={item.resultItemId}
                        className="rounded-2xl border border-slate-100 px-4 py-3"
                      >
                        <div className="font-semibold text-slate-900">
                          {item.analyteName}
                        </div>
                        <div className="mt-1 text-sm text-slate-600">
                          {item.resultValue || "--"} {item.unit || ""}
                        </div>
                        <div className="mt-1 text-xs text-slate-400">
                          Ref: {item.referenceRange || "--"} | Bất thường:{" "}
                          {item.abnormalFlag || "--"}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                <div className="text-sm font-semibold text-slate-800">Mô tả</div>
                <div className="mt-2 whitespace-pre-line text-sm leading-6 text-slate-600">
                  {selectedDetail.findings || "--"}
                </div>
              </div>
              <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                <div className="text-sm font-semibold text-slate-800">Kết luận</div>
                <div className="mt-2 whitespace-pre-line text-sm leading-6 text-slate-600">
                  {selectedDetail.impression || "--"}
                </div>
              </div>
              <div className="grid gap-3 md:grid-cols-2">
                <InfoTile label="Người ký" value={selectedDetail.signedByUsername || "--"} />
                <InfoTile
                  label="Thời điểm ký"
                  value={formatDateTime(selectedDetail.signedAtLocal)}
                />
                <InfoTile label="Liên kết báo cáo" value={selectedDetail.reportUri || "--"} />
                <InfoTile label="Tóm tắt" value={selectedDetail.summaryText || "--"} />
              </div>
            </div>
          )}
        </div>
      )}
    </Modal>
  );
}
