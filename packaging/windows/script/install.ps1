param(
    [string]$InstallationPath = "C:\Program Files\Draconware\LineCount",
    [string]$Source = "https://github.com/draconware-dev/LineCount/releases/download/__VERSION__/linecount-__VERSION__-windows-amd64.zip",
    [ValidateSet("User", "Machine")]
    [string]$Scope = "User"
)

$InstallationPath = $InstallationPath.TrimEnd('/', '\')
New-Item -ItemType Directory -Path $InstallationPath -Force

$lastUrlIndex = $Source.LastIndexOfAny([char[]]@('/','\'))
$fileName = $Source.Substring($lastUrlIndex + 1)
Write-Output "Downloading $fileName..." 

Invoke-WebRequest $Source -OutFile $InstallationPath/archive.zip

Write-Output "Extracting $fileName..."

Expand-Archive -Path $InstallationPath/archive.zip -DestinationPath $InstallationPath -Force  | Out-Null
Remove-Item -Path $InstallationPath/archive.zip -Force

Write-Output "Adding linecount to PATH..."

$environment = ($Scope -eq "User") ? "Environment" : "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"

$registry = ($Scope -eq "User") ? [Microsoft.Win32.Registry]::CurrentUser : [Microsoft.Win32.Registry]::LocalMachine
$key = $registry.OpenSubKey("$environment", $true)
$currentPATH = $key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
$currentPATH = "$currentPATH;$InstallationPath"
$key.SetValue('Path', $currentPATH, [Microsoft.Win32.RegistryValueKind]::ExpandString)
$key.Close()

Write-Output "Generating uninstall.ps1..."

New-Item -Type File -Name "uninstall.ps1" -Path $InstallationPath -Force -Value @"
`$registry = ("$Scope" -eq "User") ? [Microsoft.Win32.Registry]::CurrentUser : [Microsoft.Win32.Registry]::LocalMachine
`$key = `$registry.OpenSubKey("$environment", `$true)
`$currentPATH = `$key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
`$escaped = [Regex]::Escape("$InstallationPath")
`$currentPATH = ([regex]";?`$escaped").Replace("`$currentPATH", "", 1)
`$key.SetValue('Path', `$currentPATH, [Microsoft.Win32.RegistryValueKind]::ExpandString)
`$key.Close()
Remove-Item -Path "$InstallationPath\linecount.exe" -Force
Write-Output "Uninstallation complete."
Remove-Item -Path "$InstallationPath\uninstall.ps1" -Force
"@ | Out-Null

Write-Output "`nInstallation complete."