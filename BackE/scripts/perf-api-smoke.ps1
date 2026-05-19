$ErrorActionPreference = 'Stop'

param(
    [string]$BaseUrl = 'http://localhost:5219',
    [int]$Iterations = 20
)

function Measure-Endpoint {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    $durations = @()
    $success = 0
    $failed = 0

    for ($i = 0; $i -lt $Iterations; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            & $Action | Out-Null
            $sw.Stop()
            $durations += $sw.ElapsedMilliseconds
            $success++
        }
        catch {
            $sw.Stop()
            $durations += $sw.ElapsedMilliseconds
            $failed++
        }
    }

    [pscustomobject]@{
        Endpoint = $Name
        Total = $Iterations
        Success = $success
        Failed = $failed
        MinMs = ($durations | Measure-Object -Minimum).Minimum
        AvgMs = [math]::Round(($durations | Measure-Object -Average).Average, 2)
        MaxMs = ($durations | Measure-Object -Maximum).Maximum
    }
}

$results = @()
$results += Measure-Endpoint -Name 'health_live' -Action { Invoke-RestMethod -Method Get -Uri "$BaseUrl/health/live" }
$results += Measure-Endpoint -Name 'health_ready' -Action { Invoke-RestMethod -Method Get -Uri "$BaseUrl/health/ready" }
$results += Measure-Endpoint -Name 'metrics' -Action { Invoke-WebRequest -Method Get -Uri "$BaseUrl/metrics" }

$results | Format-Table -AutoSize
