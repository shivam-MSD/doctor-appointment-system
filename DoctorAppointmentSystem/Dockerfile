# Use the official .NET 10 SDK image to compile the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the csproj file and restore dependencies
COPY ["DoctorAppointmentSystem/DoctorAppointmentSystem.csproj", "DoctorAppointmentSystem/"]
RUN dotnet restore "DoctorAppointmentSystem/DoctorAppointmentSystem.csproj"

# Copy all source files and build the project
COPY . .
WORKDIR "/src/DoctorAppointmentSystem"
RUN dotnet build "DoctorAppointmentSystem.csproj" -c Release -o /app/build

# Publish the release build
FROM build AS publish
RUN dotnet publish "DoctorAppointmentSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core 10 runtime image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render dynamically assigns a PORT environment variable and expects Kestrel to listen to it
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DoctorAppointmentSystem.dll"]
