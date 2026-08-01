FROM mysql:9.7
COPY generated/ca-cert.pem /etc/mysql/certs/ca-cert.pem
COPY generated/server-cert.pem /etc/mysql/certs/server-cert.pem
COPY generated/server-key.pem /etc/mysql/certs/server-key.pem
RUN chown mysql:mysql /etc/mysql/certs/ca-cert.pem /etc/mysql/certs/server-cert.pem /etc/mysql/certs/server-key.pem \
 && chmod 600 /etc/mysql/certs/server-key.pem \
 && chmod 644 /etc/mysql/certs/ca-cert.pem /etc/mysql/certs/server-cert.pem
CMD ["mysqld", "--ssl-ca=/etc/mysql/certs/ca-cert.pem", "--ssl-cert=/etc/mysql/certs/server-cert.pem", "--ssl-key=/etc/mysql/certs/server-key.pem", "--require_secure_transport=ON"]
