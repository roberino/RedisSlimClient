FROM redis:alpine

COPY ./master.conf /usr/local/etc/redis/master.conf
COPY ./slave.conf /usr/local/etc/redis/slave.conf

EXPOSE 6379

CMD [ "redis-server", "/usr/local/etc/redis/master.conf" ]