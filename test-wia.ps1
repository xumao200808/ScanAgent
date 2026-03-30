try {
    $wia = New-Object -ComObject WIA.DeviceManager
    Write-Host "WIA DeviceManager created successfully"
    Write-Host "Device count: $($wia.DeviceInfos.Count)"
    
    for ($i = 1; $i -le $wia.DeviceInfos.Count; $i++) {
        $device = $wia.DeviceInfos.Item($i)
        Write-Host "Device $i :"
        Write-Host "  Type: $($device.Type)"
        Write-Host "  Name: $($device.Name)"
        Write-Host "  Manufacturer: $($device.Manufacturer)"
        Write-Host "  DeviceID: $($device.DeviceID)"
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Stack: $($_.Exception.StackTrace)"
}
