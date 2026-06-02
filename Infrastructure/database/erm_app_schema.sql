SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
GO

CREATE TABLE Doctors (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    FullName NVARCHAR(MAX) NOT NULL,
    Specialty NVARCHAR(MAX) NOT NULL
);
GO

CREATE TABLE Medicines (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL
);
GO

CREATE TABLE AppUsers (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Username NVARCHAR(450) NOT NULL,
    Name NVARCHAR(MAX) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(MAX) NOT NULL,
    RefreshTokenHash NVARCHAR(MAX) NULL,
    RefreshTokenExpiresAt DATETIME2 NULL,
    RefreshTokenRevokedAt DATETIME2 NULL,
    MfaEnabled BIT NOT NULL CONSTRAINT DF_AppUsers_MfaEnabled DEFAULT 0,
    MfaEnabledAt DATETIME2 NULL,
    MfaSecretProtected NVARCHAR(MAX) NULL
);
GO

CREATE UNIQUE INDEX IX_AppUsers_Username ON AppUsers(Username);
GO

CREATE TABLE Patients (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    FullName NVARCHAR(MAX) NOT NULL,
    DateOfBirth DATETIME2 NOT NULL,
    Gender NVARCHAR(MAX) NOT NULL,
    Phone NVARCHAR(MAX) NOT NULL,
    Address NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    AppUserId UNIQUEIDENTIFIER NULL,
    EmergencyContactName NVARCHAR(MAX) NULL,
    EmergencyContactPhone NVARCHAR(MAX) NULL,
    EmergencyContactRelationship NVARCHAR(MAX) NULL
);
GO

CREATE TABLE Appointments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    AppointmentDate DATETIME2 NOT NULL,
    Status NVARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_Appointments_Doctors_DoctorId FOREIGN KEY (DoctorId) REFERENCES Doctors(Id),
    CONSTRAINT FK_Appointments_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);
GO

CREATE INDEX IX_Appointments_DoctorId ON Appointments(DoctorId);
CREATE INDEX IX_Appointments_PatientId ON Appointments(PatientId);
GO

CREATE TABLE MedicalRecords (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    AppointmentId UNIQUEIDENTIFIER NOT NULL,
    Symptoms NVARCHAR(MAX) NOT NULL,
    Diagnosis NVARCHAR(MAX) NOT NULL,
    Notes NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_MedicalRecords_Appointments_AppointmentId FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id)
);
GO

CREATE UNIQUE INDEX IX_MedicalRecords_AppointmentId ON MedicalRecords(AppointmentId);
GO

CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    MedicalRecordId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Prescriptions_MedicalRecords_MedicalRecordId FOREIGN KEY (MedicalRecordId) REFERENCES MedicalRecords(Id)
);
GO

CREATE UNIQUE INDEX IX_Prescriptions_MedicalRecordId ON Prescriptions(MedicalRecordId);
GO

CREATE TABLE PrescriptionItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    MedicineId UNIQUEIDENTIFIER NOT NULL,
    Dosage NVARCHAR(MAX) NOT NULL,
    Duration NVARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_PrescriptionItems_Medicines_MedicineId FOREIGN KEY (MedicineId) REFERENCES Medicines(Id),
    CONSTRAINT FK_PrescriptionItems_Prescriptions_PrescriptionId FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id)
);
GO

CREATE INDEX IX_PrescriptionItems_MedicineId ON PrescriptionItems(MedicineId);
CREATE INDEX IX_PrescriptionItems_PrescriptionId ON PrescriptionItems(PrescriptionId);
GO
