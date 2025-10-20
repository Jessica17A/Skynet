# Usa la imagen oficial de .NET 8 para construir la app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia los archivos del proyecto
COPY *.csproj ./
RUN dotnet restore

# Copia todo y publica en carpeta /app
COPY . ./
RUN dotnet publish -c Release -o /app

# Usa la imagen de runtime para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Expone el puerto en el que correrá la app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comando de inicio
ENTRYPOINT ["dotnet", "SkyNet.dll"]
