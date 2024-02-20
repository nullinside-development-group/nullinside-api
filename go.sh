docker build -t nullinside-api:latest .
docker container stop nullinside-api
docker container prune -f

echo $MYSQL_SERVER
echo $MYSQL_USERNAME
echo $MYSQL_PASSWORD
docker run -d --name=nullinside-api -e MYSQL_SERVER=$MYSQL_SERVER -e MYSQL_USERNAME=$MYSQL_USERNAME -e MYSQL_PASSWORD=$MYSQL_PASSWORD -p 8081:8080 -p 8082:8081 --restart unless-stopped nullinside-api:latest
