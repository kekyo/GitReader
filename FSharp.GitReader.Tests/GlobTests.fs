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

open NUnit.Framework.Legacy;

type public GlobTests() =

    [<Test>]
    member _.``isMatch should work with basic patterns``() =
        // Basic pattern matching
        ClassicAssert.IsTrue(Glob.isMatch("test.txt", "*.txt"))
        ClassicAssert.IsFalse(Glob.isMatch("test.jpg", "*.txt"))
        ClassicAssert.IsTrue(Glob.isMatch("file", "*"))
        
    [<Test>]
    member _.``isMatch should work with directory patterns``() =
        // Directory patterns
        ClassicAssert.IsTrue(Glob.isMatch("bin/debug", "bin/*"))
        ClassicAssert.IsTrue(Glob.isMatch("src/main/file.cs", "src/**"))
        ClassicAssert.IsFalse(Glob.isMatch("test.txt", "bin/*"))

    [<Test>]
    member _.``createExcludeFilter should return F# function type``() =
        // Test that it returns F# function type (string -> FilterDecision)
        let f = Glob.createExcludeFilter([| "*.log"; "bin/" |])
        let filter = Glob.applyFilter(f)

        // Test function call syntax
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "src/main.cs")  // Should be neutral (not matching exclude patterns)
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "debug.log")   // Should be excluded (ignored)
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "bin/output")  // Should be excluded (ignored)

    [<Test>]
    member _.``createExcludeFilter should work with multiple patterns``() =
        let f = Glob.createExcludeFilter([| "*.log"; "*.tmp"; "bin/"; "obj/" |])
        let filter = Glob.applyFilter(f)

        // Files that should be neutral (not matching exclude patterns)
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "src/Program.cs")
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "README.md")
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "package.json")
        
        // Files that should be excluded (ignored)
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "error.log")
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "temp.tmp")
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "bin/Debug/app.exe")
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "obj/Debug/temp.dll")

    [<Test>]
    member _.``getCommonIgnoreFilter should return F# function type``() =
        let f = Glob.getCommonIgnoreFilter()
        let filter = Glob.applyFilter(f)
        
        // Common included files (not matching ignore patterns)
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "src/Program.cs")
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "README.md")
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "package.json")
        
        // Common ignored files/directories
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "bin/Debug/app.exe")
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "obj/Debug/temp.dll")
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "node_modules/package/index.js")

    [<Test>]
    member _.``combine method should work with static class API``() =
        let filter1 = Glob.createExcludeFilter([| "*.log"; "*.tmp" |])
        let filter2 = Glob.createExcludeFilter([| "*.bak" |])
        
        // Use the new combine method
        let f = Glob.combine([| filter1; filter2 |])
        let combinedFilter = Glob.applyFilter(f)
        
        // Test files that should be excluded by filter1
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "app.log")      // .log file is ignored
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "temp.tmp")     // .tmp file is ignored
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "README.bak")   // .bak file is ignored

        // Test files that don't match any patterns
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "script.js")    // .js file not in include patterns
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Program.cs")    // .cs file and not .log/.tmp
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Library.fs")    // .fs file and not .log/.tmp
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "README.md")     // .md file and not .log/.tmp

    [<Test>]
    member _.``combine method should work with empty array``() =
        // Test combine method with empty array - should include all
        let f = Glob.combine()
        let combinedFilter = Glob.applyFilter(f)
        
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "any-file.txt")
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "another-file.log")

    [<Test>]
    member _.``combine method should work with single filter``() =
        let originalFilter = Glob.createExcludeFilter([| "*.log" |])
        let f = Glob.combine([| originalFilter |])
        let combinedFilter = Glob.applyFilter(f)
        
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "test.txt")
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "test.log")

    [<Test>]
    member _.``combine method should work with multiple filters``() =
        let filter1 = Glob.createExcludeFilter([| "*.log" |])
        let filter2 = Glob.createExcludeFilter([| "*.tmp" |])
        let filter3 = Glob.createExcludeFilter([| "*.bak" |])
        
        let f = Glob.combine([| filter1; filter2; filter3 |])
        let combinedFilter = Glob.applyFilter(f)
        
        // Should fail at least one filter
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "app.log")      // .log file fails filter1
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "temp.tmp")     // .tmp file fails filter2
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "README.bak")   // .tmp file fails filter3

        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Program.cs")   // .cs file, not .log, not .tmp
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Library.fs")   // .fs file, not .log, not .tmp
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "README.md")    // .md file doesn't match filter3

    [<Test>]
    member _.``F# list processing should work``() =
        let f = Glob.createExcludeFilter([| "*.log"; "*.tmp" |])
        let filter = Glob.applyFilter(f)
        
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
        
        ClassicAssert.AreEqual(expectedFiles, includedFiles)

    [<Test>]
    member _.``F# pattern matching should work with filters``() =
        let filter = Glob.createExcludeFilter([| "*.log" |])
        
        let categorizeFile path =
            match Glob.applyFilter(filter) path with
            | GlobFilterStates.Exclude -> "ignored"
            | GlobFilterStates.NotExclude -> "neutral"
            | _ -> failwith "Fail"
        
        ClassicAssert.AreEqual("neutral", categorizeFile "Program.cs")
        ClassicAssert.AreEqual("ignored", categorizeFile "error.log")
        ClassicAssert.AreEqual("neutral", categorizeFile "README.md")

    [<Test>]
    member _.``F# pipe operator should work with filters``() =
        let f = Glob.getCommonIgnoreFilter()
        let filter = Glob.applyFilter(f)
        
        let result = 
            "src/Program.cs"
            |> filter
            
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, result)
        
        let result2 = 
            "bin/Debug/app.exe"
            |> filter
            
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, result2)

    // .gitignore related tests
    [<Test>]
    member _.``createGitignoreFilter should work with basic patterns``() =
        task {
            let gitignoreContent = "*.log\ntemp/\n*.tmp\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let filter = Glob.applyFilter(f)

            // Should exclude files matching patterns
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "debug.log")
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "app.log")
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "temp/file.txt")
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "cache.tmp")

            // Should be neutral for files not matching patterns
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "Program.cs")
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "README.md")
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "logs/app.txt") // doesn't match *.log exactly
        }

    [<Test>]
    member _.``createGitignoreFilter should work with negation patterns``() =
        task {
            let gitignoreContent = "*.log\n!important.log\ntemp/\n!temp/keep.txt\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let filter = Glob.applyFilter(f)

            // Should exclude files matching exclude patterns
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "debug.log")
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, filter "temp/file.txt")

            // Should include files matching negation patterns
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "important.log")
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "temp/keep.txt")

            // Should be neutral for files not matching any patterns
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, filter "Program.cs")
        }

    [<Test>]
    member _.``F# async computation should work with gitignore filters``() =
        task {
            let gitignoreContent = "*.log\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let filter = Glob.applyFilter(f)

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

            ClassicAssert.AreEqual(expectedFiles, includedFiles)
        }

    [<Test>]
    member _.``createExcludeFilterFromGitignore should work with combine method``() =
        task {
            let gitignoreContent = "*.log\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))
            
            let baseFilter = Glob.createExcludeFilter([| "*.tmp" |])
            let! gitignoreFilter = Glob.createExcludeFilterFromGitignore(stream)
            let f = Glob.combine([| baseFilter; gitignoreFilter |])
            let combinedFilter = Glob.applyFilter(f)
            
            // Should exclude .log files (gitignore) and .tmp files (base)
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "error.log")
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, combinedFilter "temp.tmp")
            
            // Should include important.log due to negation pattern
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "important.log")

            // Should be neutral for other files
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, combinedFilter "Program.cs")
        }

    [<Test>]
    member _.``createExcludeFilterFromGitignore should work without base filter``() =
        task {
            let gitignoreContent = "*.log\nbuild/\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))
            
            let! f = Glob.createExcludeFilterFromGitignore(stream)
            let gitignoreFilter = Glob.applyFilter(f)

            ClassicAssert.AreEqual(GlobFilterStates.Exclude, gitignoreFilter "debug.log")
            ClassicAssert.AreEqual(GlobFilterStates.Exclude, gitignoreFilter "build/output.exe")
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, gitignoreFilter "important.log")  // Negation pattern
            ClassicAssert.AreEqual(GlobFilterStates.NotExclude, gitignoreFilter "Program.cs")
        }

    [<Test>]
    member _.``applyFilter should work``() =
        let filter = Glob.createExcludeFilter([| "*.log" |])
        ClassicAssert.AreEqual(GlobFilterStates.Exclude, Glob.applyFilter(filter) "debug.log")
        ClassicAssert.AreEqual(GlobFilterStates.NotExclude, Glob.applyFilter(filter) "Program.cs")
