$dateTag = Get-Date -Format "yyyy.MM.dd"
$imgPath = $env:MUDDLR__DOCKER__TAG

docker build --rm -f ./Api.DockerFile -t $imgPath":"$dateTag ./src;
Write-Host "Tagging latest build as 'latest'" -ForegroundColor Yellow
docker tag $imgPath":"$dateTag latest
Write-Host "Done" -ForegroundColor Yellow