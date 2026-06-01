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
    ('Admin', N'Quan tri he thong', 1),
    ('Doctor', N'Bac si', 1),
    ('Receptionist', N'Le tan', 1),
    ('Nurse', N'Dieu duong', 1),
    ('Pharmacist', N'Duoc si', 1),
    ('LabTech', N'Ky thuat vien xet nghiem', 1),
    ('Cashier', N'Thu ngan', 1),
    ('Patient', N'Benh nhan', 1);
GO

