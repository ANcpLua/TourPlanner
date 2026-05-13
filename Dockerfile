FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:dc8430e6024d454edadad1e160e1973be3cabbb7125998ef190d9e5c6adf7dbb AS build
WORKDIR /src

COPY *.sln ./
COPY Directory.Packages.props Directory.Build.props Version.props global.json ./
COPY API/*.csproj API/
COPY BL/*.csproj BL/
COPY DAL/*.csproj DAL/
COPY Contracts/*.csproj Contracts/
COPY UI.Client/*.csproj UI.Client/
COPY Tests/*.csproj Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish "API/API.csproj" -c Release -o /app/api/publish
RUN dotnet publish "UI.Client/UI.Client.csproj" -c Release -o /app/ui/publish /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:9b5222b0ff8e9eb991a7c1a64b25f0f771d21ccc05dfa1c834f5668ffd9cd73f AS api
WORKDIR /app

COPY --from=build /app/api/publish ./

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "API.dll"]

FROM nginx:alpine@sha256:5616878291a2eed594aee8db4dade5878cf7edcb475e59193904b198d9b830de AS ui
WORKDIR /usr/share/nginx/html

COPY --from=build /app/ui/publish/wwwroot ./
COPY nginx.conf /etc/nginx/nginx.conf

CMD ["nginx", "-g", "daemon off;"]
