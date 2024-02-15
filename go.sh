docker build -t nullinside-api:latest .
docker container stop nullinside-api
docker container prune -f
docker run -d --name=nullinside-api -p 8081:8080 -p 8082:8081 --restart unless-stopped nullinside-api:latest
