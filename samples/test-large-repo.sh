#!/bin/bash

echo "---------------------------------------"
echo "Test CenterCLR.RelaxVersioner..."

rm -rf CenterCLR.RelaxVersioner
git clone https://github.com/kekyo/CenterCLR.RelaxVersioner.git
cd CenterCLR.RelaxVersioner
git log --decorate --all > gitlog1.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --fix gitlog1.txt > gitlog2.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."
