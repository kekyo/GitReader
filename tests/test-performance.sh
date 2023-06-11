#!/bin/bash

if [ ! -f git ]; then
    git clone https://github.com/git/git.git
fi

cd git

echo "Start..."

start_time=`date +%s`

dotnet ../gitlogtest/bin/Release/net6.0/gitlogtest.dll . > output.txt

end_time=`date +%s`
 
echo "Finished."

run_time=$((end_time - start_time))
echo "Time:" $run_time

cd ..

echo "Done."
echo ""
