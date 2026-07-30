SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
GO

/*
  Du lieu mau nghiep vu cho he thong benh vien tu ERM.
  Uu tien ten goi va noi dung bang tieng Viet, giu nguyen ten thuoc va ma chuan khi can.
*/

MERGE [identity].Roles AS target
USING (VALUES
    ('Admin', N'Quan tri he thong', 1),
    ('Doctor', N'Bac si', 1),
    ('Cashier', N'Thu ngan', 1),
    ('Patient', N'Benh nhan', 1)
) AS source (Code, Name, IsSystemRole)
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        IsSystemRole = source.IsSystemRole
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, IsSystemRole)
    VALUES (source.Code, source.Name, source.IsSystemRole);
GO

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'OPD', N'Khối khám ngoại trú', N'Tiếp nhận và điều phối khám ngoại trú'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'OPD');

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'LAB', N'Trung tâm xét nghiệm', N'Vận hành các dịch vụ xét nghiệm và lấy mẫu'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'LAB');

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'IMG', N'Chẩn đoán hình ảnh', N'Các dịch vụ X-quang, CT, MRI, siêu âm'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'IMG');

INSERT INTO org.Departments (Id, DepartmentCode, Name, Description)
SELECT NEWID(), 'PHA', N'Nhà thuốc bệnh viện', N'Quản lý cấp phát thuốc và tồn kho'
WHERE NOT EXISTS (SELECT 1 FROM org.Departments WHERE DepartmentCode = 'PHA');
GO

DECLARE @OpdDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD');
DECLARE @LabDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'LAB');
DECLARE @ImgDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'IMG');
GO

MERGE org.Specialties AS target
USING (
    SELECT code.SpecialtyCode, code.Name, d.Id AS DepartmentId, code.IsActive
    FROM (VALUES
        ('CARD', N'Tim mach', 1),
        ('GASTRO', N'Tieu hoa - gan mat', 1),
        ('OBGYN', N'San phu khoa', 1),
        ('PED', N'Nhi khoa', 1),
        ('MSK', N'Co xuong khop', 1),
        ('NEURO', N'Than kinh', 1),
        ('GEN', N'Noi tong quat', 0)
    ) code (SpecialtyCode, Name, IsActive)
    CROSS JOIN org.Departments d
    WHERE d.DepartmentCode = 'OPD'
) AS source (SpecialtyCode, Name, DepartmentId, IsActive)
ON target.SpecialtyCode = source.SpecialtyCode
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        DepartmentId = source.DepartmentId,
        IsActive = source.IsActive
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, SpecialtyCode, Name, DepartmentId, IsActive)
    VALUES (NEWID(), source.SpecialtyCode, source.Name, source.DepartmentId, source.IsActive);
GO

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'CARD', N'Tim mạch', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD')
WHERE NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'CARD');

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'OBGYN', N'Sản phụ khoa', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD')
WHERE NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'OBGYN');

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'PED', N'Nhi khoa', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD')
WHERE NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'PED');

INSERT INTO org.Specialties (Id, SpecialtyCode, Name, DepartmentId)
SELECT NEWID(), 'GEN', N'Nội tổng quát', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD')
WHERE NOT EXISTS (SELECT 1 FROM org.Specialties WHERE SpecialtyCode = 'GEN');
GO

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'CLN-01', N'Phòng khám tổng quát 01', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD'), N'Tầng 2', N'P201'
WHERE NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'CLN-01');

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'CLN-02', N'Phòng khám tim mạch', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD'), N'Tầng 2', N'P205'
WHERE NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'CLN-02');

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'LAB-01', N'Khu lấy mẫu xét nghiệm', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'LAB'), N'Tầng 1', N'P103'
WHERE NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'LAB-01');

INSERT INTO org.Clinics (Id, ClinicCode, Name, DepartmentId, FloorLabel, RoomLabel)
SELECT NEWID(), 'IMG-01', N'Khu siêu âm và X-quang', (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'IMG'), N'Tầng 1', N'P110'
WHERE NOT EXISTS (SELECT 1 FROM org.Clinics WHERE ClinicCode = 'IMG-01');
GO

INSERT INTO [identity].Users (Id, Username, Email, PasswordHash, PrimaryRoleCode)
SELECT NEWID(), 'bs.nguyen.minh.ha', 'ha.nguyen@erm.local', 'SEED_ONLY_NO_LOGIN', 'Doctor'
WHERE NOT EXISTS (SELECT 1 FROM [identity].Users WHERE Username = 'bs.nguyen.minh.ha');

INSERT INTO [identity].Users (Id, Username, Email, PasswordHash, PrimaryRoleCode)
SELECT NEWID(), 'bs.le.thu.ha', 'ha.le@erm.local', 'SEED_ONLY_NO_LOGIN', 'Doctor'
WHERE NOT EXISTS (SELECT 1 FROM [identity].Users WHERE Username = 'bs.le.thu.ha');

INSERT INTO [identity].Users (Id, Username, Email, PasswordHash, PrimaryRoleCode)
SELECT NEWID(), 'bs.tran.huu.nam', 'nam.tran@erm.local', 'SEED_ONLY_NO_LOGIN', 'Doctor'
WHERE NOT EXISTS (SELECT 1 FROM [identity].Users WHERE Username = 'bs.tran.huu.nam');
GO

INSERT INTO [identity].UserRoles (UserId, RoleCode, GrantedByUserId)
SELECT u.Id, 'Doctor', NULL
FROM [identity].Users u
WHERE u.Username = 'bs.nguyen.minh.ha'
  AND NOT EXISTS (
      SELECT 1
      FROM [identity].UserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleCode = 'Doctor'
  );

INSERT INTO [identity].UserRoles (UserId, RoleCode, GrantedByUserId)
SELECT u.Id, 'Doctor', NULL
FROM [identity].Users u
WHERE u.Username = 'bs.le.thu.ha'
  AND NOT EXISTS (
      SELECT 1
      FROM [identity].UserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleCode = 'Doctor'
  );

INSERT INTO [identity].UserRoles (UserId, RoleCode, GrantedByUserId)
SELECT u.Id, 'Doctor', NULL
FROM [identity].Users u
WHERE u.Username = 'bs.tran.huu.nam'
  AND NOT EXISTS (
      SELECT 1
      FROM [identity].UserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleCode = 'Doctor'
  );
GO

INSERT INTO org.StaffProfiles (Id, UserId, StaffCode, FullName, DepartmentId, Phone, Email, HireDate)
SELECT NEWID(), u.Id, 'BS001', N'PGS.TS.BS Nguyễn Minh Hà', d.Id, '0901112233', 'ha.nguyen@erm.local', '2015-03-01'
FROM [identity].Users u
CROSS JOIN org.Departments d
WHERE u.Username = 'bs.nguyen.minh.ha'
  AND d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.StaffProfiles WHERE StaffCode = 'BS001');

INSERT INTO org.StaffProfiles (Id, UserId, StaffCode, FullName, DepartmentId, Phone, Email, HireDate)
SELECT NEWID(), u.Id, 'BS002', N'TS.BS Lê Thu Hà', d.Id, '0902223344', 'ha.le@erm.local', '2017-06-15'
FROM [identity].Users u
CROSS JOIN org.Departments d
WHERE u.Username = 'bs.le.thu.ha'
  AND d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.StaffProfiles WHERE StaffCode = 'BS002');

INSERT INTO org.StaffProfiles (Id, UserId, StaffCode, FullName, DepartmentId, Phone, Email, HireDate)
SELECT NEWID(), u.Id, 'BS003', N'ThS.BS Trần Hữu Nam', d.Id, '0903334455', 'nam.tran@erm.local', '2018-09-20'
FROM [identity].Users u
CROSS JOIN org.Departments d
WHERE u.Username = 'bs.tran.huu.nam'
  AND d.DepartmentCode = 'OPD'
  AND NOT EXISTS (SELECT 1 FROM org.StaffProfiles WHERE StaffCode = 'BS003');
GO

INSERT INTO org.DoctorProfiles (Id, StaffProfileId, SpecialtyId, LicenseNumber, Biography, YearsOfExperience, ConsultationFee, IsBookable)
SELECT NEWID(), sp.Id, s.Id, 'CARD-001', N'Chuyên gia tim mạch can thiệp và theo dõi tăng huyết áp.', 15, 450000, 1
FROM org.StaffProfiles sp
CROSS JOIN org.Specialties s
WHERE sp.StaffCode = 'BS001'
  AND s.SpecialtyCode = 'CARD'
  AND NOT EXISTS (SELECT 1 FROM org.DoctorProfiles WHERE StaffProfileId = sp.Id);

INSERT INTO org.DoctorProfiles (Id, StaffProfileId, SpecialtyId, LicenseNumber, Biography, YearsOfExperience, ConsultationFee, IsBookable)
SELECT NEWID(), sp.Id, s.Id, 'OBGYN-001', N'Phụ trách theo dõi thai kỳ và nội tiết sản phụ khoa.', 12, 420000, 1
FROM org.StaffProfiles sp
CROSS JOIN org.Specialties s
WHERE sp.StaffCode = 'BS002'
  AND s.SpecialtyCode = 'OBGYN'
  AND NOT EXISTS (SELECT 1 FROM org.DoctorProfiles WHERE StaffProfileId = sp.Id);

INSERT INTO org.DoctorProfiles (Id, StaffProfileId, SpecialtyId, LicenseNumber, Biography, YearsOfExperience, ConsultationFee, IsBookable)
SELECT NEWID(), sp.Id, s.Id, 'GEN-001', N'Khám nội tổng quát và quản lý bệnh mạn tính cho gia đình.', 10, 250000, 1
FROM org.StaffProfiles sp
CROSS JOIN org.Specialties s
WHERE sp.StaffCode = 'BS003'
  AND s.SpecialtyCode = 'GEN'
  AND NOT EXISTS (SELECT 1 FROM org.DoctorProfiles WHERE StaffProfileId = sp.Id);
GO

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 1, '08:00', '11:30', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-02'
WHERE sp.StaffCode = 'BS001'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 1 AND ds.StartTime = '08:00'
  );

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 4, '13:30', '17:00', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-02'
WHERE sp.StaffCode = 'BS001'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 4 AND ds.StartTime = '13:30'
  );

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 2, '08:00', '11:30', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-01'
WHERE sp.StaffCode = 'BS002'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 2 AND ds.StartTime = '08:00'
  );

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 5, '08:00', '11:30', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-01'
WHERE sp.StaffCode = 'BS002'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 5 AND ds.StartTime = '08:00'
  );

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 1, '13:30', '17:00', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-01'
WHERE sp.StaffCode = 'BS003'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 1 AND ds.StartTime = '13:30'
  );

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 3, '13:30', '17:00', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-01'
WHERE sp.StaffCode = 'BS003'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 3 AND ds.StartTime = '13:30'
  );

INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(), dp.Id, c.Id, 6, '08:00', '11:30', 30, '2026-01-01', NULL, 1
FROM org.DoctorProfiles dp
JOIN org.StaffProfiles sp ON sp.Id = dp.StaffProfileId
JOIN org.Clinics c ON c.ClinicCode = 'CLN-01'
WHERE sp.StaffCode = 'BS003'
  AND NOT EXISTS (
      SELECT 1 FROM org.DoctorSchedules ds
      WHERE ds.DoctorProfileId = dp.Id AND ds.ClinicId = c.Id AND ds.DayOfWeek = 6 AND ds.StartTime = '08:00'
  );
GO

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'CONS-GEN', N'Khám nội tổng quát', N'Consultation', 250000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'CONS-GEN');

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'CONS-CARD', N'Khám tim mạch chuyên sâu', N'Consultation', 450000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'CONS-CARD');

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'LAB-CBC', N'Tổng phân tích tế bào máu ngoại vi', N'Laboratory', 120000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'LAB-CBC');

INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice)
SELECT NEWID(), 'IMG-US-ABD', N'Siêu âm ổ bụng tổng quát', N'Imaging', 280000
WHERE NOT EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE ServiceCode = 'IMG-US-ABD');
GO

INSERT INTO lab.LabServices (Id, ServiceCode, Name, SampleType, UnitPrice)
SELECT NEWID(), 'LAB-CBC', N'Tổng phân tích tế bào máu ngoại vi', N'Máu toàn phần', 120000
WHERE NOT EXISTS (SELECT 1 FROM lab.LabServices WHERE ServiceCode = 'LAB-CBC');

INSERT INTO lab.LabServices (Id, ServiceCode, Name, SampleType, UnitPrice)
SELECT NEWID(), 'LAB-GLU', N'Đường huyết lúc đói', N'Huyết thanh', 60000
WHERE NOT EXISTS (SELECT 1 FROM lab.LabServices WHERE ServiceCode = 'LAB-GLU');
GO

INSERT INTO imaging.ImagingServices (Id, ServiceCode, Name, Modality, UnitPrice)
SELECT NEWID(), 'IMG-US-ABD', N'Siêu âm ổ bụng tổng quát', N'Ultrasound', 280000
WHERE NOT EXISTS (SELECT 1 FROM imaging.ImagingServices WHERE ServiceCode = 'IMG-US-ABD');

INSERT INTO imaging.ImagingServices (Id, ServiceCode, Name, Modality, UnitPrice)
SELECT NEWID(), 'IMG-XR-CHEST', N'X-quang ngực thẳng', N'XRay', 180000
WHERE NOT EXISTS (SELECT 1 FROM imaging.ImagingServices WHERE ServiceCode = 'IMG-XR-CHEST');
GO

INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit)
SELECT NEWID(), 'MED-PAR-500', N'Paracetamol 500mg', N'Paracetamol', N'500mg', N'Viên nén', N'Viên'
WHERE NOT EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE DrugCode = 'MED-PAR-500');

INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit)
SELECT NEWID(), 'MED-AMO-500', N'Amoxicillin 500mg', N'Amoxicillin', N'500mg', N'Viên nang', N'Viên'
WHERE NOT EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE DrugCode = 'MED-AMO-500');

INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit)
SELECT NEWID(), 'MED-AMO-250', N'Amoxicillin 250mg', N'Amoxicillin', N'250mg', N'Viên nang', N'Viên'
WHERE NOT EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE DrugCode = 'MED-AMO-250');
GO


INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_CREATED', 'Email', N'Xac nhan lich hen {{AppointmentNumber}}', N'Xin chao {{PatientName}}, lich hen {{AppointmentNumber}} voi {{DoctorName}} tai {{ClinicName}} da duoc tiep nhan vao luc {{AppointmentStartLocal}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'APPOINTMENT_CREATED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_CREATED', 'SMS', NULL, N'Lich hen {{AppointmentNumber}} voi {{DoctorName}} tai {{ClinicName}} da duoc tiep nhan luc {{AppointmentStartLocal}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'APPOINTMENT_CREATED' AND ChannelCode = 'SMS'
);
GO

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_UPDATED', 'Email', N'Cap nhat lich hen {{AppointmentNumber}}', N'Xin chao {{PatientName}}, lich hen {{AppointmentNumber}} voi {{DoctorName}} tai {{ClinicName}} da duoc cap nhat sang trang thai {{CurrentStatus}}. Thoi gian hen: {{AppointmentStartLocal}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'APPOINTMENT_UPDATED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_UPDATED', 'SMS', NULL, N'Lich hen {{AppointmentNumber}} da duoc cap nhat trang thai {{CurrentStatus}}. Thoi gian hen: {{AppointmentStartLocal}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'APPOINTMENT_UPDATED' AND ChannelCode = 'SMS'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_CANCELLED', 'Email', N'Huy lich hen {{AppointmentNumber}}', N'Xin chao {{PatientName}}, lich hen {{AppointmentNumber}} voi {{DoctorName}} tai {{ClinicName}} da duoc huy. Neu can, vui long dat lich moi.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'APPOINTMENT_CANCELLED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'APPOINTMENT_CANCELLED', 'SMS', NULL, N'Lich hen {{AppointmentNumber}} da duoc huy. Vui long dat lich moi neu can.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'APPOINTMENT_CANCELLED' AND ChannelCode = 'SMS'
);
GO

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'MEDICAL_RECORD_FINALIZED', 'Email', N'Ho so kham {{AppointmentNumber}} da hoan tat', N'Xin chao {{PatientName}}, ho so kham {{AppointmentNumber}} voi chan doan {{DiagnosisName}} da duoc bac si {{DoctorName}} hoan tat.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'MEDICAL_RECORD_FINALIZED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'MEDICAL_RECORD_FINALIZED', 'SMS', NULL, N'Ho so kham {{AppointmentNumber}} da hoan tat voi chan doan {{DiagnosisName}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'MEDICAL_RECORD_FINALIZED' AND ChannelCode = 'SMS'
);
GO

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'PRESCRIPTION_ISSUED', 'Email', N'Toa thuoc {{PrescriptionNumber}} da duoc phat hanh', N'Xin chao {{PatientName}}, toa thuoc {{PrescriptionNumber}} cho lan kham {{EncounterNumber}} da duoc phat hanh.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'PRESCRIPTION_ISSUED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'PRESCRIPTION_ISSUED', 'SMS', NULL, N'Toa thuoc {{PrescriptionNumber}} da duoc phat hanh cho lan kham {{EncounterNumber}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'PRESCRIPTION_ISSUED' AND ChannelCode = 'SMS'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'PRESCRIPTION_DISPENSED', 'Email', N'Toa thuoc {{PrescriptionNumber}} da duoc cap', N'Xin chao {{PatientName}}, toa thuoc {{PrescriptionNumber}} da duoc cap thuoc thanh cong.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'PRESCRIPTION_DISPENSED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'PRESCRIPTION_DISPENSED', 'SMS', NULL, N'Toa thuoc {{PrescriptionNumber}} da duoc cap thuoc.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'PRESCRIPTION_DISPENSED' AND ChannelCode = 'SMS'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'INVOICE_ISSUED', 'Email', N'Hoa don {{InvoiceNumber}} da duoc lap', N'Xin chao {{PatientName}}, hoa don {{InvoiceNumber}} da duoc lap cho ho so cua quy khach.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'INVOICE_ISSUED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'INVOICE_ISSUED', 'SMS', NULL, N'Hoa don {{InvoiceNumber}} da duoc lap. Vui long kiem tra de thanh toan.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'INVOICE_ISSUED' AND ChannelCode = 'SMS'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'INVOICE_PAYMENT_RECEIVED', 'Email', N'Da ghi nhan thanh toan cho hoa don {{InvoiceNumber}}', N'Xin chao {{PatientName}}, he thong da ghi nhan thanh toan {{Amount}} cho hoa don {{InvoiceNumber}} qua {{PaymentMethod}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'INVOICE_PAYMENT_RECEIVED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'INVOICE_PAYMENT_RECEIVED', 'SMS', NULL, N'Da ghi nhan thanh toan {{Amount}} cho hoa don {{InvoiceNumber}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'INVOICE_PAYMENT_RECEIVED' AND ChannelCode = 'SMS'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'INVOICE_REFUNDED', 'Email', N'Da hoan tien cho hoa don {{InvoiceNumber}}', N'Xin chao {{PatientName}}, he thong da ghi nhan hoan tien {{Amount}} cho hoa don {{InvoiceNumber}} qua {{PaymentMethod}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'INVOICE_REFUNDED' AND ChannelCode = 'Email'
);

INSERT INTO notification.NotificationTemplates (Id, TemplateCode, ChannelCode, SubjectTemplate, BodyTemplate, IsActive)
SELECT NEWID(), 'INVOICE_REFUNDED', 'SMS', NULL, N'Da hoan tien {{Amount}} cho hoa don {{InvoiceNumber}}.', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM notification.NotificationTemplates
    WHERE TemplateCode = 'INVOICE_REFUNDED' AND ChannelCode = 'SMS'
);
GO
