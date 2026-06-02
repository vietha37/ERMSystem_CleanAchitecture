$ErrorActionPreference = "Stop"

$env:USERPROFILE = "D:\ERMSystem\.localuser"
$env:APPDATA = "D:\ERMSystem\.localuser\AppData\Roaming"
$env:DOTNET_CLI_HOME = "D:\ERMSystem\.dotnet-home"
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "0"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

$apiProj = "D:\ERMSystem\BackE\ERMSystem.API\ERMSystem.API.csproj"
$baseUrl = "http://localhost:5219"

function Wait-ApiReady {
  param(
    [Parameter(Mandatory = $true)]
    [string] $HealthUrl,
    [int] $MaxAttempts = 30,
    [int] $DelaySeconds = 2
  )

  for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    try {
      Invoke-RestMethod -Method Get -Uri $HealthUrl -TimeoutSec 5 | Out-Null
      return
    }
    catch {
      Start-Sleep -Seconds $DelaySeconds
    }
  }

  throw "API khong san sang sau $MaxAttempts lan kiem tra."
}

function Get-CandidateScheduleSlots {
  param(
    [Parameter(Mandatory = $true)]
    $Doctor
  )

  $today = (Get-Date).Date
  $minDate = $today.AddDays(14)
  $candidates = New-Object System.Collections.Generic.List[object]

  foreach ($schedule in @($Doctor.schedules | Sort-Object dayOfWeek, startTime)) {
    for ($offset = 0; $offset -lt 120; $offset++) {
      $candidate = $minDate.AddDays($offset)
      if ([int]$candidate.DayOfWeek -ne [int]$schedule.dayOfWeek) {
        continue
      }

      $validFrom = [DateTime]::Parse($schedule.validFrom.ToString()).Date
      if ($candidate -lt $validFrom) {
        continue
      }

      if ($null -ne $schedule.validTo) {
        $validTo = [DateTime]::Parse($schedule.validTo.ToString()).Date
        if ($candidate -gt $validTo) {
          continue
        }
      }

      $startTime = [TimeSpan]::Parse($schedule.startTime.ToString())
      $endTime = [TimeSpan]::Parse($schedule.endTime.ToString())
      $slotMinutes = [int]$schedule.slotMinutes
      $availableSlots = [Math]::Max(1, [int][Math]::Floor((($endTime - $startTime).TotalMinutes) / $slotMinutes))

      for ($slotIndex = 0; $slotIndex -lt $availableSlots; $slotIndex++) {
        $preferredTime = $startTime.Add([TimeSpan]::FromMinutes($slotIndex * $slotMinutes))
        $candidates.Add([pscustomobject]@{
          PreferredDate = $candidate.ToString("yyyy-MM-dd")
          PreferredTime = ([DateTime]::Today + $preferredTime).ToString("HH:mm:ss")
        })

        if ($candidates.Count -ge 80) {
          return $candidates
        }
      }
    }
  }

  if ($candidates.Count -eq 0) {
    throw "Khong tim thay lich lam viec hop le de dat lich."
  }

  return $candidates
}

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = "dotnet"
$startInfo.Arguments = "run --no-build --project `"$apiProj`" --urls `"$baseUrl`""
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = "D:\ERMSystem\BackE"

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $startInfo
$process.Start() | Out-Null

try {
  Wait-ApiReady -HealthUrl "$baseUrl/health/live"

  $doctors = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/hospital-doctors"
  $doctor = @($doctors | Where-Object { $_.isBookable -and $_.schedules.Count -gt 0 -and $_.consultationFee -gt 0 }) | Select-Object -First 1
  if ($null -eq $doctor) {
    throw "Khong tim thay bac si co consultation fee va lich bookable de test reconciliation."
  }

  $slots = Get-CandidateScheduleSlots -Doctor $doctor
  $suffix = [DateTime]::Now.ToString("yyyyMMddHHmmss")
  $phone = ("097" + $suffix.Substring($suffix.Length - 7))
  $email = "billing.recon.$suffix@erm.local"
  $booking = $null

  foreach ($slot in $slots) {
    $bookingBody = [ordered]@{
      fullName        = "Benh nhan Billing Recon $suffix"
      phone           = $phone
      email           = $email
      dateOfBirth     = "1990-03-15"
      gender          = "Nu"
      doctorProfileId = $doctor.doctorProfileId
      specialtyId     = $doctor.specialtyId
      preferredDate   = $slot.PreferredDate
      preferredTime   = $slot.PreferredTime
      chiefComplaint  = "Ho kho"
      notes           = "Smoke test billing reconciliation"
    } | ConvertTo-Json

    try {
      $booking = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseUrl/api/hospital-appointments/public-booking" `
        -ContentType "application/json" `
        -Body $bookingBody
      break
    }
    catch {
      continue
    }
  }

  if ($null -eq $booking) {
    throw "Khong dat duoc lich hen sau khi thu cac slot hop le."
  }

  $adminUsername = "billing_recon_admin_$suffix"
  $password = "123456"

  Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/auth/register" `
    -ContentType "application/json" `
    -Body (@{ username = $adminUsername; password = $password; role = "Admin" } | ConvertTo-Json) | Out-Null

  $login = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/auth/login" `
    -ContentType "application/json" `
    -Body (@{ username = $adminUsername; password = $password } | ConvertTo-Json)

  $token = if ($login.PSObject.Properties.Name -contains "accessToken") { $login.accessToken } else { $login.token }
  $headers = @{ Authorization = "Bearer $token" }

  $checkedIn = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-appointments/$($booking.appointmentId)/check-in" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{ counterLabel = "Counter R" } | ConvertTo-Json)

  if ($checkedIn.status -ne "CheckedIn") {
    throw "Check-in cho reconciliation smoke khong thanh cong."
  }

  $encounter = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-encounters" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body ([ordered]@{
      appointmentId = $booking.appointmentId
      diagnosisName = "Viem duong ho hap tren"
      diagnosisCode = "J06.9"
      diagnosisType = "Working"
      encounterStatus = "Finalized"
      summary = "Encounter finalize de tao payment intent doi soat"
      subjective = "Ho kho, met moi"
      objective = "Sinh ton on dinh"
      assessment = "Theo doi ngoai tru"
      carePlan = "Dung thuoc va nghi ngoi"
    } | ConvertTo-Json)

  $invoice = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{
      encounterId = $encounter.encounterId
      discountAmount = 0
      insuranceAmount = 0
    } | ConvertTo-Json)

  if ($invoice.invoiceStatus -ne "Issued") {
    throw "Tao invoice reconciliation smoke khong thanh cong."
  }

  $paymentIntent = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing/$($invoice.invoiceId)/payment-intents" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{
      gatewayProvider = "MockGateway"
      paymentMethod = "QR"
      paymentReference = "RECON-$suffix"
      amount = $invoice.totalAmount
    } | ConvertTo-Json)

  if ($paymentIntent.paymentStatus -ne "Pending") {
    throw "Payment intent khong o trang thai Pending."
  }

  $partnerItems = @(
    @{
      partnerRecordId = "PARTNER-$suffix"
      paymentReference = $paymentIntent.paymentReference
      externalTransactionId = $paymentIntent.externalTransactionId
      amount = $paymentIntent.amount
      gatewayStatus = "Captured"
      paidAtUtc = [DateTime]::UtcNow.ToString("o")
    }
  )

  $preview = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing/reconciliation/preview" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{
      gatewayProvider = "MockGateway"
      items = $partnerItems
    } | ConvertTo-Json -Depth 5)

  if ($preview.totalPartnerItems -ne 1 -or $preview.matchedItems -ne 0) {
    throw "Preview reconciliation khong tra ve so lieu mong doi."
  }

  $previewItem = @($preview.items) | Select-Object -First 1
  if ($null -eq $previewItem -or $previewItem.resolutionCode -ne "STATUS_MISMATCH") {
    throw "Preview reconciliation dang le phai bao STATUS_MISMATCH truoc khi apply."
  }

  $apply = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing/reconciliation/apply" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{
      gatewayProvider = "MockGateway"
      pendingOnly = $true
      items = $partnerItems
    } | ConvertTo-Json -Depth 5)

  if ($apply.appliedCount -ne 1 -or $apply.errorCount -ne 0) {
    throw "Apply reconciliation khong ap duoc giao dich nhu mong doi."
  }

  $applyItem = @($apply.items) | Select-Object -First 1
  if ($null -eq $applyItem -or $applyItem.actionCode -ne "APPLIED") {
    throw "Ket qua apply reconciliation khong tra ve APPLIED."
  }

  $invoiceAfterApply = Invoke-RestMethod `
    -Method Get `
    -Uri "$baseUrl/api/hospital-billing/$($invoice.invoiceId)" `
    -Headers $headers

  if ($invoiceAfterApply.invoiceStatus -ne "Paid") {
    throw "Invoice sau reconciliation apply khong sang Paid."
  }

  $paymentAfterApply = @($invoiceAfterApply.payments | Where-Object { $_.paymentId -eq $paymentIntent.paymentId }) | Select-Object -First 1
  if ($null -eq $paymentAfterApply) {
    throw "Khong tim thay payment sau khi apply reconciliation."
  }

  if ($paymentAfterApply.paymentStatus -ne "Captured") {
    throw "Payment sau reconciliation apply khong sang Captured."
  }

  if ($paymentAfterApply.gatewayProvider -ne "MockGateway") {
    throw "GatewayProvider sau reconciliation apply khong dung."
  }

  $applyRepeat = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing/reconciliation/apply" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body (@{
      gatewayProvider = "MockGateway"
      pendingOnly = $true
      items = $partnerItems
    } | ConvertTo-Json -Depth 5)

  $repeatItem = @($applyRepeat.items) | Select-Object -First 1
  if ($null -eq $repeatItem -or $repeatItem.actionCode -ne "SKIPPED_NOT_PENDING") {
    throw "Lan apply thu hai dang le phai skip vi payment khong con Pending."
  }

  Write-Output ("invoice_number=" + $invoice.invoiceNumber)
  Write-Output ("payment_reference=" + $paymentIntent.paymentReference)
  Write-Output ("payment_external_transaction_id=" + $paymentIntent.externalTransactionId)
  Write-Output ("preview_resolution=" + $previewItem.resolutionCode)
  Write-Output ("apply_action=" + $applyItem.actionCode)
  Write-Output ("invoice_status_after_apply=" + $invoiceAfterApply.invoiceStatus)
  Write-Output ("payment_status_after_apply=" + $paymentAfterApply.paymentStatus)
  Write-Output ("gateway_provider_after_apply=" + $paymentAfterApply.gatewayProvider)
  Write-Output ("repeat_apply_action=" + $repeatItem.actionCode)
}
finally {
  if ($process -and -not $process.HasExited) {
    Stop-Process -Id $process.Id -Force
    $process.WaitForExit()
  }
}
