#!/bin/bash

echo "---------------------------------------"
echo "Test CenterCLR.RelaxVersioner..."

rm -rf CenterCLR.RelaxVersioner
git clone https://github.com/kekyo/CenterCLR.RelaxVersioner.git
cd CenterCLR.RelaxVersioner
git log --abbrev-commit --abbrev=40 --decorate --all > gitlog1.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --fix gitlog1.txt > gitlog2.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."
echo ""

echo "---------------------------------------"
echo "Test IL2C..."

rm -rf IL2C
git clone https://github.com/kekyo/IL2C.git
cd IL2C
git log --abbrev-commit --abbrev=40 --decorate --all > gitlog1.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --fix gitlog1.txt > gitlog2.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."

echo "---------------------------------------"
echo "Test git..."

rm -rf git
git clone https://github.com/git/git.git
cd git
git log --abbrev-commit --abbrev=40 --decorate --all > gitlog1.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --fix gitlog1.txt > gitlog2.txt
mono ../gitlogtest/bin/Debug/net48/gitlogtest.exe --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."
