#!/bin/bash

rm -rf git
git clone https://github.com/git/git.git

echo "Step 1: checkout with git command."

cd git
git checkout 5597cfdf47db94825213fefe78c4485e6a5702d8
git checkout-index -a -f --prefix=../export.orig/
cd ..

echo "Step 2: extract with gitexporttest."

rm -rf export
mkdir export
cd export
dotnet ../gitexporttest/bin/Release/net6.0/gitexporttest.dll ../git 5597cfdf47db94825213fefe78c4485e6a5702d8
cd ..

echo "Step 3: Makes differ."

diff -r -u export.orig/ export/

echo "Done."
echo ""
