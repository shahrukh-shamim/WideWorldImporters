#!/bin/bash

# Wait for SQL Server to actually be ready, instead of guessing a fixed sleep.
echo "Waiting for SQL Server to start..."
for i in $(seq 1 60); do
  if /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" &>/dev/null; then
    echo "SQL Server is ready."
    break
  fi
  sleep 2
done

# Skip setup entirely if the database already exists (e.g. after a restart with a persistent volume).
DB_EXISTS=$(/opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "$MSSQL_SA_PASSWORD" -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'WideWorldImporters'" | tr -d '[:space:]')

if [ "$DB_EXISTS" = "1" ]; then
  echo "WideWorldImporters database already exists, skipping restore/setup."
else
  echo "Restoring WideWorldImporters database..."
  /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "$MSSQL_SA_PASSWORD" -d master -i restore.sql
  /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "$MSSQL_SA_PASSWORD" -d master -i security.sql
  /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "$MSSQL_SA_PASSWORD" -d master -i users.sql
fi