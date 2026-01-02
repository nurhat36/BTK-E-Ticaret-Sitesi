# 1. Build Aþamasý
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasýný kopyala ve paketleri indir
COPY ["BTKETicaretSitesi/BTKETicaretSitesi.csproj", "BTKETicaretSitesi/"]
RUN dotnet restore "BTKETicaretSitesi/BTKETicaretSitesi.csproj"

# Kalan tüm dosyalarý kopyala
COPY . .

# Build ve Publish iþlemi
WORKDIR "/src/BTKETicaretSitesi"
RUN dotnet build "BTKETicaretSitesi.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "BTKETicaretSitesi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Aþamasý
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "BTKETicaretSitesi.dll"]