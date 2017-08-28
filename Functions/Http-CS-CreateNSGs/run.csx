#r "Newtonsoft.Json"
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

// Get the contents of a table in the form of a dictionary
private static List<Dictionary<string, string>> GetTableFromDb(string tableName, TraceWriter log) {
    try
    {
        var myList = new List<Dictionary<string,string>>();
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "SELECT * FROM " + tableName;
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        connection.Open();
        // create data adapter
        SqlDataAdapter da = new SqlDataAdapter(cmd);
        var dataTable = new DataTable();
        // this will query the database and return the result to the datatable
        da.Fill(dataTable);
        // loop through the datatable and fill in myList
        if (dataTable.Rows.Count > 0) {
            foreach (DataRow row in dataTable.Rows) {
                var thisItem = new Dictionary<string, string>();
                foreach (DataColumn column in dataTable.Columns) {
                    thisItem.Add(column.ColumnName.ToString(), row[column.ColumnName].ToString());
                }
                myList.Add(thisItem);
            }
        }
        connection.Close();
        da.Dispose();
        return myList;
    }
    catch (Exception e)
    {
        log.Info("An error occurred: " + e.Message);
        return null;
    }    

}

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

// Sends a GET request to get the details for the Resource Group from the ARM REST API
public static async Task<JObject> GetResourceGroupJSON (string token, string subscriptionId, string ResourceGroup, TraceWriter log) {
    string apiVersion = "2016-09-01";
    string url = "https://management.azure.com/subscriptions/" + subscriptionId + "/resourcegroups/" + ResourceGroup + "?api-version=" + apiVersion;
    log.Info("Sending GET to " + url);
    string contentType = "application/json";
    JObject jsonResponse = await sendHttpRequestAsync("GET", url, contentType: contentType, token: token);
    return jsonResponse;
}

// Sends a PUT request to create/update the NSG through the ARM REST API
public static async Task<JObject> CreateNSG (string token, string subscriptionId, List<Dictionary<string, string>> Rules, string nsgName, string ResourceGroup, string location, TraceWriter log) {
    string apiVersion = "2016-09-01";
    string url = "https://management.azure.com/subscriptions/" + subscriptionId + "/resourcegroups/" + ResourceGroup + 
               "/providers/Microsoft.Network/networkSecurityGroups/" + nsgName + "?api-version=" + apiVersion;
    log.Info("Sending PUT to " + url);
    string contentType = "application/json";
    string body = "{'location': '" + location + "', 'properties':{ 'securityRules':[";
    foreach (var rule in Rules) {
        string sourcePortRange = "*";
        if (rule["srcPort"].Length > 0) { sourcePortRange = rule["srcPort"]; }
        string sourceAddressPrefix = "*";
        if (rule["srcIp"].Length > 0) { sourceAddressPrefix = rule["srcIp"]; }
        string destinationPortRange = "*";
        if (rule["dstPort"].Length > 0) { destinationPortRange = rule["dstPort"]; }
        string destinationAddressPrefix = "*";
        if (rule["dstIp"].Length > 0) { destinationAddressPrefix = rule["dstIp"]; }
        string Access = "Deny";
        if (rule["action"] == "permit") { Access = "Allow"; }
        int order = Convert.ToInt32(rule["order"]);
        string ruleName = rule["direction"] + rule["order"];
        body = body + "{ 'name': '" + ruleName + "', 'properties': { 'description': ''," +
               "'protocol': '" + rule["dstProt"] + "', " +
               "'sourcePortRange': '" + sourcePortRange + "', 'sourceAddressPrefix': '" + sourceAddressPrefix + "', " +
               "'destinationPortRange': '" + destinationPortRange + "', 'destinationAddressPrefix': '" + destinationAddressPrefix + "', " +
               "'access': '" + Access + "', 'direction': '" + rule["direction"] + "', 'priority': " + order.ToString() + "} }, "; 
    }
    body = body + "]}}";
    log.Info ("Sending REST payload: " + body);
    JObject jsonResponse = await sendHttpRequestAsync("PUT", url, payload: body, contentType: contentType, token: token);
    log.Info ("REST Answer: " + jsonResponse.ToString(Formatting.None));
    return jsonResponse;
}



// Get the resource group for a specific VM out of the dictionary downloaded from the database
private static string GetVmResourceGroup (string vmName, List<Dictionary<string, string>> vmList) {
    foreach (var vm in vmList) {
        if (vm["Name"] == vmName) {
            return vm["ResourceGroup"];
        }
    }
    return "";
}

// Get list with unique names of NSGs
private static List<string> GetNSGlist(List<Dictionary<string, string>> nsgRules) {
    var myNsgList = new List<string>();
    foreach (var nsgRule in nsgRules) {
        if (!(myNsgList.Contains(nsgRule["nsgName"]))) {
            myNsgList.Add(nsgRule["nsgName"]);
        }
    }
    return myNsgList;
}

// Main function
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // Suffix that will be appended to the NSG names
    string nsgSuffix = "-dynNSG";

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

    // Log into Azure
    string token = await getArmTokenAsync(TenantId, AppId, AppSecret);
    log.Info("Azure token obtained: " + token);

    // Get NSG rules with all information from the database
    var nsgRules = new List<Dictionary<string, string>>();
    nsgRules = GetTableFromDb("NSGrules", log);
    log.Info(nsgRules.Count.ToString() + " NSG rules extracted from the database");

    // Elaborate a list of NSGs to create
    var nsgList = new List<string>();
    nsgList = GetNSGlist (nsgRules);
    log.Info(nsgList.Count.ToString() + " unique NSGs identified");

    // Get the list of VMs with their Resource Groups from the database
    var vmList = new List<Dictionary<string, string>>();
    vmList = GetTableFromDb("VMs", log);
    log.Info(vmList.Count.ToString() + " VMs in the database");

    // Loop through the NSGs
    foreach(string nsg in nsgList) {

        // Find out in which resource group the NSG will be created
        string resourceGroup = GetVmResourceGroup(nsg, vmList);
        log.Info("Resource group for NSG " + nsg + " is " + resourceGroup);

        // Find out the location of the resource group, we will be the same one for the NSG
        JObject resourceGroupJson = await GetResourceGroupJSON(token, SubscriptionId, resourceGroup, log);
        string RGlocation = resourceGroupJson.SelectToken("location").ToString();
        log.Info ("That resource group seems to be in " + RGlocation);

        // Create a filtered list with the rules for this NSG
        var thisNsgInputRules = new List<Dictionary<string, string>>();
        thisNsgInputRules = nsgRules.Where(r => r["nsgName"] == nsg).ToList();
        log.Info ("This NSG seems to have " + thisNsgInputRules.Count.ToString() + " rules");

        // Create the NSG in Azure
        JObject createNsgJson = await CreateNSG (token, SubscriptionId, thisNsgInputRules, nsg+nsgSuffix, resourceGroup, RGlocation, log);
    }

    return null;
}
