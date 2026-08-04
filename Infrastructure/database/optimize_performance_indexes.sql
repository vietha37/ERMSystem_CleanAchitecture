-- Script tao Index toi uu hoa toc do truy van & sap xep danh sach trong ERMSystemHospitalDb

-- 1. Index cho scheduling.Appointments
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_scheduling_Appointments_CreatedAtUtc' AND object_id = OBJECT_ID('scheduling.Appointments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_scheduling_Appointments_CreatedAtUtc 
    ON scheduling.Appointments(CreatedAtUtc DESC) 
    INCLUDE (Id, Status, PatientId, DoctorProfileId, AppointmentStartUtc);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_scheduling_Appointments_Status_Start' AND object_id = OBJECT_ID('scheduling.Appointments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_scheduling_Appointments_Status_Start 
    ON scheduling.Appointments(Status, AppointmentStartUtc DESC);
END;

-- 2. Index cho emr.Encounters
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_emr_Encounters_CreatedAtUtc' AND object_id = OBJECT_ID('emr.Encounters'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_emr_Encounters_CreatedAtUtc 
    ON emr.Encounters(CreatedAtUtc DESC) 
    INCLUDE (Id, EncounterStatus, PatientId, DoctorProfileId, AppointmentId);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_emr_Encounters_PatientId' AND object_id = OBJECT_ID('emr.Encounters'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_emr_Encounters_PatientId 
    ON emr.Encounters(PatientId, CreatedAtUtc DESC);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_emr_Encounters_DoctorProfileId' AND object_id = OBJECT_ID('emr.Encounters'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_emr_Encounters_DoctorProfileId 
    ON emr.Encounters(DoctorProfileId, CreatedAtUtc DESC);
END;

-- 3. Index cho billing.Invoices
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_billing_Invoices_IssuedAtUtc' AND object_id = OBJECT_ID('billing.Invoices'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_billing_Invoices_IssuedAtUtc 
    ON billing.Invoices(IssuedAtUtc DESC) 
    INCLUDE (Id, InvoiceStatus, PatientId, EncounterId, TotalAmount);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_billing_Invoices_Status' AND object_id = OBJECT_ID('billing.Invoices'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_billing_Invoices_Status 
    ON billing.Invoices(InvoiceStatus, IssuedAtUtc DESC);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_billing_InvoiceItems_InvoiceId' AND object_id = OBJECT_ID('billing.InvoiceItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_billing_InvoiceItems_InvoiceId 
    ON billing.InvoiceItems(InvoiceId);
END;

-- 4. Index cho patient.Patients
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_patient_Patients_CreatedAtUtc' AND object_id = OBJECT_ID('patient.Patients'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_patient_Patients_CreatedAtUtc 
    ON patient.Patients(CreatedAtUtc DESC) 
    INCLUDE (Id, FullName, MedicalRecordNumber, Phone);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_patient_Patients_Search' AND object_id = OBJECT_ID('patient.Patients'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_patient_Patients_Search 
    ON patient.Patients(MedicalRecordNumber, Phone, FullName);
END;

-- 5. Index cho emr.OrderHeaders & Order tables
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_emr_OrderHeaders_EncounterId' AND object_id = OBJECT_ID('emr.OrderHeaders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_emr_OrderHeaders_EncounterId 
    ON emr.OrderHeaders(EncounterId);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_lab_LabOrders_OrderHeaderId' AND object_id = OBJECT_ID('lab.LabOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_lab_LabOrders_OrderHeaderId 
    ON lab.LabOrders(OrderHeaderId, OrderStatus);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_imaging_ImagingOrders_OrderHeaderId' AND object_id = OBJECT_ID('imaging.ImagingOrders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_imaging_ImagingOrders_OrderHeaderId 
    ON imaging.ImagingOrders(OrderHeaderId, OrderStatus);
END;

-- 6. Index cho pharmacy.Prescriptions & PrescriptionItems
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_pharmacy_Prescriptions_OrderHeaderId' AND object_id = OBJECT_ID('pharmacy.Prescriptions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_pharmacy_Prescriptions_OrderHeaderId 
    ON pharmacy.Prescriptions(OrderHeaderId, Status);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_pharmacy_PrescriptionItems_PrescriptionId' AND object_id = OBJECT_ID('pharmacy.PrescriptionItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_pharmacy_PrescriptionItems_PrescriptionId 
    ON pharmacy.PrescriptionItems(PrescriptionId);
END;

-- 7. Index cho notification.NotificationDeliveries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_notification_NotificationDeliveries_Status' AND object_id = OBJECT_ID('notification.NotificationDeliveries'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_notification_NotificationDeliveries_Status 
    ON notification.NotificationDeliveries(DeliveryStatus, LastAttemptAtUtc);
END;
