FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Meran.Back.sln .
COPY Meran.Back/Meran.Back.csproj Meran.Back/
COPY Meran.Back.Tests/Meran.Back.Tests.csproj Meran.Back.Tests/
RUN dotnet restore Meran.Back/Meran.Back.csproj

COPY . .
RUN dotnet publish Meran.Back/Meran.Back.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Meran.Back.dll"]