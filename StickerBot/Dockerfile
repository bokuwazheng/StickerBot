#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY *.sln .
COPY ./src/StickerBot.csproj ./src/
RUN dotnet restore
COPY . .
RUN dotnet publish "StickerBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:5.0
WORKDIR /app
COPY --from=build /app/publish .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet StickerBot.dll

#FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443
#
#FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
#WORKDIR /src
#COPY ["StickerBot/StickerBot.csproj", "StickerBot/"]
#RUN dotnet restore "StickerBot/StickerBot.csproj"
#COPY . .
#WORKDIR "/src/StickerBot"
#RUN dotnet build "StickerBot.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "StickerBot.csproj" -c Release -o /app/publish
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "StickerBot.dll"]