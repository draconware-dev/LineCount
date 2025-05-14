param(
    [string]$InstallationPath = "C:\Program Files\Draconware\LineCount",
    [string]$Source = "https:\\github.com\draconware-dev\SpanExtensions.Net\releases\download\v1.5.1\SpanExtensions.Net.1.5.1.snupkg",
    [ValidateSet("User", "Machine")]
    [string]$Scope = "User"
)

$InstallationPath = $InstallationPath.TrimEnd('/', '\')
New-Item -ItemType Directory -Path $InstallationPath -Force

$response = Invoke-WebRequest $Source
$path = $response.BaseResponse.ResponseUri.AbsolutePath

if($path.EndsWith(".zip"))
{
    Expand-Archive -Path $path -DestinationPath $InstallationPath -Force
}
else
{
    Copy-Item -Path $path -Destination "$InstallationPath\linecount.exe" -Force
}

$root = $Scope == "User" ? "HKCU:" : "HKLM:"

New-Item -Path "$root\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\linecount.exe" -Force | Out-Null
Set-ItemProperty -Path "$root\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\linecount.exe" -Name "(Default)" -Value "$InstallationPath\linecount.exe"

New-Item -Type File -Name "uninstall.ps1" -Path $InstallationPath -Force -Value @"
Remove-Item -Path "$root\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\linecount.exe" -Force | Out-Null
Remove-Item -Path "$InstallationPath\linecount.exe" -Force | Out-Null
Remove-Item -Path "$InstallationPath\linecount.exe" -Force | Out-Null
"@ | Out-Null