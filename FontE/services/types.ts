export type Id = string;

export type AppointmentStatus = "Pending" | "Completed" | "Cancelled";
export type UserRole = "Admin" | "Doctor" | "Receptionist" | "Patient";

export type PaginatedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
};

export type Patient = {
  id: Id;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  phone: string;
  address: string;
  createdAt: string;
};

export type Doctor = {
  id: Id;
  fullName: string;
  specialty: string;
};

export type Appointment = {
  id: Id;
  patientId: Id;
  doctorId: Id;
  appointmentDate: string;
  status: AppointmentStatus;
};

export type MedicalRecord = {
  id: Id;
  appointmentId: Id;
  symptoms: string;
  diagnosis: string;
  notes: string;
  createdAt?: string;
};

export type Medicine = {
  id: Id;
  name: string;
  description: string;
};

export type CreateMedicinePayload = {
  name: string;
  description: string;
};

export type UpdateMedicinePayload = CreateMedicinePayload;

export type PrescriptionItem = {
  id: Id;
  prescriptionId: Id;
  medicineId: Id;
  dosage: string;
  duration: string;
};

export type Prescription = {
  id: Id;
  medicalRecordId: Id;
  createdAt: string;
  items: PrescriptionItem[];
};

export type DashboardStats = {
  totalPatients: number;
  appointmentsToday: number;
  pendingAppointments: number;
  completedAppointments: number;
  cancelledAppointments: number;
  revisitAppointmentsToday: number;
  completionRatePercent: number;
  cancellationRatePercent: number;
  revisitRatePercent: number;
  totalInvoices: number;
  paidInvoices: number;
  issuedAmountThisMonth: number;
  collectedAmountThisMonth: number;
  outstandingBalanceAmount: number;
  collectionRatePercent: number;
  topDiagnoses: Record<string, number>;
};

export type DashboardTrendPoint = {
  label: string;
  patientsCount: number;
  appointmentsCount: number;
  prescriptionsCount: number;
};

export type DashboardTrends = {
  period: "daily" | "monthly";
  fromDate: string;
  toDate: string;
  currentPatientsTotal: number;
  currentAppointmentsTotal: number;
  currentPrescriptionsTotal: number;
  previousPatientsTotal: number;
  previousAppointmentsTotal: number;
  previousPrescriptionsTotal: number;
  points: DashboardTrendPoint[];
};

export type AppointmentNotification = {
  appointmentId: string;
  appointmentDate: string;
  patientName: string;
  doctorName: string;
  status: string;
  message: string;
};

export type TodayNotifications = {
  unreadCount: number;
  notifications: AppointmentNotification[];
};

export type NotificationDeliveryStatus = "Queued" | "Delivered" | "Failed" | "Skipped";

export type NotificationDelivery = {
  id: Id;
  outboxMessageId: Id;
  channelCode: string;
  recipient: string;
  deliveryStatus: NotificationDeliveryStatus;
  providerMessageId?: string | null;
  attemptCount: number;
  lastAttemptAtUtc?: string | null;
  deliveredAtUtc?: string | null;
  errorMessage?: string | null;
};

export type NotificationDeliveryListResult = {
  totalCount: number;
  items: NotificationDelivery[];
};

export type NotificationDeliverySummary = {
  totalCount: number;
  queuedCount: number;
  deliveredCount: number;
  failedCount: number;
  skippedCount: number;
  actionRequiredCount: number;
  staleQueuedCount: number;
  oldestQueuedAtUtc?: string | null;
  generatedAtUtc: string;
};

export type HospitalPatientPortalProfile = {
  patientId: Id;
  medicalRecordNumber: string;
  fullName: string;
  dateOfBirth: string;
  gender: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  portalStatus: string;
  activatedAtUtc: string;
};

export type HospitalPatientPortalAppointment = {
  appointmentId: Id;
  appointmentNumber: string;
  status: string;
  appointmentType: string;
  bookingChannel: string;
  appointmentStartLocal: string;
  appointmentEndLocal?: string | null;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  chiefComplaint?: string | null;
};

export type HospitalPatientPortalOverview = {
  profile: HospitalPatientPortalProfile;
  upcomingAppointments: HospitalPatientPortalAppointment[];
  recentAppointments: HospitalPatientPortalAppointment[];
  recentPrescriptions: HospitalPatientPortalPrescription[];
  recentClinicalOrders: HospitalPatientPortalClinicalOrder[];
  recentInvoices: HospitalPatientPortalInvoice[];
};

export type HospitalPatientVisitHistoryItem = {
  appointmentId: Id;
  appointmentNumber: string;
  appointmentStatus: string;
  appointmentType: string;
  bookingChannel: string;
  appointmentStartLocal: string;
  appointmentEndLocal?: string | null;
  checkInTimeLocal?: string | null;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  chiefComplaint?: string | null;
  encounterId?: Id | null;
  encounterNumber?: string | null;
  encounterStatus?: string | null;
  encounterStartedLocal?: string | null;
  encounterEndedLocal?: string | null;
  primaryDiagnosisName?: string | null;
  clinicalSummary?: string | null;
  prescriptionCount: number;
  clinicalOrderCount: number;
  invoiceCount: number;
  totalInvoiceAmount: number;
  totalPaidAmount: number;
  outstandingBalanceAmount: number;
};

export type HospitalPatientVisitHistoryResult = {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  items: HospitalPatientVisitHistoryItem[];
};

export type HospitalPatientPortalPrescriptionItem = {
  prescriptionItemId: Id;
  medicineName: string;
  drugCode: string;
  doseInstruction: string;
  route?: string | null;
  frequency?: string | null;
  durationDays?: number | null;
  quantity: number;
  unit?: string | null;
};

export type HospitalPatientPortalPrescription = {
  prescriptionId: Id;
  prescriptionNumber: string;
  status: string;
  encounterNumber: string;
  doctorName: string;
  specialtyName: string;
  primaryDiagnosisName?: string | null;
  totalItems: number;
  createdAtLocal: string;
  dispensedAtLocal?: string | null;
  notes?: string | null;
  items: HospitalPatientPortalPrescriptionItem[];
};

export type HospitalPatientPortalLabResultItem = {
  resultItemId: Id;
  analyteName: string;
  resultValue?: string | null;
  unit?: string | null;
  referenceRange?: string | null;
  abnormalFlag?: string | null;
};

export type HospitalPatientPortalClinicalOrder = {
  clinicalOrderId: Id;
  orderNumber: string;
  category: string;
  status: string;
  encounterNumber: string;
  serviceName: string;
  serviceCode: string;
  doctorName: string;
  requestedAtLocal: string;
  completedAtLocal?: string | null;
  summaryText?: string | null;
  findings?: string | null;
  impression?: string | null;
  reportUri?: string | null;
  resultItems: HospitalPatientPortalLabResultItem[];
};

export type HospitalPatientPortalInvoiceItem = {
  invoiceItemId: Id;
  itemType: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineAmount: number;
};

export type HospitalPatientPortalPayment = {
  paymentId: Id;
  paymentReference: string;
  paymentMethod: string;
  amount: number;
  paymentStatus: string;
  paidAtLocal?: string | null;
};

export type HospitalPatientPortalInvoice = {
  invoiceId: Id;
  invoiceNumber: string;
  invoiceStatus: string;
  encounterNumber?: string | null;
  totalAmount: number;
  paidAmount: number;
  balanceAmount: number;
  totalItems: number;
  totalPayments: number;
  issuedAtLocal: string;
  dueAtLocal?: string | null;
  items: HospitalPatientPortalInvoiceItem[];
  payments: HospitalPatientPortalPayment[];
};

export type HospitalAppointmentWorklistStatus =
  | "Scheduled"
  | "CheckedIn"
  | "Completed"
  | "Cancelled";

export type HospitalAppointmentWorklistItem = {
  appointmentId: Id;
  appointmentNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  patientPhone?: string | null;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  floorLabel?: string | null;
  roomLabel?: string | null;
  appointmentType: string;
  bookingChannel: string;
  status: HospitalAppointmentWorklistStatus;
  appointmentStartLocal: string;
  appointmentEndLocal?: string | null;
  chiefComplaint?: string | null;
  counterLabel?: string | null;
  queueNumber?: string | null;
  checkInTimeLocal?: string | null;
};

export type HospitalAppointmentWorklistQuery = {
  pageNumber?: number;
  pageSize?: number;
  status?: HospitalAppointmentWorklistStatus | "All";
  appointmentDate?: string;
  textSearch?: string;
};

export type HospitalAppointmentCheckInPayload = {
  counterLabel?: string;
};

export type HospitalAppointmentCancelPayload = {
  reason?: string;
};

export type HospitalAppointmentReschedulePayload = {
  preferredDate: string;
  preferredTime: string;
  reason?: string;
};

export type HospitalEncounterStatus = "InProgress" | "Finalized";

export type HospitalEncounterSummary = {
  encounterId: Id;
  encounterNumber: string;
  appointmentId?: Id | null;
  appointmentNumber?: string | null;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  appointmentStartLocal?: string | null;
  encounterStatus: HospitalEncounterStatus;
  primaryDiagnosisName?: string | null;
  summary?: string | null;
  startedAtLocal: string;
  endedAtLocal?: string | null;
  updatedAtLocal: string;
};

export type HospitalEncounterDetail = HospitalEncounterSummary & {
  encounterType: string;
  diagnosisCode?: string | null;
  diagnosisType?: string | null;
  subjective?: string | null;
  objective?: string | null;
  assessment?: string | null;
  carePlan?: string | null;
  heightCm?: number | null;
  weightKg?: number | null;
  temperatureC?: number | null;
  pulseRate?: number | null;
  respiratoryRate?: number | null;
  systolicBp?: number | null;
  diastolicBp?: number | null;
  oxygenSaturation?: number | null;
};

export type HospitalEncounterEligibleAppointment = {
  appointmentId: Id;
  appointmentNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  appointmentStartLocal: string;
  appointmentStatus: string;
  existingEncounterId?: Id | null;
  existingEncounterNumber?: string | null;
};

export type HospitalEncounterWorklistQuery = {
  pageNumber?: number;
  pageSize?: number;
  encounterStatus?: HospitalEncounterStatus | "All";
  appointmentDate?: string;
  textSearch?: string;
};

export type CreateHospitalEncounterPayload = {
  appointmentId: Id;
  diagnosisName: string;
  diagnosisCode?: string;
  diagnosisType?: string;
  encounterStatus?: HospitalEncounterStatus;
  summary?: string;
  subjective?: string;
  objective?: string;
  assessment?: string;
  carePlan?: string;
  heightCm?: number | null;
  weightKg?: number | null;
  temperatureC?: number | null;
  pulseRate?: number | null;
  respiratoryRate?: number | null;
  systolicBp?: number | null;
  diastolicBp?: number | null;
  oxygenSaturation?: number | null;
};

export type UpdateHospitalEncounterPayload = Omit<
  CreateHospitalEncounterPayload,
  "appointmentId"
>;

export type HospitalPrescriptionStatus = "Issued" | "Dispensed" | "Cancelled";

export type HospitalMedicineCatalog = {
  medicineId: Id;
  drugCode: string;
  name: string;
  genericName?: string | null;
  strength?: string | null;
  dosageForm?: string | null;
  unit?: string | null;
  isControlled: boolean;
};

export type HospitalPrescriptionSummary = {
  prescriptionId: Id;
  prescriptionNumber: string;
  status: HospitalPrescriptionStatus;
  latestDispensingStatus?: string | null;
  encounterId: Id;
  encounterNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  primaryDiagnosisName?: string | null;
  totalItems: number;
  createdAtLocal: string;
  dispensedAtLocal?: string | null;
  dispensedByUsername?: string | null;
  notes?: string | null;
};

export type HospitalPrescriptionItem = {
  prescriptionItemId: Id;
  medicineId: Id;
  drugCode: string;
  medicineName: string;
  genericName?: string | null;
  strength?: string | null;
  dosageForm?: string | null;
  unit?: string | null;
  doseInstruction: string;
  route?: string | null;
  frequency?: string | null;
  durationDays?: number | null;
  quantity: number;
  unitPrice?: number | null;
};

export type HospitalPrescriptionDetail = HospitalPrescriptionSummary & {
  latestDispensingId?: Id | null;
  dispensingNotes?: string | null;
  items: HospitalPrescriptionItem[];
};

export type HospitalPrescriptionEligibleEncounter = {
  encounterId: Id;
  encounterNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  encounterStatus: string;
  primaryDiagnosisName?: string | null;
  startedAtLocal: string;
  existingPrescriptionId?: Id | null;
  existingPrescriptionNumber?: string | null;
};

export type HospitalPrescriptionWorklistQuery = {
  pageNumber?: number;
  pageSize?: number;
  status?: HospitalPrescriptionStatus | "All";
  textSearch?: string;
};

export type CreateHospitalPrescriptionItemPayload = {
  medicineId: Id;
  doseInstruction: string;
  route?: string;
  frequency?: string;
  durationDays?: number | null;
  quantity: number;
};

export type CreateHospitalPrescriptionPayload = {
  encounterId: Id;
  status?: HospitalPrescriptionStatus;
  notes?: string;
  items: CreateHospitalPrescriptionItemPayload[];
};

export type DispenseHospitalPrescriptionPayload = {
  notes?: string;
};

export type HospitalClinicalOrderCategory = "Lab" | "Imaging";
export type HospitalClinicalOrderStatus = "Requested" | "Completed";

export type HospitalClinicalOrderCatalogItem = {
  serviceId: Id;
  category: HospitalClinicalOrderCategory;
  serviceCode: string;
  serviceName: string;
  extraLabel?: string | null;
};

export type HospitalClinicalOrderEligibleEncounter = {
  encounterId: Id;
  encounterNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  encounterStatus: string;
  primaryDiagnosisName?: string | null;
  startedAtLocal: string;
};

export type HospitalClinicalOrderSummary = {
  clinicalOrderId: Id;
  orderHeaderId: Id;
  orderNumber: string;
  category: HospitalClinicalOrderCategory;
  status: HospitalClinicalOrderStatus;
  encounterId: Id;
  encounterNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  serviceCode: string;
  serviceName: string;
  priorityCode?: string | null;
  requestedAtLocal: string;
  completedAtLocal?: string | null;
  orderedByUsername?: string | null;
  resultItemCount: number;
  summaryText?: string | null;
};

export type HospitalLabResultItem = {
  resultItemId: Id;
  analyteCode?: string | null;
  analyteName: string;
  resultValue?: string | null;
  unit?: string | null;
  referenceRange?: string | null;
  abnormalFlag?: string | null;
};

export type HospitalClinicalOrderDetail = HospitalClinicalOrderSummary & {
  specimenId?: Id | null;
  specimenCode?: string | null;
  specimenStatus?: string | null;
  collectedAtLocal?: string | null;
  receivedAtLocal?: string | null;
  findings?: string | null;
  impression?: string | null;
  reportUri?: string | null;
  signedByUsername?: string | null;
  signedAtLocal?: string | null;
  resultItems: HospitalLabResultItem[];
};

export type HospitalClinicalOrderWorklistQuery = {
  pageNumber?: number;
  pageSize?: number;
  category?: HospitalClinicalOrderCategory | "All";
  status?: HospitalClinicalOrderStatus | "All";
  textSearch?: string;
};

export type CreateHospitalClinicalOrderPayload = {
  encounterId: Id;
  category: HospitalClinicalOrderCategory;
  serviceId: Id;
  priorityCode?: string;
};

export type RecordHospitalLabResultItemPayload = {
  analyteCode?: string;
  analyteName: string;
  resultValue?: string;
  unit?: string;
  referenceRange?: string;
  abnormalFlag?: string;
};

export type RecordHospitalLabResultPayload = {
  specimenCode?: string;
  resultItems: RecordHospitalLabResultItemPayload[];
};

export type RecordHospitalImagingReportPayload = {
  findings?: string;
  impression?: string;
  reportUri?: string;
};

export type HospitalInvoiceStatus = "Issued" | "PartiallyPaid" | "Paid" | "Cancelled";

export type HospitalInvoiceSummary = {
  invoiceId: Id;
  invoiceNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  encounterId?: Id | null;
  encounterNumber?: string | null;
  invoiceStatus: HospitalInvoiceStatus;
  subtotalAmount: number;
  discountAmount: number;
  insuranceAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceAmount: number;
  totalItems: number;
  totalPayments: number;
  issuedAtLocal: string;
  dueAtLocal?: string | null;
};

export type HospitalInvoiceItem = {
  invoiceItemId: Id;
  serviceCatalogId?: Id | null;
  itemType: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineAmount: number;
  referenceType?: string | null;
  referenceId?: Id | null;
};

export type HospitalPayment = {
  paymentId: Id;
  paymentReference: string;
  paymentMethod: string;
  amount: number;
  paymentStatus: string;
  paidAtLocal?: string | null;
  receivedByUsername?: string | null;
  externalTransactionId?: string | null;
};

export type HospitalInvoiceDetail = HospitalInvoiceSummary & {
  doctorName?: string | null;
  specialtyName?: string | null;
  clinicName?: string | null;
  items: HospitalInvoiceItem[];
  payments: HospitalPayment[];
};

export type HospitalBillingEligibleEncounter = {
  encounterId: Id;
  encounterNumber: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  consultationFee: number;
  completedLabOrders: number;
  completedImagingOrders: number;
  billedPrescriptionItems: number;
  existingInvoiceId?: Id | null;
  existingInvoiceNumber?: string | null;
  startedAtLocal: string;
};

export type HospitalInvoiceWorklistQuery = {
  pageNumber?: number;
  pageSize?: number;
  invoiceStatus?: HospitalInvoiceStatus | "All";
  textSearch?: string;
};

export type CreateHospitalInvoicePayload = {
  encounterId: Id;
  discountAmount?: number;
  insuranceAmount?: number;
};

export type ReceiveHospitalPaymentPayload = {
  paymentMethod: string;
  paymentReference?: string;
  amount: number;
  externalTransactionId?: string;
};

export type HospitalDoctorWorklistItem = {
  appointmentId: Id;
  appointmentNumber: string;
  appointmentStatus: string;
  appointmentStartLocal: string;
  patientId: Id;
  patientName: string;
  medicalRecordNumber: string;
  doctorProfileId: Id;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  encounterId?: Id | null;
  encounterNumber?: string | null;
  encounterStatus?: string | null;
  primaryDiagnosisName?: string | null;
  prescriptionId?: Id | null;
  prescriptionNumber?: string | null;
  workflowStage: string;
};

export type HospitalDoctorWorklistResponse = {
  workDate: string;
  doctorProfileId?: Id | null;
  doctorName?: string | null;
  specialtyName?: string | null;
  isDoctorResolved: boolean;
  resolutionMessage?: string | null;
  totalAppointments: number;
  checkedInAppointments: number;
  inProgressEncounters: number;
  finalizedEncounters: number;
  issuedPrescriptions: number;
  items: HospitalDoctorWorklistItem[];
};

export type AuthResponse = {
  token: string;
  accessToken: string;
  refreshToken: string;
  username: string;
  role: string;
  expiresAt: string;
};

export type CreatePatientPayload = {
  fullName: string;
  dateOfBirth: string;
  gender: string;
  phone: string;
  address: string;
};

export type PatientRegisterPayload = CreatePatientPayload & {
  username: string;
  password: string;
};

export type UpdatePatientPayload = CreatePatientPayload;

export type CreateDoctorPayload = {
  fullName: string;
  specialty: string;
};

export type UpdateDoctorPayload = CreateDoctorPayload;

export type StaffUser = {
  id: Id;
  username: string;
  role: Exclude<UserRole, "Admin" | "Patient">;
};

export type CreateStaffUserPayload = {
  username: string;
  password: string;
  role: Exclude<UserRole, "Admin" | "Patient">;
};

export type UpdateStaffUserPayload = {
  username: string;
  role: Exclude<UserRole, "Admin" | "Patient">;
  password?: string;
};

export type AppointmentPayload = {
  patientId: Id;
  doctorId: Id;
  appointmentDate: string;
  status: AppointmentStatus;
};

export type CreateMedicalRecordPayload = {
  appointmentId: Id;
  symptoms: string;
  diagnosis: string;
  notes: string;
};

export type UpdateMedicalRecordPayload = CreateMedicalRecordPayload;

export type CreatePrescriptionPayload = {
  medicalRecordId: Id;
};

export type AddPrescriptionItemPayload = {
  medicineId: Id;
  dosage: string;
  duration: string;
};
