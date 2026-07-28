# Lex

Документы и чек-листы.

[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-v8.15.0-blue)](https://mudblazor.com/)

<img width="834" height="644" alt="скриншоты lex" src="https://github.com/user-attachments/assets/8c0aadc0-cd98-474e-8d27-a0e42462367b" />

## Что есть

#### Документы

- Каталог шаблонов
- Создание документа на основе шаблона
- Редактирование документа (.md) включая доступ и видимость для других пользователей
- Просмотр документа в формате .md
- Список документов пользователя

#### Чек-листы

- Каталог чек-листов
- Прохождение (страт) чек-листа
- Список чек-листов пользователя

#### Организации

- Список публичных
- Создание и редактирование своей
- Добавление сотрудников, прикрепление документов и чек листов

#### Дополнительно

- Страницы для администрирования (создания/удаления шаблонов и управления пользователями)
- Приватность реализована в виде поля в доменных моделях и обработаны права доступа в сервисах и компонентах

#### UI/UX

- Микроанимации для компонентов
- Действия сопровождаются уведомлениями (в снек баре)
- Минимально, но приемлемо адаптированно на мобильные устройства

## Стек технологий

- .NET 9
- MudBlazor
- SQLite + EF Core
- ASP.NET Core Identity

## Быстрый старт

```bash
git clone https://github.com/just-between-us/LexFlow.git
cd LexFlow
dotnet restore
dotnet ef migrations add InitialCreate --project Lex.Infrastructure/Lex.Infrastructure.csproj --startup-project Lex/Lex.csproj
dotnet ef database update --project Lex.Infrastructure/Lex.Infrastructure.csproj --startup-project Lex/Lex.csproj
```

## Скриншоты

<p align="center">
  <img width="1332" height="852" alt="image" src="https://github.com/user-attachments/assets/27dad699-f805-4f68-b569-36002d1748f9" />
  <img width="1277" height="601" alt="image" src="https://github.com/user-attachments/assets/e1d0c34e-ef2c-4150-9b84-5fc927b71612" />
</p>

#### Список документов пользователя

<p align="center">
  <img width="1014" height="516" align="center" alt="image" src="https://github.com/user-attachments/assets/933d2aa6-87d2-4c79-93fb-512f5ee8d22a" />
</p>

#### Прохождение чек-листа

<p align="center">
  <img width="773" height="843"  align="center" alt="image" src="https://github.com/user-attachments/assets/1ccd001e-83f8-4c45-bdf7-af0f9084bcc7" />
</p>

#### Профиль пользователя

<p align="center">
 <img width="773" height="872" align="center" alt="image" src="https://github.com/user-attachments/assets/50ca5df5-cca6-464c-85b5-9287b3e73a6c" />
</p>

#### Профиль организации 

<p align="center">
  <img width="787" height="885" align="center" alt="image" src="https://github.com/user-attachments/assets/e2725c68-0003-49eb-abf4-571328836719" />
</p>

#### Редактирование документа

<p align="center">
  <img width="1904" height="937" align="center" alt="image" src="https://github.com/user-attachments/assets/2cb0ba5f-6349-4e5d-85c8-28e308cd6d54" />
</p>

####

<p align="center">
  <img width="1835" height="853" align="center" alt="image" src="https://github.com/user-attachments/assets/91d9154f-eb83-45a8-9276-036db1e63e83" />
</p>

#### Вход 
``` 
¯\_(ツ)_/¯
```

<p align="center">
  <img width="468" height="568" align="center" alt="image" src="https://github.com/user-attachments/assets/475e4603-c23c-49c1-b73c-9ffb48441835" />
</p>
