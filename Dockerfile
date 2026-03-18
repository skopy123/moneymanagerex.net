FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY mmex.net.core/mmex.net.core.csproj mmex.net.core/
COPY mmex.net.webServer/mmex.net.webServer.csproj mmex.net.webServer/
RUN dotnet restore mmex.net.webServer/mmex.net.webServer.csproj

COPY mmex.net.core/ mmex.net.core/
COPY mmex.net.webServer/ mmex.net.webServer/
RUN dotnet publish mmex.net.webServer/mmex.net.webServer.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "mmex.net.webServer.dll"]
