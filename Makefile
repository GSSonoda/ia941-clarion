USER_ID := $(shell id -u)
GROUP_ID := $(shell id -g)
USER_NAME := $(shell whoami)

run:
	docker compose run --rm monodevelop -c "\
		echo Starting monodevelop from docker; \
		if ! getent group $(GROUP_ID) > /dev/null; then groupadd -g $(GROUP_ID) $(USER_NAME); fi; \
		useradd -r -d /home/$(USER_NAME) -s /bin/bash -g $$(id -gn) -G sudo -u $(USER_ID) $(USER_NAME); \
		su -c '/launch.sh $$DISPLAY' $(USER_NAME)"

