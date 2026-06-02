param(
    [string]$ServerInstance = "VietHa\MSSQLSERVER01",
    [string[]]$LegacyDatabaseNames = @("ERMSystemDb"),
    [string]$HospitalDatabaseName = "ERMSystemHospitalDb"
)

$ErrorActionPreference = "Stop"

$hospitalSchemaFile = Join-Path $PSScriptRoot "erm_private_hospital_schema.sql"
$hospitalBaseSeedFile = Join-Path $PSScriptRoot "erm_private_hospital_catalog_seed.sql"
$hospitalFullSeedFile = Join-Path $PSScriptRoot "erm_private_hospital_full_test_seed.sql"
$gatewayPatchFile = Join-Path $PSScriptRoot "patch_add_gateway_provider_to_billing_payments.sql"

function Invoke-SqlCmdFile {
    param(
        [string]$DatabaseName,
        [string]$InputFile
    )

    sqlcmd -b -S $ServerInstance -E -d $DatabaseName -i $InputFile
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed for file: $InputFile"
    }
}

foreach ($path in @($hospitalSchemaFile, $hospitalBaseSeedFile, $hospitalFullSeedFile)) {
    if (-not (Test-Path $path)) {
        throw "Khong tim thay file can thiet: $path"
    }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "Khong tim thay sqlcmd trong PATH."
}

$dropStatements = New-Object System.Collections.Generic.List[string]
foreach ($legacyDatabaseName in $LegacyDatabaseNames) {
    if ([string]::IsNullOrWhiteSpace($legacyDatabaseName)) {
        continue
    }

    $dropStatements.Add(@"
IF DB_ID(N'$legacyDatabaseName') IS NOT NULL
BEGIN
    ALTER DATABASE [$legacyDatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$legacyDatabaseName];
END;
"@)
}

$dropStatements.Add(@"
IF DB_ID(N'$HospitalDatabaseName') IS NOT NULL
BEGIN
    ALTER DATABASE [$HospitalDatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$HospitalDatabaseName];
END;

CREATE DATABASE [$HospitalDatabaseName];
"@)

sqlcmd -b -S $ServerInstance -E -Q ($dropStatements -join [Environment]::NewLine)

if ($LASTEXITCODE -ne 0) {
    throw "Khong the reset database."
}

Write-Host "Creating hospital schema..." -ForegroundColor Cyan
Invoke-SqlCmdFile -DatabaseName $HospitalDatabaseName -InputFile $hospitalSchemaFile

Write-Host "Seeding hospital base catalog..." -ForegroundColor Cyan
Invoke-SqlCmdFile -DatabaseName $HospitalDatabaseName -InputFile $hospitalBaseSeedFile

if (Test-Path $gatewayPatchFile) {
    Write-Host "Applying hospital payment patch..." -ForegroundColor Cyan
    Invoke-SqlCmdFile -DatabaseName $HospitalDatabaseName -InputFile $gatewayPatchFile
}

Write-Host "Seeding hospital full test dataset..." -ForegroundColor Cyan
Invoke-SqlCmdFile -DatabaseName $HospitalDatabaseName -InputFile $hospitalFullSeedFile

Write-Host ""
Write-Host "Seed credentials" -ForegroundColor Green
Write-Host "  password: 123456"
Write-Host "  users: admin00..19, doctor00..19, reception00..19, patient00..19, nurse00..19, pharmacist00..19, labtech00..19, cashier00..19"
Write-Host ""

Write-Host "Hospital DB summary" -ForegroundColor Green
sqlcmd -S $ServerInstance -E -d $HospitalDatabaseName -Q @"
SET NOCOUNT ON;
SELECT 'identity.Users' AS Metric, COUNT(*) AS Value FROM [identity].Users
UNION ALL
SELECT 'org.StaffProfiles', COUNT(*) FROM org.StaffProfiles
UNION ALL
SELECT 'patient.Patients', COUNT(*) FROM patient.Patients
UNION ALL
SELECT 'patient.PatientAccounts', COUNT(*) FROM patient.PatientAccounts
UNION ALL
SELECT 'scheduling.Appointments', COUNT(*) FROM scheduling.Appointments
UNION ALL
SELECT 'emr.Encounters', COUNT(*) FROM emr.Encounters
UNION ALL
SELECT 'pharmacy.Prescriptions', COUNT(*) FROM pharmacy.Prescriptions
UNION ALL
SELECT 'billing.Invoices', COUNT(*) FROM billing.Invoices
UNION ALL
SELECT 'billing.Payments', COUNT(*) FROM billing.Payments
UNION ALL
SELECT 'notification.NotificationDeliveries', COUNT(*) FROM notification.NotificationDeliveries;

SELECT PrimaryRoleCode, COUNT(*) AS UserCount
FROM [identity].Users
WHERE PrimaryRoleCode IN ('Admin','Doctor','Receptionist','Patient','Nurse','Pharmacist','LabTech','Cashier')
GROUP BY PrimaryRoleCode
ORDER BY PrimaryRoleCode;

SELECT Status, COUNT(*) AS AppointmentCount
FROM scheduling.Appointments
GROUP BY Status
ORDER BY Status;

SELECT EncounterStatus, COUNT(*) AS EncounterCount
FROM emr.Encounters
GROUP BY EncounterStatus
ORDER BY EncounterStatus;

SELECT InvoiceStatus, COUNT(*) AS InvoiceCount
FROM billing.Invoices
GROUP BY InvoiceStatus
ORDER BY InvoiceStatus;

SELECT PaymentStatus, COUNT(*) AS PaymentCount
FROM billing.Payments
GROUP BY PaymentStatus
ORDER BY PaymentStatus;
"@
