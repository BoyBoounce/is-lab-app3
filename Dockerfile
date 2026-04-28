FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IsLabApp.csproj ./
RUN dotnet restore ./IsLabApp.csproj

COPY . ./
RUN dotnet publish ./IsLabApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "IsLabApp.dll"]
