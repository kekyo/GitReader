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
    member _.``createIgnoreFilter should return F# function type``() =
        // Test that it returns F# function type (string -> bool)
        let filter = Glob.createIgnoreFilter([| "*.log"; "bin/" |])
        
        // Test function call syntax
        Assert.IsTrue(filter "src/main.cs")  // Should be included (not ignored)
        Assert.IsFalse(filter "debug.log")   // Should be excluded (ignored)
        Assert.IsFalse(filter "bin/output")  // Should be excluded (ignored)

    [<Test>]
    member _.``createIgnoreFilter should work with multiple patterns``() =
        let filter = Glob.createIgnoreFilter([| "*.log"; "*.tmp"; "bin/"; "obj/" |])
        
        // Files that should be included (not ignored)
        Assert.IsTrue(filter "src/Program.cs")
        Assert.IsTrue(filter "README.md")
        Assert.IsTrue(filter "package.json")
        
        // Files that should be excluded (ignored)
        Assert.IsFalse(filter "error.log")
        Assert.IsFalse(filter "temp.tmp")
        Assert.IsFalse(filter "bin/Debug/app.exe")
        Assert.IsFalse(filter "obj/Debug/temp.dll")

    [<Test>]
    member _.``createIncludeFilter should return F# function type``() =
        let filter = Glob.createIncludeFilter([| "*.cs"; "*.fs" |])
        
        // Test function call syntax
        Assert.IsTrue(filter "Program.cs")
        Assert.IsTrue(filter "Library.fs")
        Assert.IsFalse(filter "README.md")
        Assert.IsFalse(filter "package.json")

    [<Test>]
    member _.``createIncludeFilter should work with directory patterns``() =
        let filter = Glob.createIncludeFilter([| "src/**"; "test/**" |])
        
        Assert.IsTrue(filter "src/main/Program.cs")
        Assert.IsTrue(filter "test/unit/Test.fs")
        Assert.IsFalse(filter "bin/Debug/app.exe")
        Assert.IsFalse(filter "README.md")

    [<Test>]
    member _.``createCommonIgnoreFilter should return F# function type``() =
        let filter = Glob.createCommonIgnoreFilter()
        
        // Common included files
        Assert.IsTrue(filter "src/Program.cs")
        Assert.IsTrue(filter "README.md")
        Assert.IsTrue(filter "package.json")
        
        // Common ignored files/directories
        Assert.IsFalse(filter "bin/Debug/app.exe")
        Assert.IsFalse(filter "obj/Debug/temp.dll")
        Assert.IsFalse(filter "node_modules/package/index.js")

    [<Test>]
    member _.``F# function composition should work``() =
        let ignoreFilter = Glob.createIgnoreFilter([| "*.log" |])
        let includeFilter = Glob.createIncludeFilter([| "*.cs"; "*.fs" |])
        
        // Combine filters using F# function composition
        let combinedFilter path = includeFilter path && ignoreFilter path
        
        Assert.IsTrue(combinedFilter "Program.cs")   // CS file and not log
        Assert.IsTrue(combinedFilter "Library.fs")   // FS file and not log
        Assert.IsFalse(combinedFilter "error.log")   // Log file (excluded by ignore)
        Assert.IsFalse(combinedFilter "README.md")   // Not CS/FS file (excluded by include)

    [<Test>]
    member _.``F# list processing should work``() =
        let filter = Glob.createIgnoreFilter([| "*.log"; "*.tmp" |])
        
        let testFiles = [
            "src/Program.cs"
            "test/Test.fs"
            "debug.log"
            "temp.tmp"
            "README.md"
        ]
        
        // Use F# list processing with the filter
        let includedFiles = 
            testFiles 
            |> List.filter filter
            
        let expectedFiles = [
            "src/Program.cs"
            "test/Test.fs"
            "README.md"
        ]
        
        Assert.AreEqual(expectedFiles, includedFiles)

    [<Test>]
    member _.``F# pattern matching should work with filters``() =
        let filter = Glob.createIgnoreFilter([| "*.log" |])
        
        let categorizeFile path =
            if filter path then
                "included"
            else
                "ignored"
        
        Assert.AreEqual("included", categorizeFile "Program.cs")
        Assert.AreEqual("ignored", categorizeFile "error.log")
        Assert.AreEqual("included", categorizeFile "README.md")

    [<Test>]
    member _.``F# pipe operator should work with filters``() =
        let filter = Glob.createCommonIgnoreFilter()
        
        let result = 
            "src/Program.cs"
            |> filter
            
        Assert.IsTrue(result)
        
        let result2 = 
            "bin/Debug/app.exe"
            |> filter
            
        Assert.IsFalse(result2) 

    // .gitignore related tests
    [<Test>]
    member _.``createGitignoreFilter should work with basic patterns``() =
        async {
            let gitignoreContent = "*.log\ntemp/\n*.tmp\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! filter = Glob.createGitignoreFilter(stream)

            // Should exclude files matching patterns
            Assert.IsFalse(filter "debug.log")
            Assert.IsFalse(filter "app.log")
            Assert.IsFalse(filter "temp/file.txt")
            Assert.IsFalse(filter "cache.tmp")

            // Should include files not matching patterns
            Assert.IsTrue(filter "Program.cs")
            Assert.IsTrue(filter "README.md")
            Assert.IsTrue(filter "logs/app.txt") // doesn't match *.log exactly
        } |> Async.RunSynchronously

    [<Test>]
    member _.``createGitignoreFilter should work with negation patterns``() =
        async {
            let gitignoreContent = "*.log\n!important.log\ntemp/\n!temp/keep.txt\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! filter = Glob.createGitignoreFilter(stream)

            // Should exclude files matching exclude patterns
            Assert.IsFalse(filter "debug.log")
            Assert.IsFalse(filter "temp/file.txt")

            // Should include files matching negation patterns
            Assert.IsTrue(filter "important.log")
            Assert.IsTrue(filter "temp/keep.txt")

            // Should include files not matching any patterns
            Assert.IsTrue(filter "Program.cs")
        } |> Async.RunSynchronously

    [<Test>]
    member _.``combineWithGitignore should override base filter with negation``() =
        async {
            // .gitignore negates what base filter excludes
            let gitignoreContent = "!important.tmp\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let baseFilter = Glob.createIgnoreFilter([| "*.tmp" |])
            let! combinedFilter = Glob.combineWithGitignore(stream, baseFilter)

            // .gitignore should allow important.tmp even though base filter excludes *.tmp
            Assert.IsTrue(combinedFilter "important.tmp")

            // Other .tmp files should still be excluded by base filter
            Assert.IsFalse(combinedFilter "cache.tmp")
        } |> Async.RunSynchronously

    [<Test>]
    member _.``combineWithGitignore should work without base filter``() =
        async {
            let gitignoreContent = "*.log\ntemp/\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! combinedFilter = Glob.combineWithGitignore(stream)

            // Should exclude files matching .gitignore patterns
            Assert.IsFalse(combinedFilter "debug.log")
            Assert.IsFalse(combinedFilter "temp/file.txt")

            // Should include files not matching patterns
            Assert.IsTrue(combinedFilter "Program.cs")
        } |> Async.RunSynchronously

    [<Test>]
    member _.``F# async computation should work with gitignore filters``() =
        async {
            let gitignoreContent = "*.log\n!important.log\n"
            use stream = new MemoryStream(Encoding.UTF8.GetBytes(gitignoreContent))

            let! filter = Glob.createGitignoreFilter(stream)

            let testFiles = [
                "src/Program.cs"
                "debug.log"
                "important.log"
                "README.md"
            ]

            // Use F# list processing with async filter
            let includedFiles = 
                testFiles 
                |> List.filter filter

            let expectedFiles = [
                "src/Program.cs"
                "important.log"
                "README.md"
            ]

            Assert.AreEqual(expectedFiles, includedFiles)
        } |> Async.RunSynchronously 