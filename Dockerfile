# Imagen base para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo .csproj desde la carpeta SkyNet
COPY SkyNet/*.csproj SkyNet/
RUN dotnet restore SkyNet/SkyNet.csproj

# Copiar el resto del código
COPY . .

# Publicar el proyecto en modo Release
RUN dotnet publish SkyNet/SkyNet.csproj -c Release -o /app

# Imagen base para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Render usa el puerto 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comando de inicio
ENTRYPOINT ["dotnet", "SkyNet.dll"]
