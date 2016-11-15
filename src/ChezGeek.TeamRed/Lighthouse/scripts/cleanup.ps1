Write-Output "Deleting firewall rules..."

Remove-NetFirewallRule -Name @("LighthouseInbound", "LighthouseOutbound") -ErrorAction SilentlyContinue | Out-Null