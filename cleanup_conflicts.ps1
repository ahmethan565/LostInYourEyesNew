# Final cleanup script to remove all remaining conflict markers

$filePath = "Assets\Scenes\Puzzle1.unity"
$tempFile = "Assets\Scenes\Puzzle1_clean.unity"

Write-Host "Starting final cleanup of conflict markers..."

try {
    # Read the file content
    $content = Get-Content $filePath -Raw -ErrorAction Stop
    
    Write-Host "File loaded successfully. Size: $($content.Length) characters"
    
    # Count remaining markers
    $headMarkers = ([regex]::Matches($content, '<<<<<<< HEAD')).Count
    $separators = ([regex]::Matches($content, '=======')).Count
    $endMarkers = ([regex]::Matches($content, '>>>>>>> Stashed changes')).Count
    
    Write-Host "Found $headMarkers HEAD markers, $separators separators, $endMarkers end markers"
    
    # Remove all conflict markers completely
    $content = $content -replace '<<<<<<< HEAD', ''
    $content = $content -replace '=======', ''
    $content = $content -replace '>>>>>>> Stashed changes', ''
    
    # Write the cleaned content
    $content | Out-File -FilePath $tempFile -Encoding UTF8 -ErrorAction Stop
    
    Write-Host "Cleanup completed successfully!"
    Write-Host "Cleaned file saved as: $tempFile"
    Write-Host ""
    Write-Host "To apply the cleaned file:"
    Write-Host "copy '$tempFile' '$filePath'"
    
} catch {
    Write-Host "Error: $($_.Exception.Message)"
} 