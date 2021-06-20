FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["StickerBot/StickerBot.csproj", "StickerBot/"]
COPY ["JournalApiClient/JournalApiClient.csproj", "JournalApiClient/"]
RUN dotnet restore "StickerBot/StickerBot.csproj"
COPY . .
WORKDIR "/src/StickerBot"

FROM build AS publish
RUN dotnet publish "StickerBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
COPY --from=publish /app/publish .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet StickerBot.dll