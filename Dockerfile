FROM scratch AS base
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM docker.io/oven/bun:alpine AS frontend
WORKDIR /src 
COPY ./frontend .
RUN bun build ./index.html --minify --outdir=./dist

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY DevPortal/DevPortal.csproj .
RUN dotnet restore DevPortal.csproj

COPY DevPortal .
COPY --from=frontend /src/dist ./wwwroot
COPY ./frontend/assets ./wwwroot/assets

RUN dotnet publish DevPortal.csproj \
    -c Release \
    -r linux-musl-x64 \
    --self-contained true \
    -o /out \
    /p:PublishAot=true \
    /p:StaticExecutable=true 

RUN ls -lah /out

FROM base AS final
WORKDIR /app
COPY --from=build /out/DevPortal /out/DevPortal.staticwebassets.endpoints.json .
COPY --from=build /out/wwwroot ./wwwroot
ENTRYPOINT ["/app/DevPortal"]
