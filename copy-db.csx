#r "nuget: Microsoft.SqlServer.SqlManagementObjects, 161.46367.54"

using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;


void EnableCopyAllExcept(Transfer transfer, params string[] propsToFalse)
{
    var props = transfer.GetType().GetProperties().Where(p =>
        p.PropertyType == typeof(bool) && p.Name.StartsWith("CopyAll"));
    foreach (var prop in props)
    {
        var value = !propsToFalse.Contains(prop.Name);
        prop.SetValue(transfer, value);
    }
}

var connectionStringSrc = new SqlConnectionStringBuilder(Args[0]);
var connectionStringDest = new SqlConnectionStringBuilder(Args[1]);
var connectionMasterDest = new SqlConnectionStringBuilder(connectionStringDest.ConnectionString)
{
    InitialCatalog = "master"
};

var sourceServer = new Server(new ServerConnection(new SqlConnection(connectionStringSrc.ConnectionString)));
var destMasterConnection = new SqlConnection(connectionMasterDest.ConnectionString);
destMasterConnection.Open();
var destServerConnection = new ServerConnection(destMasterConnection);
var destServer = new Server(destServerConnection);

var dropCmd = destMasterConnection.CreateCommand();
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

        
