Write-Output "Deleting firewall rules..."

Remove-NetFirewallRule -Name @("AkkaTestInbound", "AkkaTestOutbound")