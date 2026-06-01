SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
GO

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'OPD', N'Khoi kham ngoai tru', N'Tiep nhan va dieu phoi kham ngoai tru'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'OPD');

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'LAB', N'Trung tam xet nghiem', N'Van hanh cac dich vu xet nghiem va lay mau'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'LAB');

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'IMG', N'Chan doan hinh anh', N'Cac dich vu X-quang, CT, MRI, sieu am'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'IMG');

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'PHA', N'Nha thuoc benh vien', N'Quan ly cap phat thuoc va ton kho'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'PHA');
GO

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'CARD', N'Tim mach', d.Id
FROM org.Departments d
WHERE d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'CARD');

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'OBGYN', N'San phu khoa', d.Id
FROM org.Departments d
WHERE d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'OBGYN');

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'PED', N'Nhi khoa', d.Id
FROM org.Departments d
WHERE d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'PED');

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'GEN', N'Noi tong quat', d.Id
FROM org.Departments d
WHERE d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'GEN');
GO

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'CLN-01', N'Phong kham tong quat 01', d.Id, N'Tang 2', N'P201'
FROM org.Departments d
WHERE d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'CLN-01');

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'CLN-02', N'Phong kham tim mach', d.Id, N'Tang 2', N'P205'
FROM org.Departments d
WHERE d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'CLN-02');

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'LAB-01', N'Khu lay mau xet nghiem', d.Id, N'Tang 1', N'P103'
FROM org.Departments d
WHERE d.DepartmentCode = 'LAB'
  AND NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'LAB-01');

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'IMG-01', N'Khu sieu am va X-quang', d.Id, N'Tang 1', N'P110'
FROM org.Departments d
WHERE d.DepartmentCode = 'IMG'
  AND NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'IMG-01');
GO

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'CONS-GEN', N'Kham noi tong quat', N'Consultation', 250000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'CONS-GEN');

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'CONS-CARD', N'Kham tim mach chuyen sau', N'Consultation', 450000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'CONS-CARD');

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'LAB-CBC', N'Tong phan tich te bao mau ngoai vi', N'Laboratory', 120000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'LAB-CBC');

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'IMG-US-ABD', N'Sieu am o bung tong quat', N'Imaging', 280000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'IMG-US-ABD');
GO

INSERT INTO lab.LabServices (Id, ServiceCode, Name, SampleType, UnitPrice)
SELECT NEWID(), 'LAB-CBC', N'Tong phan tich te bao mau ngoai vi', N'Mau toan phan', 120000
WHERE NOT EXISTS (SELECT 1 FROM lab.LabServices WHERE ServiceCode = 'LAB-CBC');

INSERT INTO lab.LabServices (Id, ServiceCode, Name, SampleType, UnitPrice)
SELECT NEWID(), 'LAB-GLU', N'Duong huyet luc doi', N'Huyet thanh', 60000
WHERE NOT EXISTS (SELECT 1 FROM lab.LabServices WHERE ServiceCode = 'LAB-GLU');
GO

INSERT INTO imaging.ImagingServices (Id, ServiceCode, Name, Modality, UnitPrice)
SELECT NEWID(), 'IMG-US-ABD', N'Sieu am o bung tong quat', N'Ultrasound', 280000
WHERE NOT EXISTS (SELECT 1 FROM imaging.ImagingServices WHERE ServiceCode = 'IMG-US-ABD');

INSERT INTO imaging.ImagingServices (Id, ServiceCode, Name, Modality, UnitPrice)
SELECT NEWID(), 'IMG-XR-CHEST', N'X-quang nguc thang', N'XRay', 180000
WHERE NOT EXISTS (SELECT 1 FROM imaging.ImagingServices WHERE ServiceCode = 'IMG-XR-CHEST');
GO

INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit)
SELECT NEWID(), 'MED-PAR-500', N'Paracetamol 500mg', N'Paracetamol', N'500mg', N'Vien nen', N'Vien'
WHERE NOT EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE DrugCode = 'MED-PAR-500');

INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit)
SELECT NEWID(), 'MED-AMO-500', N'Amoxicillin 500mg', N'Amoxicillin', N'500mg', N'Vien nang', N'Vien'
WHERE NOT EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE DrugCode = 'MED-AMO-500');

INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit)
SELECT NEWID(), 'MED-AMO-250', N'Amoxicillin 250mg', N'Amoxicillin', N'250mg', N'Vien nang', N'Vien'
WHERE NOT EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE DrugCode = 'MED-AMO-250');
GO

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_CREATED', 'Email', N'Xac nhan lich hen {{AppointmentNumber}}', N'Xin chao {{PatientName}}, lich hen {{AppointmentNumber}} voi {{DoctorName}} tai {{ClinicName}} da duoc tiep nhan vao luc {{AppointmentStartLocal}}.', 1
WHERE NOT EXISTS (SELECT 1 FROM notification.NotificationTemplates WHERE TemplateCode = 'APPOINTMENT_CREATED' AND ChannelCode = 'Email');

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_CREATED', 'SMS', NULL, N'Lich hen {{AppointmentNumber}} voi {{DoctorName}} tai {{ClinicName}} da duoc tiep nhan luc {{AppointmentStartLocal}}.', 1
WHERE NOT EXISTS (SELECT 1 FROM notification.NotificationTemplates WHERE TemplateCode = 'APPOINTMENT_CREATED' AND ChannelCode = 'SMS');
GO
