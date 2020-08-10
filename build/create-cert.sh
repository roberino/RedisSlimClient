 #!/bin/bash

openssl genrsa -out redis-key.pem 4096
openssl req -new -x509 -key redis-key.pem -out redis-cert.pem

cat redis-key.pem redis-cert.pem > stunnel/rediscert.pem

# docker run -d --name redis-ssl redis

# docker run -d  --link redis-ssl:redis-ssl -v `pwd` redis-cert.pem:redis-private.pem:ro -p 6380:6380 runnable/redis-stunnel