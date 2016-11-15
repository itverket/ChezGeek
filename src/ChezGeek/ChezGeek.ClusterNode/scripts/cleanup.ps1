Write-Output "Deleting firewall rules..."

Remove-NetFirewallRule -Name @("ChezClusterNodeInbound", "ChezClusterNodeOutbound") -ErrorAction SilentlyContinue