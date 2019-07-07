FROM alpine

ARG redis_host

RUN apk add stunnel

VOLUME /stunnel
COPY ./stunnel.conf /stunnel/
COPY ./rediscert.pem /stunnel/private.pem
CMD chmod 600 /stunnel/private.pem

EXPOSE 6380

CMD echo "connect = $redis_host:6379" >> /stunnel/stunnel.conf
CMD stunnel /stunnel/stunnel.conf