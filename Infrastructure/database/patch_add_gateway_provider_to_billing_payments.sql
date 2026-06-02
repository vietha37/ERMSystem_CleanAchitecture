SET NOCOUNT ON;

IF COL_LENGTH('billing.Payments', 'GatewayProvider') IS NULL
BEGIN
    ALTER TABLE billing.Payments
    ADD GatewayProvider NVARCHAR(50) NULL;
END;

GO

UPDATE billing.Payments
SET GatewayProvider =
    CASE
        WHEN PaymentMethod = 'Cash' THEN NULL
        WHEN ExternalTransactionId IS NULL OR LTRIM(RTRIM(ExternalTransactionId)) = '' THEN 'UnknownGateway'
        WHEN CHARINDEX('-', ExternalTransactionId) > 1 THEN LEFT(ExternalTransactionId, CHARINDEX('-', ExternalTransactionId) - 1)
        ELSE 'UnknownGateway'
    END
WHERE GatewayProvider IS NULL;
