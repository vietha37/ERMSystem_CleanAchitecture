-- ==========================================
-- SECTION: erm_private_hospital_schema.sql
-- ==========================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
  Schema dich cho he thong benh vien tu ERM theo huong modular monolith.
  Database muc tieu: SQL Server.
*/

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'identity') EXEC('CREATE SCHEMA [identity]');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'org') EXEC('CREATE SCHEMA org');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'patient') EXEC('CREATE SCHEMA patient');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'scheduling') EXEC('CREATE SCHEMA scheduling');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'emr') EXEC('CREATE SCHEMA emr');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'lab') EXEC('CREATE SCHEMA lab');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'imaging') EXEC('CREATE SCHEMA imaging');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'pharmacy') EXEC('CREATE SCHEMA pharmacy');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing') EXEC('CREATE SCHEMA billing');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'notification') EXEC('CREATE SCHEMA notification');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'integration') EXEC('CREATE SCHEMA integration');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'audit') EXEC('CREATE SCHEMA audit');
GO

CREATE TABLE [identity].Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Username NVARCHAR(150) NOT NULL,
    Email NVARCHAR(255) NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    PrimaryRoleCode NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_identity_Users_IsActive DEFAULT 1,
    EmailVerifiedAtUtc DATETIME2 NULL,
    LastLoginAtUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_identity_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_identity_Users_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    DeletedAtUtc DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE UNIQUE INDEX UX_identity_Users_Username ON [identity].Users(Username) WHERE DeletedAtUtc IS NULL;
CREATE UNIQUE INDEX UX_identity_Users_Email ON [identity].Users(Email) WHERE Email IS NOT NULL AND DeletedAtUtc IS NULL;
GO

CREATE TABLE [identity].Roles (
    Code NVARCHAR(50) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsSystemRole BIT NOT NULL CONSTRAINT DF_identity_Roles_IsSystemRole DEFAULT 1
);
GO

CREATE TABLE [identity].UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleCode NVARCHAR(50) NOT NULL,
    GrantedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_identity_UserRoles_GrantedAtUtc DEFAULT SYSUTCDATETIME(),
    GrantedByUserId UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_identity_UserRoles PRIMARY KEY (UserId, RoleCode),
    CONSTRAINT FK_identity_UserRoles_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id),
    CONSTRAINT FK_identity_UserRoles_Role FOREIGN KEY (RoleCode) REFERENCES [identity].Roles(Code)
);
GO

CREATE TABLE [identity].RefreshTokens (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    TokenHash NVARCHAR(500) NOT NULL,
    DeviceName NVARCHAR(150) NULL,
    DeviceIp NVARCHAR(64) NULL,
    UserAgent NVARCHAR(1000) NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    RotatedAtUtc DATETIME2 NULL,
    RevokedAtUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_identity_RefreshTokens_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_identity_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id)
);
GO

CREATE INDEX IX_identity_RefreshTokens_UserId ON [identity].RefreshTokens(UserId, ExpiresAtUtc DESC);
GO

CREATE TABLE [identity].UserSessions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SessionCode NVARCHAR(100) NOT NULL,
    DeviceName NVARCHAR(150) NULL,
    DeviceIp NVARCHAR(64) NULL,
    UserAgent NVARCHAR(1000) NULL,
    StartedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_identity_UserSessions_StartedAtUtc DEFAULT SYSUTCDATETIME(),
    LastSeenAtUtc DATETIME2 NULL,
    EndedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_identity_UserSessions_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id)
);
GO

CREATE UNIQUE INDEX UX_identity_UserSessions_SessionCode ON [identity].UserSessions(SessionCode);
GO

CREATE TABLE [identity].SecurityEvents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NULL,
    EventType NVARCHAR(100) NOT NULL,
    Severity NVARCHAR(30) NOT NULL,
    Detail NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(64) NULL,
    UserAgent NVARCHAR(1000) NULL,
    OccurredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_identity_SecurityEvents_OccurredAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_identity_SecurityEvents_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id)
);
GO

CREATE TABLE org.Departments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    DepartmentCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_org_Departments_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_org_Departments_CreatedAtUtc DEFAULT SYSUTCDATETIME()
);
GO

CREATE UNIQUE INDEX UX_org_Departments_DepartmentCode ON org.Departments(DepartmentCode);
GO

CREATE TABLE org.Specialties (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    SpecialtyCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    DepartmentId UNIQUEIDENTIFIER NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_org_Specialties_IsActive DEFAULT 1,
    CONSTRAINT FK_org_Specialties_Department FOREIGN KEY (DepartmentId) REFERENCES org.Departments(Id)
);
GO

CREATE UNIQUE INDEX UX_org_Specialties_SpecialtyCode ON org.Specialties(SpecialtyCode);
GO

CREATE TABLE org.Clinics (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ClinicCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    DepartmentId UNIQUEIDENTIFIER NULL,
    FloorLabel NVARCHAR(50) NULL,
    RoomLabel NVARCHAR(50) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_org_Clinics_IsActive DEFAULT 1,
    CONSTRAINT FK_org_Clinics_Department FOREIGN KEY (DepartmentId) REFERENCES org.Departments(Id)
);
GO

CREATE UNIQUE INDEX UX_org_Clinics_ClinicCode ON org.Clinics(ClinicCode);
GO

CREATE TABLE org.StaffProfiles (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    StaffCode NVARCHAR(50) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    DepartmentId UNIQUEIDENTIFIER NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(255) NULL,
    HireDate DATE NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_org_StaffProfiles_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_org_StaffProfiles_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_org_StaffProfiles_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id),
    CONSTRAINT FK_org_StaffProfiles_Department FOREIGN KEY (DepartmentId) REFERENCES org.Departments(Id)
);
GO

CREATE UNIQUE INDEX UX_org_StaffProfiles_UserId ON org.StaffProfiles(UserId);
CREATE UNIQUE INDEX UX_org_StaffProfiles_StaffCode ON org.StaffProfiles(StaffCode);
GO

CREATE TABLE org.DoctorProfiles (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    StaffProfileId UNIQUEIDENTIFIER NOT NULL,
    SpecialtyId UNIQUEIDENTIFIER NOT NULL,
    LicenseNumber NVARCHAR(100) NULL,
    Biography NVARCHAR(MAX) NULL,
    YearsOfExperience INT NULL,
    ConsultationFee DECIMAL(18,2) NULL,
    IsBookable BIT NOT NULL CONSTRAINT DF_org_DoctorProfiles_IsBookable DEFAULT 1,
    CONSTRAINT FK_org_DoctorProfiles_StaffProfile FOREIGN KEY (StaffProfileId) REFERENCES org.StaffProfiles(Id),
    CONSTRAINT FK_org_DoctorProfiles_Specialty FOREIGN KEY (SpecialtyId) REFERENCES org.Specialties(Id)
);
GO

CREATE UNIQUE INDEX UX_org_DoctorProfiles_StaffProfileId ON org.DoctorProfiles(StaffProfileId);
GO

CREATE TABLE org.DoctorSchedules (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    DoctorProfileId UNIQUEIDENTIFIER NOT NULL,
    ClinicId UNIQUEIDENTIFIER NOT NULL,
    DayOfWeek TINYINT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    SlotMinutes INT NOT NULL,
    ValidFrom DATE NOT NULL,
    ValidTo DATE NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_org_DoctorSchedules_IsActive DEFAULT 1,
    CONSTRAINT FK_org_DoctorSchedules_Doctor FOREIGN KEY (DoctorProfileId) REFERENCES org.DoctorProfiles(Id),
    CONSTRAINT FK_org_DoctorSchedules_Clinic FOREIGN KEY (ClinicId) REFERENCES org.Clinics(Id)
);
GO

CREATE TABLE patient.Patients (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MedicalRecordNumber NVARCHAR(50) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Gender NVARCHAR(20) NOT NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(255) NULL,
    AddressLine1 NVARCHAR(255) NULL,
    AddressLine2 NVARCHAR(255) NULL,
    Ward NVARCHAR(150) NULL,
    District NVARCHAR(150) NULL,
    Province NVARCHAR(150) NULL,
    Nationality NVARCHAR(100) NULL,
    IdentityNumber NVARCHAR(100) NULL,
    Occupation NVARCHAR(150) NULL,
    MaritalStatus NVARCHAR(50) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_patient_Patients_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_patient_Patients_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    DeletedAtUtc DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE UNIQUE INDEX UX_patient_Patients_MRN ON patient.Patients(MedicalRecordNumber);
CREATE INDEX IX_patient_Patients_FullName_Dob ON patient.Patients(FullName, DateOfBirth);
GO

CREATE TABLE patient.PatientAccounts (
    PatientId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    ActivatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_patient_PatientAccounts_ActivatedAtUtc DEFAULT SYSUTCDATETIME(),
    PortalStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_patient_PatientAccounts_PortalStatus DEFAULT 'Active',
    CONSTRAINT FK_patient_PatientAccounts_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id),
    CONSTRAINT FK_patient_PatientAccounts_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id)
);
GO

CREATE UNIQUE INDEX UX_patient_PatientAccounts_UserId ON patient.PatientAccounts(UserId);
GO

CREATE TABLE patient.PatientIdentifiers (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    IdentifierType NVARCHAR(50) NOT NULL,
    IdentifierValue NVARCHAR(150) NOT NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_patient_PatientIdentifiers_IsPrimary DEFAULT 0,
    IssuedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_patient_PatientIdentifiers_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE INDEX IX_patient_PatientIdentifiers_PatientId ON patient.PatientIdentifiers(PatientId);
CREATE UNIQUE INDEX UX_patient_PatientIdentifiers_TypeValue ON patient.PatientIdentifiers(IdentifierType, IdentifierValue);
GO

CREATE TABLE patient.PatientContacts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    ContactType NVARCHAR(50) NOT NULL,
    ContactValue NVARCHAR(255) NOT NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_patient_PatientContacts_IsPrimary DEFAULT 0,
    CONSTRAINT FK_patient_PatientContacts_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE patient.PatientEmergencyContacts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Relationship NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(30) NOT NULL,
    Address NVARCHAR(255) NULL,
    CONSTRAINT FK_patient_PatientEmergencyContacts_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE patient.PatientInsurancePolicies (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    ProviderName NVARCHAR(200) NOT NULL,
    PolicyNumber NVARCHAR(100) NOT NULL,
    CardNumber NVARCHAR(100) NULL,
    EffectiveFrom DATE NULL,
    EffectiveTo DATE NULL,
    CoveragePercent DECIMAL(5,2) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_patient_PatientInsurancePolicies_IsActive DEFAULT 1,
    CONSTRAINT FK_patient_PatientInsurancePolicies_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE patient.PatientConsents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    ConsentType NVARCHAR(100) NOT NULL,
    GrantedAtUtc DATETIME2 NOT NULL,
    RevokedAtUtc DATETIME2 NULL,
    EvidenceUri NVARCHAR(1000) NULL,
    CONSTRAINT FK_patient_PatientConsents_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE scheduling.AppointmentSlots (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    DoctorScheduleId UNIQUEIDENTIFIER NOT NULL,
    SlotStartUtc DATETIME2 NOT NULL,
    SlotEndUtc DATETIME2 NOT NULL,
    Capacity INT NOT NULL CONSTRAINT DF_scheduling_AppointmentSlots_Capacity DEFAULT 1,
    ReservedCount INT NOT NULL CONSTRAINT DF_scheduling_AppointmentSlots_ReservedCount DEFAULT 0,
    SlotStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_scheduling_AppointmentSlots_SlotStatus DEFAULT 'Open',
    CONSTRAINT FK_scheduling_AppointmentSlots_DoctorSchedule FOREIGN KEY (DoctorScheduleId) REFERENCES org.DoctorSchedules(Id)
);
GO

CREATE INDEX IX_scheduling_AppointmentSlots_Start ON scheduling.AppointmentSlots(SlotStartUtc);
GO

CREATE TABLE scheduling.Appointments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    AppointmentNumber NVARCHAR(50) NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorProfileId UNIQUEIDENTIFIER NOT NULL,
    ClinicId UNIQUEIDENTIFIER NOT NULL,
    AppointmentSlotId UNIQUEIDENTIFIER NULL,
    AppointmentType NVARCHAR(50) NOT NULL,
    BookingChannel NVARCHAR(50) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    AppointmentStartUtc DATETIME2 NOT NULL,
    AppointmentEndUtc DATETIME2 NULL,
    ChiefComplaint NVARCHAR(1000) NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_scheduling_Appointments_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_scheduling_Appointments_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_scheduling_Appointments_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id),
    CONSTRAINT FK_scheduling_Appointments_Doctor FOREIGN KEY (DoctorProfileId) REFERENCES org.DoctorProfiles(Id),
    CONSTRAINT FK_scheduling_Appointments_Clinic FOREIGN KEY (ClinicId) REFERENCES org.Clinics(Id),
    CONSTRAINT FK_scheduling_Appointments_Slot FOREIGN KEY (AppointmentSlotId) REFERENCES scheduling.AppointmentSlots(Id),
    CONSTRAINT FK_scheduling_Appointments_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE UNIQUE INDEX UX_scheduling_Appointments_Number ON scheduling.Appointments(AppointmentNumber);
CREATE INDEX IX_scheduling_Appointments_PatientId ON scheduling.Appointments(PatientId, AppointmentStartUtc DESC);
GO

CREATE TABLE scheduling.CheckIns (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    AppointmentId UNIQUEIDENTIFIER NOT NULL,
    CheckInTimeUtc DATETIME2 NOT NULL,
    CounterLabel NVARCHAR(50) NULL,
    CheckInStatus NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_scheduling_CheckIns_Appointment FOREIGN KEY (AppointmentId) REFERENCES scheduling.Appointments(Id)
);
GO

CREATE UNIQUE INDEX UX_scheduling_CheckIns_AppointmentId ON scheduling.CheckIns(AppointmentId);
GO

CREATE TABLE scheduling.QueueTickets (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    AppointmentId UNIQUEIDENTIFIER NOT NULL,
    QueueNumber NVARCHAR(30) NOT NULL,
    QueueStatus NVARCHAR(30) NOT NULL,
    CalledAtUtc DATETIME2 NULL,
    ServedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_scheduling_QueueTickets_Appointment FOREIGN KEY (AppointmentId) REFERENCES scheduling.Appointments(Id)
);
GO

CREATE TABLE emr.Encounters (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EncounterNumber NVARCHAR(50) NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    AppointmentId UNIQUEIDENTIFIER NULL,
    DoctorProfileId UNIQUEIDENTIFIER NOT NULL,
    ClinicId UNIQUEIDENTIFIER NOT NULL,
    EncounterType NVARCHAR(50) NOT NULL,
    EncounterStatus NVARCHAR(30) NOT NULL,
    StartedAtUtc DATETIME2 NOT NULL,
    EndedAtUtc DATETIME2 NULL,
    Summary NVARCHAR(MAX) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_Encounters_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_Encounters_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_emr_Encounters_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id),
    CONSTRAINT FK_emr_Encounters_Appointment FOREIGN KEY (AppointmentId) REFERENCES scheduling.Appointments(Id),
    CONSTRAINT FK_emr_Encounters_Doctor FOREIGN KEY (DoctorProfileId) REFERENCES org.DoctorProfiles(Id),
    CONSTRAINT FK_emr_Encounters_Clinic FOREIGN KEY (ClinicId) REFERENCES org.Clinics(Id)
);
GO

CREATE UNIQUE INDEX UX_emr_Encounters_Number ON emr.Encounters(EncounterNumber);
GO

CREATE TABLE emr.VitalSigns (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EncounterId UNIQUEIDENTIFIER NOT NULL,
    HeightCm DECIMAL(6,2) NULL,
    WeightKg DECIMAL(6,2) NULL,
    TemperatureC DECIMAL(4,1) NULL,
    PulseRate INT NULL,
    RespiratoryRate INT NULL,
    SystolicBp INT NULL,
    DiastolicBp INT NULL,
    OxygenSaturation DECIMAL(5,2) NULL,
    RecordedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_VitalSigns_RecordedAtUtc DEFAULT SYSUTCDATETIME(),
    RecordedByUserId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_emr_VitalSigns_Encounter FOREIGN KEY (EncounterId) REFERENCES emr.Encounters(Id),
    CONSTRAINT FK_emr_VitalSigns_User FOREIGN KEY (RecordedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE TABLE emr.Diagnoses (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EncounterId UNIQUEIDENTIFIER NOT NULL,
    DiagnosisType NVARCHAR(50) NOT NULL,
    DiagnosisCode NVARCHAR(50) NULL,
    DiagnosisName NVARCHAR(255) NOT NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_emr_Diagnoses_IsPrimary DEFAULT 0,
    NotedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_Diagnoses_NotedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_emr_Diagnoses_Encounter FOREIGN KEY (EncounterId) REFERENCES emr.Encounters(Id)
);
GO

CREATE TABLE emr.ClinicalNotes (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EncounterId UNIQUEIDENTIFIER NOT NULL,
    NoteType NVARCHAR(50) NOT NULL,
    Subjective NVARCHAR(MAX) NULL,
    Objective NVARCHAR(MAX) NULL,
    Assessment NVARCHAR(MAX) NULL,
    CarePlan NVARCHAR(MAX) NULL,
    AuthoredByUserId UNIQUEIDENTIFIER NULL,
    AuthoredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_ClinicalNotes_AuthoredAtUtc DEFAULT SYSUTCDATETIME(),
    SignedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_emr_ClinicalNotes_Encounter FOREIGN KEY (EncounterId) REFERENCES emr.Encounters(Id),
    CONSTRAINT FK_emr_ClinicalNotes_User FOREIGN KEY (AuthoredByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE TABLE emr.Allergies (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    AllergenName NVARCHAR(255) NOT NULL,
    Reaction NVARCHAR(255) NULL,
    Severity NVARCHAR(30) NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_emr_Allergies_Status DEFAULT 'Active',
    RecordedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_Allergies_RecordedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_emr_Allergies_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE emr.ChronicConditions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    ConditionCode NVARCHAR(50) NULL,
    ConditionName NVARCHAR(255) NOT NULL,
    DiagnosedOn DATE NULL,
    Status NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_emr_ChronicConditions_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE emr.ClinicalDocuments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EncounterId UNIQUEIDENTIFIER NOT NULL,
    DocumentType NVARCHAR(50) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StorageUri NVARCHAR(1000) NOT NULL,
    MimeType NVARCHAR(150) NULL,
    UploadedByUserId UNIQUEIDENTIFIER NULL,
    UploadedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_ClinicalDocuments_UploadedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_emr_ClinicalDocuments_Encounter FOREIGN KEY (EncounterId) REFERENCES emr.Encounters(Id),
    CONSTRAINT FK_emr_ClinicalDocuments_User FOREIGN KEY (UploadedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE TABLE emr.OrderHeaders (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EncounterId UNIQUEIDENTIFIER NOT NULL,
    OrderNumber NVARCHAR(50) NOT NULL,
    OrderCategory NVARCHAR(50) NOT NULL,
    OrderStatus NVARCHAR(30) NOT NULL,
    OrderedByUserId UNIQUEIDENTIFIER NULL,
    OrderedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_emr_OrderHeaders_OrderedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_emr_OrderHeaders_Encounter FOREIGN KEY (EncounterId) REFERENCES emr.Encounters(Id),
    CONSTRAINT FK_emr_OrderHeaders_User FOREIGN KEY (OrderedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE UNIQUE INDEX UX_emr_OrderHeaders_OrderNumber ON emr.OrderHeaders(OrderNumber);
GO

CREATE TABLE lab.LabServices (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ServiceCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    SampleType NVARCHAR(100) NULL,
    UnitPrice DECIMAL(18,2) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_lab_LabServices_IsActive DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_lab_LabServices_Code ON lab.LabServices(ServiceCode);
GO

CREATE TABLE lab.LabOrders (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderHeaderId UNIQUEIDENTIFIER NOT NULL,
    LabServiceId UNIQUEIDENTIFIER NOT NULL,
    OrderStatus NVARCHAR(30) NOT NULL,
    PriorityCode NVARCHAR(30) NULL,
    RequestedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_lab_LabOrders_RequestedAtUtc DEFAULT SYSUTCDATETIME(),
    ResultedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_lab_LabOrders_OrderHeader FOREIGN KEY (OrderHeaderId) REFERENCES emr.OrderHeaders(Id),
    CONSTRAINT FK_lab_LabOrders_LabService FOREIGN KEY (LabServiceId) REFERENCES lab.LabServices(Id)
);
GO

CREATE TABLE lab.Specimens (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    LabOrderId UNIQUEIDENTIFIER NOT NULL,
    SpecimenCode NVARCHAR(50) NOT NULL,
    CollectedAtUtc DATETIME2 NULL,
    ReceivedAtUtc DATETIME2 NULL,
    Status NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_lab_Specimens_LabOrder FOREIGN KEY (LabOrderId) REFERENCES lab.LabOrders(Id)
);
GO

CREATE UNIQUE INDEX UX_lab_Specimens_Code ON lab.Specimens(SpecimenCode);
GO

CREATE TABLE lab.LabResultItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    LabOrderId UNIQUEIDENTIFIER NOT NULL,
    AnalyteCode NVARCHAR(50) NULL,
    AnalyteName NVARCHAR(255) NOT NULL,
    ResultValue NVARCHAR(100) NULL,
    Unit NVARCHAR(50) NULL,
    ReferenceRange NVARCHAR(100) NULL,
    AbnormalFlag NVARCHAR(20) NULL,
    VerifiedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_lab_LabResultItems_LabOrder FOREIGN KEY (LabOrderId) REFERENCES lab.LabOrders(Id)
);
GO

CREATE TABLE imaging.ImagingServices (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ServiceCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Modality NVARCHAR(50) NULL,
    UnitPrice DECIMAL(18,2) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_imaging_ImagingServices_IsActive DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_imaging_ImagingServices_Code ON imaging.ImagingServices(ServiceCode);
GO

CREATE TABLE imaging.ImagingOrders (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderHeaderId UNIQUEIDENTIFIER NOT NULL,
    ImagingServiceId UNIQUEIDENTIFIER NOT NULL,
    OrderStatus NVARCHAR(30) NOT NULL,
    RequestedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_imaging_ImagingOrders_RequestedAtUtc DEFAULT SYSUTCDATETIME(),
    ReportedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_imaging_ImagingOrders_OrderHeader FOREIGN KEY (OrderHeaderId) REFERENCES emr.OrderHeaders(Id),
    CONSTRAINT FK_imaging_ImagingOrders_Service FOREIGN KEY (ImagingServiceId) REFERENCES imaging.ImagingServices(Id)
);
GO

CREATE TABLE imaging.ImagingReports (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ImagingOrderId UNIQUEIDENTIFIER NOT NULL,
    Findings NVARCHAR(MAX) NULL,
    Impression NVARCHAR(MAX) NULL,
    ReportUri NVARCHAR(1000) NULL,
    SignedByUserId UNIQUEIDENTIFIER NULL,
    SignedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_imaging_ImagingReports_Order FOREIGN KEY (ImagingOrderId) REFERENCES imaging.ImagingOrders(Id),
    CONSTRAINT FK_imaging_ImagingReports_User FOREIGN KEY (SignedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE UNIQUE INDEX UX_imaging_ImagingReports_OrderId ON imaging.ImagingReports(ImagingOrderId);
GO

CREATE TABLE pharmacy.Medicines (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    DrugCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    GenericName NVARCHAR(255) NULL,
    Strength NVARCHAR(100) NULL,
    DosageForm NVARCHAR(100) NULL,
    Unit NVARCHAR(50) NULL,
    IsControlled BIT NOT NULL CONSTRAINT DF_pharmacy_Medicines_IsControlled DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_pharmacy_Medicines_IsActive DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_pharmacy_Medicines_DrugCode ON pharmacy.Medicines(DrugCode);
GO

CREATE TABLE pharmacy.Prescriptions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderHeaderId UNIQUEIDENTIFIER NOT NULL,
    PrescriptionNumber NVARCHAR(50) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_pharmacy_Prescriptions_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_pharmacy_Prescriptions_OrderHeader FOREIGN KEY (OrderHeaderId) REFERENCES emr.OrderHeaders(Id)
);
GO

CREATE UNIQUE INDEX UX_pharmacy_Prescriptions_Number ON pharmacy.Prescriptions(PrescriptionNumber);
GO

CREATE TABLE pharmacy.PrescriptionItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    MedicineId UNIQUEIDENTIFIER NOT NULL,
    DoseInstruction NVARCHAR(255) NOT NULL,
    Route NVARCHAR(100) NULL,
    Frequency NVARCHAR(100) NULL,
    DurationDays INT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NULL,
    CONSTRAINT FK_pharmacy_PrescriptionItems_Prescription FOREIGN KEY (PrescriptionId) REFERENCES pharmacy.Prescriptions(Id),
    CONSTRAINT FK_pharmacy_PrescriptionItems_Medicine FOREIGN KEY (MedicineId) REFERENCES pharmacy.Medicines(Id)
);
GO

CREATE TABLE pharmacy.Dispensings (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    DispensingStatus NVARCHAR(30) NOT NULL,
    DispensedAtUtc DATETIME2 NULL,
    DispensedByUserId UNIQUEIDENTIFIER NULL,
    Notes NVARCHAR(1000) NULL,
    CONSTRAINT FK_pharmacy_Dispensings_Prescription FOREIGN KEY (PrescriptionId) REFERENCES pharmacy.Prescriptions(Id),
    CONSTRAINT FK_pharmacy_Dispensings_User FOREIGN KEY (DispensedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE TABLE pharmacy.InventoryBatches (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MedicineId UNIQUEIDENTIFIER NOT NULL,
    BatchNumber NVARCHAR(100) NOT NULL,
    ExpiryDate DATE NULL,
    QuantityOnHand DECIMAL(18,2) NOT NULL,
    UnitCost DECIMAL(18,2) NULL,
    WarehouseCode NVARCHAR(50) NULL,
    CONSTRAINT FK_pharmacy_InventoryBatches_Medicine FOREIGN KEY (MedicineId) REFERENCES pharmacy.Medicines(Id)
);
GO

CREATE UNIQUE INDEX UX_pharmacy_InventoryBatches_Medicine_Batch ON pharmacy.InventoryBatches(MedicineId, BatchNumber);
GO

CREATE TABLE pharmacy.InventoryTransactions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InventoryBatchId UNIQUEIDENTIFIER NOT NULL,
    TransactionType NVARCHAR(30) NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    OccurredAtUtc DATETIME2 NOT NULL CONSTRAINT DF_pharmacy_InventoryTransactions_OccurredAtUtc DEFAULT SYSUTCDATETIME(),
    ReferenceType NVARCHAR(50) NULL,
    ReferenceId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_pharmacy_InventoryTransactions_Batch FOREIGN KEY (InventoryBatchId) REFERENCES pharmacy.InventoryBatches(Id)
);
GO

CREATE TABLE billing.ServiceCatalog (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ServiceCode NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_billing_ServiceCatalog_IsActive DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_billing_ServiceCatalog_Code ON billing.ServiceCatalog(ServiceCode);
GO

CREATE TABLE billing.Invoices (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    EncounterId UNIQUEIDENTIFIER NULL,
    InvoiceStatus NVARCHAR(30) NOT NULL,
    CurrencyCode NVARCHAR(10) NOT NULL CONSTRAINT DF_billing_Invoices_CurrencyCode DEFAULT 'VND',
    SubtotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_billing_Invoices_SubtotalAmount DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_billing_Invoices_DiscountAmount DEFAULT 0,
    InsuranceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_billing_Invoices_InsuranceAmount DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_billing_Invoices_TotalAmount DEFAULT 0,
    IssuedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_billing_Invoices_IssuedAtUtc DEFAULT SYSUTCDATETIME(),
    DueAtUtc DATETIME2 NULL,
    CONSTRAINT FK_billing_Invoices_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id),
    CONSTRAINT FK_billing_Invoices_Encounter FOREIGN KEY (EncounterId) REFERENCES emr.Encounters(Id)
);
GO

CREATE UNIQUE INDEX UX_billing_Invoices_Number ON billing.Invoices(InvoiceNumber);
GO

CREATE TABLE billing.InvoiceItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InvoiceId UNIQUEIDENTIFIER NOT NULL,
    ServiceCatalogId UNIQUEIDENTIFIER NULL,
    ItemType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineAmount DECIMAL(18,2) NOT NULL,
    ReferenceType NVARCHAR(50) NULL,
    ReferenceId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_billing_InvoiceItems_Invoice FOREIGN KEY (InvoiceId) REFERENCES billing.Invoices(Id),
    CONSTRAINT FK_billing_InvoiceItems_ServiceCatalog FOREIGN KEY (ServiceCatalogId) REFERENCES billing.ServiceCatalog(Id)
);
GO

CREATE TABLE billing.Payments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InvoiceId UNIQUEIDENTIFIER NOT NULL,
    PaymentReference NVARCHAR(100) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    GatewayProvider NVARCHAR(50) NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentStatus NVARCHAR(30) NOT NULL,
    PaidAtUtc DATETIME2 NULL,
    ReceivedByUserId UNIQUEIDENTIFIER NULL,
    ExternalTransactionId NVARCHAR(150) NULL,
    CONSTRAINT FK_billing_Payments_Invoice FOREIGN KEY (InvoiceId) REFERENCES billing.Invoices(Id),
    CONSTRAINT FK_billing_Payments_User FOREIGN KEY (ReceivedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE UNIQUE INDEX UX_billing_Payments_Reference ON billing.Payments(PaymentReference);
GO

CREATE TABLE billing.Refunds (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(255) NULL,
    RefundedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_billing_Refunds_RefundedAtUtc DEFAULT SYSUTCDATETIME(),
    RefundedByUserId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_billing_Refunds_Payment FOREIGN KEY (PaymentId) REFERENCES billing.Payments(Id),
    CONSTRAINT FK_billing_Refunds_User FOREIGN KEY (RefundedByUserId) REFERENCES [identity].Users(Id)
);
GO

CREATE TABLE billing.InsuranceClaims (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    InvoiceId UNIQUEIDENTIFIER NOT NULL,
    PatientInsurancePolicyId UNIQUEIDENTIFIER NOT NULL,
    ClaimNumber NVARCHAR(100) NOT NULL,
    ClaimStatus NVARCHAR(30) NOT NULL,
    ClaimedAmount DECIMAL(18,2) NOT NULL,
    ApprovedAmount DECIMAL(18,2) NULL,
    SubmittedAtUtc DATETIME2 NULL,
    SettledAtUtc DATETIME2 NULL,
    CONSTRAINT FK_billing_InsuranceClaims_Invoice FOREIGN KEY (InvoiceId) REFERENCES billing.Invoices(Id),
    CONSTRAINT FK_billing_InsuranceClaims_Policy FOREIGN KEY (PatientInsurancePolicyId) REFERENCES patient.PatientInsurancePolicies(Id)
);
GO

CREATE UNIQUE INDEX UX_billing_InsuranceClaims_ClaimNumber ON billing.InsuranceClaims(ClaimNumber);
GO

CREATE TABLE notification.NotificationTemplates (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    TemplateCode NVARCHAR(100) NOT NULL,
    ChannelCode NVARCHAR(30) NOT NULL,
    SubjectTemplate NVARCHAR(255) NULL,
    BodyTemplate NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_notification_NotificationTemplates_IsActive DEFAULT 1
);
GO

CREATE UNIQUE INDEX UX_notification_NotificationTemplates_CodeChannel
ON notification.NotificationTemplates(TemplateCode, ChannelCode);
GO

CREATE TABLE notification.NotificationPreferences (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    ChannelCode NVARCHAR(30) NOT NULL,
    IsEnabled BIT NOT NULL CONSTRAINT DF_notification_NotificationPreferences_IsEnabled DEFAULT 1,
    CONSTRAINT FK_notification_NotificationPreferences_Patient FOREIGN KEY (PatientId) REFERENCES patient.Patients(Id)
);
GO

CREATE TABLE notification.OutboxMessages (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    AggregateType NVARCHAR(100) NOT NULL,
    AggregateId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_notification_OutboxMessages_Status DEFAULT 'Pending',
    AvailableAtUtc DATETIME2 NOT NULL CONSTRAINT DF_notification_OutboxMessages_AvailableAtUtc DEFAULT SYSUTCDATETIME(),
    PublishedAtUtc DATETIME2 NULL
);
GO

CREATE INDEX IX_notification_OutboxMessages_Status_Available
ON notification.OutboxMessages(Status, AvailableAtUtc);
GO

CREATE TABLE notification.NotificationDeliveries (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OutboxMessageId UNIQUEIDENTIFIER NOT NULL,
    ChannelCode NVARCHAR(30) NOT NULL,
    Recipient NVARCHAR(255) NOT NULL,
    DeliveryStatus NVARCHAR(30) NOT NULL,
    ProviderMessageId NVARCHAR(150) NULL,
    AttemptCount INT NOT NULL CONSTRAINT DF_notification_NotificationDeliveries_AttemptCount DEFAULT 0,
    LastAttemptAtUtc DATETIME2 NULL,
    DeliveredAtUtc DATETIME2 NULL,
    ErrorMessage NVARCHAR(1000) NULL,
    CONSTRAINT FK_notification_NotificationDeliveries_Outbox FOREIGN KEY (OutboxMessageId) REFERENCES notification.OutboxMessages(Id)
);
GO

CREATE TABLE integration.InboxMessages (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    SourceSystem NVARCHAR(100) NOT NULL,
    MessageKey NVARCHAR(150) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    ProcessedAtUtc DATETIME2 NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_integration_InboxMessages_Status DEFAULT 'Pending'
);
GO

CREATE UNIQUE INDEX UX_integration_InboxMessages_Source_MessageKey
ON integration.InboxMessages(SourceSystem, MessageKey);
GO

CREATE TABLE integration.ExternalMappings (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    LocalEntityType NVARCHAR(100) NOT NULL,
    LocalEntityId UNIQUEIDENTIFIER NOT NULL,
    ExternalSystem NVARCHAR(100) NOT NULL,
    ExternalIdentifier NVARCHAR(150) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_integration_ExternalMappings_CreatedAtUtc DEFAULT SYSUTCDATETIME()
);
GO

CREATE UNIQUE INDEX UX_integration_ExternalMappings_System_ExternalId
ON integration.ExternalMappings(ExternalSystem, ExternalIdentifier);
GO

CREATE TABLE integration.WebhookLogs (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ExternalSystem NVARCHAR(100) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    ReceivedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_integration_WebhookLogs_ReceivedAtUtc DEFAULT SYSUTCDATETIME(),
    ProcessingStatus NVARCHAR(30) NOT NULL,
    ErrorMessage NVARCHAR(1000) NULL
);
GO

CREATE TABLE audit.AuditLogs (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NULL,
    EntityType NVARCHAR(100) NOT NULL,
    EntityId UNIQUEIDENTIFIER NULL,
    ActionCode NVARCHAR(50) NOT NULL,
    BeforeJson NVARCHAR(MAX) NULL,
    AfterJson NVARCHAR(MAX) NULL,
    CorrelationId NVARCHAR(100) NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_audit_AuditLogs_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_audit_AuditLogs_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id)
);
GO

CREATE INDEX IX_audit_AuditLogs_Entity ON audit.AuditLogs(EntityType, EntityId, CreatedAtUtc DESC);
GO

CREATE TABLE audit.EntityAccessLogs (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    EntityType NVARCHAR(100) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    AccessType NVARCHAR(50) NOT NULL,
    AccessedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_audit_EntityAccessLogs_AccessedAtUtc DEFAULT SYSUTCDATETIME(),
    IpAddress NVARCHAR(64) NULL,
    CONSTRAINT FK_audit_EntityAccessLogs_User FOREIGN KEY (UserId) REFERENCES [identity].Users(Id)
);
GO

INSERT INTO [identity].Roles (Code, Name, IsSystemRole)
VALUES
    ('Admin', N'Quản trị hệ thống', 1),
    ('Doctor', N'Bác sĩ', 1),
    ('Cashier', N'Thu ngân', 1),
    ('Patient', N'Bệnh nhân', 1);
GO



GO

-- ==========================================
-- SECTION: erm_private_hospital_catalog_seed.sql
-- ==========================================
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


GO

-- ==========================================
-- SECTION: erm_private_hospital_full_test_seed.sql
-- ==========================================
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
    WHERE pa.PatientId = p.Id OR pa.UserId = u.Id
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






GO

-- ==========================================
-- SECTION: seed_rich_demo_data.sql
-- ==========================================
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


GO

-- ==========================================
-- SECTION: patch_normalize_vietnamese_display_data.sql
-- ==========================================
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
    WHEN 'GASTRO' THEN N'Tiêu hóa - gan mật'
    WHEN 'OBGYN' THEN N'Sản phụ khoa'
    WHEN 'PED' THEN N'Nhi khoa'
    WHEN 'MSK' THEN N'Cơ xương khớp'
    WHEN 'NEURO' THEN N'Thần kinh'
    WHEN 'GEN' THEN N'Nội tổng quát'
    ELSE dbo.NormalizeVietnameseSeedText(Name)
END,
    IsActive = CASE
        WHEN SpecialtyCode IN ('CARD', 'GASTRO', 'OBGYN', 'PED', 'MSK', 'NEURO') THEN 1
        WHEN SpecialtyCode = 'GEN' THEN 0
        ELSE IsActive
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


GO

-- ==========================================
-- SECTION: optimize_performance_indexes.sql
-- ==========================================
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


GO
