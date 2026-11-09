# TodoApi

ASP.NET Core (.NET 10) Minimal API + PostgreSQL (EF Core) + Swagger UI, запуск в Docker.

## Быстрый старт
    cp .env.example .env        # задайте POSTGRES_PASSWORD
    docker compose up -d --build

- Swagger UI: http://localhost:8080/swagger
- Liveness:   http://localhost:8080/health
- Readiness:  http://localhost:8080/health/ready

## Локальная разработка (API на хосте, БД в Docker)
    docker compose up -d db
    dotnet run
Строка подключения по умолчанию — в appsettings.json (localhost:5432).

## Конфигурация через переменные окружения
| Переменная                  | Назначение                              |
|-----------------------------|-----------------------------------------|
| ConnectionStrings__Default  | строка подключения к PostgreSQL         |
| Swagger__Enabled            | true — показать Swagger вне Development |
| Database__AutoCreate        | true — создать схему БД при старте      |
| POSTGRES_* , SWAGGER_ENABLED| значения для docker-compose (.env)      |

## Миграции EF Core (вместо AutoCreate)
    dotnet tool install --global dotnet-ef
    dotnet ef migrations add Init
В Program.cs замените `EnsureCreatedAsync()` на `MigrateAsync()`.

## CI/CD
.github/workflows/ci.yml: сборка проекта, затем сборка и публикация образа
в GitHub Container Registry (ghcr.io/<owner>/<repo>) при пуше в main и по тегам v*.
Запуск образа на сервере: `docker pull ghcr.io/<owner>/<repo>:main`.
