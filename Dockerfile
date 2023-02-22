#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["scraper.salamnews.org.csproj", "."]
RUN dotnet restore "./scraper.salamnews.org.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "scraper.salamnews.org.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "scraper.salamnews.org.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "scraper.salamnews.org.dll"]