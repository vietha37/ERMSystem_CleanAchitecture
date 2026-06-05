SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER FUNCTION dbo.NormalizeVietnameseSeedText(@value NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    IF @value IS NULL
    BEGIN
        RETURN NULL;
    END

    DECLARE @result NVARCHAR(MAX) = @value;

    SET @result = REPLACE(@result, N'Nguyen', N'Nguyễn');
    SET @result = REPLACE(@result, N'Tran', N'Trần');
    SET @result = REPLACE(@result, N'Le ', N'Lê ');
    SET @result = REPLACE(@result, N'Pham', N'Phạm');
    SET @result = REPLACE(@result, N'Hoang', N'Hoàng');
    SET @result = REPLACE(@result, N'Vo ', N'Võ ');
    SET @result = REPLACE(@result, N'Dang', N'Đặng');
    SET @result = REPLACE(@result, N'Bui', N'Bùi');
    SET @result = REPLACE(@result, N'Do ', N'Đỗ ');

    SET @result = REPLACE(@result, N'Thi ', N'Thị ');
    SET @result = REPLACE(@result, N'Van ', N'Văn ');
    SET @result = REPLACE(@result, N'Ngoc', N'Ngọc');
    SET @result = REPLACE(@result, N'Duc', N'Đức');
    SET @result = REPLACE(@result, N'Huu', N'Hữu');
    SET @result = REPLACE(@result, N'Bao', N'Bảo');
    SET @result = REPLACE(@result, N'Quynh', N'Quỳnh');

    SET @result = REPLACE(@result, N'Binh', N'Bình');
    SET @result = REPLACE(@result, N'Chau', N'Châu');
    SET @result = REPLACE(@result, N'Dung', N'Dũng');
    SET @result = REPLACE(@result, N'Hanh', N'Hạnh');
    SET @result = REPLACE(@result, N'Khanh', N'Khánh');
    SET @result = REPLACE(@result, N'Lam', N'Lâm');
    SET @result = REPLACE(@result, N'Phuc', N'Phúc');

    SET @result = REPLACE(@result, N'So ', N'Số ');
    SET @result = REPLACE(@result, N'Duong ', N'Đường ');
    SET @result = REPLACE(@result, N'Phuong ', N'Phường ');
    SET @result = REPLACE(@result, N'Quan ', N'Quận ');
    SET @result = REPLACE(@result, N'Tang', N'Tầng');
    SET @result = REPLACE(@result, N'Viet Nam', N'Việt Nam');
    SET @result = REPLACE(@result, N'Hue', N'Huệ');
    SET @result = REPLACE(@result, N'Loi', N'Lợi');
    SET @result = REPLACE(@result, N'Ben ', N'Bến ');
    SET @result = REPLACE(@result, N'Bến Nghe', N'Bến Nghé');
    SET @result = REPLACE(@result, N'Bến Thanh', N'Bến Thành');
    SET @result = REPLACE(@result, N'Cach Mang Thang 8', N'Cách Mạng Tháng 8');
    SET @result = REPLACE(@result, N'Phan Xich Long', N'Phan Xích Long');
    SET @result = REPLACE(@result, N'Thi Sau', N'Thị Sáu');

    SET @result = REPLACE(@result, N'THạnh', N'Thanh');
    SET @result = REPLACE(@result, N'Trầng', N'Trang');
    SET @result = REPLACE(@result, N'KHạnh', N'Khánh');
    SET @result = REPLACE(@result, N'Hang', N'Hằng');
    SET @result = REPLACE(@result, N'Khoa', N'Khoa');

    RETURN @result;
END;
GO

UPDATE [identity].Roles
SET Name = CASE Code
    WHEN 'Admin' THEN N'Quản trị hệ thống'
    WHEN 'Doctor' THEN N'Bác sĩ'
    WHEN 'Cashier' THEN N'Thu ngân'
    WHEN 'Patient' THEN N'Bệnh nhân'
    ELSE Name
END;

UPDATE org.Departments
SET Name = CASE DepartmentCode
        WHEN 'OPD' THEN N'Khối khám ngoại trú'
        WHEN 'LAB' THEN N'Trung tâm xét nghiệm'
        WHEN 'IMG' THEN N'Chẩn đoán hình ảnh'
        WHEN 'PHA' THEN N'Nhà thuốc bệnh viện'
        ELSE dbo.NormalizeVietnameseSeedText(Name)
    END,
    Description = CASE DepartmentCode
        WHEN 'OPD' THEN N'Tiếp nhận và điều phối khám ngoại trú'
        WHEN 'LAB' THEN N'Vận hành các dịch vụ xét nghiệm và lấy mẫu'
        WHEN 'IMG' THEN N'Các dịch vụ X-quang, CT, MRI, siêu âm'
        WHEN 'PHA' THEN N'Quản lý cấp phát thuốc và tồn kho'
        ELSE dbo.NormalizeVietnameseSeedText(Description)
    END;

UPDATE org.Specialties
SET Name = CASE SpecialtyCode
    WHEN 'CARD' THEN N'Tim mạch'
    WHEN 'OBGYN' THEN N'Sản phụ khoa'
    WHEN 'PED' THEN N'Nhi khoa'
    WHEN 'GEN' THEN N'Nội tổng quát'
    ELSE dbo.NormalizeVietnameseSeedText(Name)
END;

UPDATE org.Clinics
SET Name = CASE ClinicCode
        WHEN 'CLN-01' THEN N'Phòng khám tổng quát 01'
        WHEN 'CLN-02' THEN N'Phòng khám tim mạch'
        WHEN 'LAB-01' THEN N'Khu lấy mẫu xét nghiệm'
        WHEN 'IMG-01' THEN N'Khu siêu âm và X-quang'
        ELSE dbo.NormalizeVietnameseSeedText(Name)
    END,
    FloorLabel = dbo.NormalizeVietnameseSeedText(FloorLabel);

UPDATE org.StaffProfiles
SET FullName = dbo.NormalizeVietnameseSeedText(FullName);

;WITH StaffNameScope AS
(
    SELECT sp.Id,
           sp.FullName,
           ROW_NUMBER() OVER (PARTITION BY sp.FullName ORDER BY sp.StaffCode) AS DuplicateIndex,
           COUNT(*) OVER (PARTITION BY sp.FullName) AS StaffNameCount,
           CASE WHEN EXISTS (
               SELECT 1
               FROM patient.Patients p
               WHERE p.DeletedAtUtc IS NULL
                 AND p.FullName = sp.FullName
           ) THEN 1 ELSE 0 END AS HasPatientNameCollision
    FROM org.StaffProfiles sp
)
UPDATE sp
SET FullName = CONCAT(
    scope.FullName,
    N' ',
    CHOOSE(((scope.DuplicateIndex - 1) % 12) + 1,
        N'Ánh', N'Uyên', N'Vy', N'Nhi', N'Tú', N'Tâm',
        N'Hiền', N'Kiên', N'Long', N'Sơn', N'Tiến', N'Vinh')
)
FROM org.StaffProfiles sp
JOIN StaffNameScope scope ON scope.Id = sp.Id
WHERE scope.StaffNameCount > 1
   OR scope.HasPatientNameCollision = 1;

UPDATE org.DoctorProfiles
SET Biography = CASE
    WHEN Biography IS NULL THEN NULL
    ELSE N'Bác sĩ khám ngoại trú phục vụ kiểm thử hệ thống.'
END;

UPDATE patient.Patients
SET FullName = dbo.NormalizeVietnameseSeedText(FullName),
    AddressLine1 = dbo.NormalizeVietnameseSeedText(AddressLine1),
    Ward = dbo.NormalizeVietnameseSeedText(Ward),
    District = dbo.NormalizeVietnameseSeedText(District),
    Province = dbo.NormalizeVietnameseSeedText(Province),
    Nationality = dbo.NormalizeVietnameseSeedText(Nationality),
    Occupation = CASE
        WHEN Occupation = N'Nhan vien van phong' THEN N'Nhân viên văn phòng'
        WHEN Occupation = N'Nghi huu' THEN N'Nghỉ hưu'
        WHEN Occupation = N'Ke toan' THEN N'Kế toán'
        WHEN Occupation = N'Hoc sinh' THEN N'Học sinh'
        WHEN Occupation = N'Tu doanh' THEN N'Tự doanh'
        ELSE dbo.NormalizeVietnameseSeedText(Occupation)
    END;

UPDATE patient.Patients
SET Ward = REPLACE(Ward, N'Bến Thanh', N'Bến Thành')
WHERE Ward LIKE N'%Bến Thanh%';

UPDATE patient.PatientEmergencyContacts
SET FullName = dbo.NormalizeVietnameseSeedText(FullName),
    Relationship = CASE Relationship
        WHEN N'Spouse' THEN N'Vợ/chồng'
        WHEN N'Daughter' THEN N'Con gái'
        WHEN N'Mother' THEN N'Mẹ'
        WHEN N'Parent' THEN N'Cha/mẹ'
        ELSE dbo.NormalizeVietnameseSeedText(Relationship)
    END,
    Address = dbo.NormalizeVietnameseSeedText(Address);

UPDATE patient.PatientInsurancePolicies
SET ProviderName = CASE
    WHEN ProviderName = N'Bao hiem Y te Thanh pho' THEN N'Bảo hiểm Y tế Thành phố'
    ELSE dbo.NormalizeVietnameseSeedText(ProviderName)
END;

UPDATE scheduling.Appointments
SET ChiefComplaint = dbo.NormalizeVietnameseSeedText(ChiefComplaint),
    Notes = dbo.NormalizeVietnameseSeedText(Notes);

UPDATE emr.Allergies
SET Reaction = CASE
    WHEN Reaction = N'Phat ban do' THEN N'Phát ban đỏ'
    ELSE dbo.NormalizeVietnameseSeedText(Reaction)
END;

UPDATE emr.ChronicConditions
SET ConditionName = CASE
    WHEN ConditionName = N'Tang huyet ap nguyen phat' THEN N'Tăng huyết áp nguyên phát'
    ELSE dbo.NormalizeVietnameseSeedText(ConditionName)
END;

UPDATE emr.Diagnoses
SET DiagnosisName = dbo.NormalizeVietnameseSeedText(DiagnosisName);

UPDATE emr.ClinicalNotes
SET Subjective = dbo.NormalizeVietnameseSeedText(Subjective),
    Objective = dbo.NormalizeVietnameseSeedText(Objective),
    Assessment = dbo.NormalizeVietnameseSeedText(Assessment),
    CarePlan = dbo.NormalizeVietnameseSeedText(CarePlan);

UPDATE billing.ServiceCatalog
SET Name = dbo.NormalizeVietnameseSeedText(Name),
    Category = CASE Category
        WHEN N'Consultation' THEN N'Khám bệnh'
        WHEN N'Lab' THEN N'Xét nghiệm'
        WHEN N'Imaging' THEN N'Chẩn đoán hình ảnh'
        WHEN N'Pharmacy' THEN N'Nhà thuốc'
        ELSE dbo.NormalizeVietnameseSeedText(Category)
    END;

UPDATE billing.Refunds
SET Reason = CASE
    WHEN Reason = N'Hoan tien mot phan do dich vu hinh anh huy' THEN N'Hoàn tiền một phần do dịch vụ hình ảnh hủy'
    ELSE dbo.NormalizeVietnameseSeedText(Reason)
END;

UPDATE [identity].SecurityEvents
SET Detail = dbo.NormalizeVietnameseSeedText(Detail);
GO
