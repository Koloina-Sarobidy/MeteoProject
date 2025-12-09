# StationControl - ASP.NET Core MVC avec Docker et MariaDB
Ce projet est une application **ASP.NET Core MVC** connectée à une base de données **MariaDB**, entièrement dockerisée pour un déploiement facile et reproductible.


## 1️⃣ Prérequis
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installé  
- [Docker Compose](https://docs.docker.com/compose/) inclus  
- Optionnel : [.NET SDK 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) si compilation locale

## 2️⃣ Structure du projet
StationControl/
    Dockerfile
    docker-compose.yml
    StationControl.csproj
    Program.cs
    Controllers/
    Views/
    wwwroot/

## 3️⃣ Construire l’image Docker
- docker-compose build
Construit l’image web pour l’application ASP.NET Core MVC
Utilise l’image mariadb:11.8 pour la base de données

## 4️⃣ Lancer les conteneurs
- docker compose up -d
Ports exposés :
    - Application MVC : http://localhost:8000
    - Base de données MariaDB : localhost:3308:3306

## 5️⃣ Vérifier les conteneurs
- docker ps

## 6️⃣ Accéder à l’application
Ouvre ton navigateur et va sur :
Local: http://localhost:8000
En ligne: https://overrudely-malodorous-stephani.ngrok-free.dev


## Acceder a la base de donnees
Via docker: docker exec -it mariadb_db mariadb -h 127.0.0.1 -u root -p
Local: mysql -h 127.0.0.1 -P 3308 -u root -p
- mot de passe: root

## Email de test de l'app
- Email = girlspower434@gmail.com
- Mot de passe d'application = sjgu swux jdhb ghld


## Lancer ngrok
ngrok http --region=eu 8000
Lien: https://overrudely-malodorous-stephani.ngrok-free.dev