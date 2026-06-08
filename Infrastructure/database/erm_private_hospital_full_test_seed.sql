SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @PasswordHash NVARCHAR(255) = '$2a$11$Um9s9E6FMMXBU96niwJTVugama.vZhZDaNyWQPc5fC0Bp90krJIHq';
DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();
DECLARE @TodayLocal DATE = CONVERT(DATE, DATEADD(HOUR, 7, @NowUtc));
DECLARE @Today0800Utc DATETIME2 = DATEADD(HOUR, -7, DATEADD(HOUR, 8, CAST(@TodayLocal AS DATETIME2)));
DECLARE @Today0830Utc DATETIME2 = DATEADD(MINUTE, 30, @Today0800Utc);
DECLARE @Today0900Utc DATETIME2 = DATEADD(HOUR, 1, @Today0800Utc);
DECLARE @Today1000Utc DATETIME2 = DATEADD(HOUR, 2, @Today0800Utc);
DECLARE @Tomorrow0900Utc DATETIME2 = DATEADD(DAY, 1, @Today0900Utc);
DECLARE @TwoDays0900Utc DATETIME2 = DATEADD(DAY, 2, @Today0900Utc);
DECLARE @Yesterday1000Utc DATETIME2 = DATEADD(DAY, -1, @Today1000Utc);
DECLARE @ThreeDaysAgo1400Utc DATETIME2 = DATEADD(HOUR, 6, DATEADD(DAY, -3, @Today0800Utc));
DECLARE @FourDaysAgo0900Utc DATETIME2 = DATEADD(DAY, -4, @Today0900Utc);

DECLARE @AdminUserId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @DoctorUserId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @CashierFrontDeskUserId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @PatientUserId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @CashierOpsUserId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';
DECLARE @CashierPharmacyUserId UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666666';
DECLARE @CashierLabUserId UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777777';
DECLARE @CashierUserId UNIQUEIDENTIFIER = '88888888-8888-8888-8888-888888888888';

DECLARE @AdminStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @DoctorStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @CashierFrontDeskStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @CashierOpsStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
DECLARE @CashierPharmacyStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
DECLARE @CashierLabStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006';
DECLARE @CashierStaffId UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000007';

DECLARE @DoctorProfileId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @DoctorScheduleMorningId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000011';
DECLARE @DoctorScheduleAfternoonId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000012';
DECLARE @DoctorScheduleSaturdayId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000013';

DECLARE @PortalPatientHospitalId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @SeniorPatientHospitalId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @MaternityPatientHospitalId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000003';
DECLARE @PediatricPatientHospitalId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000004';

DECLARE @PortalPolicyId UNIQUEIDENTIFIER = '31000000-0000-0000-0000-000000000001';
DECLARE @PortalConsentId UNIQUEIDENTIFIER = '32000000-0000-0000-0000-000000000001';

DECLARE @SlotFutureId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @SlotTodayId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000002';
DECLARE @SlotYesterdayId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000003';
DECLARE @SlotPastId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000004';

DECLARE @AppointmentFutureId UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000001';
DECLARE @AppointmentCheckedInId UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000002';
DECLARE @AppointmentCompletedId UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000003';
DECLARE @AppointmentApprovedId UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000004';
DECLARE @AppointmentCancelledId UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000005';

DECLARE @CheckInTodayId UNIQUEIDENTIFIER = '42000000-0000-0000-0000-000000000001';
DECLARE @QueueTodayId UNIQUEIDENTIFIER = '43000000-0000-0000-0000-000000000001';

DECLARE @EncounterInProgressId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @EncounterFinalizedId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';
DECLARE @EncounterApprovedId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000003';

DECLARE @VitalInProgressId UNIQUEIDENTIFIER = '51000000-0000-0000-0000-000000000001';
DECLARE @VitalFinalizedId UNIQUEIDENTIFIER = '51000000-0000-0000-0000-000000000002';
DECLARE @VitalApprovedId UNIQUEIDENTIFIER = '51000000-0000-0000-0000-000000000003';

DECLARE @Diagnosis1Id UNIQUEIDENTIFIER = '52000000-0000-0000-0000-000000000001';
DECLARE @Diagnosis2Id UNIQUEIDENTIFIER = '52000000-0000-0000-0000-000000000002';
DECLARE @Diagnosis3Id UNIQUEIDENTIFIER = '52000000-0000-0000-0000-000000000003';

DECLARE @Note1Id UNIQUEIDENTIFIER = '53000000-0000-0000-0000-000000000001';
DECLARE @Note2Id UNIQUEIDENTIFIER = '53000000-0000-0000-0000-000000000002';
DECLARE @Note3Id UNIQUEIDENTIFIER = '53000000-0000-0000-0000-000000000003';
DECLARE @ApprovalNoteId UNIQUEIDENTIFIER = '53000000-0000-0000-0000-000000000004';

DECLARE @Document1Id UNIQUEIDENTIFIER = '54000000-0000-0000-0000-000000000001';
DECLARE @Document2Id UNIQUEIDENTIFIER = '54000000-0000-0000-0000-000000000002';

DECLARE @AllergyId UNIQUEIDENTIFIER = '55000000-0000-0000-0000-000000000001';
DECLARE @ChronicConditionId UNIQUEIDENTIFIER = '56000000-0000-0000-0000-000000000001';

DECLARE @LabOrderHeaderPendingId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000001';
DECLARE @LabOrderPendingId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000011';
DECLARE @LabOrderHeaderCompletedId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000002';
DECLARE @LabOrderCompletedId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000012';
DECLARE @SpecimenCompletedId UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000021';
DECLARE @LabResult1Id UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000031';
DECLARE @LabResult2Id UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000032';

DECLARE @ImagingOrderHeaderId UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000001';
DECLARE @ImagingOrderId UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000011';
DECLARE @ImagingReportId UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000021';

DECLARE @PrescriptionOrderHeaderIssuedId UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000001';
DECLARE @PrescriptionIssuedId UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000011';
DECLARE @PrescriptionOrderHeaderDispensedId UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000002';
DECLARE @PrescriptionDispensedId UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000012';
DECLARE @PrescriptionItem1Id UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000021';
DECLARE @PrescriptionItem2Id UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000022';
DECLARE @PrescriptionItem3Id UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000023';
DECLARE @DispensingId UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000031';

DECLARE @InventoryBatchAId UNIQUEIDENTIFIER = '63000000-0000-0000-0000-000000000001';
DECLARE @InventoryBatchBId UNIQUEIDENTIFIER = '63000000-0000-0000-0000-000000000002';
DECLARE @InventoryTxReceiptAId UNIQUEIDENTIFIER = '63000000-0000-0000-0000-000000000011';
DECLARE @InventoryTxIssueAId UNIQUEIDENTIFIER = '63000000-0000-0000-0000-000000000012';
DECLARE @InventoryTxReceiptBId UNIQUEIDENTIFIER = '63000000-0000-0000-0000-000000000013';

DECLARE @InvoicePartialId UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000001';
DECLARE @InvoicePaidId UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000002';
DECLARE @InvoiceRefundedId UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000003';

DECLARE @InvoiceItem1Id UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000001';
DECLARE @InvoiceItem2Id UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000002';
DECLARE @InvoiceItem3Id UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000003';
DECLARE @InvoiceItem4Id UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000004';
DECLARE @InvoiceItem5Id UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000005';
DECLARE @InvoiceItem6Id UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000006';

DECLARE @PaymentPartialId UNIQUEIDENTIFIER = '72000000-0000-0000-0000-000000000001';
DECLARE @PaymentPaidId UNIQUEIDENTIFIER = '72000000-0000-0000-0000-000000000002';
DECLARE @PaymentRefundedId UNIQUEIDENTIFIER = '72000000-0000-0000-0000-000000000003';
DECLARE @RefundId UNIQUEIDENTIFIER = '73000000-0000-0000-0000-000000000001';
DECLARE @InsuranceClaimId UNIQUEIDENTIFIER = '74000000-0000-0000-0000-000000000001';

DECLARE @OutboxPendingId UNIQUEIDENTIFIER = '80000000-0000-0000-0000-000000000001';
DECLARE @OutboxDeliveredId UNIQUEIDENTIFIER = '80000000-0000-0000-0000-000000000002';
DECLARE @OutboxFailedId UNIQUEIDENTIFIER = '80000000-0000-0000-0000-000000000003';
DECLARE @DeliveryQueuedId UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000001';
DECLARE @DeliveryDeliveredId UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000002';
DECLARE @DeliveryFailedId UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000003';

DECLARE @SecurityEvent1Id UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000001';
DECLARE @SecurityEvent2Id UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000002';
DECLARE @Audit1Id UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000001';
DECLARE @Audit2Id UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000002';
DECLARE @Access1Id UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000001';
DECLARE @Access2Id UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000002';

DECLARE @OpdDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'OPD');
DECLARE @LabDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'LAB');
DECLARE @ImgDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'IMG');
DECLARE @PhaDepartmentId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Departments WHERE DepartmentCode = 'PHA');
DECLARE @GeneralSpecialtyId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Specialties WHERE SpecialtyCode = 'GEN');
DECLARE @ClinicGeneralId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Clinics WHERE ClinicCode = 'CLN-01');
DECLARE @ClinicCardioId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Clinics WHERE ClinicCode = 'CLN-02');
DECLARE @LabServiceCbcId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM lab.LabServices WHERE ServiceCode = 'LAB-CBC');
DECLARE @ImagingServiceAbdId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM imaging.ImagingServices WHERE ServiceCode = 'IMG-US-ABD');
DECLARE @ServiceConsultGeneralId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM billing.ServiceCatalog WHERE ServiceCode = 'CONS-GEN');
DECLARE @ServiceLabCbcId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM billing.ServiceCatalog WHERE ServiceCode = 'LAB-CBC');
DECLARE @ServiceImagingAbdId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM billing.ServiceCatalog WHERE ServiceCode = 'IMG-US-ABD');
DECLARE @ServicePharmacyId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM billing.ServiceCatalog WHERE ServiceCode = 'MED-LOS-50');
DECLARE @MedicineLosartanId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM pharmacy.Medicines WHERE DrugCode = 'MED-LOS-50');
DECLARE @MedicineOmeprazoleId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM pharmacy.Medicines WHERE DrugCode = 'MED-OME-20');

IF @OpdDepartmentId IS NULL OR @LabDepartmentId IS NULL OR @ImgDepartmentId IS NULL OR @PhaDepartmentId IS NULL
    THROW 50001, 'Hospital base seed is missing departments.', 1;

IF @GeneralSpecialtyId IS NULL OR @ClinicGeneralId IS NULL OR @ClinicCardioId IS NULL
    THROW 50002, 'Hospital base seed is missing specialties or clinics.', 1;

IF @LabServiceCbcId IS NULL OR @ImagingServiceAbdId IS NULL OR @ServiceConsultGeneralId IS NULL OR @ServiceLabCbcId IS NULL OR @ServiceImagingAbdId IS NULL
    THROW 50003, 'Hospital base seed is missing services or medicines.', 1;

IF @ServicePharmacyId IS NULL
BEGIN
    DECLARE @NewServicePharmacyId UNIQUEIDENTIFIER = '75000000-0000-0000-0000-000000000001';
    IF EXISTS (SELECT 1 FROM billing.ServiceCatalog WHERE Id = @NewServicePharmacyId)
    BEGIN
        UPDATE billing.ServiceCatalog
        SET ServiceCode = 'MED-LOS-50',
            Name = N'Cap phat Losartan 50mg',
            Category = N'Pharmacy',
            UnitPrice = 4000,
            IsActive = 1
        WHERE Id = @NewServicePharmacyId;
    END
    ELSE
    BEGIN
        INSERT INTO billing.ServiceCatalog (Id, ServiceCode, Name, Category, UnitPrice, IsActive)
        VALUES (@NewServicePharmacyId, 'MED-LOS-50', N'Cap phat Losartan 50mg', N'Pharmacy', 4000, 1);
    END

    SET @ServicePharmacyId = @NewServicePharmacyId;
END

IF @MedicineLosartanId IS NULL
BEGIN
    DECLARE @NewMedicineLosartanId UNIQUEIDENTIFIER = '76000000-0000-0000-0000-000000000001';
    IF EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE Id = @NewMedicineLosartanId)
    BEGIN
        UPDATE pharmacy.Medicines
        SET DrugCode = 'MED-LOS-50',
            Name = N'Losartan 50mg',
            GenericName = N'Losartan',
            Strength = N'50mg',
            DosageForm = N'Vien nen',
            Unit = N'Vien',
            IsControlled = 0,
            IsActive = 1
        WHERE Id = @NewMedicineLosartanId;
    END
    ELSE
    BEGIN
        INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit, IsControlled, IsActive)
        VALUES (@NewMedicineLosartanId, 'MED-LOS-50', N'Losartan 50mg', N'Losartan', N'50mg', N'Vien nen', N'Vien', 0, 1);
    END

    SET @MedicineLosartanId = @NewMedicineLosartanId;
END

IF @MedicineOmeprazoleId IS NULL
BEGIN
    DECLARE @NewMedicineOmeprazoleId UNIQUEIDENTIFIER = '76000000-0000-0000-0000-000000000002';
    IF EXISTS (SELECT 1 FROM pharmacy.Medicines WHERE Id = @NewMedicineOmeprazoleId)
    BEGIN
        UPDATE pharmacy.Medicines
        SET DrugCode = 'MED-OME-20',
            Name = N'Omeprazole 20mg',
            GenericName = N'Omeprazole',
            Strength = N'20mg',
            DosageForm = N'Vien nang',
            Unit = N'Vien',
            IsControlled = 0,
            IsActive = 1
        WHERE Id = @NewMedicineOmeprazoleId;
    END
    ELSE
    BEGIN
        INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit, IsControlled, IsActive)
        VALUES (@NewMedicineOmeprazoleId, 'MED-OME-20', N'Omeprazole 20mg', N'Omeprazole', N'20mg', N'Vien nang', N'Vien', 0, 1);
    END

    SET @MedicineOmeprazoleId = @NewMedicineOmeprazoleId;
END

MERGE [identity].Users AS target
USING (VALUES
    (@AdminUserId, 'admin00', 'admin00@erm.local', @PasswordHash, 'Admin'),
    (@DoctorUserId, 'doctor00', 'doctor00@erm.local', @PasswordHash, 'Doctor'),
    (@CashierFrontDeskUserId, 'cashier.frontdesk', 'cashier.frontdesk@erm.local', @PasswordHash, 'Cashier'),
    (@PatientUserId, 'patient00', 'patient00@erm.local', @PasswordHash, 'Patient'),
    (@CashierOpsUserId, 'cashier.ops', 'cashier.ops@erm.local', @PasswordHash, 'Cashier'),
    (@CashierPharmacyUserId, 'cashier.pharmacy', 'cashier.pharmacy@erm.local', @PasswordHash, 'Cashier'),
    (@CashierLabUserId, 'cashier.lab', 'cashier.lab@erm.local', @PasswordHash, 'Cashier'),
    (@CashierUserId, 'cashier00', 'cashier00@erm.local', @PasswordHash, 'Cashier')
) AS source (Id, Username, Email, PasswordHash, PrimaryRoleCode)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Username = source.Username,
        Email = source.Email,
        PasswordHash = source.PasswordHash,
        PrimaryRoleCode = source.PrimaryRoleCode,
        IsActive = 1,
        DeletedAtUtc = NULL,
        UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Username, Email, PasswordHash, PrimaryRoleCode, IsActive, EmailVerifiedAtUtc, LastLoginAtUtc, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (source.Id, source.Username, source.Email, source.PasswordHash, source.PrimaryRoleCode, 1, DATEADD(DAY, -30, @NowUtc), DATEADD(DAY, -1, @NowUtc), DATEADD(DAY, -120, @NowUtc), @NowUtc, NULL);

MERGE [identity].UserRoles AS target
USING (VALUES
    (@AdminUserId, 'Admin'),
    (@DoctorUserId, 'Doctor'),
    (@CashierFrontDeskUserId, 'Cashier'),
    (@PatientUserId, 'Patient'),
    (@CashierOpsUserId, 'Cashier'),
    (@CashierPharmacyUserId, 'Cashier'),
    (@CashierLabUserId, 'Cashier'),
    (@CashierUserId, 'Cashier')
) AS source (UserId, RoleCode)
ON target.UserId = source.UserId AND target.RoleCode = source.RoleCode
WHEN MATCHED THEN
    UPDATE SET
        GrantedByUserId = @AdminUserId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (UserId, RoleCode, GrantedAtUtc, GrantedByUserId)
    VALUES (source.UserId, source.RoleCode, DATEADD(DAY, -120, @NowUtc), @AdminUserId);

MERGE org.StaffProfiles AS target
USING (VALUES
    (@AdminStaffId, @AdminUserId, 'ADM001', N'Pham Hoang An', @OpdDepartmentId, '0908111001', 'admin00@erm.local', CAST('2023-01-10' AS DATE)),
    (@DoctorStaffId, @DoctorUserId, 'BS100', N'Tran Anh Khoa', @OpdDepartmentId, '0908111002', 'doctor00@erm.local', CAST('2020-03-15' AS DATE)),
    (@CashierFrontDeskStaffId, @CashierFrontDeskUserId, 'LT100', N'Le Thu Hang', @OpdDepartmentId, '0908111003', 'cashier.frontdesk@erm.local', CAST('2022-04-01' AS DATE)),
    (@CashierOpsStaffId, @CashierOpsUserId, 'DD100', N'Nguyen Ha My', @OpdDepartmentId, '0908111004', 'cashier.ops@erm.local', CAST('2021-06-12' AS DATE)),
    (@CashierPharmacyStaffId, @CashierPharmacyUserId, 'DS100', N'Vo Thanh Tung', @PhaDepartmentId, '0908111005', 'cashier.pharmacy@erm.local', CAST('2021-08-20' AS DATE)),
    (@CashierLabStaffId, @CashierLabUserId, 'XN100', N'Pham Kien Minh', @LabDepartmentId, '0908111006', 'cashier.lab@erm.local', CAST('2022-02-11' AS DATE)),
    (@CashierStaffId, @CashierUserId, 'TN100', N'Tran Thu Ha', @OpdDepartmentId, '0908111007', 'cashier00@erm.local', CAST('2022-10-05' AS DATE))
) AS source (Id, UserId, StaffCode, FullName, DepartmentId, Phone, Email, HireDate)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        UserId = source.UserId,
        StaffCode = source.StaffCode,
        FullName = source.FullName,
        DepartmentId = source.DepartmentId,
        Phone = source.Phone,
        Email = source.Email,
        HireDate = source.HireDate,
        IsActive = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, UserId, StaffCode, FullName, DepartmentId, Phone, Email, HireDate, IsActive, CreatedAtUtc)
    VALUES (source.Id, source.UserId, source.StaffCode, source.FullName, source.DepartmentId, source.Phone, source.Email, source.HireDate, 1, DATEADD(DAY, -120, @NowUtc));

IF EXISTS (SELECT 1 FROM org.DoctorProfiles WHERE Id = @DoctorProfileId)
BEGIN
    UPDATE org.DoctorProfiles
    SET StaffProfileId = @DoctorStaffId,
        SpecialtyId = @GeneralSpecialtyId,
        LicenseNumber = 'GEN-100',
        Biography = N'Bac si noi tong quat phuc vu bo du lieu kiem thu.',
        YearsOfExperience = 8,
        ConsultationFee = 250000,
        IsBookable = 1
    WHERE Id = @DoctorProfileId;
END
ELSE
BEGIN
    INSERT INTO org.DoctorProfiles (Id, StaffProfileId, SpecialtyId, LicenseNumber, Biography, YearsOfExperience, ConsultationFee, IsBookable)
    VALUES (@DoctorProfileId, @DoctorStaffId, @GeneralSpecialtyId, 'GEN-100', N'Bac si noi tong quat phuc vu bo du lieu kiem thu.', 8, 250000, 1);
END

MERGE org.DoctorSchedules AS target
USING (VALUES
    (@DoctorScheduleMorningId, @DoctorProfileId, @ClinicGeneralId, CAST(1 AS TINYINT), CAST('08:00' AS TIME), CAST('11:30' AS TIME), 30, CAST('2026-01-01' AS DATE), NULL, 1),
    (@DoctorScheduleAfternoonId, @DoctorProfileId, @ClinicGeneralId, CAST(3 AS TINYINT), CAST('13:30' AS TIME), CAST('17:00' AS TIME), 30, CAST('2026-01-01' AS DATE), NULL, 1),
    (@DoctorScheduleSaturdayId, @DoctorProfileId, @ClinicCardioId, CAST(6 AS TINYINT), CAST('08:00' AS TIME), CAST('11:00' AS TIME), 30, CAST('2026-01-01' AS DATE), NULL, 1)
) AS source (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DoctorProfileId = source.DoctorProfileId,
        ClinicId = source.ClinicId,
        DayOfWeek = source.DayOfWeek,
        StartTime = source.StartTime,
        EndTime = source.EndTime,
        SlotMinutes = source.SlotMinutes,
        ValidFrom = source.ValidFrom,
        ValidTo = source.ValidTo,
        IsActive = source.IsActive
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
    VALUES (source.Id, source.DoctorProfileId, source.ClinicId, source.DayOfWeek, source.StartTime, source.EndTime, source.SlotMinutes, source.ValidFrom, source.ValidTo, source.IsActive);

MERGE patient.Patients AS target
USING (VALUES
    (@PortalPatientHospitalId, 'MRN-0001', N'Nguyen Minh Anh', CAST('1994-05-12' AS DATE), N'Female', '0909000001', 'patient00@erm.local', N'12 Nguyen Hue', NULL, N'Ben Nghe', N'Quan 1', N'TP.HCM', N'Viet Nam', '079123456789', N'Nhan vien van phong', N'Married'),
    (@SeniorPatientHospitalId, 'MRN-SEED-0002', N'Le Van Binh', CAST('1968-10-20' AS DATE), N'Male', '0909000002', 'binh.le@erm.local', N'88 Cach Mang Thang 8', NULL, N'Vo Thi Sau', N'Quan 3', N'TP.HCM', N'Viet Nam', '079987654321', N'Nghi huu', N'Married'),
    (@MaternityPatientHospitalId, 'MRN-SEED-0003', N'Pham Ngoc Lan', CAST('1991-03-03' AS DATE), N'Female', '0909000004', 'lan.pham@erm.local', N'150 Nguyen Thi Minh Khai', NULL, N'Ben Thanh', N'Quan 1', N'TP.HCM', N'Viet Nam', '079556677889', N'Ke toan', N'Married'),
    (@PediatricPatientHospitalId, 'MRN-SEED-0004', N'Tran Gia Han', CAST('2017-02-15' AS DATE), N'Female', '0909000003', 'han.tran@erm.local', N'26 Phan Xich Long', NULL, N'Ward 2', N'Phu Nhuan', N'TP.HCM', N'Viet Nam', NULL, N'Hoc sinh', N'Single')
) AS source (Id, MedicalRecordNumber, FullName, DateOfBirth, Gender, Phone, Email, AddressLine1, AddressLine2, Ward, District, Province, Nationality, IdentityNumber, Occupation, MaritalStatus)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        MedicalRecordNumber = source.MedicalRecordNumber,
        FullName = source.FullName,
        DateOfBirth = source.DateOfBirth,
        Gender = source.Gender,
        Phone = source.Phone,
        Email = source.Email,
        AddressLine1 = source.AddressLine1,
        AddressLine2 = source.AddressLine2,
        Ward = source.Ward,
        District = source.District,
        Province = source.Province,
        Nationality = source.Nationality,
        IdentityNumber = source.IdentityNumber,
        Occupation = source.Occupation,
        MaritalStatus = source.MaritalStatus,
        DeletedAtUtc = NULL,
        UpdatedAtUtc = @NowUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, MedicalRecordNumber, FullName, DateOfBirth, Gender, Phone, Email, AddressLine1, AddressLine2, Ward, District, Province, Nationality, IdentityNumber, Occupation, MaritalStatus, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
    VALUES (source.Id, source.MedicalRecordNumber, source.FullName, source.DateOfBirth, source.Gender, source.Phone, source.Email, source.AddressLine1, source.AddressLine2, source.Ward, source.District, source.Province, source.Nationality, source.IdentityNumber, source.Occupation, source.MaritalStatus, DATEADD(DAY, -120, @NowUtc), @NowUtc, NULL);

IF EXISTS (SELECT 1 FROM patient.PatientAccounts WHERE PatientId = @PortalPatientHospitalId)
BEGIN
    UPDATE patient.PatientAccounts
    SET UserId = @PatientUserId,
        ActivatedAtUtc = DATEADD(DAY, -60, @NowUtc),
        PortalStatus = 'Active'
    WHERE PatientId = @PortalPatientHospitalId;
END
ELSE
BEGIN
    INSERT INTO patient.PatientAccounts (PatientId, UserId, ActivatedAtUtc, PortalStatus)
    VALUES (@PortalPatientHospitalId, @PatientUserId, DATEADD(DAY, -60, @NowUtc), 'Active');
END

MERGE patient.PatientIdentifiers AS target
USING (VALUES
    ('33000000-0000-0000-0000-000000000001', @PortalPatientHospitalId, 'NationalId', '079123456789', 1),
    ('33000000-0000-0000-0000-000000000002', @PortalPatientHospitalId, 'InsuranceNumber', 'BHYT-0001', 0),
    ('33000000-0000-0000-0000-000000000003', @SeniorPatientHospitalId, 'NationalId', '079987654321', 1),
    ('33000000-0000-0000-0000-000000000004', @MaternityPatientHospitalId, 'NationalId', '079556677889', 1)
) AS source (Id, PatientId, IdentifierType, IdentifierValue, IsPrimary)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PatientId = source.PatientId,
        IdentifierType = source.IdentifierType,
        IdentifierValue = source.IdentifierValue,
        IsPrimary = source.IsPrimary
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PatientId, IdentifierType, IdentifierValue, IsPrimary, IssuedAtUtc)
    VALUES (source.Id, source.PatientId, source.IdentifierType, source.IdentifierValue, source.IsPrimary, DATEADD(DAY, -100, @NowUtc));

MERGE patient.PatientContacts AS target
USING (VALUES
    ('34000000-0000-0000-0000-000000000001', @PortalPatientHospitalId, 'Phone', '0909000001', 1),
    ('34000000-0000-0000-0000-000000000002', @PortalPatientHospitalId, 'Email', 'patient00@erm.local', 0),
    ('34000000-0000-0000-0000-000000000003', @SeniorPatientHospitalId, 'Phone', '0909000002', 1),
    ('34000000-0000-0000-0000-000000000004', @MaternityPatientHospitalId, 'Phone', '0909000004', 1),
    ('34000000-0000-0000-0000-000000000005', @PediatricPatientHospitalId, 'Phone', '0909000003', 1)
) AS source (Id, PatientId, ContactType, ContactValue, IsPrimary)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PatientId = source.PatientId,
        ContactType = source.ContactType,
        ContactValue = source.ContactValue,
        IsPrimary = source.IsPrimary
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PatientId, ContactType, ContactValue, IsPrimary)
    VALUES (source.Id, source.PatientId, source.ContactType, source.ContactValue, source.IsPrimary);

MERGE patient.PatientEmergencyContacts AS target
USING (VALUES
    ('35000000-0000-0000-0000-000000000001', @PortalPatientHospitalId, N'Nguyen Van Nam', N'Spouse', '0909000901', N'12 Nguyen Hue, Quan 1'),
    ('35000000-0000-0000-0000-000000000002', @SeniorPatientHospitalId, N'Le Thu Trang', N'Daughter', '0909000902', N'88 Cach Mang Thang 8, Quan 3'),
    ('35000000-0000-0000-0000-000000000003', @PediatricPatientHospitalId, N'Tran Hoai Thu', N'Mother', '0909000903', N'26 Phan Xich Long, Phu Nhuan')
) AS source (Id, PatientId, FullName, Relationship, Phone, Address)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PatientId = source.PatientId,
        FullName = source.FullName,
        Relationship = source.Relationship,
        Phone = source.Phone,
        Address = source.Address
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PatientId, FullName, Relationship, Phone, Address)
    VALUES (source.Id, source.PatientId, source.FullName, source.Relationship, source.Phone, source.Address);

IF EXISTS (SELECT 1 FROM patient.PatientInsurancePolicies WHERE Id = @PortalPolicyId)
BEGIN
    UPDATE patient.PatientInsurancePolicies
    SET PatientId = @PortalPatientHospitalId,
        ProviderName = N'Bao hiem Y te Thanh pho',
        PolicyNumber = 'BHYT-SEED-0001',
        CardNumber = 'HC-0001',
        EffectiveFrom = CAST('2026-01-01' AS DATE),
        EffectiveTo = CAST('2026-12-31' AS DATE),
        CoveragePercent = 80,
        IsActive = 1
    WHERE Id = @PortalPolicyId;
END
ELSE
BEGIN
    INSERT INTO patient.PatientInsurancePolicies (Id, PatientId, ProviderName, PolicyNumber, CardNumber, EffectiveFrom, EffectiveTo, CoveragePercent, IsActive)
    VALUES (@PortalPolicyId, @PortalPatientHospitalId, N'Bao hiem Y te Thanh pho', 'BHYT-SEED-0001', 'HC-0001', CAST('2026-01-01' AS DATE), CAST('2026-12-31' AS DATE), 80, 1);
END

IF EXISTS (SELECT 1 FROM patient.PatientConsents WHERE Id = @PortalConsentId)
BEGIN
    UPDATE patient.PatientConsents
    SET PatientId = @PortalPatientHospitalId,
        ConsentType = 'PortalAccess',
        GrantedAtUtc = DATEADD(DAY, -60, @NowUtc),
        RevokedAtUtc = NULL,
        EvidenceUri = 'local://hoso/consents/patient-portal.pdf'
    WHERE Id = @PortalConsentId;
END
ELSE
BEGIN
    INSERT INTO patient.PatientConsents (Id, PatientId, ConsentType, GrantedAtUtc, RevokedAtUtc, EvidenceUri)
    VALUES (@PortalConsentId, @PortalPatientHospitalId, 'PortalAccess', DATEADD(DAY, -60, @NowUtc), NULL, 'local://hoso/consents/patient-portal.pdf');
END

MERGE notification.NotificationPreferences AS target
USING (VALUES
    ('36000000-0000-0000-0000-000000000001', @PortalPatientHospitalId, 'Sms', 1),
    ('36000000-0000-0000-0000-000000000002', @PortalPatientHospitalId, 'Email', 1),
    ('36000000-0000-0000-0000-000000000003', @PortalPatientHospitalId, 'App', 1),
    ('36000000-0000-0000-0000-000000000004', @SeniorPatientHospitalId, 'Sms', 1),
    ('36000000-0000-0000-0000-000000000005', @SeniorPatientHospitalId, 'Email', 0)
) AS source (Id, PatientId, ChannelCode, IsEnabled)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PatientId = source.PatientId,
        ChannelCode = source.ChannelCode,
        IsEnabled = source.IsEnabled
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PatientId, ChannelCode, IsEnabled)
    VALUES (source.Id, source.PatientId, source.ChannelCode, source.IsEnabled);

IF EXISTS (SELECT 1 FROM emr.Allergies WHERE Id = @AllergyId)
BEGIN
    UPDATE emr.Allergies
    SET PatientId = @PortalPatientHospitalId,
        AllergenName = N'Penicillin',
        Reaction = N'Phat ban do',
        Severity = 'Moderate',
        Status = 'Active',
        RecordedAtUtc = DATEADD(DAY, -100, @NowUtc)
    WHERE Id = @AllergyId;
END
ELSE
BEGIN
    INSERT INTO emr.Allergies (Id, PatientId, AllergenName, Reaction, Severity, Status, RecordedAtUtc)
    VALUES (@AllergyId, @PortalPatientHospitalId, N'Penicillin', N'Phat ban do', 'Moderate', 'Active', DATEADD(DAY, -100, @NowUtc));
END

IF EXISTS (SELECT 1 FROM emr.ChronicConditions WHERE Id = @ChronicConditionId)
BEGIN
    UPDATE emr.ChronicConditions
    SET PatientId = @SeniorPatientHospitalId,
        ConditionCode = 'I10',
        ConditionName = N'Tang huyet ap nguyen phat',
        DiagnosedOn = CAST('2021-05-20' AS DATE),
        Status = 'Active'
    WHERE Id = @ChronicConditionId;
END
ELSE
BEGIN
    INSERT INTO emr.ChronicConditions (Id, PatientId, ConditionCode, ConditionName, DiagnosedOn, Status)
    VALUES (@ChronicConditionId, @SeniorPatientHospitalId, 'I10', N'Tang huyet ap nguyen phat', CAST('2021-05-20' AS DATE), 'Active');
END

MERGE scheduling.AppointmentSlots AS target
USING (VALUES
    (@SlotFutureId, @DoctorScheduleMorningId, @TwoDays0900Utc, DATEADD(MINUTE, 30, @TwoDays0900Utc), 1, 1, 'Reserved'),
    (@SlotTodayId, @DoctorScheduleMorningId, @Today0830Utc, DATEADD(MINUTE, 30, @Today0830Utc), 1, 1, 'Reserved'),
    (@SlotYesterdayId, @DoctorScheduleMorningId, @Yesterday1000Utc, DATEADD(MINUTE, 30, @Yesterday1000Utc), 1, 1, 'Closed'),
    (@SlotPastId, @DoctorScheduleAfternoonId, @ThreeDaysAgo1400Utc, DATEADD(MINUTE, 30, @ThreeDaysAgo1400Utc), 1, 1, 'Closed')
) AS source (Id, DoctorScheduleId, SlotStartUtc, SlotEndUtc, Capacity, ReservedCount, SlotStatus)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DoctorScheduleId = source.DoctorScheduleId,
        SlotStartUtc = source.SlotStartUtc,
        SlotEndUtc = source.SlotEndUtc,
        Capacity = source.Capacity,
        ReservedCount = source.ReservedCount,
        SlotStatus = source.SlotStatus
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DoctorScheduleId, SlotStartUtc, SlotEndUtc, Capacity, ReservedCount, SlotStatus)
    VALUES (source.Id, source.DoctorScheduleId, source.SlotStartUtc, source.SlotEndUtc, source.Capacity, source.ReservedCount, source.SlotStatus);

MERGE scheduling.Appointments AS target
USING (VALUES
    (@AppointmentFutureId, 'APT-SEED-0001', @PortalPatientHospitalId, @DoctorProfileId, @ClinicGeneralId, @SlotFutureId, 'FollowUp', 'Portal', 'Scheduled', @TwoDays0900Utc, DATEADD(MINUTE, 30, @TwoDays0900Utc), N'Tai kham da day va xac nhan toa thuoc', N'Benh nhan muon kham buoi sang', @PatientUserId),
    (@AppointmentCheckedInId, 'APT-SEED-0002', @SeniorPatientHospitalId, @DoctorProfileId, @ClinicGeneralId, @SlotTodayId, 'Consultation', 'Reception', 'CheckedIn', @Today0830Utc, DATEADD(MINUTE, 30, @Today0830Utc), N'Dau nguc nhe khi di bo', N'Da do huyet ap tai quay tiep nhan', @CashierFrontDeskUserId),
    (@AppointmentCompletedId, 'APT-SEED-0003', @MaternityPatientHospitalId, @DoctorProfileId, @ClinicGeneralId, @SlotYesterdayId, 'Consultation', 'Reception', 'Completed', @Yesterday1000Utc, DATEADD(MINUTE, 30, @Yesterday1000Utc), N'Dau bung duoi va non nghen', N'Da lam sieu am', @CashierFrontDeskUserId),
    (@AppointmentApprovedId, 'APT-SEED-0004', @PortalPatientHospitalId, @DoctorProfileId, @ClinicGeneralId, @SlotPastId, 'Consultation', 'Portal', 'Completed', @ThreeDaysAgo1400Utc, DATEADD(MINUTE, 30, @ThreeDaysAgo1400Utc), N'Tang huyet ap va can tu van thuoc', N'Co toa thuoc va hen tai kham', @PatientUserId),
    (@AppointmentCancelledId, 'APT-SEED-0005', @PediatricPatientHospitalId, @DoctorProfileId, @ClinicGeneralId, NULL, 'Consultation', 'CallCenter', 'Cancelled', @Tomorrow0900Utc, DATEADD(MINUTE, 30, @Tomorrow0900Utc), N'Sot nhe, ho', N'Gia dinh xin huy lich do tre da do hon', @CashierFrontDeskUserId)
) AS source (Id, AppointmentNumber, PatientId, DoctorProfileId, ClinicId, AppointmentSlotId, AppointmentType, BookingChannel, Status, AppointmentStartUtc, AppointmentEndUtc, ChiefComplaint, Notes, CreatedByUserId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        AppointmentNumber = source.AppointmentNumber,
        PatientId = source.PatientId,
        DoctorProfileId = source.DoctorProfileId,
        ClinicId = source.ClinicId,
        AppointmentSlotId = source.AppointmentSlotId,
        AppointmentType = source.AppointmentType,
        BookingChannel = source.BookingChannel,
        Status = source.Status,
        AppointmentStartUtc = source.AppointmentStartUtc,
        AppointmentEndUtc = source.AppointmentEndUtc,
        ChiefComplaint = source.ChiefComplaint,
        Notes = source.Notes,
        CreatedByUserId = source.CreatedByUserId,
        UpdatedAtUtc = @NowUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, AppointmentNumber, PatientId, DoctorProfileId, ClinicId, AppointmentSlotId, AppointmentType, BookingChannel, Status, AppointmentStartUtc, AppointmentEndUtc, ChiefComplaint, Notes, CreatedByUserId, CreatedAtUtc, UpdatedAtUtc)
    VALUES (source.Id, source.AppointmentNumber, source.PatientId, source.DoctorProfileId, source.ClinicId, source.AppointmentSlotId, source.AppointmentType, source.BookingChannel, source.Status, source.AppointmentStartUtc, source.AppointmentEndUtc, source.ChiefComplaint, source.Notes, source.CreatedByUserId, DATEADD(DAY, -15, @NowUtc), @NowUtc);

IF EXISTS (SELECT 1 FROM scheduling.CheckIns WHERE Id = @CheckInTodayId)
BEGIN
    UPDATE scheduling.CheckIns
    SET AppointmentId = @AppointmentCheckedInId,
        CheckInTimeUtc = DATEADD(MINUTE, -10, @Today0830Utc),
        CounterLabel = 'COUNTER-02',
        CheckInStatus = 'CheckedIn'
    WHERE Id = @CheckInTodayId;
END
ELSE
BEGIN
    INSERT INTO scheduling.CheckIns (Id, AppointmentId, CheckInTimeUtc, CounterLabel, CheckInStatus)
    VALUES (@CheckInTodayId, @AppointmentCheckedInId, DATEADD(MINUTE, -10, @Today0830Utc), 'COUNTER-02', 'CheckedIn');
END

IF EXISTS (SELECT 1 FROM scheduling.QueueTickets WHERE Id = @QueueTodayId)
BEGIN
    UPDATE scheduling.QueueTickets
    SET AppointmentId = @AppointmentCheckedInId,
        QueueNumber = 'A102',
        QueueStatus = 'Waiting',
        CalledAtUtc = NULL,
        ServedAtUtc = NULL
    WHERE Id = @QueueTodayId;
END
ELSE
BEGIN
    INSERT INTO scheduling.QueueTickets (Id, AppointmentId, QueueNumber, QueueStatus, CalledAtUtc, ServedAtUtc)
    VALUES (@QueueTodayId, @AppointmentCheckedInId, 'A102', 'Waiting', NULL, NULL);
END

MERGE emr.Encounters AS target
USING (VALUES
    (@EncounterInProgressId, 'ENC-SEED-0001', @SeniorPatientHospitalId, @AppointmentCheckedInId, @DoctorProfileId, @ClinicGeneralId, 'Outpatient', 'InProgress', DATEADD(MINUTE, 5, @Today0830Utc), NULL, N'Benh nhan dang duoc kham va chi dinh xet nghiem them.'),
    (@EncounterFinalizedId, 'ENC-SEED-0002', @MaternityPatientHospitalId, @AppointmentCompletedId, @DoctorProfileId, @ClinicGeneralId, 'Outpatient', 'Finalized', DATEADD(MINUTE, 2, @Yesterday1000Utc), DATEADD(MINUTE, 40, @Yesterday1000Utc), N'Ho so da ky sau khi co ket qua sieu am.'),
    (@EncounterApprovedId, 'ENC-SEED-0003', @PortalPatientHospitalId, @AppointmentApprovedId, @DoctorProfileId, @ClinicGeneralId, 'Outpatient', 'Approved', DATEADD(MINUTE, 3, @ThreeDaysAgo1400Utc), DATEADD(MINUTE, 35, @ThreeDaysAgo1400Utc), N'Ho so da duoc phe duyet va co toa thuoc da cap.')
) AS source (Id, EncounterNumber, PatientId, AppointmentId, DoctorProfileId, ClinicId, EncounterType, EncounterStatus, StartedAtUtc, EndedAtUtc, Summary)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        EncounterNumber = source.EncounterNumber,
        PatientId = source.PatientId,
        AppointmentId = source.AppointmentId,
        DoctorProfileId = source.DoctorProfileId,
        ClinicId = source.ClinicId,
        EncounterType = source.EncounterType,
        EncounterStatus = source.EncounterStatus,
        StartedAtUtc = source.StartedAtUtc,
        EndedAtUtc = source.EndedAtUtc,
        Summary = source.Summary,
        UpdatedAtUtc = @NowUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, EncounterNumber, PatientId, AppointmentId, DoctorProfileId, ClinicId, EncounterType, EncounterStatus, StartedAtUtc, EndedAtUtc, Summary, CreatedAtUtc, UpdatedAtUtc)
    VALUES (source.Id, source.EncounterNumber, source.PatientId, source.AppointmentId, source.DoctorProfileId, source.ClinicId, source.EncounterType, source.EncounterStatus, source.StartedAtUtc, source.EndedAtUtc, source.Summary, DATEADD(DAY, -15, @NowUtc), @NowUtc);

MERGE emr.VitalSigns AS target
USING (VALUES
    (@VitalInProgressId, @EncounterInProgressId, 167.00, 72.50, 36.8, 86, 18, 148, 92, 97.00, DATEADD(MINUTE, 8, @Today0830Utc), @CashierOpsUserId),
    (@VitalFinalizedId, @EncounterFinalizedId, 160.00, 55.00, 36.7, 82, 18, 110, 70, 99.00, DATEADD(MINUTE, 10, @Yesterday1000Utc), @CashierOpsUserId),
    (@VitalApprovedId, @EncounterApprovedId, 162.00, 58.00, 36.6, 78, 17, 138, 88, 98.00, DATEADD(MINUTE, 8, @ThreeDaysAgo1400Utc), @CashierOpsUserId)
) AS source (Id, EncounterId, HeightCm, WeightKg, TemperatureC, PulseRate, RespiratoryRate, SystolicBp, DiastolicBp, OxygenSaturation, RecordedAtUtc, RecordedByUserId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        EncounterId = source.EncounterId,
        HeightCm = source.HeightCm,
        WeightKg = source.WeightKg,
        TemperatureC = source.TemperatureC,
        PulseRate = source.PulseRate,
        RespiratoryRate = source.RespiratoryRate,
        SystolicBp = source.SystolicBp,
        DiastolicBp = source.DiastolicBp,
        OxygenSaturation = source.OxygenSaturation,
        RecordedAtUtc = source.RecordedAtUtc,
        RecordedByUserId = source.RecordedByUserId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, EncounterId, HeightCm, WeightKg, TemperatureC, PulseRate, RespiratoryRate, SystolicBp, DiastolicBp, OxygenSaturation, RecordedAtUtc, RecordedByUserId)
    VALUES (source.Id, source.EncounterId, source.HeightCm, source.WeightKg, source.TemperatureC, source.PulseRate, source.RespiratoryRate, source.SystolicBp, source.DiastolicBp, source.OxygenSaturation, source.RecordedAtUtc, source.RecordedByUserId);

MERGE emr.Diagnoses AS target
USING (VALUES
    (@Diagnosis1Id, @EncounterInProgressId, 'Working', 'I20.9', N'Nghi dau that nguc on dinh', 1, DATEADD(MINUTE, 15, @Today0830Utc)),
    (@Diagnosis2Id, @EncounterFinalizedId, 'Final', 'O26.8', N'Dau bung thai ky, khong co dau hieu nguy cap', 1, DATEADD(MINUTE, 35, @Yesterday1000Utc)),
    (@Diagnosis3Id, @EncounterApprovedId, 'Final', 'I10', N'Tang huyet ap nguyen phat', 1, DATEADD(MINUTE, 25, @ThreeDaysAgo1400Utc))
) AS source (Id, EncounterId, DiagnosisType, DiagnosisCode, DiagnosisName, IsPrimary, NotedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        EncounterId = source.EncounterId,
        DiagnosisType = source.DiagnosisType,
        DiagnosisCode = source.DiagnosisCode,
        DiagnosisName = source.DiagnosisName,
        IsPrimary = source.IsPrimary,
        NotedAtUtc = source.NotedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, EncounterId, DiagnosisType, DiagnosisCode, DiagnosisName, IsPrimary, NotedAtUtc)
    VALUES (source.Id, source.EncounterId, source.DiagnosisType, source.DiagnosisCode, source.DiagnosisName, source.IsPrimary, source.NotedAtUtc);

MERGE emr.ClinicalNotes AS target
USING (VALUES
    (@Note1Id, @EncounterInProgressId, 'Progress', N'Dau nguc nhe khi di bo khoang 200m.', N'Huyet ap tang, mach deu.', N'Theo doi nguy co tim mach, can dien tim va cong thuc mau.', N'Chi dinh xet nghiem, tu van an nhat va nghi ngoi.', @DoctorUserId, DATEADD(MINUTE, 18, @Today0830Utc), NULL),
    (@Note2Id, @EncounterFinalizedId, 'SOAP', N'Dau bung duoi va non nghen 2 ngay.', N'Sieu am bung khong ghi nhan bat thuong cap tinh.', N'Tinh trang on dinh, chua can nhap vien.', N'Theo doi tai nha, tai kham neu dau tang.', @DoctorUserId, DATEADD(MINUTE, 38, @Yesterday1000Utc), DATEADD(MINUTE, 42, @Yesterday1000Utc)),
    (@Note3Id, @EncounterApprovedId, 'SOAP', N'Huyet ap cao dai dang, dau dau nhe.', N'Khong dau hieu ton thuong co quan dich.', N'Tang huyet ap chua bien chung.', N'Duy tri losartan, tai kham sau 2 tuan.', @DoctorUserId, DATEADD(MINUTE, 28, @ThreeDaysAgo1400Utc), DATEADD(MINUTE, 33, @ThreeDaysAgo1400Utc)),
    (@ApprovalNoteId, @EncounterApprovedId, 'Approval', N'Ho so da duoc truong bo phan kiem tra.', N'Du thong tin can lam sang va toa thuoc.', N'Approved', N'Khong can bo sung.', @AdminUserId, DATEADD(MINUTE, 50, @ThreeDaysAgo1400Utc), DATEADD(MINUTE, 50, @ThreeDaysAgo1400Utc))
) AS source (Id, EncounterId, NoteType, Subjective, Objective, Assessment, CarePlan, AuthoredByUserId, AuthoredAtUtc, SignedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        EncounterId = source.EncounterId,
        NoteType = source.NoteType,
        Subjective = source.Subjective,
        Objective = source.Objective,
        Assessment = source.Assessment,
        CarePlan = source.CarePlan,
        AuthoredByUserId = source.AuthoredByUserId,
        AuthoredAtUtc = source.AuthoredAtUtc,
        SignedAtUtc = source.SignedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, EncounterId, NoteType, Subjective, Objective, Assessment, CarePlan, AuthoredByUserId, AuthoredAtUtc, SignedAtUtc)
    VALUES (source.Id, source.EncounterId, source.NoteType, source.Subjective, source.Objective, source.Assessment, source.CarePlan, source.AuthoredByUserId, source.AuthoredAtUtc, source.SignedAtUtc);

MERGE emr.ClinicalDocuments AS target
USING (VALUES
    (@Document1Id, @EncounterFinalizedId, 'Ultrasound', 'finalized-ultrasound.pdf', 'local://hoso/documents/finalized-ultrasound.pdf', 'application/pdf', @DoctorUserId, DATEADD(MINUTE, 41, @Yesterday1000Utc)),
    (@Document2Id, @EncounterApprovedId, 'PrescriptionReview', 'approved-prescription.pdf', 'local://hoso/documents/approved-prescription.pdf', 'application/pdf', @DoctorUserId, DATEADD(MINUTE, 34, @ThreeDaysAgo1400Utc))
) AS source (Id, EncounterId, DocumentType, FileName, StorageUri, MimeType, UploadedByUserId, UploadedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        EncounterId = source.EncounterId,
        DocumentType = source.DocumentType,
        FileName = source.FileName,
        StorageUri = source.StorageUri,
        MimeType = source.MimeType,
        UploadedByUserId = source.UploadedByUserId,
        UploadedAtUtc = source.UploadedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, EncounterId, DocumentType, FileName, StorageUri, MimeType, UploadedByUserId, UploadedAtUtc)
    VALUES (source.Id, source.EncounterId, source.DocumentType, source.FileName, source.StorageUri, source.MimeType, source.UploadedByUserId, source.UploadedAtUtc);

MERGE emr.OrderHeaders AS target
USING (VALUES
    (@LabOrderHeaderPendingId, @EncounterInProgressId, 'ORD-LAB-0001', 'Laboratory', 'Pending', @DoctorUserId, DATEADD(MINUTE, 20, @Today0830Utc)),
    (@LabOrderHeaderCompletedId, @EncounterApprovedId, 'ORD-LAB-0002', 'Laboratory', 'Completed', @DoctorUserId, DATEADD(MINUTE, 20, @ThreeDaysAgo1400Utc)),
    (@ImagingOrderHeaderId, @EncounterFinalizedId, 'ORD-IMG-0001', 'Imaging', 'Completed', @DoctorUserId, DATEADD(MINUTE, 22, @Yesterday1000Utc)),
    (@PrescriptionOrderHeaderIssuedId, @EncounterInProgressId, 'ORD-RX-0001', 'Pharmacy', 'Pending', @DoctorUserId, DATEADD(MINUTE, 22, @Today0830Utc)),
    (@PrescriptionOrderHeaderDispensedId, @EncounterApprovedId, 'ORD-RX-0002', 'Pharmacy', 'Completed', @DoctorUserId, DATEADD(MINUTE, 24, @ThreeDaysAgo1400Utc))
) AS source (Id, EncounterId, OrderNumber, OrderCategory, OrderStatus, OrderedByUserId, OrderedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        EncounterId = source.EncounterId,
        OrderNumber = source.OrderNumber,
        OrderCategory = source.OrderCategory,
        OrderStatus = source.OrderStatus,
        OrderedByUserId = source.OrderedByUserId,
        OrderedAtUtc = source.OrderedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, EncounterId, OrderNumber, OrderCategory, OrderStatus, OrderedByUserId, OrderedAtUtc)
    VALUES (source.Id, source.EncounterId, source.OrderNumber, source.OrderCategory, source.OrderStatus, source.OrderedByUserId, source.OrderedAtUtc);

MERGE lab.LabOrders AS target
USING (VALUES
    (@LabOrderPendingId, @LabOrderHeaderPendingId, @LabServiceCbcId, 'Pending', 'Urgent', DATEADD(MINUTE, 21, @Today0830Utc), NULL),
    (@LabOrderCompletedId, @LabOrderHeaderCompletedId, @LabServiceCbcId, 'Completed', 'Routine', DATEADD(MINUTE, 21, @ThreeDaysAgo1400Utc), DATEADD(HOUR, 2, @ThreeDaysAgo1400Utc))
) AS source (Id, OrderHeaderId, LabServiceId, OrderStatus, PriorityCode, RequestedAtUtc, ResultedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        OrderHeaderId = source.OrderHeaderId,
        LabServiceId = source.LabServiceId,
        OrderStatus = source.OrderStatus,
        PriorityCode = source.PriorityCode,
        RequestedAtUtc = source.RequestedAtUtc,
        ResultedAtUtc = source.ResultedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, OrderHeaderId, LabServiceId, OrderStatus, PriorityCode, RequestedAtUtc, ResultedAtUtc)
    VALUES (source.Id, source.OrderHeaderId, source.LabServiceId, source.OrderStatus, source.PriorityCode, source.RequestedAtUtc, source.ResultedAtUtc);

IF EXISTS (SELECT 1 FROM lab.Specimens WHERE Id = @SpecimenCompletedId)
BEGIN
    UPDATE lab.Specimens
    SET LabOrderId = @LabOrderCompletedId,
        SpecimenCode = 'SPC-SEED-0001',
        CollectedAtUtc = DATEADD(MINUTE, 40, @ThreeDaysAgo1400Utc),
        ReceivedAtUtc = DATEADD(MINUTE, 55, @ThreeDaysAgo1400Utc),
        Status = 'Received'
    WHERE Id = @SpecimenCompletedId;
END
ELSE
BEGIN
    INSERT INTO lab.Specimens (Id, LabOrderId, SpecimenCode, CollectedAtUtc, ReceivedAtUtc, Status)
    VALUES (@SpecimenCompletedId, @LabOrderCompletedId, 'SPC-SEED-0001', DATEADD(MINUTE, 40, @ThreeDaysAgo1400Utc), DATEADD(MINUTE, 55, @ThreeDaysAgo1400Utc), 'Received');
END

MERGE lab.LabResultItems AS target
USING (VALUES
    (@LabResult1Id, @LabOrderCompletedId, 'HGB', N'Hemoglobin', '13.4', 'g/dL', '12.0-16.0', 'Normal', DATEADD(HOUR, 2, @ThreeDaysAgo1400Utc)),
    (@LabResult2Id, @LabOrderCompletedId, 'WBC', N'Bach cau', '11.8', '10^9/L', '4.0-10.0', 'High', DATEADD(HOUR, 2, @ThreeDaysAgo1400Utc))
) AS source (Id, LabOrderId, AnalyteCode, AnalyteName, ResultValue, Unit, ReferenceRange, AbnormalFlag, VerifiedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        LabOrderId = source.LabOrderId,
        AnalyteCode = source.AnalyteCode,
        AnalyteName = source.AnalyteName,
        ResultValue = source.ResultValue,
        Unit = source.Unit,
        ReferenceRange = source.ReferenceRange,
        AbnormalFlag = source.AbnormalFlag,
        VerifiedAtUtc = source.VerifiedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, LabOrderId, AnalyteCode, AnalyteName, ResultValue, Unit, ReferenceRange, AbnormalFlag, VerifiedAtUtc)
    VALUES (source.Id, source.LabOrderId, source.AnalyteCode, source.AnalyteName, source.ResultValue, source.Unit, source.ReferenceRange, source.AbnormalFlag, source.VerifiedAtUtc);

IF EXISTS (SELECT 1 FROM imaging.ImagingOrders WHERE Id = @ImagingOrderId)
BEGIN
    UPDATE imaging.ImagingOrders
    SET OrderHeaderId = @ImagingOrderHeaderId,
        ImagingServiceId = @ImagingServiceAbdId,
        OrderStatus = 'Completed',
        RequestedAtUtc = DATEADD(MINUTE, 23, @Yesterday1000Utc),
        ReportedAtUtc = DATEADD(HOUR, 1, @Yesterday1000Utc)
    WHERE Id = @ImagingOrderId;
END
ELSE
BEGIN
    INSERT INTO imaging.ImagingOrders (Id, OrderHeaderId, ImagingServiceId, OrderStatus, RequestedAtUtc, ReportedAtUtc)
    VALUES (@ImagingOrderId, @ImagingOrderHeaderId, @ImagingServiceAbdId, 'Completed', DATEADD(MINUTE, 23, @Yesterday1000Utc), DATEADD(HOUR, 1, @Yesterday1000Utc));
END

IF EXISTS (SELECT 1 FROM imaging.ImagingReports WHERE Id = @ImagingReportId)
BEGIN
    UPDATE imaging.ImagingReports
    SET ImagingOrderId = @ImagingOrderId,
        Findings = N'Khong co dich o bung, khong thay bat thuong cap tinh.',
        Impression = N'Hinh anh bung tong quat trong gioi han cho phep.',
        ReportUri = 'local://chan-doan-hinh-anh/report-0001.pdf',
        SignedByUserId = @DoctorUserId,
        SignedAtUtc = DATEADD(HOUR, 1, @Yesterday1000Utc)
    WHERE Id = @ImagingReportId;
END
ELSE
BEGIN
    INSERT INTO imaging.ImagingReports (Id, ImagingOrderId, Findings, Impression, ReportUri, SignedByUserId, SignedAtUtc)
    VALUES (@ImagingReportId, @ImagingOrderId, N'Khong co dich o bung, khong thay bat thuong cap tinh.', N'Hinh anh bung tong quat trong gioi han cho phep.', 'local://chan-doan-hinh-anh/report-0001.pdf', @DoctorUserId, DATEADD(HOUR, 1, @Yesterday1000Utc));
END

MERGE pharmacy.Prescriptions AS target
USING (VALUES
    (@PrescriptionIssuedId, @PrescriptionOrderHeaderIssuedId, 'RX-SEED-0001', 'Issued', N'Toa thuoc dang cho cap phat.', DATEADD(MINUTE, 25, @Today0830Utc)),
    (@PrescriptionDispensedId, @PrescriptionOrderHeaderDispensedId, 'RX-SEED-0002', 'Dispensed', N'Toa thuoc da cap xong tai quay thuoc.', DATEADD(MINUTE, 26, @ThreeDaysAgo1400Utc))
) AS source (Id, OrderHeaderId, PrescriptionNumber, Status, Notes, CreatedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        OrderHeaderId = source.OrderHeaderId,
        PrescriptionNumber = source.PrescriptionNumber,
        Status = source.Status,
        Notes = source.Notes,
        CreatedAtUtc = source.CreatedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, OrderHeaderId, PrescriptionNumber, Status, Notes, CreatedAtUtc)
    VALUES (source.Id, source.OrderHeaderId, source.PrescriptionNumber, source.Status, source.Notes, source.CreatedAtUtc);

MERGE pharmacy.PrescriptionItems AS target
USING (VALUES
    (@PrescriptionItem1Id, @PrescriptionIssuedId, @MedicineOmeprazoleId, N'1 vien truoc bua sang', N'Oral', N'OnceDaily', 14, 14, 3500),
    (@PrescriptionItem2Id, @PrescriptionDispensedId, @MedicineLosartanId, N'1 vien sau bua sang', N'Oral', N'OnceDaily', 30, 30, 4000),
    (@PrescriptionItem3Id, @PrescriptionDispensedId, @MedicineOmeprazoleId, N'1 vien truoc bua sang', N'Oral', N'OnceDaily', 14, 14, 3500)
) AS source (Id, PrescriptionId, MedicineId, DoseInstruction, Route, Frequency, DurationDays, Quantity, UnitPrice)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        PrescriptionId = source.PrescriptionId,
        MedicineId = source.MedicineId,
        DoseInstruction = source.DoseInstruction,
        Route = source.Route,
        Frequency = source.Frequency,
        DurationDays = source.DurationDays,
        Quantity = source.Quantity,
        UnitPrice = source.UnitPrice
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, PrescriptionId, MedicineId, DoseInstruction, Route, Frequency, DurationDays, Quantity, UnitPrice)
    VALUES (source.Id, source.PrescriptionId, source.MedicineId, source.DoseInstruction, source.Route, source.Frequency, source.DurationDays, source.Quantity, source.UnitPrice);

IF EXISTS (SELECT 1 FROM pharmacy.Dispensings WHERE Id = @DispensingId)
BEGIN
    UPDATE pharmacy.Dispensings
    SET PrescriptionId = @PrescriptionDispensedId,
        DispensingStatus = 'Dispensed',
        DispensedAtUtc = DATEADD(HOUR, 2, @ThreeDaysAgo1400Utc),
        DispensedByUserId = @CashierPharmacyUserId,
        Notes = N'Da doi chieu toa va huong dan cach dung cho benh nhan.'
    WHERE Id = @DispensingId;
END
ELSE
BEGIN
    INSERT INTO pharmacy.Dispensings (Id, PrescriptionId, DispensingStatus, DispensedAtUtc, DispensedByUserId, Notes)
    VALUES (@DispensingId, @PrescriptionDispensedId, 'Dispensed', DATEADD(HOUR, 2, @ThreeDaysAgo1400Utc), @CashierPharmacyUserId, N'Da doi chieu toa va huong dan cach dung cho benh nhan.');
END

MERGE pharmacy.InventoryBatches AS target
USING (VALUES
    (@InventoryBatchAId, @MedicineLosartanId, 'LOS-2026-01', CAST('2027-12-31' AS DATE), 180, 2500, 'MAIN'),
    (@InventoryBatchBId, @MedicineOmeprazoleId, 'OME-2026-01', CAST('2027-08-31' AS DATE), 240, 1800, 'MAIN')
) AS source (Id, MedicineId, BatchNumber, ExpiryDate, QuantityOnHand, UnitCost, WarehouseCode)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        MedicineId = source.MedicineId,
        BatchNumber = source.BatchNumber,
        ExpiryDate = source.ExpiryDate,
        QuantityOnHand = source.QuantityOnHand,
        UnitCost = source.UnitCost,
        WarehouseCode = source.WarehouseCode
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, MedicineId, BatchNumber, ExpiryDate, QuantityOnHand, UnitCost, WarehouseCode)
    VALUES (source.Id, source.MedicineId, source.BatchNumber, source.ExpiryDate, source.QuantityOnHand, source.UnitCost, source.WarehouseCode);

MERGE pharmacy.InventoryTransactions AS target
USING (VALUES
    (@InventoryTxReceiptAId, @InventoryBatchAId, 'Receipt', 200, DATEADD(DAY, -10, @NowUtc), 'PurchaseOrder', NULL),
    (@InventoryTxIssueAId, @InventoryBatchAId, 'Issue', -20, DATEADD(HOUR, 2, @ThreeDaysAgo1400Utc), 'Prescription', @PrescriptionDispensedId),
    (@InventoryTxReceiptBId, @InventoryBatchBId, 'Receipt', 250, DATEADD(DAY, -12, @NowUtc), 'PurchaseOrder', NULL)
) AS source (Id, InventoryBatchId, TransactionType, Quantity, OccurredAtUtc, ReferenceType, ReferenceId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        InventoryBatchId = source.InventoryBatchId,
        TransactionType = source.TransactionType,
        Quantity = source.Quantity,
        OccurredAtUtc = source.OccurredAtUtc,
        ReferenceType = source.ReferenceType,
        ReferenceId = source.ReferenceId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, InventoryBatchId, TransactionType, Quantity, OccurredAtUtc, ReferenceType, ReferenceId)
    VALUES (source.Id, source.InventoryBatchId, source.TransactionType, source.Quantity, source.OccurredAtUtc, source.ReferenceType, source.ReferenceId);

MERGE billing.Invoices AS target
USING (VALUES
    (@InvoicePartialId, 'INV-SEED-0001', @SeniorPatientHospitalId, @EncounterInProgressId, 'PartiallyPaid', 'VND', 370000, 0, 0, 370000, DATEADD(MINUTE, 25, @Today0830Utc), DATEADD(DAY, 1, @Today0830Utc)),
    (@InvoicePaidId, 'INV-SEED-0002', @PortalPatientHospitalId, @EncounterApprovedId, 'Paid', 'VND', 490000, 20000, 150000, 320000, DATEADD(HOUR, 3, @ThreeDaysAgo1400Utc), DATEADD(DAY, 7, @ThreeDaysAgo1400Utc)),
    (@InvoiceRefundedId, 'INV-SEED-0003', @MaternityPatientHospitalId, @EncounterFinalizedId, 'Paid', 'VND', 530000, 0, 0, 530000, DATEADD(HOUR, 2, @Yesterday1000Utc), DATEADD(DAY, 5, @Yesterday1000Utc))
) AS source (Id, InvoiceNumber, PatientId, EncounterId, InvoiceStatus, CurrencyCode, SubtotalAmount, DiscountAmount, InsuranceAmount, TotalAmount, IssuedAtUtc, DueAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        InvoiceNumber = source.InvoiceNumber,
        PatientId = source.PatientId,
        EncounterId = source.EncounterId,
        InvoiceStatus = source.InvoiceStatus,
        CurrencyCode = source.CurrencyCode,
        SubtotalAmount = source.SubtotalAmount,
        DiscountAmount = source.DiscountAmount,
        InsuranceAmount = source.InsuranceAmount,
        TotalAmount = source.TotalAmount,
        IssuedAtUtc = source.IssuedAtUtc,
        DueAtUtc = source.DueAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, InvoiceNumber, PatientId, EncounterId, InvoiceStatus, CurrencyCode, SubtotalAmount, DiscountAmount, InsuranceAmount, TotalAmount, IssuedAtUtc, DueAtUtc)
    VALUES (source.Id, source.InvoiceNumber, source.PatientId, source.EncounterId, source.InvoiceStatus, source.CurrencyCode, source.SubtotalAmount, source.DiscountAmount, source.InsuranceAmount, source.TotalAmount, source.IssuedAtUtc, source.DueAtUtc);

MERGE billing.InvoiceItems AS target
USING (VALUES
    (@InvoiceItem1Id, @InvoicePartialId, @ServiceConsultGeneralId, 'Consultation', N'Kham noi tong quat', 1, 250000, 250000, 'Encounter', @EncounterInProgressId),
    (@InvoiceItem2Id, @InvoicePartialId, @ServiceLabCbcId, 'Laboratory', N'Cong thuc mau CBC', 1, 120000, 120000, 'LabOrder', @LabOrderPendingId),
    (@InvoiceItem3Id, @InvoicePaidId, @ServiceConsultGeneralId, 'Consultation', N'Kham tang huyet ap', 1, 250000, 250000, 'Encounter', @EncounterApprovedId),
    (@InvoiceItem4Id, @InvoicePaidId, @ServicePharmacyId, 'Pharmacy', N'Cap phat Losartan 50mg', 30, 4000, 120000, 'Prescription', @PrescriptionDispensedId),
    (@InvoiceItem5Id, @InvoiceRefundedId, @ServiceConsultGeneralId, 'Consultation', N'Kham san phu khoa', 1, 250000, 250000, 'Encounter', @EncounterFinalizedId),
    (@InvoiceItem6Id, @InvoiceRefundedId, @ServiceImagingAbdId, 'Imaging', N'Sieu am bung tong quat', 1, 280000, 280000, 'ImagingOrder', @ImagingOrderId)
) AS source (Id, InvoiceId, ServiceCatalogId, ItemType, Description, Quantity, UnitPrice, LineAmount, ReferenceType, ReferenceId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        InvoiceId = source.InvoiceId,
        ServiceCatalogId = source.ServiceCatalogId,
        ItemType = source.ItemType,
        Description = source.Description,
        Quantity = source.Quantity,
        UnitPrice = source.UnitPrice,
        LineAmount = source.LineAmount,
        ReferenceType = source.ReferenceType,
        ReferenceId = source.ReferenceId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, InvoiceId, ServiceCatalogId, ItemType, Description, Quantity, UnitPrice, LineAmount, ReferenceType, ReferenceId)
    VALUES (source.Id, source.InvoiceId, source.ServiceCatalogId, source.ItemType, source.Description, source.Quantity, source.UnitPrice, source.LineAmount, source.ReferenceType, source.ReferenceId);

MERGE billing.Payments AS target
USING (VALUES
    (@PaymentPartialId, @InvoicePartialId, 'PAY-SEED-0001', 'Cash', 'CashDesk', 200000, 'Captured', DATEADD(MINUTE, 40, @Today0830Utc), @CashierUserId, 'MANUAL-CASH-0001'),
    (@PaymentPaidId, @InvoicePaidId, 'PAY-SEED-0002', 'QrCode', 'MockGateway', 320000, 'Captured', DATEADD(HOUR, 4, @ThreeDaysAgo1400Utc), NULL, 'MOCK-TXN-0002'),
    (@PaymentRefundedId, @InvoiceRefundedId, 'PAY-SEED-0003', 'Card', 'MockGateway', 530000, 'Refunded', DATEADD(HOUR, 3, @Yesterday1000Utc), @CashierUserId, 'MOCK-TXN-REFUND-0003')
) AS source (Id, InvoiceId, PaymentReference, PaymentMethod, GatewayProvider, Amount, PaymentStatus, PaidAtUtc, ReceivedByUserId, ExternalTransactionId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        InvoiceId = source.InvoiceId,
        PaymentReference = source.PaymentReference,
        PaymentMethod = source.PaymentMethod,
        GatewayProvider = source.GatewayProvider,
        Amount = source.Amount,
        PaymentStatus = source.PaymentStatus,
        PaidAtUtc = source.PaidAtUtc,
        ReceivedByUserId = source.ReceivedByUserId,
        ExternalTransactionId = source.ExternalTransactionId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, InvoiceId, PaymentReference, PaymentMethod, GatewayProvider, Amount, PaymentStatus, PaidAtUtc, ReceivedByUserId, ExternalTransactionId)
    VALUES (source.Id, source.InvoiceId, source.PaymentReference, source.PaymentMethod, source.GatewayProvider, source.Amount, source.PaymentStatus, source.PaidAtUtc, source.ReceivedByUserId, source.ExternalTransactionId);

IF EXISTS (SELECT 1 FROM billing.Refunds WHERE Id = @RefundId)
BEGIN
    UPDATE billing.Refunds
    SET PaymentId = @PaymentRefundedId,
        Amount = 80000,
        Reason = N'Hoan tien mot phan do dich vu hinh anh huy',
        RefundedAtUtc = DATEADD(HOUR, 6, @Yesterday1000Utc),
        RefundedByUserId = @CashierUserId
    WHERE Id = @RefundId;
END
ELSE
BEGIN
    INSERT INTO billing.Refunds (Id, PaymentId, Amount, Reason, RefundedAtUtc, RefundedByUserId)
    VALUES (@RefundId, @PaymentRefundedId, 80000, N'Hoan tien mot phan do dich vu hinh anh huy', DATEADD(HOUR, 6, @Yesterday1000Utc), @CashierUserId);
END

IF EXISTS (SELECT 1 FROM billing.InsuranceClaims WHERE Id = @InsuranceClaimId)
BEGIN
    UPDATE billing.InsuranceClaims
    SET InvoiceId = @InvoicePaidId,
        PatientInsurancePolicyId = @PortalPolicyId,
        ClaimNumber = 'CLM-SEED-0001',
        ClaimStatus = 'Approved',
        ClaimedAmount = 150000,
        ApprovedAmount = 150000,
        SubmittedAtUtc = DATEADD(HOUR, 5, @ThreeDaysAgo1400Utc),
        SettledAtUtc = DATEADD(DAY, -1, @NowUtc)
    WHERE Id = @InsuranceClaimId;
END
ELSE
BEGIN
    INSERT INTO billing.InsuranceClaims (Id, InvoiceId, PatientInsurancePolicyId, ClaimNumber, ClaimStatus, ClaimedAmount, ApprovedAmount, SubmittedAtUtc, SettledAtUtc)
    VALUES (@InsuranceClaimId, @InvoicePaidId, @PortalPolicyId, 'CLM-SEED-0001', 'Approved', 150000, 150000, DATEADD(HOUR, 5, @ThreeDaysAgo1400Utc), DATEADD(DAY, -1, @NowUtc));
END

MERGE notification.OutboxMessages AS target
USING (VALUES
    (@OutboxPendingId, 'Appointment', @AppointmentFutureId, 'AppointmentReminder.v1', N'{"appointmentNumber":"APT-SEED-0001","channel":"Sms"}', 'Pending', DATEADD(MINUTE, 15, @NowUtc), NULL),
    (@OutboxDeliveredId, 'Invoice', @InvoicePaidId, 'InvoiceIssued.v1', N'{"invoiceNumber":"INV-SEED-0002","amount":320000}', 'Published', DATEADD(HOUR, -12, @NowUtc), DATEADD(HOUR, -11, @NowUtc)),
    (@OutboxFailedId, 'Prescription', @PrescriptionIssuedId, 'PrescriptionIssued.v1', N'{"prescriptionNumber":"RX-SEED-0001","channel":"Email"}', 'Published', DATEADD(HOUR, -8, @NowUtc), DATEADD(HOUR, -7, @NowUtc))
) AS source (Id, AggregateType, AggregateId, EventType, PayloadJson, Status, AvailableAtUtc, PublishedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        AggregateType = source.AggregateType,
        AggregateId = source.AggregateId,
        EventType = source.EventType,
        PayloadJson = source.PayloadJson,
        Status = source.Status,
        AvailableAtUtc = source.AvailableAtUtc,
        PublishedAtUtc = source.PublishedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, AggregateType, AggregateId, EventType, PayloadJson, Status, AvailableAtUtc, PublishedAtUtc)
    VALUES (source.Id, source.AggregateType, source.AggregateId, source.EventType, source.PayloadJson, source.Status, source.AvailableAtUtc, source.PublishedAtUtc);

MERGE notification.NotificationDeliveries AS target
USING (VALUES
    (@DeliveryQueuedId, @OutboxPendingId, 'Sms', '0909000001', 'Queued', NULL, 0, NULL, NULL, NULL),
    (@DeliveryDeliveredId, @OutboxDeliveredId, 'App', 'portal-nguyen-minh-anh', 'Delivered', 'MSG-0002', 1, DATEADD(HOUR, -11, @NowUtc), DATEADD(HOUR, -11, @NowUtc), NULL),
    (@DeliveryFailedId, @OutboxFailedId, 'Email', 'patient00@erm.local', 'Failed', 'MSG-0003', 2, DATEADD(HOUR, -7, @NowUtc), NULL, N'SMTP timeout')
) AS source (Id, OutboxMessageId, ChannelCode, Recipient, DeliveryStatus, ProviderMessageId, AttemptCount, LastAttemptAtUtc, DeliveredAtUtc, ErrorMessage)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        OutboxMessageId = source.OutboxMessageId,
        ChannelCode = source.ChannelCode,
        Recipient = source.Recipient,
        DeliveryStatus = source.DeliveryStatus,
        ProviderMessageId = source.ProviderMessageId,
        AttemptCount = source.AttemptCount,
        LastAttemptAtUtc = source.LastAttemptAtUtc,
        DeliveredAtUtc = source.DeliveredAtUtc,
        ErrorMessage = source.ErrorMessage
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, OutboxMessageId, ChannelCode, Recipient, DeliveryStatus, ProviderMessageId, AttemptCount, LastAttemptAtUtc, DeliveredAtUtc, ErrorMessage)
    VALUES (source.Id, source.OutboxMessageId, source.ChannelCode, source.Recipient, source.DeliveryStatus, source.ProviderMessageId, source.AttemptCount, source.LastAttemptAtUtc, source.DeliveredAtUtc, source.ErrorMessage);

MERGE [identity].SecurityEvents AS target
USING (VALUES
    (@SecurityEvent1Id, @AdminUserId, 'LoginSucceeded', 'Info', N'Tai khoan quan tri dang nhap thanh cong tu may tram noi bo.', '127.0.0.1', 'PortalClient/1.0', DATEADD(DAY, -1, @NowUtc)),
    (@SecurityEvent2Id, @PatientUserId, 'MfaChallengeIssued', 'Info', N'Tai khoan benh nhan yeu cau xac thuc MFA trong qua trinh dang nhap.', '127.0.0.1', 'PortalClient/1.0', DATEADD(HOUR, -10, @NowUtc))
) AS source (Id, UserId, EventType, Severity, Detail, IpAddress, UserAgent, OccurredAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        UserId = source.UserId,
        EventType = source.EventType,
        Severity = source.Severity,
        Detail = source.Detail,
        IpAddress = source.IpAddress,
        UserAgent = source.UserAgent,
        OccurredAtUtc = source.OccurredAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, UserId, EventType, Severity, Detail, IpAddress, UserAgent, OccurredAtUtc)
    VALUES (source.Id, source.UserId, source.EventType, source.Severity, source.Detail, source.IpAddress, source.UserAgent, source.OccurredAtUtc);

MERGE audit.AuditLogs AS target
USING (VALUES
    (@Audit1Id, @CashierUserId, 'Invoice', @InvoicePartialId, 'COLLECT_PAYMENT', N'{"status":"Issued"}', N'{"status":"PartiallyPaid","amount":200000}', 'billing-001', DATEADD(MINUTE, 41, @Today0830Utc)),
    (@Audit2Id, @DoctorUserId, 'Encounter', @EncounterApprovedId, 'APPROVE_MEDICAL_RECORD', N'{"status":"Finalized"}', N'{"status":"Approved"}', 'emr-001', DATEADD(MINUTE, 50, @ThreeDaysAgo1400Utc))
) AS source (Id, UserId, EntityType, EntityId, ActionCode, BeforeJson, AfterJson, CorrelationId, CreatedAtUtc)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        UserId = source.UserId,
        EntityType = source.EntityType,
        EntityId = source.EntityId,
        ActionCode = source.ActionCode,
        BeforeJson = source.BeforeJson,
        AfterJson = source.AfterJson,
        CorrelationId = source.CorrelationId,
        CreatedAtUtc = source.CreatedAtUtc
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, UserId, EntityType, EntityId, ActionCode, BeforeJson, AfterJson, CorrelationId, CreatedAtUtc)
    VALUES (source.Id, source.UserId, source.EntityType, source.EntityId, source.ActionCode, source.BeforeJson, source.AfterJson, source.CorrelationId, source.CreatedAtUtc);

MERGE audit.EntityAccessLogs AS target
USING (VALUES
    (@Access1Id, @DoctorUserId, 'Patient', @SeniorPatientHospitalId, 'Read', DATEADD(MINUTE, 12, @Today0830Utc), '127.0.0.1'),
    (@Access2Id, @PatientUserId, 'Invoice', @InvoicePaidId, 'Read', DATEADD(HOUR, -9, @NowUtc), '127.0.0.1')
) AS source (Id, UserId, EntityType, EntityId, AccessType, AccessedAtUtc, IpAddress)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        UserId = source.UserId,
        EntityType = source.EntityType,
        EntityId = source.EntityId,
        AccessType = source.AccessType,
        AccessedAtUtc = source.AccessedAtUtc,
        IpAddress = source.IpAddress
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, UserId, EntityType, EntityId, AccessType, AccessedAtUtc, IpAddress)
    VALUES (source.Id, source.UserId, source.EntityType, source.EntityId, source.AccessType, source.AccessedAtUtc, source.IpAddress);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO [identity].Users (Id, Username, Email, PasswordHash, PrimaryRoleCode, IsActive, EmailVerifiedAtUtc, LastLoginAtUtc, CreatedAtUtc, UpdatedAtUtc, DeletedAtUtc)
SELECT NEWID(),
       CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2)),
       CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2), '@erm.local'),
       @PasswordHash,
       seed.RoleCode,
       1,
       DATEADD(DAY, -30, @NowUtc),
       DATEADD(DAY, -1, @NowUtc),
       DATEADD(DAY, -120, @NowUtc),
       @NowUtc,
       NULL
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
    FROM [identity].Users existing
    WHERE existing.Username = CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
)
OPTION (MAXRECURSION 19);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO [identity].UserRoles (UserId, RoleCode, GrantedAtUtc, GrantedByUserId)
SELECT u.Id, seed.RoleCode, DATEADD(DAY, -120, @NowUtc), @AdminUserId
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
JOIN [identity].Users u
    ON u.Username = CONCAT(seed.Prefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
WHERE NOT EXISTS (
    SELECT 1
    FROM [identity].UserRoles ur
    WHERE ur.UserId = u.Id
      AND ur.RoleCode = seed.RoleCode
)
OPTION (MAXRECURSION 19);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO org.StaffProfiles (Id, UserId, StaffCode, FullName, DepartmentId, Phone, Email, HireDate, IsActive, CreatedAtUtc)
SELECT NEWID(),
       u.Id,
       CONCAT(seed.StaffPrefix, RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3)),
       CONCAT(seed.DisplayName, N' ', RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2)),
       dept.Id,
       CONCAT('0907', RIGHT(CONCAT('000000', CAST(seed.PhoneSeed + n.NumberValue AS VARCHAR(6))), 6)),
       u.Email,
       DATEADD(DAY, -1 * (600 + n.NumberValue), CAST(@NowUtc AS DATE)),
       1,
       DATEADD(DAY, -120, @NowUtc)
FROM Numbers n
CROSS JOIN (VALUES
    ('ADX', N'Nguyen Gia An', 'admin', 'OPD', 1000),
    ('BSX', N'Tran Minh Khang', 'doctor', 'OPD', 2000),
    ('TNQ', N'Le Thu Quynh', 'cashierrec', 'OPD', 3000),
    ('TNO', N'Pham Gia Han', 'cashierops', 'OPD', 4000),
    ('TNP', N'Vo Duc Huy', 'cashierpha', 'PHA', 5000),
    ('TNL', N'Dang Bao Chau', 'cashierlab', 'LAB', 6000),
    ('TNX', N'Bui Thanh Ha', 'cashier', 'OPD', 7000)
) seed (StaffPrefix, DisplayName, UsernamePrefix, DepartmentCode, PhoneSeed)
JOIN [identity].Users u
    ON u.Username = CONCAT(seed.UsernamePrefix, RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
JOIN org.Departments dept
    ON dept.DepartmentCode = seed.DepartmentCode
WHERE NOT EXISTS (
    SELECT 1
    FROM org.StaffProfiles sp
    WHERE sp.StaffCode = CONCAT(seed.StaffPrefix, RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
)
OPTION (MAXRECURSION 19);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO org.DoctorProfiles (Id, StaffProfileId, SpecialtyId, LicenseNumber, Biography, YearsOfExperience, ConsultationFee, IsBookable)
SELECT NEWID(),
       sp.Id,
       spec.Id,
       CONCAT('GEN-X-', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3)),
       N'Bac si kham ngoai tru phuc vu kiem thu he thong.',
       4 + n.NumberValue,
       220000 + (n.NumberValue * 1000),
       1
FROM Numbers n
JOIN org.StaffProfiles sp
    ON sp.StaffCode = CONCAT('BSX', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
JOIN org.Specialties spec
    ON spec.SpecialtyCode = CASE n.NumberValue % 6
        WHEN 1 THEN 'CARD'
        WHEN 2 THEN 'GASTRO'
        WHEN 3 THEN 'OBGYN'
        WHEN 4 THEN 'PED'
        WHEN 5 THEN 'MSK'
        ELSE 'NEURO'
    END
WHERE NOT EXISTS (
    SELECT 1
    FROM org.DoctorProfiles dp
    WHERE dp.StaffProfileId = sp.Id
)
OPTION (MAXRECURSION 19);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
UPDATE dp
SET SpecialtyId = spec.Id,
    LicenseNumber = CONCAT(spec.SpecialtyCode, '-X-', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3)),
    Biography = N'Bac si kham ngoai tru phuc vu kiem thu he thong.',
    IsBookable = 1
FROM Numbers n
JOIN org.StaffProfiles sp
    ON sp.StaffCode = CONCAT('BSX', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
JOIN org.DoctorProfiles dp
    ON dp.StaffProfileId = sp.Id
JOIN org.Specialties spec
    ON spec.SpecialtyCode = CASE n.NumberValue % 6
        WHEN 1 THEN 'CARD'
        WHEN 2 THEN 'GASTRO'
        WHEN 3 THEN 'OBGYN'
        WHEN 4 THEN 'PED'
        WHEN 5 THEN 'MSK'
        ELSE 'NEURO'
    END
OPTION (MAXRECURSION 19);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO org.DoctorSchedules (Id, DoctorProfileId, ClinicId, DayOfWeek, StartTime, EndTime, SlotMinutes, ValidFrom, ValidTo, IsActive)
SELECT NEWID(),
       dp.Id,
       clinic.Id,
       CASE WHEN n.NumberValue % 6 = 0 THEN 6 ELSE ((n.NumberValue - 1) % 5) + 1 END,
       CAST('08:00' AS TIME),
       CAST('11:30' AS TIME),
       30,
       CAST('2026-01-01' AS DATE),
       NULL,
       1
FROM Numbers n
JOIN org.StaffProfiles sp
    ON sp.StaffCode = CONCAT('BSX', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
JOIN org.DoctorProfiles dp
    ON dp.StaffProfileId = sp.Id
JOIN org.Clinics clinic
    ON clinic.ClinicCode = CASE WHEN n.NumberValue % 2 = 0 THEN 'CLN-02' ELSE 'CLN-01' END
WHERE NOT EXISTS (
    SELECT 1
    FROM org.DoctorSchedules ds
    WHERE ds.DoctorProfileId = dp.Id
      AND ds.DayOfWeek = CASE WHEN n.NumberValue % 6 = 0 THEN 6 ELSE ((n.NumberValue - 1) % 5) + 1 END
      AND ds.StartTime = CAST('08:00' AS TIME)
)
OPTION (MAXRECURSION 19);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 56
)
INSERT INTO patient.Patients
(
    Id,
    MedicalRecordNumber,
    FullName,
    DateOfBirth,
    Gender,
    Phone,
    Email,
    AddressLine1,
    AddressLine2,
    Ward,
    District,
    Province,
    Nationality,
    IdentityNumber,
    Occupation,
    MaritalStatus,
    CreatedAtUtc,
    UpdatedAtUtc,
    DeletedAtUtc
)
SELECT NEWID(),
       CONCAT('MRN-BULK-', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3)),
       CONCAT(
            CHOOSE(((n.NumberValue + 2 - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
            N' ',
            CHOOSE(((n.NumberValue + 4 - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
            N' ',
            CHOOSE(((n.NumberValue + 6 - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
       ),
       DATEADD(DAY, -1 * (7000 + n.NumberValue), CAST(@NowUtc AS DATE)),
       CASE WHEN n.NumberValue % 2 = 0 THEN N'Female' ELSE N'Male' END,
       CONCAT('0933', RIGHT(CONCAT('000000', CAST(n.NumberValue AS VARCHAR(6))), 6)),
       CONCAT('benhnhan', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3), '@erm.local'),
       CONCAT(N'So ', 20 + n.NumberValue, N' Duong Le Loi'),
       NULL,
       N'Phuong Ben Thanh',
       CASE WHEN n.NumberValue % 3 = 0 THEN N'Quan 1' ELSE N'Quan 3' END,
       N'TP.HCM',
       N'Viet Nam',
       CONCAT('079', RIGHT(CONCAT('000000000', CAST(100000000 + n.NumberValue AS VARCHAR(9))), 9)),
       CASE WHEN n.NumberValue % 4 = 0 THEN N'Nhan vien van phong' ELSE N'Tu doanh' END,
       CASE WHEN n.NumberValue % 5 = 0 THEN N'Single' ELSE N'Married' END,
       DATEADD(DAY, -90, @NowUtc),
       @NowUtc,
       NULL
FROM Numbers n
WHERE NOT EXISTS (
    SELECT 1
    FROM patient.Patients p
    WHERE p.MedicalRecordNumber = CONCAT('MRN-BULK-', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
)
OPTION (MAXRECURSION 56);

;WITH Numbers AS
(
    SELECT 1 AS NumberValue
    UNION ALL
    SELECT NumberValue + 1
    FROM Numbers
    WHERE NumberValue < 19
)
INSERT INTO patient.PatientAccounts (PatientId, UserId, ActivatedAtUtc, PortalStatus)
SELECT p.Id, u.Id, DATEADD(DAY, -45, @NowUtc), 'Active'
FROM Numbers n
JOIN patient.Patients p
    ON p.MedicalRecordNumber = CONCAT('MRN-BULK-', RIGHT(CONCAT('000', CAST(n.NumberValue AS VARCHAR(3))), 3))
JOIN [identity].Users u
    ON u.Username = CONCAT('patient', RIGHT(CONCAT('00', CAST(n.NumberValue AS VARCHAR(2))), 2))
WHERE NOT EXISTS (
    SELECT 1
    FROM patient.PatientAccounts pa
    WHERE pa.PatientId = p.Id
)
OPTION (MAXRECURSION 19);

;WITH NumberedInternalUsers AS
(
    SELECT u.Id,
           u.Username,
           u.PrimaryRoleCode,
           TRY_CONVERT(INT, RIGHT(u.Username, 2)) AS NumberValue
    FROM [identity].Users u
    WHERE u.Username LIKE '%[0-9][0-9]'
      AND u.PrimaryRoleCode IN ('Admin', 'Doctor', 'Cashier')
)
UPDATE sp
SET FullName = CONCAT(
        CHOOSE(((niu.NumberValue + roleOffset.OffsetA - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
        N' ',
        CHOOSE(((niu.NumberValue + roleOffset.OffsetB - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
        N' ',
        CHOOSE(((niu.NumberValue + roleOffset.OffsetC - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
    ),
    Email = CONCAT(niu.Username, '@erm.local')
FROM org.StaffProfiles sp
JOIN NumberedInternalUsers niu ON niu.Id = sp.UserId
CROSS APPLY
(
    SELECT CASE niu.PrimaryRoleCode
            WHEN 'Admin' THEN 1
            WHEN 'Doctor' THEN 2
            WHEN 'Cashier' THEN 3
            ELSE 7
        END AS OffsetA,
        CASE niu.PrimaryRoleCode
            WHEN 'Admin' THEN 4
            WHEN 'Doctor' THEN 5
            WHEN 'Cashier' THEN 6
            ELSE 10
        END AS OffsetB,
        CASE niu.PrimaryRoleCode
            WHEN 'Admin' THEN 7
            WHEN 'Doctor' THEN 8
            WHEN 'Cashier' THEN 9
            ELSE 13
        END AS OffsetC
) roleOffset;

;WITH NumberedPortalPatients AS
(
    SELECT p.Id,
           u.Username,
           TRY_CONVERT(INT, RIGHT(u.Username, 2)) AS NumberValue
    FROM patient.PatientAccounts pa
    JOIN patient.Patients p ON p.Id = pa.PatientId
    JOIN [identity].Users u ON u.Id = pa.UserId
    WHERE u.Username LIKE 'patient[0-9][0-9]'
)
UPDATE p
SET FullName = CONCAT(
        CHOOSE(((npp.NumberValue + 2 - 1) % 10) + 1, N'Nguyen', N'Tran', N'Le', N'Pham', N'Hoang', N'Vo', N'Dang', N'Bui', N'Do', N'Phan'),
        N' ',
        CHOOSE(((npp.NumberValue + 5 - 1) % 10) + 1, N'Gia', N'Thanh', N'Minh', N'Thu', N'Ngoc', N'Anh', N'Duc', N'Huu', N'Bao', N'Quynh'),
        N' ',
        CHOOSE(((npp.NumberValue + 8 - 1) % 12) + 1, N'An', N'Binh', N'Chau', N'Dung', N'Hanh', N'Khanh', N'Lam', N'Mai', N'Nam', N'Phuc', N'Quang', N'Trang')
    ),
    Email = CONCAT(npp.Username, '@erm.local'),
    MedicalRecordNumber = CONCAT('MRN-', RIGHT(CONCAT('0000', CAST(100 + npp.NumberValue AS VARCHAR(4))), 4))
FROM patient.Patients p
JOIN NumberedPortalPatients npp ON npp.Id = p.Id;
GO




