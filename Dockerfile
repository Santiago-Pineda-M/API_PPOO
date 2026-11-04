FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ApiPoo2.sln", "./"]
COPY ["src/ApiPoo2.WebApi/ApiPoo2.WebApi.csproj", "src/ApiPoo2.WebApi/"]
COPY ["src/ApiPoo2.Application/ApiPoo2.Application.csproj", "src/ApiPoo2.Application/"]
COPY ["src/ApiPoo2.Domain/ApiPoo2.Domain.csproj", "src/ApiPoo2.Domain/"]
COPY ["src/ApiPoo2.Infrastructure/ApiPoo2.Infrastructure.csproj", "src/ApiPoo2.Infrastructure/"]
RUN dotnet restore "src/ApiPoo2.WebApi/ApiPoo2.WebApi.csproj"
COPY . .
WORKDIR "/src/src/ApiPoo2.WebApi"
RUN dotnet publish "ApiPoo2.WebApi.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiPoo2.WebApi.dll"]
