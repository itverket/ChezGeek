Write-Output "Deleting firewall rules..."

Remove-NetFirewallRule -Name @("ChezUIInbound", "ChezUIOutbound") -ErrorAction SilentlyContinue