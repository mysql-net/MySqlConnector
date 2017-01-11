#!/bin/bash
cd $(dirname $0)

sudo mkdir -p mysqld
sudo chmod 777 mysqld

docker pull mysql:5.7
docker run -d \
	-v $(pwd)/mysqld:/var/run/mysqld \
	-v $(pwd)/server:/etc/mysql/conf.d \
	-p 3307:3306 \
	--name mysql \
	-e MYSQL_ROOT_PASSWORD='test' \
	mysql:5.7

for i in `seq 1 30`; do
	# wait for mysql to come up
	sleep 1
	# try running the init script
	docker exec -it mysql bash -c 'mysql -uroot -ptest < /etc/mysql/conf.d/init.sql' >/dev/null 2>&1
	# exit if successful
	if [ $? -eq 0 ]; then
		docker exec -it mysql mysql -ussltest -ptest \
			--ssl-mode=REQUIRED \
			--ssl-ca=/etc/mysql/conf.d/certs/ssl-ca-cert.pem \
			--ssl-cert=/etc/mysql/conf.d/certs/ssl-client-cert.pem \
			--ssl-key=/etc/mysql/conf.d/certs/ssl-client-key.pem \
			-e "SELECT 1"
		if [ $? -ne 0 ]; then
			>&2 echo "Problem with SSL"
			exit 1
		fi
		echo "Ran Init Script"
		exit 0
	fi
done

# init script did not run
>&2 echo "Unable to Run Init Script"
exit 1
