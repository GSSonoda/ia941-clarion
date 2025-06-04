#!/bin/bash

docker run --rm -it --name coppelia-sim \
-e DISPLAY=$DISPLAY \
--net=host \
--device /dev/snd \
--privileged \
brgsil/ws3d-coppelia
