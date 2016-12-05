Write-Output "Deleting firewall rules..."

Remove-NetFirewallRule -Name @("LighthouseInbound", "LighthouseOutbound") | Out-Null