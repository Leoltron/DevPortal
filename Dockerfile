FROM scratch AS base
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM docker.io/oven/bun:alpine AS frontend
WORKDIR /src 
COPY --exclude=./frontend/public ./frontend .
RUN bun build ./index.html --minify --outdir=./dist
COPY ./frontend/public ./dist

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY DevPortal/DevPortal.csproj .
RUN dotnet restore DevPortal.csproj

COPY DevPortal .
COPY --from=frontend /src/dist ./wwwroot

RUN dotnet publish DevPortal.csproj \
    -c Release \
    -r linux-musl-x64 \
    --self-contained true \
    -o /out \
    -p:IlcGenerateStackTraceData=true \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -p:PublishAot=true \
    -p:StaticExecutable=true 

FROM base AS final
WORKDIR /app
COPY --from=build --chown=10001:10001 /out/DevPortal /out/DevPortal.staticwebassets.endpoints.json .
COPY --from=build --chown=10001:10001 /out/wwwroot ./wwwroot
USER 10001:10001
ENTRYPOINT ["/app/DevPortal"]
