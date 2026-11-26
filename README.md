# CarWashSelfService.API
API мойки самообслуживания: ASP.NET Core (.NET 10) + EF Core + PostgreSQL.
JWT-аутентификация, ролевая авторизация (Admin/Client) + контроль владельца ресурса.

## Требования
- .NET 10 SDK
- Docker Desktop

## Запуск
1. git clone <url> и cd CarWashSelfService.API
2. docker compose up -d   (поднимает PostgreSQL)
3. Visual Studio: открыть решение, в Консоли диспетчера пакетов выполнить Update-Database
4. F5. Swagger: https://localhost:7003/swagger
   Либо из CLI: dotnet ef database update, затем dotnet run --project CarWashSelfService.API

## Тестирование ролей
1. POST /api/auth/register - создаёт пользователя с ролью Client.
2. Повысить до Admin (Exec контейнера БД): psql -U postgres -d CarWashDb -c "UPDATE \"Users\" SET \"Role\" = 1 WHERE \"Email\" = '<email>';"
1. POST /api/auth/register - создаёт пользователя с ролью Client.
2. Повысить до Admin (Exec контейнера БД): psql -U postgres -d CarWashDb -c "UPDATE \"Users\" SET \"Role\" = 1 WHERE \"Email\" = '<email>';"
3. Повторный POST /api/auth/login - роль зашивается в токен в момент логина.
4. Матрица: без токена 401; Client на чужом/админском 403; Admin 200/201.

Пароли и Jwt-ключ в appsettings - учебные значения.
Остановка БД: docker compose down; полный сброс данных: docker compose down -v.







