$branch = & git rev-parse --abbrev-ref HEAD
$branch = $branch.Trim()
& "Photon.CLI\bin\Debug\PhotonCLI.exe" build run -f="Tasks\Publish.json" -r="$branch"
