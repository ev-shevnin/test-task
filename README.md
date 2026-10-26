# TestTask - C#/.NET Backend Developer

## Стек

- .NET 9, ASP.NET Core Minimal API
- PostgreSQL + EF Core (миграции применяются автоматически при старте)
- RabbitMQ (только publish)
- Docker Compose

## Запуск

**1. Создать `.env` из примера:**

```bash
cp .env.example .env
```

**2. Поднять приложение:**

```bash
docker compose up -d
```

API будет доступно на `http://localhost:8080`.

## API

| Метод | Эндпоинт | Описание |
|-------|----------|----------|
| POST | `/tasks` | Создать задачу |
| GET | `/tasks` | Получить все задачи |
| PUT | `/tasks/{id}/complete` | Завершить задачу |
| DELETE | `/tasks/{id}` | Удалить задачу |

Интерактивная документация (Scalar): `http://localhost:8080/scalar/v1`

## Тест

Через Testcontainers
