using System;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace mssql_copy_script
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionStringSrc = new SqlConnectionStringBuilder(args[0]);
            var connectionStringDest = new SqlConnectionStringBuilder(args[1]);

            var connectionMasterDest = new SqlConnectionStringBuilder(args[1])
            {
                InitialCatalog = "master"
            };

            var sourceServer = new Server(new ServerConnection(new SqlConnection(connectionStringSrc.ConnectionString)));
            using var destMasterConnection = new SqlConnection(connectionMasterDest.ConnectionString);
            destMasterConnection.Open();
            var destServerConnection = new ServerConnection(destMasterConnection);
            var destServer = new Server(destServerConnection);

            using var dropCmd = destMasterConnection.CreateCommand();
            dropCmd.CommandText = $"drop database if exists [{connectionStringDest.InitialCatalog}]";
            dropCmd.ExecuteNonQuery();
            
            var destDb = new Database(destServer, connectionStringDest.InitialCatalog);
            destDb.Create();

            var sourceDb = sourceServer.Databases[connectionStringSrc.InitialCatalog];
            
            var transfer = new Transfer(sourceDb);
            transfer.Options.WithDependencies = true;
            transfer.Options.ContinueScriptingOnError = true;

            transfer.DestinationServerConnection = destServerConnection;
            transfer.DestinationDatabase = connectionStringDest.InitialCatalog;

            EnableCopyAllExcept(transfer,
                nameof(transfer.CopyAllLogins),
                nameof(transfer.CopyAllObjects),
                nameof(transfer.CopyAllUsers));

            transfer.CopySchema = true;
            transfer.CopyData = true;

            transfer.TransferData();
        }

        private static void EnableCopyAllExcept(Transfer transfer, params string[] propsToFalse)
        {
            var props = transfer.GetType().GetProperties().Where(p =>
                p.PropertyType == typeof(bool) && p.Name.StartsWith("CopyAll"));
            foreach (var prop in props)
            {
                var value = !propsToFalse.Contains(prop.Name);
                prop.SetValue(transfer, value);
            }
        }
    }
}
