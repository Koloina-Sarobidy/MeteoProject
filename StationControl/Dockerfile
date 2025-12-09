# Étape 1 : Build de l'application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copie du fichier csproj et restauration des dépendances
COPY *.csproj ./
RUN dotnet restore

# Copie du reste du code et publication de l'application
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Étape 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Exposition du port de l'application
EXPOSE 8000

# Lancement de l'application l'application
ENTRYPOINT ["dotnet", "StationControl.dll"]
