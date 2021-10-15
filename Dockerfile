#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0.11-alpine3.13 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0.402 AS build
WORKDIR /src
COPY ["RoslynDiscord/RoslynDiscord.csproj", "RoslynDiscord/"]
COPY ["RoslynDiscord.Shared/RoslynDiscord.Shared.csproj", "RoslynDiscord.Shared/"]
RUN dotnet restore "RoslynDiscord/RoslynDiscord.csproj"
COPY . .
WORKDIR "/src/RoslynDiscord"
RUN dotnet build "RoslynDiscord.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RoslynDiscord.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RoslynDiscord.dll"]