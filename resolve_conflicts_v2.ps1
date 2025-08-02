# Improved script to resolve merge conflicts in Puzzle1.unity
# This script will keep the current branch changes (HEAD) and remove conflict markers

$filePath = "Assets\Scenes\Puzzle1.unity"
$tempFile = "Assets\Scenes\Puzzle1_resolved.unity"

Write-Host "Starting conflict resolution for $filePath..."

try {
    # Read the file content
    $content = Get-Content $filePath -Raw -ErrorAction Stop
    
    Write-Host "File loaded successfully. Size: $($content.Length) characters"
    
    # Count conflicts before resolution
    $conflictsBefore = ([regex]::Matches($content, '<<<<<<< HEAD')).Count
    Write-Host "Found $conflictsBefore conflicts to resolve"
    
    if ($conflictsBefore -eq 0) {
        Write-Host "No conflicts found. File is already clean."
        exit
    }
    
    # Remove all conflict markers and keep only the HEAD version
    # Pattern to match complete conflict blocks and keep only HEAD content
    $pattern = '(?s)<<<<<<< HEAD(.*?)=======.*?>>>>>>> Stashed changes'
    $replacement = '$1'
    
    # Apply the pattern multiple times to handle nested or overlapping conflicts
    $previousContent = ""
    $currentContent = $content
    $iterations = 0
    $maxIterations = 10
    
    while ($currentContent -ne $previousContent -and $iterations -lt $maxIterations) {
        $previousContent = $currentContent
        $currentContent = $currentContent -replace $pattern, $replacement
        $iterations++
    }
    
    # Count conflicts after resolution
    $conflictsAfter = ([regex]::Matches($currentContent, '<<<<<<< HEAD')).Count
    Write-Host "Resolved $($conflictsBefore - $conflictsAfter) conflicts in $iterations iterations"
    
    if ($conflictsAfter -gt 0) {
        Write-Host "Warning: $conflictsAfter conflicts remain unresolved"
    }
    
    # Write the resolved content to a new file
    $currentContent | Out-File -FilePath $tempFile -Encoding UTF8 -ErrorAction Stop
    
    Write-Host "Conflict resolution completed successfully!"
    Write-Host "Original file: $filePath"
    Write-Host "Resolved file: $tempFile"
    Write-Host "Backup file: Assets\Scenes\Puzzle1_backup.unity"
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. Review the resolved file: $tempFile"
    Write-Host "2. If satisfied, replace the original: copy '$tempFile' '$filePath'"
    Write-Host "3. If not satisfied, the original is backed up as Puzzle1_backup.unity"
    
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Please check if the file exists and is accessible."
} 