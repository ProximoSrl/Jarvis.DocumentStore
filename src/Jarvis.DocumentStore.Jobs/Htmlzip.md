###Why two format of the HtmlZip converter

The old one uses the old version of the [TuesPechkin](https://github.com/tuespetre/TuesPechkin) library, while the project without old suffix uses latest Nuget package.

During unit test, ve verified erratic tests using converter, with some runners the test is red, with other runner the test is green, and there is no clue of the reason. With the new runner, when the test is red, we verified also that the converter starts using one entire CPU 100% and never stops using it. 

To avoid jobs consuming too much resource in production, we decided to keep both version in separate assembly, so we can verify what behave better in production.

 