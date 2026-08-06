param(
    [string]$ServerInstance = "localhost\MSSQLSERVER01",
    [string]$DatabaseName = "ERMSystemHospitalDb",
    [switch]$DropAndRecreate = $true
)

$ErrorActionPreference = "Stop"

$masterSeedFile = Join-Path $PSScriptRoot "seed_full_database.sql"

if (-not (Test-Path $masterSeedFile)) {
    throw "Khong tim thay file SQL master seed: $masterSeedFile"
}

if ($DropAndRecreate) {
    sqlcmd -b -S $ServerInstance -E -Q @"
IF DB_ID('$DatabaseName') IS NOT NULL
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DatabaseName];
END;
CREATE DATABASE [$DatabaseName];
"@
}
else {
    sqlcmd -b -S $ServerInstance -E -Q "IF DB_ID('$DatabaseName') IS NULL CREATE DATABASE [$DatabaseName];"
}

Write-Host "Executing master seed SQL script ($masterSeedFile)..." -ForegroundColor Cyan
sqlcmd -b -f 65001 -S $ServerInstance -E -d $DatabaseName -i $masterSeedFile

Write-Host ""
Write-Host "Database bootstrap complete. Summary:" -ForegroundColor Green
sqlcmd -b -S $ServerInstance -E -d $DatabaseName -Q @"
SET NOCOUNT ON;
SELECT 'identity.Users' AS Metric, COUNT(*) AS Value FROM [identity].Users
UNION ALL
SELECT 'org.StaffProfiles', COUNT(*) FROM org.StaffProfiles
UNION ALL
SELECT 'patient.Patients', COUNT(*) FROM patient.Patients
UNION ALL
SELECT 'scheduling.Appointments', COUNT(*) FROM scheduling.Appointments
UNION ALL
SELECT 'emr.Encounters', COUNT(*) FROM emr.Encounters
UNION ALL
SELECT 'pharmacy.Prescriptions', COUNT(*) FROM pharmacy.Prescriptions
UNION ALL
SELECT 'billing.Invoices', COUNT(*) FROM billing.Invoices
UNION ALL
SELECT 'billing.Payments', COUNT(*) FROM billing.Payments;
"@
