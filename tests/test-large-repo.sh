#!/bin/bash

git clone https://github.com/kekyo/CenterCLR.RelaxVersioner.git
cd CenterCLR.RelaxVersioner
git log --decorate --all | mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe -f > ../verified.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe -w . > ../output.txt

