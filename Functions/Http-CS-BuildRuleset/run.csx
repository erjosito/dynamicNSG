using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Linq;
using System.IO;


private static Dictionary<string, List<string>> groupVms = new Dictionary<string, List<string>>();
private static Dictionary<string, List<string>> groupIps = new Dictionary<string, List<string>>();

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


// Get the contents of a table in the form of a dictionary
private static List<Dictionary<string, string>> GetTableFromDb(string tableName) {
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
    catch
    {
        return null;
    }    

}


// This function builds the dictionary groupVms
private static Dictionary<string, List<string>> buildGroupVmsDict()
{
    // Empty variable that will be returned
    Dictionary<string, List<string>> newDict = new Dictionary<string, List<string>>();
    // Build a list with all VMs
    var vmList = new List<Dictionary<string,string>>();
    vmList = GetTableFromDb ("VMs");
    // Build a list with all Groups
    var groupList = new List<Dictionary<string,string>>();
    groupList = GetTableFromDb("Groups");
    // For each group, check of each VM belongs to it
    foreach (var group in groupList)
    {
        List<string> myVmList = new List<string>();
        string myGroupId = group["GroupId"];
        foreach (var myVm in vmList)
        {
            if (vmInGroup(myVm, myGroupId))
            {
                myVmList.Add(myVm["Name"]);
            }
        }
        newDict.Add(myGroupId, myVmList);
    }
    return newDict;
}

// This function builds the dictionary groupIps
private static Dictionary<string, List<string>> buildGroupIpsDict(Dictionary<string, List<string>> groupVms)
{
    // Empty variable that will be returned
    Dictionary<string, List<string>> newDict = new Dictionary<string, List<string>>();
    // Build a list with all VMs
    var vmList = new List<Dictionary<string, string>>();
    vmList = GetTableFromDb("VMs");
    // Build a list with all Groups
    var groupList = new List<Dictionary<string, string>>();
    groupList = GetTableFromDb("Groups");
    // For each group check all VMs, and for each VM get all IP addresses
    foreach (var group in groupList)
    {
        List<string> thisGroupIpList = new List<string>();
        string myGroupId = group["GroupId"];
        foreach (string vmName in groupVms[myGroupId])
        {
            var thisVmIpList = getAllIps(vmName);
            thisGroupIpList.AddRange(thisVmIpList);
        }
        newDict.Add(myGroupId, thisGroupIpList);
    }
    return newDict;
}

// This function returns a list of all IP address for a certain VM
private static List<string> getAllIps(string vmId)
{
    // Empty variable that will be returned
    List<string> newIpList = new List<string>();
    // Build a list with all NICs for our VM
    var unfilteredNicList = new List<Dictionary<string, string>>();
    unfilteredNicList = GetTableFromDb("NICs");
    var nicList = unfilteredNicList.Where(n => n["VmId"] == vmId).ToList();
    // Build a list with all IPs. We do this single call to the database at the beginning
    var ipList = new List<Dictionary<string, string>>();
    ipList = GetTableFromDb("IPs");

    foreach (Dictionary<string, string> nic in nicList)
    {
        List<Dictionary<string, string>> thisNicIpList = ipList.Where(i => i["NicId"] == nic["NicId"]).ToList();
        List<string> thisNicIpListString = thisNicIpList.ConvertAll(i => i["Id"]).ToList();
        newIpList.AddRange(thisNicIpListString);
    }

    return newIpList;
}

// Verify whether a specific group ID exists in a list of groups
private static bool groupExists(string myGroupId, List<Dictionary<string, string>> groupList)
{
    List<Dictionary<string, string>> myList = groupList.Where(g => g["GroupId"] == myGroupId).ToList();
    if (myList.Count > 0)
    {
        return true;
    } else
    {
        return false;
    }
}

// This function checks wheter two operands match, according to a certain operator
private static bool operandsMatch (string Operand1, string Operator, string Operand2)
{
    // ToLower is used to normalize case
    if (Operator == "equals")
    {
        if (Operand1.ToLower() == Operand2.ToLower()) { return true; }
    }
    else if (Operator == "contains")
    {
        if (Operand1.ToLower().Contains(Operand2.ToLower())) { return true; }
    }
    else if (Operator == "beginswith")
    {
        if (Operand1.ToLower().StartsWith(Operand2.ToLower())) { return true; }
    }
    else if (Operator == "endswith")
    {
        if (Operand1.ToLower().EndsWith(Operand2.ToLower())) { return true; }
    }
    return false;
}


// This function checks whether a certain VM belongs to a group verifying the belonging rules
private static bool vmInGroup (Dictionary<string, string> vm, string groupId)
{
    var unfilteredRuleList = new List<Dictionary<string, string>>();
    unfilteredRuleList = GetTableFromDb("Rules");
    var ruleList = unfilteredRuleList.Where(r => r["GroupId"] == groupId).ToList();
    foreach (var rule in ruleList)
    {
        string thisOperand1 = "";
        string thisOperator = rule["Operator"];
        string thisOperand2 = rule["Operand2"];
        if (rule["Operand1"] == "name")
        {
            thisOperand1 = vm["Name"];
        }
        else if (rule["Operand1"] == "os")
        {
            thisOperand1 = vm["OS"];
        }
        else if (rule["Operand1"].StartsWith("tag"))
        {
            string tagName = rule["Operand1"].Split(':')[1];
            string tagValue = GetTagValue(vm["Name"], tagName);
            thisOperand1 = tagValue;
        }
        if (operandsMatch(thisOperand1, thisOperator, thisOperand2))
        {
            return true;
        }
    }
    return false;
}

private static string GetTagValue(string vmName, string tagName)
{
    var unfilteredTagList = new List<Dictionary<string, string>>();
    unfilteredTagList = GetTableFromDb("Tags");
    var TagList = unfilteredTagList.Where(t => (t["VmId"] == vmName) && (t["TagName"] == tagName)).ToList();
    if (TagList.Count > 0)
    {
        return TagList[0]["TagValue"];
    }
    else
    {
        return "";
    }
}


// Dumps a dictionary list to console
private static bool DumpToConsole(List<Dictionary<string, string>> myList, TraceWriter log) {
    foreach (Dictionary<string, string> thisDict in myList) {
        foreach(string key in thisDict.Keys) {
            log.Info(key.ToString() + "-> " + thisDict[key]);
        }
    }
    return true;
} 

// Dumps a dictionary list to the database
private static bool DumpToDb(List<Dictionary<string, string>> myList, TraceWriter log) {
    var cnnString  = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
    var connection = new SqlConnection(cnnString);
    connection.Open();
    foreach (Dictionary<string, string> thisDict in myList) {
        int thisId = 0; int thisOrder = 0;
        string thisNsgName = ""; string thisDirection = ""; string thisAction = "";
        string thisSrcIp = ""; string thisSrcProt = ""; string thisSrcPort = "";
        string thisDstIp = ""; string thisDstProt = ""; string thisDstPort = ""; 
        if (thisDict.Keys.Contains("Id")) { thisId = Convert.ToInt32(thisDict["Id"]); }
        if (thisDict.Keys.Contains("order")) { thisOrder = Convert.ToInt32(thisDict["order"]); }
        if (thisDict.Keys.Contains("nsgName")) { thisNsgName = thisDict["nsgName"]; }
        if (thisDict.Keys.Contains("direction")) { thisDirection = thisDict["direction"]; }
        if (thisDict.Keys.Contains("action")) { thisAction = thisDict["action"]; }
        if (thisDict.Keys.Contains("srcIp")) { thisSrcIp = thisDict["srcIp"]; }
        if (thisDict.Keys.Contains("srcProt")) { thisSrcProt = thisDict["srcProt"]; }
        if (thisDict.Keys.Contains("srcPort")) { thisSrcPort = thisDict["srcPort"]; }
        if (thisDict.Keys.Contains("dstIp")) { thisDstIp = thisDict["dstIp"]; }
        if (thisDict.Keys.Contains("dstProt")) { thisDstProt = thisDict["dstProt"]; }
        if (thisDict.Keys.Contains("dstPort")) { thisDstPort = thisDict["dstPort"]; }
        string SqlQuery = "INSERT INTO NSGrules (nsgName, direction, [order], action, srcIp, srcProt, srcPort, dstIp, dstProt, dstPort) " +
            "VALUES ('" + thisNsgName + "', '" + thisDirection + "', " + thisOrder + ", '" + thisAction + "', " +
            "'" + thisSrcIp + "', '" + thisSrcProt + "', '" + thisSrcPort + "', " +
            "'" + thisDstIp + "', '" + thisDstProt + "', '" + thisDstPort + "')";
        //log.Info("Running SQL Query: " + SqlQuery);
        SqlCommand cmd = new SqlCommand(SqlQuery, connection);
        cmd.ExecuteNonQuery();
    }
    connection.Close();
    return true;
} 

// Main function
public static Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");


    // This is the (initially empty) list that the function will eventually return
    var nsgRules = new List<Dictionary<string, string>>();
    // Build a list with all VMs
    var vmList = new List<Dictionary<string, string>>();
    vmList = GetTableFromDb("VMs");
    // Build a list with all policy rules
    var policyList = new List<Dictionary<string, string>>();
    policyList = GetTableFromDb("Policies").OrderBy(p => p["Order"]).ToList();
    // Build a list with all group rules
    var ruleList = new List<Dictionary<string, string>>();
    ruleList = GetTableFromDb("Rules");
    // Build a list with all groups
    var groupList = new List<Dictionary<string, string>>();
    groupList = GetTableFromDb("Groups");

    // Build two dictionaries for quick conversion from groups to VMs and from groups to IPs
    groupVms = buildGroupVmsDict();
    groupIps = buildGroupIpsDict(groupVms);

    // NSG Rule Id variable
    var myNsgId = new int();
    var seq = new int();
    myNsgId = 1;

    // Build an NSG for each VM
    foreach (var vm in vmList)
    {
        // Inbound NSG entries: Search the policy for matching destinations 
        seq = 100;
        foreach (var policyRule in policyList)
        {
            // First we need to check whether the destination is a group, then whether that group contains our VM
            // The check to see if the VM is contained in the group can be made either looking at the rules, or looking at the previously built dictionary
            //if (vmInGroup(vm, policyRule.Dst))
            if ((groupExists(policyRule["Dst"], groupList)) && (groupVms[policyRule["Dst"]].Contains(vm["Name"])))
            {
                // Expand the source to IPs only if it is a group, otherwise leave it as it is
                //    (it could be a CIDR address or an NSG label
                List<string> newEntryIpList = new List<string>();
                if (groupExists(policyRule["Src"], groupList))
                {
                    newEntryIpList = groupIps[policyRule["Src"]];

                }
                else
                {
                    newEntryIpList.Add(policyRule["Src"]);
                }
                foreach (var srcIp in newEntryIpList)
                {
                    Dictionary<string, string> newEntry = new Dictionary<string, string>();
                    newEntry.Add ("Id", myNsgId.ToString());
                    myNsgId += 1;
                    newEntry.Add("nsgName", vm["Name"]);
                    newEntry.Add("direction", "inbound");
                    newEntry.Add("order", seq.ToString());
                    seq += 10;
                    newEntry.Add("action", policyRule["Action"]);
                    newEntry.Add("srcIp", srcIp);
                    newEntry.Add("dstProt", policyRule["Prot"]);
                    newEntry.Add("dstPort", policyRule["Range"]);
                    nsgRules.Add(newEntry);
                }
            }
        }
 
         // Outbound NSG entries: Search the policy for matching destinations 
        seq = 100;
        foreach (var policyRule in policyList)
        {
            // First we need to check whether the source is a group, then whether that group contains our VM
            // The check to see if the VM is contained in the group can be made either looking at the rules, or looking at the previously built dictionary
            if ((groupExists(policyRule["Src"], groupList)) && (groupVms[policyRule["Src"]].Contains(vm["Name"])))
            {
                // Expand the destination to IPs only if it is a group, otherwise leave it as it is
                //    (it could be a CIDR address or an NSG label
                List<string> newEntryIpList = new List<string>();
                if (groupExists(policyRule["Dst"], groupList))
                {
                    newEntryIpList = groupIps[policyRule["Dst"]];

                }
                else
                {
                    newEntryIpList.Add(policyRule["Dst"]);
                }
                foreach (var srcIp in newEntryIpList)
                {
                    Dictionary<string, string> newEntry = new Dictionary<string, string>();
                    newEntry.Add ("Id", myNsgId.ToString());
                    myNsgId += 1;
                    newEntry.Add("nsgName", vm["Name"]);
                    newEntry.Add("direction", "outbound");
                    newEntry.Add("order", seq.ToString());
                    seq += 10;
                    newEntry.Add("action", policyRule["Action"]);
                    newEntry.Add("dstIp", srcIp);
                    newEntry.Add("dstProt", policyRule["Prot"]);
                    newEntry.Add("dstPort", policyRule["Range"]);
                    nsgRules.Add(newEntry);
                }
            }
        }
    }

    // Dump dictionary with NSG rules to database
    DeleteTableFromDb ("NSGrules", log);
    //DumpToConsole(nsgRules, log);
    DumpToDb(nsgRules, log);

    return null;
}
