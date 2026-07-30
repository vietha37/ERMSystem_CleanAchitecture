SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- 1. SEED MEDICINES (pharmacy.Medicines)
INSERT INTO pharmacy.Medicines (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit, IsControlled, IsActive)
SELECT source.Id, source.DrugCode, source.Name, source.GenericName, source.Strength, source.DosageForm, source.Unit, source.IsControlled, source.IsActive
FROM (VALUES
    ('76000000-0000-0000-0000-000000000001', 'MED-LOS-50', N'Losartan 50mg', N'Losartan', N'50mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000002', 'MED-OME-20', N'Omeprazole 20mg', N'Omeprazole', N'20mg', N'Viên nang', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000003', 'MED-PAR-500', N'Paracetamol 500mg', N'Paracetamol', N'500mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000004', 'MED-AMO-500', N'Amoxicillin 500mg', N'Amoxicillin', N'500mg', N'Viên nang', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000005', 'MED-IBU-400', N'Ibuprofen 400mg', N'Ibuprofen', N'400mg', N'Viên nén bao phim', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000006', 'MED-MET-500', N'Metformin 500mg', N'Metformin', N'500mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000007', 'MED-ATO-20', N'Atorvastatin 20mg', N'Atorvastatin', N'20mg', N'Viên bao phim', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000008', 'MED-CET-10', N'Cetirizine 10mg', N'Cetirizine', N'10mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000009', 'MED-SAL-02', N'Salbutamol 2mg', N'Salbutamol', N'2mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000010', 'MED-BER-50', N'Berberin 50mg', N'Berberin', N'50mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000011', 'MED-VIT-C500', N'Vitamin C 500mg', N'Acid Ascorbic', N'500mg', N'Viên sủi', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000012', 'MED-CEF-500', N'Cefuroxime 500mg', N'Cefuroxime', N'500mg', N'Viên nén bao phim', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000013', 'MED-LOR-10', N'Loratadine 10mg', N'Loratadine', N'10mg', N'Viên nén', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000014', 'MED-ASP-81', N'Aspirin 81mg', N'Acid Acetylsalicylic', N'81mg', N'Viên nén bao tan', N'Viên', 0, 1),
    ('76000000-0000-0000-0000-000000000015', 'MED-AZI-500', N'Azithromycin 500mg', N'Azithromycin', N'500mg', N'Viên bao phim', N'Viên', 0, 1)
) AS source (Id, DrugCode, Name, GenericName, Strength, DosageForm, Unit, IsControlled, IsActive)
WHERE NOT EXISTS (
    SELECT 1 FROM pharmacy.Medicines m WHERE m.DrugCode = source.DrugCode OR m.Id = source.Id
);

-- 2. SEED ADDITIONAL APPOINTMENTS (scheduling.Appointments)
DECLARE @Doctor1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.DoctorProfiles ORDER BY Id);
DECLARE @Doctor2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.DoctorProfiles ORDER BY Id DESC);
DECLARE @Clinic1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Clinics ORDER BY Id);
DECLARE @Clinic2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM org.Clinics ORDER BY Id DESC);
DECLARE @AdminUser UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM [identity].Users WHERE PrimaryRoleCode = 'Admin');

-- Fetch patient IDs
DECLARE @PatientsTable TABLE (RowIdx INT IDENTITY(1,1), PatientId UNIQUEIDENTIFIER);
INSERT INTO @PatientsTable (PatientId)
SELECT Id FROM patient.Patients ORDER BY CreatedAtUtc DESC;

DECLARE @NowUtc DATETIME2 = SYSUTCDATETIME();

-- Seed 20 additional Appointments
DECLARE @i INT = 1;
WHILE @i <= 20
BEGIN
    DECLARE @PatientId UNIQUEIDENTIFIER = (SELECT PatientId FROM @PatientsTable WHERE RowIdx = ((@i % 60) + 1));
    DECLARE @AptId UNIQUEIDENTIFIER = CAST(HASHBYTES('MD5', CONCAT('DEMO_APT_', @i)) AS UNIQUEIDENTIFIER);
    DECLARE @AptNum NVARCHAR(50) = CONCAT('APT-DEMO-', RIGHT(CONCAT('0000', @i), 4));
    DECLARE @Status NVARCHAR(50) = CASE WHEN @i % 4 = 0 THEN 'Completed' WHEN @i % 4 = 1 THEN 'CheckedIn' WHEN @i % 4 = 2 THEN 'Scheduled' ELSE 'Cancelled' END;
    DECLARE @OffsetMinutes INT = (@i - 10) * 120;
    DECLARE @StartUtc DATETIME2 = DATEADD(MINUTE, @OffsetMinutes, @NowUtc);
    DECLARE @EndUtc DATETIME2 = DATEADD(MINUTE, 30, @StartUtc);
    DECLARE @Complaint NVARCHAR(255) = CASE (@i % 5)
        WHEN 0 THEN N'Ho, sốt nhẹ và mệt mỏi kéo dài 3 ngày'
        WHEN 1 THEN N'Tăng huyết áp, chóng mặt buổi sáng'
        WHEN 2 THEN N'Đau vùng thượng vị sau khi ăn'
        WHEN 3 THEN N'Đau nhức khớp gối hai bên khi vận động'
        ELSE N'Tái khám và lấy thuốc định kỳ hàng tháng'
    END;
    DECLARE @SelDoctor UNIQUEIDENTIFIER = IIF(@i % 2 = 0, @Doctor1, @Doctor2);
    DECLARE @SelClinic UNIQUEIDENTIFIER = IIF(@i % 2 = 0, @Clinic1, @Clinic2);

    IF NOT EXISTS (SELECT 1 FROM scheduling.Appointments WHERE Id = @AptId)
    BEGIN
        INSERT INTO scheduling.Appointments (
            Id, AppointmentNumber, PatientId, DoctorProfileId, ClinicId, AppointmentSlotId,
            AppointmentType, BookingChannel, Status, AppointmentStartUtc, AppointmentEndUtc,
            ChiefComplaint, Notes, CreatedByUserId, CreatedAtUtc, UpdatedAtUtc
        ) VALUES (
            @AptId, @AptNum, @PatientId, @SelDoctor, @SelClinic, NULL,
            'Consultation', 'Portal', @Status, @StartUtc, @EndUtc,
            @Complaint, N'Hẹn khám qua ứng dụng ERM Hospital', @AdminUser, DATEADD(DAY, -2, @StartUtc), @NowUtc
        );
    END

    -- Seed Encounters for Completed and CheckedIn appointments
    IF @Status IN ('Completed', 'CheckedIn')
    BEGIN
        DECLARE @EncId UNIQUEIDENTIFIER = CAST(HASHBYTES('MD5', CONCAT('DEMO_ENC_', @i)) AS UNIQUEIDENTIFIER);
        DECLARE @EncNum NVARCHAR(50) = CONCAT('ENC-DEMO-', RIGHT(CONCAT('0000', @i), 4));
        DECLARE @EncStatus NVARCHAR(50) = CASE WHEN @Status = 'Completed' THEN 'Approved' ELSE 'InProgress' END;
        DECLARE @DiagCode NVARCHAR(20) = CASE (@i % 5) WHEN 0 THEN 'J06.9' WHEN 1 THEN 'I10' WHEN 2 THEN 'K29.7' WHEN 3 THEN 'M17.9' ELSE 'E11.9' END;
        DECLARE @DiagName NVARCHAR(255) = CASE (@i % 5)
            WHEN 0 THEN N'Viêm đường hô hấp trên cấp tính'
            WHEN 1 THEN N'Tăng huyết áp nguyên phát'
            WHEN 2 THEN N'Viêm dạ dày không xác định'
            WHEN 3 THEN N'Thoái hóa khớp gối'
            ELSE N'Đái tháo đường typ 2'
        END;
        DECLARE @EndUtcEnc DATETIME2 = IIF(@EncStatus = 'Approved', @EndUtc, NULL);

        IF NOT EXISTS (SELECT 1 FROM emr.Encounters WHERE Id = @EncId)
        BEGIN
            INSERT INTO emr.Encounters (
                Id, EncounterNumber, PatientId, AppointmentId, DoctorProfileId, ClinicId,
                EncounterType, EncounterStatus, StartedAtUtc, EndedAtUtc, Summary, CreatedAtUtc, UpdatedAtUtc
            ) VALUES (
                @EncId, @EncNum, @PatientId, @AptId, @SelDoctor, @SelClinic,
                'Outpatient', @EncStatus, @StartUtc, @EndUtcEnc,
                N'Bệnh nhân khám ngoại trú, sinh hiệu ổn định.', @StartUtc, @NowUtc
            );

            -- Seed Vital Signs
            INSERT INTO emr.VitalSigns (
                Id, EncounterId, HeightCm, WeightKg, TemperatureC, PulseRate, RespiratoryRate,
                SystolicBp, DiastolicBp, OxygenSaturation, RecordedAtUtc, RecordedByUserId
            ) VALUES (
                NEWID(), @EncId, 165 + (@i % 10), 55 + (@i % 25), 36.5 + ((@i % 5) * 0.2), 72 + (@i % 15), 18,
                115 + (@i % 30), 75 + (@i % 15), 98, @StartUtc, @AdminUser
            );

            -- Seed Diagnosis
            INSERT INTO emr.Diagnoses (
                Id, EncounterId, DiagnosisType, DiagnosisCode, DiagnosisName, IsPrimary, NotedAtUtc
            ) VALUES (
                NEWID(), @EncId, 'Final', @DiagCode, @DiagName, 1, @StartUtc
            );

            -- Seed Clinical Notes
            INSERT INTO emr.ClinicalNotes (
                Id, EncounterId, NoteType, Subjective, Objective, Assessment, CarePlan,
                AuthoredByUserId, AuthoredAtUtc, SignedAtUtc
            ) VALUES (
                NEWID(), @EncId, 'SOAP', @Complaint, N'Tim đập đều, phổi trong, bụng mềm không đau chói.',
                @DiagName, N'Dùng thuốc theo đơn, nghỉ ngơi hợp lý, tái khám sau 7 ngày.',
                @AdminUser, @StartUtc, @EndUtcEnc
            );

            -- Seed Prescription & Items if Approved
            IF @EncStatus = 'Approved'
            BEGIN
                DECLARE @RxHeaderId UNIQUEIDENTIFIER = NEWID();
                DECLARE @RxId UNIQUEIDENTIFIER = CAST(HASHBYTES('MD5', CONCAT('DEMO_RX_', @i)) AS UNIQUEIDENTIFIER);
                DECLARE @RxNum NVARCHAR(50) = CONCAT('RX-DEMO-', RIGHT(CONCAT('0000', @i), 4));

                IF NOT EXISTS (SELECT 1 FROM emr.OrderHeaders WHERE Id = @RxHeaderId)
                BEGIN
                    INSERT INTO emr.OrderHeaders (Id, EncounterId, OrderNumber, OrderCategory, OrderStatus, OrderedByUserId, OrderedAtUtc)
                    VALUES (@RxHeaderId, @EncId, CONCAT('ORD-RX-', @i), 'Pharmacy', 'Completed', @AdminUser, @StartUtc);
                END

                DECLARE @Med1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM pharmacy.Medicines ORDER BY Id);
                DECLARE @Med2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM pharmacy.Medicines ORDER BY Id DESC);

                IF NOT EXISTS (SELECT 1 FROM pharmacy.Prescriptions WHERE Id = @RxId)
                BEGIN
                    INSERT INTO pharmacy.Prescriptions (Id, OrderHeaderId, PrescriptionNumber, Status, Notes, CreatedAtUtc)
                    VALUES (@RxId, @RxHeaderId, @RxNum, 'Issued', N'Uống sau khi ăn no. Đủ 7 ngày.', @StartUtc);

                    -- Add prescription items
                    IF @Med1 IS NOT NULL AND @Med2 IS NOT NULL
                    BEGIN
                        INSERT INTO pharmacy.PrescriptionItems (Id, PrescriptionId, MedicineId, DoseInstruction, Route, Frequency, DurationDays, Quantity, UnitPrice)
                        VALUES
                        (NEWID(), @RxId, @Med1, N'Uống 1 viên khi sốt > 38.5C', N'Uống', N'Ngày 2-3 lần', 5, 10, 2000),
                        (NEWID(), @RxId, @Med2, N'Uống 1 viên sáng và tối sau ăn', N'Uống', N'Ngày 2 lần', 7, 14, 5000);
                    END
                END

                -- Seed Invoice & Items
                DECLARE @InvId UNIQUEIDENTIFIER = CAST(HASHBYTES('MD5', CONCAT('DEMO_INV_', @i)) AS UNIQUEIDENTIFIER);
                DECLARE @InvNum NVARCHAR(50) = CONCAT('INV-DEMO-', RIGHT(CONCAT('0000', @i), 4));
                DECLARE @InvStatus NVARCHAR(50) = CASE WHEN @i % 2 = 0 THEN 'Paid' ELSE 'Issued' END;

                IF NOT EXISTS (SELECT 1 FROM billing.Invoices WHERE Id = @InvId)
                BEGIN
                    INSERT INTO billing.Invoices (
                        Id, InvoiceNumber, PatientId, EncounterId, InvoiceStatus, CurrencyCode,
                        SubtotalAmount, DiscountAmount, InsuranceAmount, TotalAmount, IssuedAtUtc, DueAtUtc
                    ) VALUES (
                        @InvId, @InvNum, @PatientId, @EncId, @InvStatus, 'VND',
                        350000, 0, 0, 350000, @StartUtc, DATEADD(DAY, 7, @StartUtc)
                    );

                    INSERT INTO billing.InvoiceItems (
                        Id, InvoiceId, ServiceCatalogId, ItemType, Description, Quantity, UnitPrice, LineAmount, ReferenceType, ReferenceId
                    ) VALUES
                    (NEWID(), @InvId, NULL, 'Consultation', N'Khám bệnh chuyên khoa', 1, 200000, 200000, 'Encounter', @EncId),
                    (NEWID(), @InvId, NULL, 'Pharmacy', N'Đơn thuốc điều trị ngoại trú', 1, 150000, 150000, 'Prescription', @RxId);
                END
            END
        END
    END

    SET @i = @i + 1;
END;

PRINT 'Demo data seeding completed successfully!';
