$ErrorActionPreference = "Stop"

$env:USERPROFILE = "D:\ERMSystem\.localuser"
$env:APPDATA = "D:\ERMSystem\.localuser\AppData\Roaming"
$env:DOTNET_CLI_HOME = "D:\ERMSystem\.dotnet-home"
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "0"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

Add-Type -AssemblyName System.Net.Http

$apiProj = "D:\ERMSystem\BackE\ERMSystem.API\ERMSystem.API.csproj"
$baseUrl = "http://localhost:5219"

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
          ClinicName = $schedule.clinicName
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
  Start-Sleep -Seconds 8

  $doctors = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/hospital-doctors"
  $doctor = @($doctors | Where-Object { $_.isBookable -and $_.schedules.Count -gt 0 }) | Select-Object -First 1
  if ($null -eq $doctor) {
    throw "Khong tim thay bac si co lich bookable de test."
  }

  $slots = Get-CandidateScheduleSlots -Doctor $doctor
  $suffix = [DateTime]::Now.ToString("yyyyMMddHHmmss")
  $phone = ("091" + $suffix.Substring($suffix.Length - 7))
  $email = "phase3.$suffix@erm.local"
  $booking = $null

  foreach ($slot in $slots) {
    $bookingBody = [ordered]@{
      fullName        = "Benh nhan Phase3 $suffix"
      phone           = $phone
      email           = $email
      dateOfBirth     = "1991-04-12"
      gender          = "Nam"
      doctorProfileId = $doctor.doctorProfileId
      specialtyId     = $doctor.specialtyId
      preferredDate   = $slot.PreferredDate
      preferredTime   = $slot.PreferredTime
      chiefComplaint  = "Dau dau, met moi"
      notes           = "Smoke test phase 3"
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

  $adminUsername = "phase3_admin_$suffix"
  $password = "123456"

  $registerBody = @{
    username = $adminUsername
    password = $password
    role = "Admin"
  } | ConvertTo-Json

  Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/auth/register" `
    -ContentType "application/json" `
    -Body $registerBody | Out-Null

  $loginBody = @{
    username = $adminUsername
    password = $password
  } | ConvertTo-Json

  $login = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/auth/login" `
    -ContentType "application/json" `
    -Body $loginBody

  $headers = @{
    Authorization = "Bearer $($login.token)"
  }

  $checkInBody = @{ counterLabel = "Counter A" } | ConvertTo-Json
  $checkedIn = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-appointments/$($booking.appointmentId)/check-in" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $checkInBody

  if ($checkedIn.status -ne "CheckedIn") {
    throw "Check-in khong thanh cong."
  }

  $encounterBody = [ordered]@{
    appointmentId = $booking.appointmentId
    diagnosisName = "Theo doi viem hong cap"
    diagnosisCode = "J02.9"
    diagnosisType = "Final"
    encounterStatus = "Finalized"
    summary = "Khong ghi nhan dau hieu nguy hiem"
    subjective = "Dau hong 2 ngay"
    objective = "Hong do nhe"
    assessment = "Nghi viem hong cap"
    carePlan = "Theo doi, dung thuoc va tai kham neu sot cao"
    heightCm = 170
    weightKg = 63
    temperatureC = 37.2
    pulseRate = 84
    respiratoryRate = 18
    systolicBp = 118
    diastolicBp = 76
    oxygenSaturation = 98
  } | ConvertTo-Json

  $encounter = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-encounters" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $encounterBody

  if ($encounter.encounterStatus -ne "Finalized") {
    throw "Encounter khong duoc finalize."
  }

  if (-not $encounter.isClinicalNoteSigned -or $null -eq $encounter.clinicalNoteSignedAtLocal) {
    throw "Encounter finalized nhung clinical note chua co dau hieu ky."
  }

  $encounterWithAttachment = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-encounters/$($encounter.encounterId)/attachments" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body ([ordered]@{
      fileName = "phieu-kham-$suffix.pdf"
      documentType = "EncounterSummary"
      contentType = "application/pdf"
      documentUri = "https://files.local/encounters/$($encounter.encounterId)/phieu-kham-$suffix.pdf"
    } | ConvertTo-Json)

  if (-not $encounterWithAttachment.attachments -or $encounterWithAttachment.attachments.Count -lt 1) {
    throw "Encounter attachment khong duoc luu."
  }

  $uploadTempFile = Join-Path $env:TEMP ("encounter-attachment-" + $suffix + ".txt")
  [System.IO.File]::WriteAllText($uploadTempFile, "phase3 attachment smoke " + $suffix)

  $multipart = New-Object System.Net.Http.MultipartFormDataContent
  $documentTypeContent = New-Object System.Net.Http.StringContent("EncounterSummary")
  $fileStream = [System.IO.File]::OpenRead($uploadTempFile)
  $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
  $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/plain")
  $multipart.Add($documentTypeContent, "DocumentType")
  $multipart.Add($fileContent, "File", [System.IO.Path]::GetFileName($uploadTempFile))

  $httpClient = New-Object System.Net.Http.HttpClient
  $httpClient.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $login.token)
  $uploadResponse = $httpClient.PostAsync("$baseUrl/api/hospital-encounters/$($encounter.encounterId)/attachments/upload", $multipart).GetAwaiter().GetResult()
  if (-not $uploadResponse.IsSuccessStatusCode) {
    $uploadBody = $uploadResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    throw "Upload attachment khong thanh cong: $uploadBody"
  }

  $uploadedEncounter = $uploadResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
  $latestAttachment = @($uploadedEncounter.attachments | Sort-Object uploadedAtLocal -Descending) | Select-Object -First 1
  if ($null -eq $latestAttachment) {
    throw "Khong tim thay attachment vua upload de test download ticket."
  }

  $downloadTicket = Invoke-RestMethod `
    -Method Get `
    -Uri "$baseUrl/api/hospital-encounters/$($encounter.encounterId)/attachments/$($latestAttachment.attachmentId)/download-ticket" `
    -Headers $headers

  if ([string]::IsNullOrWhiteSpace($downloadTicket.storageProvider)) {
    throw "Download ticket khong tra ve storage provider."
  }

  if ($downloadTicket.accessMode -ne "ProxyTicket") {
    throw "Download ticket dang le o che do ProxyTicket voi cau hinh hien tai."
  }

  if ([string]::IsNullOrWhiteSpace($downloadTicket.accessToken) -or [string]::IsNullOrWhiteSpace($downloadTicket.downloadUrl)) {
    throw "Download ticket khong tra ve du access token/download url."
  }

  $downloadUrl = $downloadTicket.downloadUrl
  if ($downloadUrl.StartsWith("/")) {
    $downloadUrl = "$baseUrl$downloadUrl"
  }

  $downloadByTicket = Invoke-WebRequest `
    -Method Get `
    -Uri $downloadUrl `
    -UseBasicParsing

  if ($downloadByTicket.StatusCode -ne 200) {
    throw "Tai attachment bang download ticket khong thanh cong."
  }

  if (-not $downloadByTicket.Content.Contains("phase3 attachment smoke")) {
    throw "Noi dung attachment tai qua download ticket khong dung."
  }

  $medicines = Invoke-RestMethod `
    -Method Get `
    -Uri "$baseUrl/api/hospital-prescriptions/medicine-catalog" `
    -Headers $headers

  $medicine = @($medicines) | Select-Object -First 1
  if ($null -eq $medicine) {
    throw "Khong tim thay medicine catalog de test."
  }

  $amoxicillinMedicines = @($medicines | Where-Object { $_.genericName -eq "Amoxicillin" })
  $warningMedicines = if ($amoxicillinMedicines.Count -ge 2) { $amoxicillinMedicines | Select-Object -First 2 } else { @() }

  $invalidPrescriptionRejected = $false
  $invalidPrescriptionBody = [ordered]@{
    encounterId = $encounter.encounterId
    status = "Issued"
    notes = "Invalid dosage validation smoke"
    items = @(
      @{
        medicineId = $medicine.medicineId
        doseInstruction = "2 vien/lan"
        route = "Uong"
        frequency = "Ngay 3 lan"
        durationDays = 5
        quantity = 10
      }
    )
  } | ConvertTo-Json -Depth 5

  try {
    Invoke-RestMethod `
      -Method Post `
      -Uri "$baseUrl/api/hospital-prescriptions" `
      -Headers $headers `
      -ContentType "application/json" `
      -Body $invalidPrescriptionBody | Out-Null
    throw "Prescription invalid dang le phai bi chan boi dosage validation."
  }
  catch {
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode.value__ -eq 400) {
      $invalidPrescriptionRejected = $true
    }
    else {
      throw
    }
  }

  $prescriptionBody = [ordered]@{
    encounterId = $encounter.encounterId
    status = "Issued"
    notes = "Cap thuoc theo smoke test"
    items = @(
      @{
        medicineId = $medicine.medicineId
        doseInstruction = "1 vien/lần"
        route = "Uong"
        frequency = "Ngay 2 lan"
        durationDays = 5
        quantity = 10
      }
    )
  } | ConvertTo-Json -Depth 5

  if ($warningMedicines.Count -ge 2) {
    $prescriptionBody = [ordered]@{
      encounterId = $encounter.encounterId
      status = "Issued"
      notes = "Cap thuoc theo smoke test co canh bao trung hoat chat"
      items = @(
        @{
          medicineId = $warningMedicines[0].medicineId
          doseInstruction = "1 vien/lan"
          route = "Uong"
          frequency = "Ngay 2 lan"
          durationDays = 5
          quantity = 10
        },
        @{
          medicineId = $warningMedicines[1].medicineId
          doseInstruction = "1 vien/lan"
          route = "Uong"
          frequency = "Ngay 2 lan"
          durationDays = 5
          quantity = 10
        }
      )
    } | ConvertTo-Json -Depth 5
  }

  $prescription = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-prescriptions" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $prescriptionBody

  if ($prescription.status -ne "Issued") {
    throw "Prescription khong o trang thai Issued."
  }

  if ($warningMedicines.Count -ge 2 -and $prescription.warnings.Count -lt 1) {
    throw "Prescription dang le phai co warning trung hoat chat."
  }

  $dispenseBody = @{ notes = "Cap thuoc tai quay" } | ConvertTo-Json
  $dispensed = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-prescriptions/$($prescription.prescriptionId)/dispense" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $dispenseBody

  if ($dispensed.status -ne "Dispensed") {
    throw "Prescription khong duoc dispense."
  }

  $prescriptionDetail = Invoke-RestMethod `
    -Method Get `
    -Uri "$baseUrl/api/hospital-prescriptions/$($prescription.prescriptionId)" `
    -Headers $headers

  if (-not $prescriptionDetail.dispensingHistory -or $prescriptionDetail.dispensingHistory.Count -lt 1) {
    throw "Prescription dispensing history khong duoc tra ve."
  }

  if ($prescriptionDetail.dispensingHistory[0].dispensingStatus -ne "Dispensed") {
    throw "Dispensing history moi nhat khong o trang thai Dispensed."
  }

  $invoiceBody = @{
    encounterId = $encounter.encounterId
    discountAmount = 0
    insuranceAmount = 0
  } | ConvertTo-Json

  $invoice = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $invoiceBody

  if ($invoice.invoiceStatus -ne "Issued") {
    throw "Invoice khong duoc tao o trang thai Issued."
  }

  $paymentBody = @{
    paymentMethod = "Cash"
    paymentReference = "SMOKE-$suffix"
    amount = $invoice.totalAmount
  } | ConvertTo-Json

  $paidInvoice = Invoke-RestMethod `
    -Method Post `
    -Uri "$baseUrl/api/hospital-billing/$($invoice.invoiceId)/payments" `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $paymentBody

  if ($paidInvoice.invoiceStatus -ne "Paid") {
    throw "Invoice khong duoc thanh toan du."
  }

  Write-Output ("doctor=" + $doctor.fullName)
  Write-Output ("appointment_number=" + $booking.appointmentNumber)
  Write-Output ("encounter_number=" + $encounter.encounterNumber)
  Write-Output ("encounter_signed=" + $encounter.isClinicalNoteSigned)
  Write-Output ("encounter_attachments=" + $encounterWithAttachment.attachments.Count)
  Write-Output ("attachment_ticket_mode=" + $downloadTicket.accessMode)
  Write-Output ("invalid_prescription_rejected=" + $invalidPrescriptionRejected)
  Write-Output ("prescription_number=" + $prescription.prescriptionNumber)
  Write-Output ("prescription_warning_count=" + $prescription.warnings.Count)
  Write-Output ("dispensing_history_count=" + $prescriptionDetail.dispensingHistory.Count)
  Write-Output ("invoice_number=" + $invoice.invoiceNumber)
  Write-Output ("invoice_total=" + $invoice.totalAmount)
  Write-Output ("payment_status=" + $paidInvoice.invoiceStatus)
}
finally {
  if ($fileStream) {
    $fileStream.Dispose()
  }
  if ($fileContent) {
    $fileContent.Dispose()
  }
  if ($multipart) {
    $multipart.Dispose()
  }
  if ($httpClient) {
    $httpClient.Dispose()
  }
  if ($uploadTempFile -and (Test-Path $uploadTempFile)) {
    Remove-Item -LiteralPath $uploadTempFile -Force
  }
  if ($process -and -not $process.HasExited) {
    Stop-Process -Id $process.Id -Force
  }
}
