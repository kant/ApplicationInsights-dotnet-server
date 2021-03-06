# How to Contribute

If you're interested in contributing, take a look at the general [contributer's guide](https://github.com/Microsoft/ApplicationInsights-Home/blob/master/CONTRIBUTING.md) first.

## Unit Tests

To successfully run all the unit tests on your machine, make sure you've installed the following prerequisites:

* Visual Studio 2017 Community or Enterprise
* .NET 4.6

Several tests also require that you configure a strong name verification exception for Microsoft.WindowsAzure.ServiceRuntime.dll using the [Strong Name Tool](https://msdn.microsoft.com/en-us/library/k5b5tt23(v=vs.110).aspx). Run this command from the repository root to configure the exception (after building Microsoft.ApplicationInsights.Web.sln):

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -Vr ..\bin\Debug\Src\WindowsServer\WindowsServer.Net40.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
    
Once you've installed the prerequisites and configured the strong name verification, execute the ```runUnitTests.cmd``` script in the repository root.

You can also run the tests within Visual Studio using the test explorer.

You can remove the strong name verification exception by running this command:

    "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -Vu ..\bin\Debug\Src\WindowsServer\WindowsServer.Net40.Tests\Microsoft.WindowsAzure.ServiceRuntime.dll
    
## Functional Tests

To execute the functional tests, you need to install some additional prerequisites:

* IIS (Make sure Internet Information Services > World Wide Web Services > Application Development Features > ASP.NET 4.6 is enabled)

To execute the functional tests for DependencyCollector, you need to install Docker as well (Windows 10 or Windows Server 2016 required), as all these tests deploy every dependencies into Docker containers locally.

* Docker for Windows (https://docs.docker.com/docker-for-windows/install/). After installation switch Docker engine to Windows Containers.(https://blogs.msdn.microsoft.com/webdev/2017/09/07/getting-started-with-windows-containers/)

After you've done this, execute the ```runFunctionalTests.cmd``` script as administrator in the repository root. You can also run and debug the functional tests from Visual Studio by opening the solutions under the Test directory in the repository root.

If all or most of the Dependency Collector functional tests fail with messages like "Assert.AreEqual failed. Expected:<1>. Actual<0>." or "All apps are not healthy", then its likely that Docker installation has some issues.
Please make sure you can run docker run hello-world successfully to confirm that your machine is Docker ready.
Also, the very first time DependencyCollector tests are run, all Docker images are downloaded from web and this could potentially take an hour or so. This is only one time per machine.

## Debugging the SDK

* Build the project using ```buildDebug.cmd``` 
* If the build was successful, you'll find that it generated NuGet packages in <repository root>\..\bin\Debug\NuGet
* If your change is confined to one of the nuget packages (say Web sdk), and you are developing on one of VNext branches, you can get the rest of the compatible nuget packages from [myget feed](https://www.myget.org/F/applicationinsights/)  
* Create a web application project to test the SDK on, and install the Microsoft.ApplicationInsights.Web NuGet package from the above directory
* In your web application, point your project references to Microsoft.AI.Web, Microsoft.AI.WindowsServer, Microsoft.AI.PerfCounterCollector and Microsoft.AI.DependencyCollector to those DLLs in the SDK debug output folder (this makes sure you get the symbol files and that your web application is updated when you recompile the SDK).
* From your web application, open the .cs file you want your breakpoint in and set it
* Run your web application

Your breakpoints should be hit now when your web application triggers them.
