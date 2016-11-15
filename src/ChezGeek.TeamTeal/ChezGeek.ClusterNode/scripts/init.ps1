$workingDir = $PSScriptRoot -replace "\\scripts", ""

$exePath = "$($workingDir)\ChezGeek.ClusterNode.exe"
$configPath = "$($workingDir)\ChezGeek.ClusterNode.exe.config"

Write-Output "Transforming configuration..."

$configFile = (Get-Content $configPath) -as [Xml]

$akkaConfig = $configFile.configuration.akka.hocon."#cdata-section"

$ipAddress = (Test-Connection -ComputerName $env:ComputerName -Count 1).IPV4Address.IPAddressToString

$seedNodeIpAddress = Read-Host -Prompt "Seed node IP address"

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
		-Name "ChezClusterNodeInbound" `
		-DisplayName "ChezGeek Cluster Node" `
		-Program $exePath `
		-Enabled True `
		-Direction Inbound `
		-Action Allow `
		-Profile Any `
        -ErrorAction SilentlyContinue |
	Out-Null
}
catch {}

Write-Output "Creating firewall outbound rule..."

try {
	New-NetFirewallRule `
		-Name "ChezClusterNodeOutbound" `
		-DisplayName "ChezGeek Cluster Node" `
		-Program $exePath `
		-Enabled True `
		-Direction Outbound `
		-Action Allow `
		-Profile Any `
        -ErrorAction SilentlyContinue |
	Out-Null
}
catch {}