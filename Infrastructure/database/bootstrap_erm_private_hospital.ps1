param(
    [string]$ServerInstance = "localhost\MSSQLSERVER01",
    [string]$DatabaseName = "ERMSystemHospitalDb",
    [switch]$DropAndRecreate
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$schemaFile = Join-Path $PSScriptRoot "erm_private_hospital_schema.sql"
$seedFile = Join-Path $PSScriptRoot "erm_private_hospital_seed.sql"

if (-not (Test-Path $schemaFile)) {
    throw "Khong tim thay file schema: $schemaFile"
}

if (-not (Test-Path $seedFile)) {
    throw "Khong tim thay file seed: $seedFile"
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

sqlcmd -b -f 65001 -S $ServerInstance -E -d $DatabaseName -i $schemaFile
sqlcmd -b -f 65001 -S $ServerInstance -E -d $DatabaseName -i $seedFile

# In ra so bang theo tung schema nghiep vu de xac nhan bootstrap thanh cong.
sqlcmd -b -S $ServerInstance -E -d $DatabaseName -Q @"
SELECT s.name AS SchemaName, COUNT(t.object_id) AS TableCount
FROM sys.schemas s
LEFT JOIN sys.tables t ON t.schema_id = s.schema_id
WHERE s.name IN ('identity','org','patient','scheduling','emr','lab','imaging','pharmacy','billing','notification','integration','audit')
GROUP BY s.name
ORDER BY s.name;

SELECT 'identity.Roles' AS Metric, COUNT(*) AS Value FROM [identity].Roles
UNION ALL
SELECT 'org.Specialties', COUNT(*) FROM org.Specialties;
"@
