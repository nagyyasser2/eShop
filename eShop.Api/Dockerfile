# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and restore dependencies
COPY *.sln ./
COPY MyApp.Api/*.csproj MyApp.Api/
COPY MyApp.Core/*.csproj MyApp.Core/
COPY MyApp.Infrastructure/*.csproj MyApp.Infrastructure/

RUN dotnet restore MyApp.Api/MyApp.Api.csproj

# Copy the rest of the files
COPY . ./
WORKDIR /src/MyApp.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
