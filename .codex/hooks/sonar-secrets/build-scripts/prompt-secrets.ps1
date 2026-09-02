if (-not (Get-Command sonar -ErrorAction SilentlyContinue)) {
    exit 0
}
$stdinData = [Console]::In.ReadToEnd()
$stdinData | & sonar hook codex-prompt-submit
exit $LASTEXITCODE
