#!/bin/bash

echo ""

echo "---------------------------------------"
echo "Test CenterCLR.RelaxVersioner..."

rm -rf CenterCLR.RelaxVersioner
git clone https://github.com/kekyo/CenterCLR.RelaxVersioner.git
cd CenterCLR.RelaxVersioner
echo "Dump by git log..."
git log --abbrev-commit --abbrev=40 --decorate --all > gitlog1.txt
dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll --fix gitlog1.txt > gitlog2.txt
echo "Dump by gitlogtest..."
dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."
echo ""

echo "---------------------------------------"
echo "Test IL2C..."

rm -rf IL2C
git clone https://github.com/kekyo/IL2C.git
cd IL2C
echo "Dump by git log..."
git log --abbrev-commit --abbrev=40 --decorate --all > gitlog1.txt
dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll --fix gitlog1.txt > gitlog2.txt
echo "Dump by gitlogtest..."
dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."
echo ""

echo "---------------------------------------"
echo "Test git..."

rm -rf git
git clone https://github.com/git/git.git
cd git
echo "Dump by git log..."
git log --abbrev-commit --abbrev=40 --decorate --all > gitlog1.txt
dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll --fix gitlog1.txt > gitlog2.txt
echo "Dump by gitlogtest..."
dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll --log . > output.txt
diff -u gitlog2.txt output.txt
cd ..

echo "Done."
echo ""
