# Dynamic NSGs

## What is this?

This project builds on top of Azure APIs in order to create a layer of metadata that can be used to dynamically build NSGs.

This metadata essentially consists of three parts:

- Groups: VMs are assigned to groups depending on rules that match on certain attributes of the VM
- Rules: matching on the VM's name, operative system or ARM tags, rules control which VM belongs to which group
- Policy: permit/deny statements that define which groups can speak to each other.

## Example

For example, you could have the following setup:

- Groups
  * Linux: all VMs whose OS is Linux
  * Windows: all VMs whose OS is Windows
  * Web: all VMs whose name contains the string "web"
  * Database: all VMs whose name contains the string "db"
  * LowSec: all VMs that are deemed insecure, with an ARM tag security:low 
- Policy (group names are delimited by {braces})
  * 10 permit tcp any {linux} port 22
  * 20 permit tcp any {windows} port 3389
  * 30 deny ip any {lowsec} 
  * 40 permit tcp any {web} port 80
  * 50 permit tcp {web} {database} port 3306

Note that the policy entries are denoted with a sequence number, since the order is critical. For example, here the deny statement for low-security systems is placed after SSH/RDP, but before the web/database statements, so that low-security systems can still be troubleshooted over SSH/RDP.

## Benefits of dynamic NSGs:

* Simpler ruleset: the groups behave as what in traditional firewall administration is usually known as object-groups, so that rulesets are more compact
* Dynamic ruleset: whether a VM belongs to a group or not is dynamically calculated, there is no manual assignment, which eliminates administrative overhead and the possibility of errors
* Automated ruleset: this technology opens the door to automation scenarios where for example security appliances modify ARM tags of a certain VM, with the effect that its belonging to a security group is modified. For example, a vulnerability scanner detects a vulnerability on a certain VM, and sets an ARM tag of vulnerable:yes, effectively moving the VM to a different group.

## This is a hack

Note that ideally this functionality should be natively implemented in Azure, but if you need it right now you could use code similar to the one of this repo. If you wish this to be natively part of Azure, let your Microsoft point of contact know!