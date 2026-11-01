# Copies the text of store\listings\*.md into a Store submission JSON file (from `msstore submission get`).
param([Parameter(Mandatory)] $path, $root = (Split-Path $PSScriptRoot))
$submission = Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json

foreach ($file in Get-ChildItem (Join-Path $root 'store\listings') -Filter *.md) {
  $sections = @{}
  $name = $null
  foreach ($line in Get-Content $file.FullName -Encoding UTF8) {
    if ($line -match '^## (.+)') { $name = $Matches[1]; $sections[$name] = @() }
    elseif ($name) { $sections[$name] += $line }
  }
  $text = { param($section) ($sections[$section] -join "`n").Trim() }
  $list = { param($section) @($sections[$section] | Where-Object { $_ -like '- *' } | ForEach-Object { $_.Substring(2) }) }

  $language = $submission.Listings.PSObject.Properties.Name | Where-Object { $_ -eq $file.BaseName } | Select-Object -First 1
  if (-not $language) { throw "The submission has no $($file.BaseName) listing; add that language in Partner Center first." }
  $listing = $submission.Listings.$language.BaseListing
  $listing.Title = & $text 'Product name'
  $listing.ShortDescription = & $text 'Short description'
  $listing.Description = & $text 'Description'
  $listing.ReleaseNotes = & $text "What's new in this version"
  $listing.Features = & $list 'Product features'
  $listing.Keywords = & $list 'Keywords'
  $listing.CopyrightAndTrademarkInfo = & $text 'Copyright and trademark info'
  $listing.LicenseTerms = & $text 'Additional license terms'
}

[IO.File]::WriteAllText((Resolve-Path $path), ($submission | ConvertTo-Json -Depth 100))
