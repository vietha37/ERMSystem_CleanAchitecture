SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

MERGE [identity].Roles AS target
USING (VALUES
    ('Admin', N'Quản trị hệ thống', 1),
    ('Doctor', N'Bác sĩ', 1),
    ('Cashier', N'Thu ngân', 1),
    ('Patient', N'Bệnh nhân', 1)
) AS source (Code, Name, IsSystemRole)
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        IsSystemRole = source.IsSystemRole
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, IsSystemRole)
    VALUES (source.Code, source.Name, source.IsSystemRole);

UPDATE [identity].Users
SET PrimaryRoleCode = 'Cashier'
WHERE PrimaryRoleCode IN ('Receptionist', 'Nurse', 'Pharmacist', 'LabTech');

IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.AppUsers
    SET Role = 'Cashier'
    WHERE Role IN ('Receptionist', 'Nurse', 'Pharmacist', 'LabTech');
END

INSERT INTO [identity].UserRoles (UserId, RoleCode, GrantedAtUtc, GrantedByUserId)
SELECT u.Id, 'Cashier', SYSUTCDATETIME(), NULL
FROM [identity].Users u
WHERE u.PrimaryRoleCode = 'Cashier'
  AND NOT EXISTS (
      SELECT 1
      FROM [identity].UserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleCode = 'Cashier'
  );

DELETE FROM [identity].UserRoles
WHERE RoleCode IN ('Receptionist', 'Nurse', 'Pharmacist', 'LabTech');

DELETE FROM [identity].Roles
WHERE Code IN ('Receptionist', 'Nurse', 'Pharmacist', 'LabTech');

COMMIT TRANSACTION;
GO
