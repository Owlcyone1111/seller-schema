$webclient = New-Object Net.WebClient
$url = 'http://download.microsoft.com/download/3/0/A/30A5D6DD-5B5C-4E06-B331-A88AA0B53150/FSharp_Bundle.exe'
$webclient.DownloadFile($url, "$pwd\FSharp_Bundle.exe")
.\FSharp_Bundle.exe /install /quiet
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0", [System.EnvironmentVariableTarget]::Machine)