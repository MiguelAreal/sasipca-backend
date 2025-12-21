# We use the ASP.NET Core Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy the published files from the current directory into the container
COPY . .

# Set the environment variable to listen on port 5000 inside the container
ENV ASPNETCORE_URLS=http://+:5000

# Run the executable
ENTRYPOINT ["./sasipca_API"]