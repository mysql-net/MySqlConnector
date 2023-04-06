INSTALL SONAME 'auth_ed25519';
CREATE USER 'ed25519user'@'%' IDENTIFIED VIA ed25519 USING PASSWORD('Ed255!9');
GRANT ALL PRIVILEGES ON *.* TO 'ed25519user'@'%';

CREATE USER 'multiAuthUser'@'%' IDENTIFIED VIA ed25519 USING PASSWORD('Ed255!9') OR mysql_native_password as password("secret");
GRANT ALL PRIVILEGES ON *.* TO 'multiAuthUser'@'%';
