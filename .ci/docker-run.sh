#!/bin/bash
cd $(dirname $0)

display_usage() {
    echo -e "\nUsage:\n$0 [image] [name] [port]\n"
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
NAME=$2
PORT=$3

sudo mkdir -p run/$NAME
sudo chmod 777 run/$NAME

docker run -d \
	-v $(pwd)/run/$NAME:/var/run/mysqld:rw \
	-v $(pwd)/server:/etc/mysql/conf.d:ro \
	-p $PORT:3306 \
	--name $NAME \
	-e MYSQL_ROOT_PASSWORD='test' \
	$IMAGE

for i in `seq 1 30`; do
	# wait for mysql to come up
	sleep 1
	# try running the init script
	docker exec -it $NAME bash -c 'mysql -uroot -ptest < /etc/mysql/conf.d/init.sql' >/dev/null 2>&1
	# exit if successful
	if [ $? -eq 0 ]; then
		docker exec -it $NAME mysql -ussltest -ptest \
			--ssl-mode=REQUIRED \
			--ssl-ca=/etc/mysql/conf.d/certs/ssl-ca-cert.pem \
			--ssl-cert=/etc/mysql/conf.d/certs/ssl-client-cert.pem \
			--ssl-key=/etc/mysql/conf.d/certs/ssl-client-key.pem \
			-e "SELECT 1"
		if [ $? -ne 0 ]; then
			# mariadb uses --ssl=TRUE instead of --ssl-mode=REQUIRED
			docker exec -it $NAME mysql -ussltest -ptest \
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
	fi
done

# init script did not run
>&2 echo "Unable to Run Init Script"
exit 1
