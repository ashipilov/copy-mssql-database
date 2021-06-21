# copy-mssql-database
Very simple c# code to copy a mssql database from one server to another

# How to run

`dotnet run -- "<source_connection_string>" "<destination_connection_string>"`

for example, 

`dotnet run -- "Server=src_srv;Initial Catalog=src_db;User ID=scr_usr;Password=***" "Server=.;Initial Catalog=local_db;User ID=local_usr;Password=***"`

**WARNING**. The destination database will be dropped and recreated

