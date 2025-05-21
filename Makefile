gen-env:
	echo "USER=$(shell whoami)" > .env
	echo "USER_ID=$(shell id -u)" >> .env
	echo "GROUP_ID=$(shell id -g)" >> .env
	echo "DISPLAY=$(DISPLAY)" >> .env

build:
	docker compose build

up:
	docker compose up

down:
	docker compose down
