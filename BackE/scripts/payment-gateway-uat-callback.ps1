param(
  [string] $BaseUrl = $(if ($env:ERM_UAT_BASE_URL) { $env:ERM_UAT_BASE_URL } else { "http://localhost:5219" }),
  [string] $Provider = $(if ($env:ERM_UAT_GATEWAY_PROVIDER) { $env:ERM_UAT_GATEWAY_PROVIDER } else { "VNPay" }),
  [Parameter(Mandatory = $true)]
  [Guid] $InvoiceId,
  [Parameter(Mandatory = $true)]
  [string] $PaymentReference,
  [string] $ExternalTransactionId = "",
  [string] $GatewayStatus = "00",
  [Parameter(Mandatory = $true)]
  [decimal] $Amount,
  [string] $GatewayEventId = "",
  [string] $WebhookSecret = $(if ($env:ERM_UAT_GATEWAY_WEBHOOK_SECRET) { $env:ERM_UAT_GATEWAY_WEBHOOK_SECRET } else { "" }),
  [string] $SignatureHeaderName = $(if ($env:ERM_UAT_GATEWAY_SIGNATURE_HEADER) { $env:ERM_UAT_GATEWAY_SIGNATURE_HEADER } else { "X-ERM-Gateway-Signature" }),
  [switch] $CheckReturnEndpoint
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($WebhookSecret)) {
  throw "WebhookSecret is required. Pass -WebhookSecret or set ERM_UAT_GATEWAY_WEBHOOK_SECRET."
}

if ([string]::IsNullOrWhiteSpace($GatewayEventId)) {
  $GatewayEventId = "UAT-" + [Guid]::NewGuid().ToString("N")
}

$timestampUtc = [DateTime]::UtcNow
$amountText = $Amount.ToString("0.##", [Globalization.CultureInfo]::InvariantCulture)
$payloadParts = @(
  $Provider,
  $InvoiceId.ToString("D"),
  $PaymentReference.Trim(),
  $ExternalTransactionId.Trim(),
  $GatewayStatus.Trim(),
  $amountText,
  $GatewayEventId,
  $timestampUtc.ToString("O", [Globalization.CultureInfo]::InvariantCulture)
)
$signaturePayload = [string]::Join("|", $payloadParts)

$hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($WebhookSecret))
try {
  $hash = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($signaturePayload))
  $signature = -join ($hash | ForEach-Object { $_.ToString("x2") })
}
finally {
  $hmac.Dispose()
}

$body = @{
  invoiceId = $InvoiceId
  gatewayProvider = $Provider
  gatewayEventId = $GatewayEventId
  gatewayTimestampUtc = $timestampUtc.ToString("O", [Globalization.CultureInfo]::InvariantCulture)
  paymentReference = $PaymentReference
  externalTransactionId = if ([string]::IsNullOrWhiteSpace($ExternalTransactionId)) { $null } else { $ExternalTransactionId }
  gatewayStatus = $GatewayStatus
  amount = $Amount
}

$callbackUri = $BaseUrl.TrimEnd("/") + "/api/hospital-billing/payment-callbacks"
$headers = @{
  $SignatureHeaderName = $signature
}

Write-Output ("callback_uri=" + $callbackUri)
Write-Output ("gateway_event_id=" + $GatewayEventId)
Write-Output ("signature_payload=" + $signaturePayload)
Write-Output ("signature=" + $signature)

$callbackResult = Invoke-RestMethod `
  -Method Post `
  -Uri $callbackUri `
  -Headers $headers `
  -Body ($body | ConvertTo-Json -Depth 5) `
  -ContentType "application/json" `
  -TimeoutSec 30

Write-Output "callback_result:"
$callbackResult | ConvertTo-Json -Depth 8

if ($CheckReturnEndpoint) {
  $returnUri = $BaseUrl.TrimEnd("/") + "/api/hospital-billing/payment-return?invoiceId=$($InvoiceId.ToString("D"))&gatewayProvider=$([Uri]::EscapeDataString($Provider))&paymentReference=$([Uri]::EscapeDataString($PaymentReference))&externalTransactionId=$([Uri]::EscapeDataString($ExternalTransactionId))&gatewayStatus=$([Uri]::EscapeDataString($GatewayStatus))&amount=$amountText"
  Write-Output ("return_uri=" + $returnUri)
  $returnResult = Invoke-RestMethod -Method Get -Uri $returnUri -TimeoutSec 30
  Write-Output "return_result:"
  $returnResult | ConvertTo-Json -Depth 5
}
