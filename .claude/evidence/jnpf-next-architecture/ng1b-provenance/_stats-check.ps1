$csv = Import-Csv 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance\provenance-matrix.csv'
foreach ($clsName in @('P0_PLATFORM_CORE','P1_LOWCODE_RUNTIME','P2_PRODUCT_TEMPLATE','P3_DEMO_APPLICATION','P4_CUSTOMER_APPLICATION','P5_TEST_FIXTURE','P6_LEGACY','P7_ORPHAN','P8_EXTERNAL','PX_UNKNOWN')) {
    $set = $csv | Where-Object { $_.asset_class -eq $clsName }
    $g = $set | Group-Object provenance | ForEach-Object { "{0}={1}" -f $_.Name, $_.Count }
    Write-Host ("{0}: total={1} [{2}]" -f $clsName, $set.Count, ($g -join ' '))
}
$p01 = $csv | Where-Object { $_.asset_class -match '^P[01]_' }
$prov = ($p01 | Where-Object { $_.provenance -eq 'PROVEN' }).Count
Write-Host ("P0+P1 PROVEN rate: {0}/{1}" -f $prov, $p01.Count)
$p0not = $p01 | Where-Object { $_.provenance -ne 'PROVEN' }
Write-Host "P0/P1 non-PROVEN tables:"
$p0not | ForEach-Object { Write-Host ("  {0}  score={1}  {2}" -f $_.table_name, $_.score, $_.provenance) }
