### Permissions of urls

Since hosting with SelfHost try to open an http port, it should normally fail unless the program is run as adminsitrator or netsh is used to give user permission to open http protocol on given port.

	netsh http add urlacl url=http://+:5123/ user=username

If your integration tests starts to fail with a strange 503 error, it could be probably a problem of netsh. For some strange reason, sometimes if you have done the above reservation it will fail during unit tests. **If you start experiencing unit test failing with 503, remove the reservation**.

	netsh http delete urlacl url=http://+:5123/

