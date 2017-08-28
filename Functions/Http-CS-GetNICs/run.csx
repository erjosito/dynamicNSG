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
public static async Task<JObject> getNICs(string token, string subscriptionId, TraceWriter log) {
    string apiVersion = "2016-09-01";
    string url = "https://management.azure.com/subscriptions/" + subscriptionId + "/providers/Microsoft.Network/networkInterfaces?api-version=" + apiVersion;
    log.Info("Sending GET to " + url);
    string contentType = "application/json";
    JObject jsonResponse = await sendHttpRequestAsync("GET", url, contentType: contentType, token: token);
    return jsonResponse;
}

// Given an ARM ID, extract the Resource Group Name
private static string getRgFromId(string Id) {
    string[] words = Id.Split('/');
    return words[4];
}

// Add a new IP address to the Database if it did not exist
private static bool AddIpToDb(string IpAddress, string NicName)
{
    try
    {
        var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        var connection = new SqlConnection(cnnString);
        string SqlQuery = "IF NOT EXISTS " +  
            "(SELECT * FROM IPs WHERE Id = '" + IpAddress +"' AND NicId = '" + NicName + "') " +
            "BEGIN" + 
            "   INSERT INTO IPs (Id, NicId) VALUES ('" + IpAddress + "', '" + NicName + "') " +
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
    JObject JsonNics = await getNICs(token, SubscriptionId, log);
    List<JToken> JsonNicsList = new List<JToken>();
    JsonNicsList = JsonNics.SelectToken("value").ToList();
    int IpAddressCounter = 0;
    foreach (JObject Nic in JsonNicsList)
    {
        string NicName = Nic["name"].ToString();
        JObject NicProperties = Nic["properties"].Value<JObject>();
        List<JToken> NicIpConfigs = new List<JToken>();
        NicIpConfigs = NicProperties.SelectToken("ipConfigurations").ToList();
        foreach (JObject IpConfig in NicIpConfigs) {
            JObject IpConfigProperties = IpConfig["properties"].Value<JObject>();
            string IpAddress = IpConfigProperties["privateIPAddress"].ToString();
            log.Info("Adding to the DB: " + NicName + ", " + IpAddress);
            AddIpToDb (IpAddress, NicName);
            IpAddressCounter += 1;
        }
    }

    return req.CreateResponse(HttpStatusCode.OK, "Function run correctly: " + IpAddressCounter.ToString() + " IP addresses added to the database");
}
