#!/bin/bash
cd $(dirname $0)

display_usage() {
    echo -e "\nUsage:\n$0 [image] [name] [port] [omit_features]\n"
}

# check whether user had supplied -h or --help . If yes display usage
if [[ ( $# == "--help") ||  $# == "-h" ]]
then
    display_usage
    exit 0
fi

# check number of arguments
if [ $# -ne 3 ]
then
    display_usage
    exit 1
fi

IMAGE=$1
PORT=$2
OMIT_FEATURES=$3
MYSQL_EXTRA=
MYSQL=mysql

if [[ "$IMAGE" == mariadb* ]]; then
  MYSQL_EXTRA='--in-predicate-conversion-threshold=100000 --plugin-maturity=beta'
fi
if [ "$IMAGE" == "mariadb:11.4" ] || [ "$IMAGE" == "mariadb:11.6" ]; then
  MYSQL='mariadb'
fi

sudo mkdir -p run/mysql
sudo chmod 777 run/mysql

docker run -d \
	-v $(pwd)/run/mysql:/var/run/mysqld:rw \
	-v $(pwd)/server:/etc/mysql/conf.d:ro \
	-p $PORT:3306 \
	--name mysql \
	-e MYSQL_ROOT_PASSWORD='test' \
	--tmpfs /var/lib/mysql \
	$IMAGE \
  --disable-log-bin \
  --local-infile=1 \
  --secure-file-priv=/var/tmp \
  --max-connections=250 \
  $MYSQL_EXTRA

for i in `seq 1 120`; do
	# wait for mysql to come up
	sleep 1
	echo "Testing if container is responding"
	docker exec mysql $MYSQL -uroot -ptest -e "SELECT 1" >/dev/null 2>&1
	if [ $? -ne 0 ]; then continue; fi

	# try running the init script
	echo "Creating mysqltest user"
	docker exec mysql bash -c "$MYSQL -uroot -ptest < /etc/mysql/conf.d/init.sql"
	if [ $? -ne 0 ]; then continue; fi

	if [[ $OMIT_FEATURES != *"Sha256Password"* ]]; then
		echo "Creating sha256_password user"
	 	docker exec mysql bash -c "$MYSQL -uroot -ptest < /etc/mysql/conf.d/init_sha256.sql"
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	if [[ $OMIT_FEATURES != *"CachingSha2Password"* ]]; then
		echo "Creating caching_sha2_password user"
		docker exec mysql bash -c "$MYSQL -uroot -ptest < /etc/mysql/conf.d/init_caching_sha2.sql"
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	if [[ $OMIT_FEATURES != *"Ed25519"* ]]; then
		echo "Installing auth_ed25519 component"
		docker exec mysql bash -c "$MYSQL -uroot -ptest < /etc/mysql/conf.d/init_ed25519.sql"
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	if [[ $OMIT_FEATURES != *"QueryAttributes"* ]]; then
		echo "Installing query_attributes component"
		docker exec mysql $MYSQL -uroot -ptest -e "INSTALL COMPONENT 'file://component_query_attributes';"
		if [ $? -ne 0 ]; then exit $?; fi
	fi

	# exit if successful
	docker exec mysql $MYSQL -ussltest -ptest \
		--ssl-mode=REQUIRED \
		--ssl-ca=/etc/mysql/conf.d/certs/ssl-ca-cert.pem \
		--ssl-cert=/etc/mysql/conf.d/certs/ssl-client-cert.pem \
		--ssl-key=/etc/mysql/conf.d/certs/ssl-client-key.pem \
		-e "SELECT 1"
	if [ $? -ne 0 ]; then
		# mariadb uses --ssl=TRUE instead of --ssl-mode=REQUIRED
		docker exec mysql $MYSQL -ussltest -ptest \
			--ssl=TRUE \
			--ssl-ca=/etc/mysql/conf.d/certs/ssl-ca-cert.pem \
			--ssl-cert=/etc/mysql/conf.d/certs/ssl-client-cert.pem \
			--ssl-key=/etc/mysql/conf.d/certs/ssl-client-key.pem \
			-e "SELECT 1"
		if [ $? -ne 0 ]; then
			>&2 echo "Problem with SSL"
			exit 1
		fi
	fi
	echo "Ran Init Script"
	exit 0
done

# init script did not run
>&2 echo "Unable to Run Init Script"
exit 1
