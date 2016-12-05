$workingDir = $PSScriptRoot -replace "\\scripts", ""

$exePath = "$($workingDir)\Lighthouse.exe"
$configPath = "$($workingDir)\Lighthouse.exe.config"

Write-Output "Transforming config..."

$configFile = (Get-Content $configPath) -as [Xml]

$akkaConfig = $configFile.configuration.akka.hocon."#cdata-section"

$ipAddress = (
    (Get-NetIPConfiguration).IPv4Address | 
        Where-Object { $_.IPAddress -like "192.168*" } | 
        Select-Object -first 1
).IPAddress

$akkaConfig = $akkaConfig `
    -replace `
        "#?public-hostname(.)*", `
        "public-hostname = $($ipAddress)"

$akkaConfig = $akkaConfig `
    -replace `
        "@(.)*:", `
        "@$($ipAddress):"

$configFile.configuration.akka.hocon."#cdata-section" = $akkaConfig

$configFile.Save($configPath);

Write-Output "Creating firewall inbound rule..."

try {
	New-NetFirewallRule `
		-Name "LighthouseInbound" `
		-DisplayName "Lighthouse Inbound" `
		-Program $exePath `
		-Enabled True `
		-Direction Inbound `
		-Action Allow `
		-Profile Any |
    Out-Null
}
catch {}

Write-Output "Creating firewall outbound rule..."

try {
	New-NetFirewallRule `
		-Name "LighthouseOutbound" `
		-DisplayName "Lighthouse Outbound" `
		-Program $exePath `
		-Enabled True `
		-Direction Outbound `
		-Action Allow `
		-Profile Any |
    Out-Null
}
catch {}
    