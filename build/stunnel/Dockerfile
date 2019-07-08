FROM alpine

RUN apk add stunnel

VOLUME /stunnel
COPY ./stunnel1.conf /stunnel/stunnel1.conf
COPY ./stunnel2.conf /stunnel/stunnel2.conf
COPY ./rediscert.pem /stunnel/private.pem
RUN chmod 600 /stunnel/private.pem

EXPOSE 6380

CMD stunnel /stunnel/stunnel1.conf