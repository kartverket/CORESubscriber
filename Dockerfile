FROM microsoft/dotnet:2.1-runtime-alpine AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk-alpine AS build
WORKDIR /src
COPY CORESubscriber/CORESubscriber.csproj CORESubscriber/
RUN dotnet restore CORESubscriber/CORESubscriber.csproj
COPY . .
WORKDIR /src/CORESubscriber
RUN dotnet build CORESubscriber.csproj -c Release  -o /app

FROM build AS publish
RUN dotnet publish CORESubscriber.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
COPY ./cron /app/
RUN apk update cron 
