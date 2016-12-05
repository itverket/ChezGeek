Write-Output "Deleting firewall rules..."

Remove-NetFirewallRule -Name @("KjessClusterNodeInbound", "KjessClusterNodeOutbound")