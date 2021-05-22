param
(
    [string]$Targetpath,
    [string]$SolutionDir
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

$SkinsDir = Get-Item (join-path -Path $SolutionDir -ChildPath "examples")
$SkinsRegex = [regex]::Escape($SkinsDir.Fullname)

$version = ''
$skinfile = $SolutionDir + "bin\MqttClient_$version.rmskin"

#Create RMSKIN.ini
Set-Content -Path "$SolutionDir\bin\RMSKIN.ini" -Value "[rmskin]`nName=MqttPlugin`nAuthor=NetwiZe.be`nVersion=0.2.5.0522`nMinimumRainmeter=4.3.1.3321`nMinimumWindows=10.0"

# Delete Rmskin file if it exists
if (Test-Path $skinfile) { Remove-Item $skinfile }

# Create new Rmskin file
$rmskin = [System.IO.Compression.ZipFile]::Open($skinfile, 'create')

$entries = @()

# Add RMSKIN.ini
[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($rmskin, "$SolutionDir\bin\RMSKIN.ini", 'RMSKIN.ini', 'optimal')

# Add BMP

"$SkinsDir"
"$SkinsRegex"
# Add Skins
$entries += Get-ChildItem -File ("$SkinsDir") -Recurse |
Sort-Object DirectoryName, FullName |
% {
    $shortName = $_.FullName -replace $SkinsRegex,"Skins\MqttClient"
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($rmskin, $_.FullName, $shortName, 'optimal')
}

# Add Plugins
$dllfile = $Targetpath -replace "x64","x86"
if (Test-Path $dllfile) {
    $entries += [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($rmskin, $dllfile, 'Plugins\32bit\MqttClient.dll', 'optimal')
}

$dllfile = $Targetpath -replace "x86","x64"
if (Test-Path $dllfile) {
    $entries += [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($rmskin, $dllfile, 'Plugins\64bit\MqttClient.dll', 'optimal')
}
# Done
$rmskin.dispose()

# List entries in the Rmskin
$entries | ft Fullname

# Create footer
$info = get-item $skinfile
$footer = [BitConverter]::GetBytes($info.length)
$footer += 0
$footer += [Byte[]]"RMSKIN".ToCharArray()
$footer += 0

# Write footer to Rmskin
$file = [System.IO.File]::Open($skinfile, 'append', 'Write', 'None')
$file.write($footer, 0, $footer.length)
$file.Close()

# Double-check footer lengths
if ($footer.length -ne 16) { throw "Bad footer.length detected" }
$footerlength = $(get-item $skinfile).length - $info.Length
if ($footerlength -ne 16) { throw "Bad footerlength detected" }
