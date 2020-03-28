#!/bin/bash

clang++ -o stub.dylib -shared -O3 -IPluginAPI stub.cpp
cp stub.dylib ../Assets/Plugins/

nim cpp -f -o:nimgame.dylib --app:lib --verbosity:0 --passC:-IPluginAPI main.nim
cp nimgame.dylib ../Assets/Plugins/
