param(
    [string]$InstallationPath = "C:\Program Files\Draconware\LineCount",
    [string]$Source = "",
    [ValidateSet("User", "Machine")]
    [string]$Scope = "User"
)

if(![System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows))
{
    Write-Output "This is the installer for Windows. Please refer to https://github.com/draconware-dev/LineCount/releases/tag/__VERSION__."
    exit 1
}

$InstallationPath = $InstallationPath.TrimEnd('/', '\')
New-Item -ItemType Directory -Path $InstallationPath -Force | Out-Null

if([string]::IsNullOrEmpty($Source)) 
{
    if([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64)
    {
        $Source = "https://github.com/draconware-dev/LineCount/releases/download/__VERSION__/loc-__VERSION__-windows-arm64.zip"
    }
    elseif([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::X64)
    {
        $Source = "https://github.com/draconware-dev/LineCount/releases/download/__VERSION__/loc-__VERSION__-windows-amd64.zip"
    }
    elseif([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::X86)
    {
        $Source = "https://github.com/draconware-dev/LineCount/releases/download/__VERSION__/loc-__VERSION__-windows-x86.zip"
    }
    else 
    {
        exit 2
    }
}

$lastUrlIndex = $Source.LastIndexOfAny([char[]]@('/', '\'))
$fileName = $Source.Substring($lastUrlIndex + 1)

if($Source.StartsWith("http://") -or $Source.StartsWith("https://"))
{
    Write-Output "Downloading $fileName..." 
    $Archive = "$InstallationPath/archive.zip"
    Invoke-WebRequest $Source -OutFile $Archive
}
else
{
    $Archive = $Source
}
Write-Output "Extracting $fileName..."

Expand-Archive -Path $Archive -DestinationPath $InstallationPath -Force | Out-Null
Remove-Item -Path $Archive -Force

Write-Output "Adding loc to PATH..."

$environment = ($Scope -eq "User") ? "Environment" : "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"

$registry = ($Scope -eq "User") ? [Microsoft.Win32.Registry]::CurrentUser : [Microsoft.Win32.Registry]::LocalMachine
$key = $registry.OpenSubKey("$environment", $true)
$currentPATH = $key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
$currentPATH = "$currentPATH;$InstallationPath"
$key.SetValue('Path', $currentPATH, [Microsoft.Win32.RegistryValueKind]::ExpandString)
$key.Close()

Write-Output "Generating uninstall.ps1..."

New-Item -Type File -Name "uninstall.ps1" -Path $InstallationPath -Force -Value @"
`$registry = $registry
`$key = `$registry.OpenSubKey("$environment", `$true)
`$currentPATH = `$key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
`$escaped = [Regex]::Escape("$InstallationPath")
`$currentPATH = ([regex]";?`$escaped").Replace("`$currentPATH", "", 1)
`$key.SetValue('Path', `$currentPATH, [Microsoft.Win32.RegistryValueKind]::ExpandString)
`$key.Close()
Remove-Item -Path "$InstallationPath\loc.exe" -Force
Write-Output "Uninstallation complete."
Remove-Item -Path "$InstallationPath\uninstall.ps1" -Force
"@ | Out-Null

Write-Output "`nInstallation complete."