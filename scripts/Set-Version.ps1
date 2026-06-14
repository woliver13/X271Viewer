param()

$now     = Get-Date
$year    = $now.Year
$month   = $now.Month
$day     = $now.Day
$since   = $now.ToString('yyyy-MM-dd') + ' 00:00:00'

$count   = (git log --oneline --after=$since 2>$null | Measure-Object -Line).Lines
$build   = $count + 1
$version = "$year.$month.$day.$build"

$props   = Join-Path $PWD 'Directory.Build.props'

$content = [System.IO.File]::ReadAllText($props)
$content = $content -replace '(<FileVersion>)[^<]*(</FileVersion>)',     "`${1}$version`${2}"
$content = $content -replace '(<AssemblyVersion>)[^<]*(</AssemblyVersion>)', "`${1}$version`${2}"
[System.IO.File]::WriteAllText($props, $content, [System.Text.Encoding]::UTF8)

git add Directory.Build.props

Write-Host "Version stamped: $version"
