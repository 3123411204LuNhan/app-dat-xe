#!/bin/bash
# ===== BUILD & TEST SCRIPT FOR RIDE-HAILING APP =====
# Chạy trên máy ảo Linux (Ubuntu 22.04+) hoặc Windows

set -e  # Exit on error

echo "🚀 Starting Build & Test Process..."
echo "================================================"

# ===== STEP 1: INSTALL PREREQUISITES =====
echo ""
echo "📦 STEP 1: Installing Prerequisites..."
echo "================================================"

# Check if running on Windows (WSL) or Linux
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo "✅ Windows environment detected"
    # Windows: Install .NET SDK if not present
    # Download from: https://dotnet.microsoft.com/download/dotnet/10.0
else
    echo "✅ Linux/WSL environment detected"
    # Update package manager
    sudo apt-get update
    
    # Install .NET 10 SDK
    echo "Installing .NET 10 SDK..."
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --version latest --install-dir /usr/local/dotnet
    export PATH=$PATH:/usr/local/dotnet
    
    # Verify installation
    dotnet --version
fi

# ===== STEP 2: PREPARE DATABASE =====
echo ""
echo "💾 STEP 2: Setting up SQL Server..."
echo "================================================"

if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo "⚠️  Windows detected: SQL Server setup manual"
    echo ""
    echo "   Option A: SQL Server Express (Local)"
    echo "   - Download: https://www.microsoft.com/sql-server/sql-server-downloads"
    echo "   - Set sa password, enable TCP/IP"
    echo ""
    echo "   Option B: Docker (Recommended)"
    echo "   docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=YourStrong!Pass' \"
    echo "     -p 1433:1433 --name mssql -d mcr.microsoft.com/mssql/server:2022-latest"
else
    echo "Installing SQL Server on Linux..."
    
    # Add Microsoft repository
    curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
    sudo add-apt-repository "$(curl https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list)"
    
    # Install SQL Server
    sudo apt-get install -y mssql-server
    
    # Configure SQL Server (or use Docker)
    echo "Using Docker for SQL Server (easier)..."
    sudo apt-get install -y docker.io
    
    docker run -e 'ACCEPT_EULA=Y' \
               -e 'MSSQL_SA_PASSWORD=YourStrong!Pass123' \
               -p 1433:1433 \
               --name mssql \
               -d mcr.microsoft.com/mssql/server:2022-latest
    
    echo "⏳ Waiting for SQL Server to start..."
    sleep 10
fi

echo "✅ SQL Server ready on localhost:1433"

# ===== STEP 3: CREATE DATABASES =====
echo ""
echo "🗂️  STEP 3: Creating Databases..."
echo "================================================"

echo "Creating databases: north, north_rep, south, south_rep..."

# Note: This requires sqlcmd or mssql-cli to be installed
# For now, provide manual steps

cat << 'EOF'

If using Docker, connect with:
  docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong!Pass123'

Then run these SQL commands:

CREATE DATABASE north;
CREATE DATABASE north_rep;
CREATE DATABASE south;
CREATE DATABASE south_rep;

Or run the provided SQL scripts:
  sql_server_script/north.sql
  sql_server_script/south.sql

EOF

read -p "Press ENTER after creating databases..."

# ===== STEP 4: RUN DATABASE MIGRATIONS =====
echo ""
echo "📊 STEP 4: Running Database Migrations..."
echo "================================================"

echo "Running pooling schema migration on all 4 databases..."
echo ""
echo "⚠️  MANUAL STEP: Connect to each database and run:"
echo "   sql_server_script/migration_pooling_schema.sql"
echo ""
echo "Using SSMS or sqlcmd:"
echo "  sqlcmd -S localhost -U sa -P 'YourStrong!Pass123' -d north < migration_pooling_schema.sql"
echo "  sqlcmd -S localhost -U sa -P 'YourStrong!Pass123' -d north_rep < migration_pooling_schema.sql"
echo "  sqlcmd -S localhost -U sa -P 'YourStrong!Pass123' -d south < migration_pooling_schema.sql"
echo "  sqlcmd -S localhost -U sa -P 'YourStrong!Pass123' -d south_rep < migration_pooling_schema.sql"

read -p "Press ENTER after running migrations..."

# ===== STEP 5: UPDATE CONNECTION STRINGS =====
echo ""
echo "🔗 STEP 5: Updating Connection Strings..."
echo "================================================"

echo "Updating RideHailingApi/appsettings.Development.json..."
echo "Ensure connection strings point to your SQL Server instance:"
cat << 'EOF'

{
  "ConnectionStrings": {
    "North_Primary": "Server=localhost;Database=north;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;",
    "North_Replica": "Server=localhost;Database=north_rep;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;",
    "South_Primary": "Server=localhost;Database=south;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;",
    "South_Replica": "Server=localhost;Database=south_rep;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "Key": "your-secret-key-at-least-32-characters-long-1234567890",
    "Issuer": "ride-hailing-api",
    "Audience": "ride-hailing-app",
    "ExpiryMinutes": 60
  }
}

EOF

read -p "Press ENTER after updating connection strings..."

# ===== STEP 6: BUILD BACKEND =====
echo ""
echo "🔨 STEP 6: Building Backend API..."
echo "================================================"

cd RideHailingApi

echo "Restoring NuGet packages..."
dotnet restore

echo "Building solution..."
dotnet build --configuration Debug

echo "✅ Backend build successful!"

# ===== STEP 7: BUILD FRONTEND (MAUI) =====
echo ""
echo "📱 STEP 7: Building MAUI App..."
echo "================================================"

cd ../RideHailingApp

echo "Restoring NuGet packages..."
dotnet restore

# Choose target framework
echo ""
echo "Select build target:"
echo "1. Windows (net9.0-windows10.0.19041.0) - Default"
echo "2. Android (net9.0-android)"
echo "3. iOS (net9.0-ios)"
read -p "Enter choice (1-3): " choice

case $choice in
    1)
        TARGET="net9.0-windows10.0.19041.0"
        ;;
    2)
        TARGET="net9.0-android"
        ;;
    3)
        TARGET="net9.0-ios"
        ;;
    *)
        TARGET="net9.0-windows10.0.19041.0"
        ;;
esac

echo "Building for $TARGET..."
dotnet build -f $TARGET --configuration Debug

echo "✅ MAUI app build successful!"

# ===== STEP 8: RUN BACKEND =====
echo ""
echo "🚀 STEP 8: Starting Backend API..."
echo "================================================"

cd ../RideHailingApi

echo "Starting API on http://localhost:5108"
echo "To stop, press Ctrl+C"
echo ""

dotnet run --configuration Debug --no-build

