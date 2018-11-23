function Edit-XmlNodes {
param (
    [xml] $doc = $(throw "doc is a required parameter"),
    [string] $xpath = $(throw "xpath is a required parameter"),
    $namespace = $(throw "namespace is a required parameter"),
    [string] $value = $(throw "value is a required parameter"),
    [bool] $condition = $true
)    
    if ($condition -eq $true) {
        $nodes = $doc.SelectNodes($xpath, $namespace)
         
        foreach ($node in $nodes) {
            if ($node -ne $null) {
                if ($node.NodeType -eq "Element") {
                    $node.InnerXml = $value
                }
                else {
                    $node.Value = $value
                }
            }
        }
    }
}

function Switch-FrameworkReference {
param (
    [String] $sourcePrj = $(throw "sourcePrj is a required parameter")
    #[string] $destinationDir = $(throw "= is a required parameter")
)    
    
    $frameworkLocation = (get-item $sourcePrj ).parent.FullName
    Get-ChildItem "$sourcePrj" -Filter *.csproj -Recurse | 
    Foreach-Object {

        Write-Output 'Modification of project $_.FullName'
        $xml = [xml](Get-Content $_.FullName)
        $ns = new-object Xml.XmlNamespaceManager $xml.NameTable
        $ns.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
    
        Edit-XmlNodes -doc $xml -namespace $ns `
            -xpath "//msb:Reference[starts-with(@Include, 'Jarvis.Framework.Shared,')]" `
            -value "<SpecificVersion>False</SpecificVersion>
      <HintPath>$frameworkLocation\Jarvis.Framework\Jarvis.Framework.Bus.Rebus.Integration\bin\Debug\Jarvis.Framework.Shared.dll</HintPath>
      "
        Edit-XmlNodes -doc $xml -namespace $ns `
            -xpath "//msb:Reference[starts-with(@Include, 'Jarvis.NEventStoreEx,')]" `
            -value "<SpecificVersion>False</SpecificVersion>
      <HintPath>$frameworkLocation\Jarvis.Framework\Jarvis.Framework.Bus.Rebus.Integration\bin\Debug\Jarvis.NEventStoreEx.dll</HintPath>
      "
        Edit-XmlNodes -doc $xml -namespace $ns `
            -xpath "//msb:Reference[starts-with(@Include, 'Jarvis.Framework.Kernel,')]" `
            -value "<SpecificVersion>False</SpecificVersion>
      <HintPath>$frameworkLocation\Jarvis.Framework\Jarvis.Framework.Bus.Rebus.Integration\bin\Debug\Jarvis.Framework.Kernel.dll</HintPath>
      "
       Edit-XmlNodes -doc $xml -namespace $ns `
            -xpath "//msb:Reference[starts-with(@Include, 'Jarvis.Framework.Bus.Rebus.Integration,')]" `
            -value "<SpecificVersion>False</SpecificVersion>
      <HintPath>$frameworkLocation\Jarvis.Framework\Jarvis.Framework.Bus.Rebus.Integration\bin\Debug\Jarvis.Framework.Bus.Rebus.Integration.dll</HintPath>
      "
       Edit-XmlNodes -doc $xml -namespace $ns `
            -xpath "//msb:Reference[starts-with(@Include, 'Jarvis.Framework.TestHelpers,')]" `
            -value "<SpecificVersion>False</SpecificVersion>
      <HintPath>$frameworkLocation\Jarvis.Framework\Jarvis.Framework.TestHelpers\bin\Debug\Jarvis.Framework.TestHelpers.dll</HintPath>
      "
        $xml.Save($_.FullName)
      
    }
}

$runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
Switch-FrameworkReference -sourcePrj $runningDirectory