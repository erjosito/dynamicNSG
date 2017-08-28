using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq;
using System.Data.Entity;
using dynamicNSG.Models;

namespace dynamicNSG.Helper
{
    public static class NsgRuleset
    {

        private static ApplicationDbContext db = new ApplicationDbContext();
        private static Dictionary<string, List<string>> groupVms = new Dictionary<string, List<string>>();
        private static Dictionary<string, List<string>> groupIps = new Dictionary<string, List<string>>();


        // This function builds the dictionary groupVms
        private static Dictionary<string, List<string>> buildGroupVmsDict()
        {
            // Empty variable that will be returned
            Dictionary<string, List<string>> newDict = new Dictionary<string, List<string>>();
            // Build a list with all VMs
            var vmList = new List<Models.VM>();
            vmList = db.VMs.ToList();
            // Build a list with all Groups
            var groupList = new List<Models.Group>();
            groupList = db.Groups.ToList();
            // For each group, check of each VM belongs to it
            foreach (var group in groupList)
            {
                List<string> myVmList = new List<string>();
                string myGroupId = group.GroupId;
                foreach (var myVm in vmList)
                {
                    if (vmInGroup(myVm, myGroupId))
                    {
                        myVmList.Add(myVm.Name);
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
            var vmList = new List<Models.VM>();
            vmList = db.VMs.ToList();
            // Build a list with all Groups
            var groupList = new List<Models.Group>();
            groupList = db.Groups.ToList();
            // For each group check all VMs, and for each VM get all IP addresses
            foreach (var group in groupList)
            {
                List<string> thisGroupIpList = new List<string>();
                string myGroupId = group.GroupId;
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
            var nicList = new List<Models.NIC>();
            nicList = db.NICs.Where(n => n.VmId == vmId).ToList();
            // Build a list with all IPs. We do this single call to the database at the beginning
            var ipList = new List<Models.IP>();
            ipList = db.IPs.ToList();

            foreach (var nic in nicList)
            {
                List<Models.IP> thisNicIpList = ipList.Where(i => i.NicId == nic.NicId).ToList();
                List<string> thisNicIpListString = thisNicIpList.ConvertAll(i => i.Id).ToList();
                newIpList.AddRange(thisNicIpListString);
            }

            return newIpList;
        }

        // Verify whether a specific group ID exists in a list of groups
        private static bool groupExists(string myGroupId, List<Models.Group> groupList)
        {
            List<Models.Group> myList = groupList.Where(g => g.GroupId == myGroupId).ToList();
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
        private static bool vmInGroup (Models.VM vm, string groupId)
        {
            var ruleList = new List<Models.Rules>();
            ruleList = db.Rules.Where(r => r.GroupId == groupId).ToList();
            foreach (var rule in ruleList)
            {
                string thisOperand1 = "";
                string thisOperator = rule.Operator;
                string thisOperand2 = rule.Operand2;
                if (rule.Operand1 == "name")
                {
                    thisOperand1 = vm.Name;
                }
                else if (rule.Operand1 == "os")
                {
                    thisOperand1 = vm.OS;
                }
                else if (rule.Operand1.StartsWith("tag"))
                {
                    string tagName = rule.Operand1.Split(':')[1];
                    string tagValue = GetTagValue(vm.Name, tagName);
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
            var TagList = new List<Models.Tag>();
            TagList = db.Tags.Where(t => (t.VmId == vmName) && (t.TagName == tagName)).ToList();
            if (TagList.Count > 0)
            {
                return TagList[0].TagValue;
            }
            else
            {
                return "";
            }

        }

        // Main function, build the policy set
        public static List<Models.NSGrule> buildNsgRuleset()
        {
            // This is the (initially empty) list that the function will eventually return
            var nsgRules = new List<Models.NSGrule>();
            // Build a list with all VMs
            var vmList = new List<Models.VM>();
            vmList = db.VMs.ToList();
            // Build a list with all policy rules
            var policyList = new List<Models.Policy>();
            policyList = db.Policies.OrderBy(p => p.Order).ToList();
            // Build a list with all group rules
            var ruleList = new List<Models.Rules>();
            ruleList = db.Rules.ToList();
            // Build a list with all groups
            var groupList = new List<Models.Group>();
            groupList = db.Groups.ToList();

            // Build two dictionaries for quick conversion from groups to VMs and from groups to IPs
            groupVms = buildGroupVmsDict();
            groupIps = buildGroupIpsDict(groupVms);

            // NSG Rule Id variable
            var myNsgId = new int();
            myNsgId = 1;

            // Build an NSG for each VM
            foreach (var vm in vmList)
            {
                // Inbound NSG entries: Search the policy for matching destinations 
                int seq = 100;
                foreach (var policyRule in policyList)
                {
                    // First we need to check whether the destination is a group, then whether that group contains our VM
                    // The check to see if the VM is contained in the group can be made either looking at the rules, or looking at the previously built dictionary
                    //if (vmInGroup(vm, policyRule.Dst))
                    if ((groupExists(policyRule.Dst, groupList)) && (groupVms[policyRule.Dst].Contains(vm.Name)))
                    {
                        // Expand the source to IPs only if it is a group, otherwise leave it as it is
                        //    (it could be a CIDR address or an NSG label
                        List<string> newEntryIpList = new List<string>();
                        if (groupExists(policyRule.Src, groupList))
                        {
                            newEntryIpList = groupIps[policyRule.Src];

                        }
                        else
                        {
                            newEntryIpList.Add(policyRule.Src);
                        }
                        foreach (var srcIp in newEntryIpList)
                        {
                            var newEntry = new Models.NSGrule();
                            newEntry.Id = myNsgId;
                            myNsgId += 1;
                            newEntry.nsgName = vm.Name;
                            newEntry.direction = "inbound";
                            newEntry.order = seq;
                            seq += 10;
                            newEntry.action = policyRule.Action;
                            newEntry.srcIp = srcIp;
                            newEntry.dstProt = policyRule.Prot;
                            newEntry.dstPort = policyRule.Range;
                            nsgRules.Add(newEntry);
                        }
                    }
                }
                // Outbound NSG entries: Search the policy for matching destinations 
                seq = 100;
                foreach (var policyRule in policyList)
                {
                    // First we need to check whether the policy source is a group, then whether that group contains our VM
                    // The check to see if the VM is contained in the group can be made either looking at the rules, or looking at the previously built dictionary
                    //if (vmInGroup(vm, policyRule.Src))
                    if ((groupExists(policyRule.Src, groupList)) && (groupVms[policyRule.Src].Contains(vm.Name)))
                    {
                        // Expand the destination to IPs only if it is a group, otherwise leave it as it is
                        //    (it could be a CIDR address or an NSG label
                        List<string> newEntryIpList = new List<string>();
                        if (groupExists(policyRule.Dst, groupList))
                        {
                            newEntryIpList = groupIps[policyRule.Dst];

                        }
                        else
                        {
                            newEntryIpList.Add(policyRule.Dst);
                        }
                        foreach (var dstIp in newEntryIpList)
                        {
                            var newEntry = new Models.NSGrule();
                            newEntry.Id = myNsgId;
                            myNsgId += 1;
                            newEntry.nsgName = vm.Name;
                            newEntry.direction = "outbound";
                            newEntry.order = seq;
                            seq += 10;
                            newEntry.action = policyRule.Action;
                            newEntry.dstIp = dstIp;
                            newEntry.dstProt = policyRule.Prot;
                            newEntry.dstPort = policyRule.Range;
                            nsgRules.Add(newEntry);
                        }
                    }
                }
            }
            return nsgRules;
        }
    }
}