# How to use a test document store as fallback.

This is the situation where we have a *PRIMARY* Document store (production instance) and you want a test Document store that can read data from the primary. This allows for an installation of client code to require data from Test instance and the test instance will automatically redirect all GET requests for blob id not present in test system to the main system. 

## Configuration on the PRIMARY 

In configuration service you need to specify ip of the test machine in the get-only-ip-array, a configuration that contains all the address of the test instances that can call the PRIMARY system for Get Request. This will permit call from non local ip for get request only from that Ip.

## Configuration on test 

You need to insert the base address in secondary-document-store-address settings, something like "http://primaryip:port"