#r "Newtonsoft.Json"
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

// Generic HTTP request
// If a token is provided, it inserts it as Auth Header using the ARM API syntax
public static async Task<JObject> sendHttpRequestAsync(string method, string url, string token = null, string payload = null, string contentType = "application/json", string accept = "application/json")
{
    var request = WebRequest.Create(url);
    request.Method = method;
    if (token != null)
    {
        request.Headers.Add("Authorization", "Bearer " + token);
    }
    request.ContentType = contentType;
    // Add the body only for POST or PUT
    if ((method == "POST") || (method == "PUT")) {
        using (var writer = new StreamWriter(request.GetRequestStream()))
        {
            writer.Write(payload);
        }
    }
    // Send the request
    try
    {
        var response = await request.GetResponseAsync();
        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            var responseContent = reader.ReadToEnd();
            var adResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(responseContent);
            return adResponse;
        }
    }
    // If something did not work...
    catch (WebException webException)
    {
        if (webException.Response != null)
        {
            using (var reader = new StreamReader(webException.Response.GetResponseStream()))
            {
                var responseContent = reader.ReadToEnd();
                var adResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(responseContent);
                return adResponse;
            }
        }
        else
        {
            return null;
        }
    }
}


// Sends a POST request to get an ARM auth tokenn using App authentication
private static async Task<string> getArmTokenAsync(string TenantId, string AppId, string AppSecret)
{
    string contentType = "application/x-www-form-urlencoded";
    string url = "";
    string payload = "";
    url = "https://login.windows.net/" + TenantId + "/oauth2/token";
    string resource = "https://management.azure.com/";
    payload = "grant_type=client_credentials&resource=" + resource + "&client_id="
                + AppId + "&client_secret=" + AppSecret;
    JObject jsonResponse = await sendHttpRequestAsync("POST", url, payload: payload, contentType: contentType);
    string token = jsonResponse["access_token"].ToString();
    if (token != null)
    {
        return token;
    }
    else
    {
        return null;
    }
}

// Sends a GET request to get the list from the VMs from the ARM REST API
public static async Task<JObject> getVMs(string token, string subscriptionId, TraceWriter log) {
    string apiVersion = "2016-03-30";
    string url = "https://management.azure.com/subscriptions/" + subscriptionId + "/providers/Microsoft.Compute/virtualMachines?api-version=" + apiVersion;
    log.Info("Sending GET to " + url);
    string contentType = "application/json";
    JObject jsonResponse = await sendHttpRequestAsync("GET", url, contentType: contentType, token: token);
    return jsonResponse;
}


// Verify DB to see whether VM exists
private static bool VmExists(string VmName)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "SELECT * FROM VMs WHERE Name = '" + VmName + "'";
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        // create data adapter
        SqlDataAdapter da = new SqlDataAdapter(cmd);
        var dataTable = new DataTable();
        // this will query your database and return the result to your datatable
        da.Fill(dataTable);
        if (dataTable.Rows.Count > 0) {
            return true;
        }
        connection.Close();
        da.Dispose();
        return false;
    }
    catch
    {
        return false;
    }    
}

// Add a new VM to the Database
private static bool AddVmToDb(string VmName, string RGName, string OSType)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "INSERT INTO VMs (VMId, Name, OS, ResourceGroup) VALUES ('" + VmName + "', '" + VmName + "', '" + OSType + "', '" + RGName + "')";
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        cmd.ExecuteNonQuery();
        connection.Close();
        return true;
    }
    catch
    {
        return false;
    }    
}

// Delete all records from a table in the Database
private static bool DeleteTableFromDb(string TableName, TraceWriter log)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "DELETE FROM " + TableName;
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        cmd.ExecuteNonQuery();
        connection.Close();
        return true;
    }
    catch (Exception e)
    {
        log.Info("An error occurred: " + e.Message);
        return false;
    }    
}



// Update an existing VM in the Database, if it already existed
private static bool UpdateVmToDb(string VmName, string RGName, string OSType, TraceWriter log)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "IF EXISTS " +  
            "(SELECT * FROM VMs WHERE Name = '" + VmName +"') " +
            "BEGIN" + 
            "   UPDATE VMs " +
            "   SET OS='" + OSType + "', ResourceGroup='" + RGName + "'" +
            "   WHERE Name = '" + VmName + "'" +
            "END";
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        cmd.ExecuteNonQuery();
        connection.Close();
        return true;
    }
    catch (Exception e)
    {
        log.Info("An error occurred: " + e.Message);
        return false;
    }    
}

// Delete VMs, NICs and IPs tables fromt the database, so that they can be recreated
private static bool DeleteInfoFromDb (TraceWriter log)
{

    bool aux = true;
    // VMs
    if (DeleteTableFromDb("VMs", log)) {
        log.Info("VMs table deleted successfully");
    } else {
        aux = false;
        log.Info("Error deleting VMs table");
    }
    // NICs
    if (DeleteTableFromDb("NICs", log)) {
        log.Info("NICs table deleted successfully");
    } else {
        aux = false;
        log.Info("Error deleting NICs table");
    }
    // Tags
    if (DeleteTableFromDb("NICs", log)) {
        log.Info("Tags table deleted successfully");
    } else {
        aux = false;
        log.Info("Error deleting Tags table");
    }
    // Return
    return aux;
}


// Add a new VM-NIC association to the Database if it did not exist
private static bool AddVmNicToDb(string VmName, string NicName)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "IF NOT EXISTS " +  
            "(SELECT * FROM NICs WHERE NicId = '" + NicName +"' AND VmId = '" + VmName + "') " +
            "BEGIN" + 
            "   INSERT INTO NICs (VmId, NicId) VALUES ('" + VmName + "', '" + NicName + "') " +
            "END";
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        cmd.ExecuteNonQuery();
        connection.Close();
        return true;
    }
    catch
    {
        return false;
    }    
}

// Add a new VM-NIC association to the Database if it did not exist
private static bool AddTagToDb(string vmName, string tagName, string tagValue)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "IF NOT EXISTS " +  
            "(SELECT * FROM Tags WHERE VmId = '" + vmName +"' AND TagName = '" + tagName + "') " +
            "BEGIN" + 
            "   INSERT INTO Tags (VmId, TagName, TagValue) VALUES ('" + vmName + "', '" + tagName + "', '" + tagValue + "') " +
            "END";
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        cmd.ExecuteNonQuery();
        connection.Close();
        return true;
    }
    catch
    {
        return false;
    }    
}

// Given an ARM ID, extract the Resource Group Name
private static string getRgFromId(string Id) {
    string[] words = Id.Split('/');
    return words[4];
}

// Given an ARM ID for a NIC, extract the NIC name
private static string getNicNameFromId (string Id) {
    string[] words = Id.Split('/');
    return words[8];    
}

// Main
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // Auth variables
    string TenantId = "";
    string AppId = "";
    string SubscriptionId="";
    string AppSecret="";
    try
    {
        TenantId  = ConfigurationManager.AppSettings["TenantId"].ToString();
        AppId  = ConfigurationManager.AppSettings["AppId"].ToString();
        SubscriptionId  = ConfigurationManager.AppSettings["SubscriptionId"].ToString();
        AppSecret  = ConfigurationManager.AppSettings["AppSecret"].ToString();
    }
    catch
    {
        log.Info ("Error retrieving app settings");
        return null;
    }

    string token = await getArmTokenAsync(TenantId, AppId, AppSecret);
    JObject JsonVMs = await getVMs(token, SubscriptionId, log);
    List<JToken> JsonVMsValue = new List<JToken>();
    JsonVMsValue = JsonVMs.SelectToken("value").ToList();

    // These line deletes all tables to make sure we have a clean slate
    if (DeleteInfoFromDb(log)) {
        log.Info("VMs table deleted successfully");
    } else {
        log.Info("Error deleting database tables");
    }


    // Counter for VMs added to DB
    int VmsAddedToDb = 0;
    foreach (JObject Vm in JsonVMsValue)
    {
        // Add it to DB
        string VmName = Vm["name"].ToString();
        JObject VMProperties = Vm["properties"].Value<JObject>();
        JObject JStorageProfile = VMProperties["storageProfile"].Value<JObject>();
        JObject JOsDisk = JStorageProfile["osDisk"].Value<JObject>();
        string OsType = JOsDisk["osType"].ToString();
        string ResourceGroupName = getRgFromId(Vm["id"].ToString());
        bool dbOK = VmExists(Vm["name"].ToString());
        if (dbOK) {
            log.Info(Vm["name"] + " already exists in the database, updating");
            if (UpdateVmToDb(VmName, ResourceGroupName, OsType, log)) {
                log.Info("Database updated successfully");
            } else {
                log.Info("Error updating the database");
            }
        } else {
            log.Info("Finding info for " + Vm["name"] + ", in RG " + ResourceGroupName);
            log.Info("Adding to the DB: " + Vm["name"] + ", " + ResourceGroupName + ", " + OsType);
            if (AddVmToDb(VmName, ResourceGroupName, OsType)) {
                log.Info("VM added successfully to the database");
                VmsAddedToDb += 1;
            } else {
                log.Info("Error adding the VM to the database");               
            }
        }

        // Analyze tags of the retrieved VM
        try {
            if (Vm["tags"] != null) {
                foreach (JProperty Tag in Vm["tags"])
                {
                    string tagName = Tag.Name;
                    //log.Info ("Tag found for " + VmName + ": " + tagName);
                    string tagValue = Tag.Value.ToString();
                    log.Info ("Tag-Value found for " + VmName + ": " + tagName + "-" + tagValue);
                    if (AddTagToDb (VmName, tagName, tagValue)) {
                        log.Info("Tag added successfully to the database");
                    } else {
                        log.Info("Error adding tag to the database");               
                    }
                }
            }
        } catch {
            log.Info("There was some error processing tags for VM '" + VmName + "'");
        }

        // Analyze NICs of the retrieved VM
        JObject JNetworkProfile = VMProperties["networkProfile"].Value<JObject>();
        List<JToken> JNics = new List<JToken>();
        JNics = JNetworkProfile.SelectToken("networkInterfaces").ToList();
        foreach (JObject Nic in JNics) {
            string NicName = getNicNameFromId (Nic["id"].ToString());
            AddVmNicToDb(VmName, NicName);
            log.Info ("Updating DB with Nic " + NicName + " found in VM " + VmName);
        }
        
    }

    return req.CreateResponse(HttpStatusCode.OK, "Function run correctly: " + VmsAddedToDb.ToString() + " VMs added to the database");
}
