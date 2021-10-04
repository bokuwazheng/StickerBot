docker build -t %1 .

docker tag %1 registry.heroku.com/%1/web

docker push registry.heroku.com/%1/web

heroku container:release web --app %1