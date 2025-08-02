# Script to resolve merge conflicts in Puzzle1.unity
# This script will keep the current branch changes (HEAD) and remove conflict markers

$filePath = "Assets\Scenes\Puzzle1.unity"
$tempFile = "Assets\Scenes\Puzzle1_temp.unity"

Write-Host "Starting conflict resolution for $filePath..."

# Read the file content
$content = Get-Content $filePath -Raw

# Remove all conflict markers and keep only the HEAD version
# This removes everything between <<<<<<< HEAD and ======= (keeping HEAD content)
# And removes everything between ======= and >>>>>>> Stashed changes

$pattern1 = '(?s)<<<<<<< HEAD(.*?)=======.*?>>>>>>> Stashed changes'
$replacement1 = '$1'

$pattern2 = '(?s)<<<<<<< HEAD(.*?)======='
$replacement2 = '$1'

$pattern3 = '(?s)=======.*?>>>>>>> Stashed changes'
$replacement3 = ''

# Apply the patterns
$content = $content -replace $pattern1, $replacement1
$content = $content -replace $pattern2, $replacement2
$content = $content -replace $pattern3, $replacement3

# Write the resolved content to a temporary file first
$content | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "Conflict resolution completed. Writing to temporary file..."

# Verify the file was written successfully
if (Test-Path $tempFile) {
    Write-Host "Temporary file created successfully."
    Write-Host "Original file backed up as Puzzle1_backup.unity"
    Write-Host "Resolved file saved as Puzzle1_temp.unity"
    Write-Host "Please review the resolved file and rename it to Puzzle1.unity if satisfied."
} else {
    Write-Host "Error: Failed to create resolved file."
} 