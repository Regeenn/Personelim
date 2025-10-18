# .NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Render.com PORT environment variable kullanır
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Build ortamı
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Layer caching için önce .csproj
COPY ["Personelim/Personelim.csproj", "Personelim/"]
RUN dotnet restore "Personelim/Personelim.csproj"

# Tüm dosyaları kopyala
COPY . .
WORKDIR "/src/Personelim"

# Build
RUN dotnet build "Personelim.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Personelim.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render.com için environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Personelim.dll"]
