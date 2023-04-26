#!/bin/bash

rm -rf CenterCLR.RelaxVersioner
git clone https://github.com/kekyo/CenterCLR.RelaxVersioner.git
cd CenterCLR.RelaxVersioner
git log --decorate --all > ../gitlog1.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe -f ../gitlog1.txt > ../gitlog2.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe -w . > ../output.txt

