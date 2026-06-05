SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @PasswordHash NVARCHAR(255) = '$2a$11$Um9s9E6FMMXBU96niwJTVugama.vZhZDaNyWQPc5fC0Bp90krJIHq';
DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();

DECLARE @AdminUserId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @DoctorUserId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @CashierFrontDeskUserId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @PatientUserId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @CashierOpsUserId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';
DECLARE @CashierPharmacyUserId UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666666';
DECLARE @CashierLabUserId UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777777';
DECLARE @CashierUserId UNIQUEIDENTIFIER = '88888888-8888-8888-8888-888888888888';

DECLARE @PortalPatientId UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1';
DECLARE @SeniorPatientId UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2';
DECLARE @ChildPatientId UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3';

DECLARE @DoctorSeedId UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1';
DECLARE @DoctorSupportId UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2';

DECLARE @AppointmentPendingId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-ccccccccccc1';
DECLARE @AppointmentCompleted1Id UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-ccccccccccc2';
DECLARE @AppointmentCompleted2Id UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-ccccccccccc3';
DECLARE @AppointmentCancelledId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-ccccccccccc4';

DECLARE @MedicalRecord1Id UNIQUEIDENTIFIER = 'dddddddd-dddd-dddd-dddd-ddddddddddd1';
DECLARE @MedicalRecord2Id UNIQUEIDENTIFIER = 'dddddddd-dddd-dddd-dddd-ddddddddddd2';

DECLARE @MedicineAId UNIQUEIDENTIFIER = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1';
DECLARE @MedicineBId UNIQUEIDENTIFIER = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2';
DECLARE @MedicineCId UNIQUEIDENTIFIER = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3';
DECLARE @MedicineDId UNIQUEIDENTIFIER = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee4';

DECLARE @Prescription1Id UNIQUEIDENTIFIER = 'ffffffff-ffff-ffff-ffff-fffffffffff1';
DECLARE @Prescription2Id UNIQUEIDENTIFIER = 'ffffffff-ffff-ffff-ffff-fffffffffff2';
DECLARE @PrescriptionItem1Id UNIQUEIDENTIFIER = '99999999-0000-0000-0000-000000000001';
DECLARE @PrescriptionItem2Id UNIQUEIDENTIFIER = '99999999-0000-0000-0000-000000000002';
DECLARE @PrescriptionItem3Id UNIQUEIDENTIFIER = '99999999-0000-0000-0000-000000000003';

MERGE dbo.AppUsers AS target
USING (VALUES
    (@AdminUserId, 'admin00', N'Pham Hoang An', @PasswordHash, 'Admin'),
    (@DoctorUserId, 'doctor00', N'Tran Anh Khoa', @PasswordHash, 'Doctor'),
    (@CashierFrontDeskUserId, 'cashier04', N'Le Thu Hang', @PasswordHash, 'Cashier'),
    (@PatientUserId, 'patient00', N'Nguyen Minh Anh', @PasswordHash, 'Patient'),
    (@CashierOpsUserId, 'cashier01', N'Nguyen Ha My', @PasswordHash, 'Cashier'),
    (@CashierPharmacyUserId, 'cashier02', N'Vo Thanh Tung', @PasswordHash, 'Cashier'),
    (@CashierLabUserId, 'cashier03', N'Pham Kien Minh', @PasswordHash, 'Cashier'),
    (@CashierUserId, 'cashier00', N'Tran Thu Ha', @PasswordHash, 'Cashier')
) AS source (Id, Username, Name, PasswordHash, Role)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Username = source.Username,
        Name = source.Name,
        PasswordHash = source.PasswordHash,
        Role = source.Role
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Username, Name, PasswordHash, Role)
    VALUES (source.Id, source.Username, source.Name, source.PasswordHash, source.Role);

MERGE dbo.Doctors AS target
USING (VALUES
    (@DoctorSeedId, N'Tran Anh Khoa', N'General Medicine'),
    (@DoctorSupportId, N'Nguyen Bao Han', N'Cardiology')
) AS source (Id, FullName, Specialty)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        FullName = source.FullName,
        Specialty = source.Specialty
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, FullName, Specialty)
    VALUES (source.Id, source.FullName, source.Specialty);

MERGE dbo.Patients AS target
USING (VALUES
    (@PortalPatientId, N'Nguyen Minh Anh', CAST('1994-05-12' AS DATETIME2), N'Female', '0909000001', N'12 Nguyen Hue, Quan 1, TP.HCM', DATEADD(DAY, -180, @NowUtc), @PatientUserId, N'Nguyen Van Nam', '0909000901', N'Spouse'),
    (@SeniorPatientId, N'Le Van Binh', CAST('1968-10-20' AS DATETIME2), N'Male', '0909000002', N'88 Cach Mang Thang 8, Quan 3, TP.HCM', DATEADD(DAY, -120, @NowUtc), NULL, N'Le Thu Trang', '0909000902', N'Daughter'),
    (@ChildPatientId, N'Tran Gia Han', CAST('2017-02-15' AS DATETIME2), N'Female', '0909000003', N'26 Phan Xich Long, Phu Nhuan, TP.HCM', DATEADD(DAY, -90, @NowUtc), NULL, N'Tran Hoai Thu', '0909000903', N'Mother')
) AS source (Id, FullName, DateOfBirth, Gender, Phone, Address, CreatedAt, AppUserId, EmergencyContactName, EmergencyContactPhone, EmergencyContactRelationship)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        FullName = source.FullName,
        DateOfBirth = source.DateOfBirth,
        Gender = source.Gender,
        Phone = source.Phone,
        Address = source.Address,
        AppUserId = source.AppUserId,
        EmergencyContactName = source.EmergencyContactName,
        EmergencyContactPhone = source.EmergencyContactPhone,
        EmergencyContactRelationship = source.EmergencyContactRelationship
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, FullName, DateOfBirth, Gender, Phone, Address, CreatedAt, AppUserId, EmergencyContactName, EmergencyContactPhone, EmergencyContactRelationship)
    VALUES (source.Id, source.FullName, source.DateOfBirth, source.Gender, source.Phone, source.Address, source.CreatedAt, source.AppUserId, source.EmergencyContactName, source.EmergencyContactPhone, source.EmergencyContactRelationship);

MERGE dbo.Appointments AS target
USING (VALUES
    (@AppointmentPendingId, @PortalPatientId, @DoctorSeedId, DATEADD(DAY, 2, DATEADD(HOUR, 9, CAST(CAST(@NowUtc AS DATE) AS DATETIME2))), 'Pending'),
    (@AppointmentCompleted1Id, @SeniorPatientId, @DoctorSeedId, DATEADD(DAY, -3, DATEADD(HOUR, 10, CAST(CAST(@NowUtc AS DATE) AS DATETIME2))), 'Completed'),
    (@AppointmentCompleted2Id, @PortalPatientId, @DoctorSupportId, DATEADD(DAY, -15, DATEADD(HOUR, 15, CAST(CAST(@NowUtc AS DATE) AS DATETIME2))), 'Completed'),
    (@AppointmentCancelledId, @ChildPatientId, @DoctorSeedId, DATEADD(DAY, 1, DATEADD(HOUR, 14, CAST(CAST(@NowUtc AS DATE) AS DATETIME2))), 'Cancelled')
) AS source (Id, PatientId, DoctorId, AppointmentDate, Status)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PatientId = source.PatientId,
        DoctorId = source.DoctorId,
        AppointmentDate = source.AppointmentDate,
        Status = source.Status
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PatientId, DoctorId, AppointmentDate, Status)
    VALUES (source.Id, source.PatientId, source.DoctorId, source.AppointmentDate, source.Status);

MERGE dbo.MedicalRecords AS target
USING (VALUES
    (@MedicalRecord1Id, @AppointmentCompleted1Id, N'Dau nguc, kho tho nhe khi gang suc', N'Tang huyet ap va nghi benh mach vanh som', N'Theo doi huyet ap tai nha, hen tai kham sau 2 tuan', DATEADD(DAY, -3, DATEADD(HOUR, 11, CAST(CAST(@NowUtc AS DATE) AS DATETIME2)))),
    (@MedicalRecord2Id, @AppointmentCompleted2Id, N'Dau da day sau an, day hoi', N'Viem da day cap', N'Uong thuoc dung lieu, tranh do cay nong va ca phe', DATEADD(DAY, -15, DATEADD(HOUR, 16, CAST(CAST(@NowUtc AS DATE) AS DATETIME2))))
) AS source (Id, AppointmentId, Symptoms, Diagnosis, Notes, CreatedAt)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        AppointmentId = source.AppointmentId,
        Symptoms = source.Symptoms,
        Diagnosis = source.Diagnosis,
        Notes = source.Notes,
        CreatedAt = source.CreatedAt
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, AppointmentId, Symptoms, Diagnosis, Notes, CreatedAt)
    VALUES (source.Id, source.AppointmentId, source.Symptoms, source.Diagnosis, source.Notes, source.CreatedAt);

MERGE dbo.Medicines AS target
USING (VALUES
    (@MedicineAId, N'Amlodipine 5mg', N'Thuoc ha huyet ap'),
    (@MedicineBId, N'Aspirin 81mg', N'Thuoc chong ket tap tieu cau'),
    (@MedicineCId, N'Omeprazole 20mg', N'Thuoc giam tiet acid da day'),
    (@MedicineDId, N'Paracetamol 500mg', N'Thuoc giam dau ha sot')
) AS source (Id, Name, Description)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Description)
    VALUES (source.Id, source.Name, source.Description);

MERGE dbo.Prescriptions AS target
USING (VALUES
    (@Prescription1Id, @MedicalRecord1Id, DATEADD(DAY, -3, DATEADD(HOUR, 11, CAST(CAST(@NowUtc AS DATE) AS DATETIME2)))),
    (@Prescription2Id, @MedicalRecord2Id, DATEADD(DAY, -15, DATEADD(HOUR, 16, CAST(CAST(@NowUtc AS DATE) AS DATETIME2))))
) AS source (Id, MedicalRecordId, CreatedAt)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        MedicalRecordId = source.MedicalRecordId,
        CreatedAt = source.CreatedAt
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, MedicalRecordId, CreatedAt)
    VALUES (source.Id, source.MedicalRecordId, source.CreatedAt);

MERGE dbo.PrescriptionItems AS target
USING (VALUES
    (@PrescriptionItem1Id, @Prescription1Id, @MedicineAId, N'1 vien buoi sang', N'30 ngay'),
    (@PrescriptionItem2Id, @Prescription1Id, @MedicineBId, N'1 vien sau an toi', N'30 ngay'),
    (@PrescriptionItem3Id, @Prescription2Id, @MedicineCId, N'1 vien truoc bua sang', N'14 ngay')
) AS source (Id, PrescriptionId, MedicineId, Dosage, Duration)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PrescriptionId = source.PrescriptionId,
        MedicineId = source.MedicineId,
        Dosage = source.Dosage,
        Duration = source.Duration
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PrescriptionId, MedicineId, Dosage, Duration)
    VALUES (source.Id, source.PrescriptionId, source.MedicineId, source.Dosage, source.Duration);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO dbo.AppUsers (Id, Username, Name, PasswordHash, Role)
SELECT NEWID(),
       CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2)),
       CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2)),
       @PasswordHash,
       seed.RoleCode
FROM Numbers n
CROSS JOIN (VALUES
    ('admin', 'Admin'),
    ('doctor', 'Doctor'),
    ('cashierrec', 'Cashier'),
    ('patient', 'Patient'),
    ('cashierops', 'Cashier'),
    ('cashierpha', 'Cashier'),
    ('cashierlab', 'Cashier'),
    ('cashier', 'Cashier')
) seed (Prefix, RoleCode)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.AppUsers existing
    WHERE existing.Username = CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
)
OPTION (MAXRECURSION 19);

;WITH NumberedAppUsers AS
(
    SELECT au.Id,
           au.Username,
           au.Role,
           TRY_CONVERT(INT, RIGHT(au.Username, 2)) AS NumberValue
    FROM dbo.AppUsers au
    WHERE au.Username LIKE '%[0-9][0-9]'
      AND au.Role IN ('Admin', 'Doctor', 'Cashier', 'Patient')
)
UPDATE au
SET Name = CASE
        WHEN nau.Role = 'Patient' THEN CONCAT(
            CHOOSE(((nau.NumberValue + 2 - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
            N' ',
            CHOOSE(((nau.NumberValue + 5 - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
            N' ',
            CHOOSE(((nau.NumberValue + 8 - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
        )
        ELSE CONCAT(
            CHOOSE(((nau.NumberValue + roleOffset.OffsetA - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
            N' ',
            CHOOSE(((nau.NumberValue + roleOffset.OffsetB - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
            N' ',
            CHOOSE(((nau.NumberValue + roleOffset.OffsetC - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
        )
    END
FROM dbo.AppUsers au
JOIN NumberedAppUsers nau ON nau.Id = au.Id
CROSS APPLY
(
    SELECT CASE nau.Role
            WHEN 'Admin' THEN 1
            WHEN 'Doctor' THEN 2
            WHEN 'Cashier' THEN 3
            ELSE 7
        END AS OffsetA,
        CASE nau.Role
            WHEN 'Admin' THEN 4
            WHEN 'Doctor' THEN 5
            WHEN 'Cashier' THEN 6
            ELSE 10
        END AS OffsetB,
        CASE nau.Role
            WHEN 'Admin' THEN 7
            WHEN 'Doctor' THEN 8
            WHEN 'Cashier' THEN 9
            ELSE 13
        END AS OffsetC
) roleOffset;

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 18
)
INSERT INTO dbo.Doctors (Id, FullName, Specialty)
SELECT NEWID(),
       CONCAT(N'Dr. Seed Doctor ', RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2)),
       CASE n.NumberValue % 4
            WHEN 1 THEN N'General Medicine'
            WHEN 2 THEN N'Cardiology'
            WHEN 3 THEN N'Pediatrics'
            ELSE N'Obstetrics'
       END
FROM Numbers n
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Doctors d
    WHERE d.FullName = CONCAT(N'Dr. Seed Doctor ', RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
)
OPTION (MAXRECURSION 18);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 57
)
INSERT INTO dbo.Patients
(
    Id,
    FullName,
    DateOfBirth,
    Gender,
    Phone,
    Address,
    CreatedAt,
    AppUserId,
    EmergencyContactName,
    EmergencyContactPhone,
    EmergencyContactRelationship
)
SELECT NEWID(),
       CONCAT(N'Legacy Patient ', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3)),
       DATEADD(DAY, -1 * (8000 + n.NumberValue), @NowUtc),
       CASE WHEN n.NumberValue % 2 = 0 THEN N'Female' ELSE N'Male' END,
       CONCAT('0911', RIGHT(CONCAT('000000', CAST(n.NumberValue AS VARCHAR(6))), 6)),
       CONCAT(N'Seed address ', n.NumberValue, N', TP.HCM'),
       DATEADD(DAY, -1 * (60 + n.NumberValue), @NowUtc),
       patientUser.Id,
       CONCAT(N'Nguoi than ', n.NumberValue),
       CONCAT('0988', RIGHT(CONCAT('000000', CAST(n.NumberValue AS VARCHAR(6))), 6)),
       CASE WHEN n.NumberValue % 3 = 0 THEN N'Parent' ELSE N'Spouse' END
FROM Numbers n
OUTER APPLY
(
    SELECT TOP 1 au.Id
    FROM dbo.AppUsers au
    WHERE au.Username = CONCAT('patient', RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
) patientUser
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Patients p
    WHERE p.FullName = CONCAT(N'Legacy Patient ', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
)
OPTION (MAXRECURSION 57);

;WITH NumberedDoctors AS
(
    SELECT d.Id,
           TRY_CONVERT(INT, RIGHT(d.FullName, 2)) AS NumberValue
    FROM dbo.Doctors d
    WHERE d.FullName LIKE N'Dr. Seed Doctor %'
)
UPDATE d
SET FullName = CONCAT(
        CHOOSE(((nd.NumberValue + 1 - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
        N' ',
        CHOOSE(((nd.NumberValue + 3 - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
        N' ',
        CHOOSE(((nd.NumberValue + 5 - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
    ),
    Specialty = CASE nd.NumberValue % 4
        WHEN 1 THEN N'General Medicine'
        WHEN 2 THEN N'Cardiology'
        WHEN 3 THEN N'Pediatrics'
        ELSE N'Obstetrics'
    END
FROM dbo.Doctors d
JOIN NumberedDoctors nd ON nd.Id = d.Id;

;WITH NumberedPatients AS
(
    SELECT p.Id,
           TRY_CONVERT(INT, RIGHT(p.FullName, 3)) AS NumberValue
    FROM dbo.Patients p
    WHERE p.FullName LIKE N'Legacy Patient %'
)
UPDATE p
SET FullName = CONCAT(
        CHOOSE(((np.NumberValue + 2 - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
        N' ',
        CHOOSE(((np.NumberValue + 4 - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
        N' ',
        CHOOSE(((np.NumberValue + 6 - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
    ),
    Address = CONCAT(N'So ', 10 + np.NumberValue, N' Duong Tran Hung Dao, Quan ', ((np.NumberValue - 1) % 5) + 1, N', TP.HCM'),
    EmergencyContactName = CONCAT(
        CHOOSE(((np.NumberValue + 7 - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
        N' ',
        CHOOSE(((np.NumberValue + 8 - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
        N' ',
        CHOOSE(((np.NumberValue + 9 - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
    )
FROM dbo.Patients p
JOIN NumberedPatients np ON np.Id = p.Id;
GO



