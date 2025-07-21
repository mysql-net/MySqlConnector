INSTALL SONAME 'auth_parsec';
CREATE USER 'parsec-user'@'%' IDENTIFIED via parsec using PASSWORD('P@rs3c-Pa55');
GRANT ALL PRIVILEGES ON *.* TO 'parsec-user'@'%';
