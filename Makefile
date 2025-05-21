gen-env:
	echo "USER=$(shell whoami)" > .env
	echo "USER_ID=$(shell id -u)" >> .env
	echo "GROUP_ID=$(shell id -g)" >> .env
	echo "XAUTHORITY=/home/$(shell whoami)/.Xauthority" >> .env

build:
	docker compose build

up:
	xhost +
	docker compose up -d

down:
	docker compose down
