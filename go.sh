docker build -t nullinside-api:latest .
docker container stop nullinside-api
docker container prune -f
docker run -d --name=nullinside-api -e DOCKER_SERVER=$DOCKER_SERVER -e DOCKER_USERNAME=$DOCKER_USERNAME -e DOCKER_PASSWORD=$DOCKER_PASSWORD -e DOCKER_PASSWORD2=$DOCKER_PASSWORD2 -e MYSQL_SERVER=$MYSQL_SERVER -e MYSQL_USERNAME=$MYSQL_USERNAME -e MYSQL_PASSWORD=$MYSQL_PASSWORD -p 8081:8080 -p 8082:8081 --restart unless-stopped nullinside-api:latest
