$workingDir = $PSScriptRoot -replace "\\scripts", ""

$exePath = "$($workingDir)\Geek2k16.AkkaTest.exe"
$configPath = "$($workingDir)\Geek2k16.AkkaTest.exe.config"

Write-Output "Transforming configuration..."

$configFile = (Get-Content $configPath) -as [Xml]

$akkaConfig = $configFile.configuration.akka.hocon."#cdata-section"

$ipAddress = (
    (Get-NetIPConfiguration).IPv4Address | 
        Where-Object { $_.IPAddress -like "192.168*" } | 
        Select-Object -First 1
).IPAddress

$seedNodeIpAddress = ""

try {
    $seedNodeIpAddress = (
        [System.Net.Dns]::GetHostAddresses("MINI-PC-MASKINA") | 
        Where-Object { $_.IpAddressToString -like "192.168*" } | 
        Select-Object -First 1
    ).IPAddressToString
}
catch {
    $seedNodeIpAddress = Read-Host -Prompt "Seed node IP address"
}

$akkaConfig = $akkaConfig `
    -replace `
        "hostname(.)*", `
        "hostname = $($ipAddress)"

$akkaConfig = $akkaConfig `
    -replace `
        "#?public-hostname(.)*", `
        "public-hostname = $($ipAddress)"

$akkaConfig = $akkaConfig `
    -replace `
        "@(.)*:", `
        "@$($seedNodeIpAddress):"

$configFile.configuration.akka.hocon."#cdata-section" = $akkaConfig

$configFile.Save($configPath);

Write-Output "Creating firewall inbound rule..."

try {
	New-NetFirewallRule `
		-Name "AkkaTestInbound" `
		-DisplayName "AkkaTest Inbound" `
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
		-Name "AkkaTestOutbound" `
		-DisplayName "AkkaTest Outbound" `
		-Program $exePath `
		-Enabled True `
		-Direction Outbound `
		-Action Allow `
		-Profile Any |
	Out-Null
}
catch {}