#Сборка контейнеров:

`docker compose -f ./docker-compose.dev.yml --env-file=./config.env up --build -d`

#Запуск сидера:

`docker exec -it user_analytics dotnet /app/UserAnalyticsAPI.dll seed`