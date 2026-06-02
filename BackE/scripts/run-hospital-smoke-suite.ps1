param(
  [string[]]$ScriptNames = @(
    "patient-dedupe-smoke.ps1",
    "patient-portal-smoke.ps1",
    "phase3-encounter-billing-smoke.ps1",
    "clinical-orders-smoke.ps1",
    "doctor-worklist-smoke.ps1",
    "appointments-worklist-smoke.ps1",
    "notification-deliveries-smoke.ps1",
    "encounters-worklist-smoke.ps1",
    "billing-worklist-smoke.ps1",
    "billing-reconciliation-smoke.ps1"
  )
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$results = New-Object System.Collections.Generic.List[object]

foreach ($scriptName in $ScriptNames) {
  $scriptPath = Join-Path $scriptRoot $scriptName
  if (-not (Test-Path $scriptPath)) {
    $results.Add([pscustomobject]@{
      Script = $scriptName
      Status = "Missing"
      DurationSeconds = 0
      Summary = "Script khong ton tai."
    })
    continue
  }

  $startedAt = Get-Date
  try {
    $output = & $scriptPath 2>&1
    $duration = [Math]::Round(((Get-Date) - $startedAt).TotalSeconds, 1)
    $lastLine = @($output | Select-Object -Last 1) -join ""

    $results.Add([pscustomobject]@{
      Script = $scriptName
      Status = "Passed"
      DurationSeconds = $duration
      Summary = if ([string]::IsNullOrWhiteSpace($lastLine)) { "Khong co summary line." } else { $lastLine }
    })
  }
  catch {
    $duration = [Math]::Round(((Get-Date) - $startedAt).TotalSeconds, 1)
    $results.Add([pscustomobject]@{
      Script = $scriptName
      Status = "Failed"
      DurationSeconds = $duration
      Summary = $_.Exception.Message
    })
  }
}

$results | Format-Table -AutoSize | Out-String | Write-Output

$failed = @($results | Where-Object { $_.Status -ne "Passed" })
if ($failed.Count -gt 0) {
  throw ("Smoke suite co " + $failed.Count + " script khong pass.")
}
