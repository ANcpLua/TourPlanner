FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:ea8bde36c11b6e7eec2656d0e59101d4462f6bd630730f2c8201ed0572b295d5 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:7644f992230d35cf230017189d4038c0ae0f7388b13f4f7ae1900a155bafb597 AS api
WORKDIR /app

COPY --from=build /app/api/publish ./

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "API.dll"]

FROM nginx:alpine@sha256:4a73073bd557c65b759505da037898b61f1be6cbcc3c2c3aeac22d2a470c1752 AS ui
WORKDIR /usr/share/nginx/html

COPY --from=build /app/ui/publish/wwwroot ./
COPY nginx.conf /etc/nginx/nginx.conf

CMD ["nginx", "-g", "daemon off;"]
