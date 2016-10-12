CREATE USER 'mysqltest'@'%' IDENTIFIED BY 'test;key=\"val'; GRANT ALL ON *.* TO mysqltest;
CREATE USER 'no_password'@'localhost';
CREATE USER 'no_password'@'172.17.0.1';
CREATE USER 'ssltest'@'%' IDENTIFIED BY 'test'; GRANT ALL PRIVILEGES ON *.* TO 'ssltest'@'%' REQUIRE SSL;
SET GLOBAL max_allowed_packet=104857600;
CREATE SCHEMA mysqltest;

