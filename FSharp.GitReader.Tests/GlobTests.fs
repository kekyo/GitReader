////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Tests

open System.IO
open System.Text
open System.Threading.Tasks
open GitReader
open NUnit.Framework

type public GlobTests() =

    [<Test>]
    member _.``isMatch should work with basic patterns``() =
        // Basic pattern matching
        Assert.IsTrue(Glob.isMatch("test.txt", "*.txt"))
        Assert.IsFalse(Glob.isMatch("test.jpg", "*.txt"))
        Assert.IsTrue(Glob.isMatch("file", "*"))
        
    [<Test>]
    member _.``isMatch should work with directory patterns``() =
        // Directory patterns
        Assert.IsTrue(Glob.isMatch("bin/debug", "bin/*"))
        Assert.IsTrue(Glob.isMatch("src/main/file.cs", "src/**"))
        Assert.IsFalse(Glob.isMatch("test.txt", "bin/*"))

    [<Test>]
    member _.``createExcludeFilter should return F# function type``() =
        // Test that it returns F# function type (string -> FilterDecision)
        let f = Glob.createExcludeFilter([| "*.log"; "bin/" |])
        let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)

        // Test function call syntax
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "src/main.cs")  // Should be neutral (not matching exclude patterns)
        Assert.AreEqual(GlobFilterStates.Exclude, filter "debug.log")   // Should be excluded (ignored)
        Assert.AreEqual(GlobFilterStates.Exclude, filter "bin/output")  // Should be excluded (ignored)

    [<Test>]
    member _.``createExcludeFilter should work with multiple patterns``() =
        let f = Glob.createExcludeFilter([| "*.log"; "*.tmp"; "bin/"; "obj/" |])
        let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)

        // Files that should be neutral (not matching exclude patterns)
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "src/Program.cs")
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "README.md")
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "package.json")
        
        // Files that should be excluded (ignored)
        Assert.AreEqual(GlobFilterStates.Exclude, filter "error.log")
        Assert.AreEqual(GlobFilterStates.Exclude, filter "temp.tmp")
        Assert.AreEqual(GlobFilterStates.Exclude, filter "bin/Debug/app.exe")
        Assert.AreEqual(GlobFilterStates.Exclude, filter "obj/Debug/temp.dll")

    [<Test>]
    member _.``getCommonIgnoreFilter should return F# function type``() =
        let f = Glob.getCommonIgnoreFilter()
        let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        // Common included files (not matching ignore patterns)
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "src/Program.cs")
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "README.md")
        Assert.AreEqual(GlobFilterStates.NotExclude, filter "package.json")
        
        // Common ignored files/directories
        Assert.AreEqual(GlobFilterStates.Exclude, filter "bin/Debug/app.exe")
        Assert.AreEqual(GlobFilterStates.Exclude, filter "obj/Debug/temp.dll")
        Assert.AreEqual(GlobFilterStates.Exclude, filter "node_modules/package/index.js")

    [<Test>]
    member _.``combine method should work with static class API``() =
        let filter1 = Glob.createExcludeFilter([| "*.log"; "*.tmp" |])
        let filter2 = Glob.createExcludeFilter([| "*.bak" |])
        
        // Use the new combine method
        let f = Glob.combine([| filter1; filter2 |])
        let combinedFilter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        // Test files that should be excluded by filter1
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "app.log")      // .log file is ignored
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "temp.tmp")     // .tmp file is ignored
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "README.bak")   // .bak file is ignored

        // Test files that don't match any patterns
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "script.js")    // .js file not in include patterns
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Program.cs")    // .cs file and not .log/.tmp
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Library.fs")    // .fs file and not .log/.tmp
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "README.md")     // .md file and not .log/.tmp

    [<Test>]
    member _.``combine method should work with empty array``() =
        // Test combine method with empty array - should include all
        let f = Glob.combine()
        let combinedFilter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "any-file.txt")
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "another-file.log")

    [<Test>]
    member _.``combine method should work with single filter``() =
        let originalFilter = Glob.createExcludeFilter([| "*.log" |])
        let f = Glob.combine([| originalFilter |])
        let combinedFilter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "test.txt")
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "test.log")

    [<Test>]
    member _.``combine method should work with multiple filters``() =
        let filter1 = Glob.createExcludeFilter([| "*.log" |])
        let filter2 = Glob.createExcludeFilter([| "*.tmp" |])
        let filter3 = Glob.createExcludeFilter([| "*.bak" |])
        
        let f = Glob.combine([| filter1; filter2; filter3 |])
        let combinedFilter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        // Should fail at least one filter
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "app.log")      // .log file fails filter1
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "temp.tmp")     // .tmp file fails filter2
        Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "README.bak")   // .tmp file fails filter3

        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Program.cs")   // .cs file, not .log, not .tmp
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Library.fs")   // .fs file, not .log, not .tmp
        Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "README.md")    // .md file doesn't match filter3

    [<Test>]
    member _.``F# list processing should work``() =
        let f = Glob.createExcludeFilter([| "*.log"; "*.tmp" |])
        let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        let testFiles = [
            "src/Program.cs"
            "test/Test.fs"
            "debug.log"
            "temp.tmp"
            "README.md"
        ]
        
        // Use F# list processing with the filter - filter for files that are not excluded
        let includedFiles = 
            testFiles 
            |> List.filter (fun path -> filter path <> GlobFilterStates.Exclude)
            
        let expectedFiles = [
            "src/Program.cs"
            "test/Test.fs"
            "README.md"
        ]
        
        Assert.AreEqual(expectedFiles, includedFiles)

    [<Test>]
    member _.``F# pattern matching should work with filters``() =
        let filter = Glob.createExcludeFilter([| "*.log" |])
        
        let categorizeFile path =
            match filter.Invoke(GlobFilterStates.NotExclude, path) with
            | GlobFilterStates.Exclude -> "ignored"
            | GlobFilterStates.NotExclude -> "neutral"
            | _ -> failwith "Fail"
        
        Assert.AreEqual("neutral", categorizeFile "Program.cs")
        Assert.AreEqual("ignored", categorizeFile "error.log")
        Assert.AreEqual("neutral", categorizeFile "README.md")

    [<Test>]
    member _.``F# pipe operator should work with filters``() =
        let f = Glob.getCommonIgnoreFilter()
        let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
        
        let result = 
            "src/Program.cs"
            |> filter
            
        Assert.AreEqual(GlobFilterStates.NotExclude, result)
        
        let result2 = 
            "bin/Debug/app.exe"
            |> filter
            
        Assert.AreEqual(GlobFilterStates.Exclude, result2)

    // .gitignore related tests
    [<Test>]
    member _.``createGitignoreFilter should work with basic patterns``() =
        task {
            let gitignoreContent = "*.log\ntemp/\n*.tmp\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)

            // Should exclude files matching patterns
            Assert.AreEqual(GlobFilterStates.Exclude, filter "debug.log")
            Assert.AreEqual(GlobFilterStates.Exclude, filter "app.log")
            Assert.AreEqual(GlobFilterStates.Exclude, filter "temp/file.txt")
            Assert.AreEqual(GlobFilterStates.Exclude, filter "cache.tmp")

            // Should be neutral for files not matching patterns
            Assert.AreEqual(GlobFilterStates.NotExclude, filter "Program.cs")
            Assert.AreEqual(GlobFilterStates.NotExclude, filter "README.md")
            Assert.AreEqual(GlobFilterStates.NotExclude, filter "logs/app.txt") // doesn't match *.log exactly
        }

    [<Test>]
    member _.``createGitignoreFilter should work with negation patterns``() =
        task {
            let gitignoreContent = "*.log\n!important.log\ntemp/\n!temp/keep.txt\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)

            // Should exclude files matching exclude patterns
            Assert.AreEqual(GlobFilterStates.Exclude, filter "debug.log")
            Assert.AreEqual(GlobFilterStates.Exclude, filter "temp/file.txt")

            // Should include files matching negation patterns
            Assert.AreEqual(GlobFilterStates.NotExclude, filter "important.log")
            Assert.AreEqual(GlobFilterStates.NotExclude, filter "temp/keep.txt")

            // Should be neutral for files not matching any patterns
            Assert.AreEqual(GlobFilterStates.NotExclude, filter "Program.cs")
        }

    [<Test>]
    member _.``F# async computation should work with gitignore filters``() =
        task {
            let gitignoreContent = "*.log\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let filter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)

            let testFiles = [
                "src/Program.cs"
                "debug.log"
                "important.log"
                "README.md"
            ]

            // Use F# list processing with async filter - include files that are not excluded
            let includedFiles = 
                testFiles 
                |> List.filter (fun path -> filter path <> GlobFilterStates.Exclude)

            let expectedFiles = [
                "src/Program.cs"
                "important.log"
                "README.md"
            ]

            Assert.AreEqual(expectedFiles, includedFiles)
        }

    [<Test>]
    member _.``createExcludeFilterFromGitignore should work with combine method``() =
        task {
            let gitignoreContent = "*.log\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))
            
            let baseFilter = Glob.createExcludeFilter([| "*.tmp" |])
            let! gitignoreFilter = Glob.createExcludeFilterFromGitignore(stream)
            let f = Glob.combine([| baseFilter; gitignoreFilter |])
            let combinedFilter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)
            
            // Should exclude .log files (gitignore) and .tmp files (base)
            Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "error.log")
            Assert.AreEqual(GlobFilterStates.Exclude, combinedFilter "temp.tmp")
            
            // Should include important.log due to negation pattern
            Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "important.log")

            // Should be neutral for other files
            Assert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Program.cs")
        }

    [<Test>]
    member _.``createExcludeFilterFromGitignore should work without base filter``() =
        task {
            let gitignoreContent = "*.log\nbuild/\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))
            
            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let gitignoreFilter = fun path -> f.Invoke(GlobFilterStates.NotExclude, path)

            Assert.AreEqual(GlobFilterStates.Exclude, gitignoreFilter "debug.log")
            Assert.AreEqual(GlobFilterStates.Exclude, gitignoreFilter "build/output.exe")
            Assert.AreEqual(GlobFilterStates.NotExclude, gitignoreFilter "important.log")  // Negation pattern
            Assert.AreEqual(GlobFilterStates.NotExclude, gitignoreFilter "Program.cs")
        }
